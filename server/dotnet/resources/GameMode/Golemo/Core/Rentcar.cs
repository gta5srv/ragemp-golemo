using System;
using System.Collections.Generic;
using System.Text;
using GTANetworkAPI;
using Golemo.GUI;
using GolemoSDK;
using Newtonsoft.Json;
using System.Data;

namespace Golemo.Core
{
    class Rentcar : Script
    {
        private static nLog Log = new nLog("Rentcar");
        private static List<RentArea> _rentAreas = new List<RentArea>();

        [ServerEvent(Event.ResourceStart)]
        public static void OnResourceStart()
        {
            try
            {
                var result = MySQL.QueryRead($"SELECT * FROM `rentareas`");
                if (result == null || result.Rows.Count == 0)
                {
                    Log.Write("DB rentareas return null result.", nLog.Type.Warn);
                    return;
                }
                foreach (DataRow Row in result.Rows)
                {
                    Vector3 position = JsonConvert.DeserializeObject<Vector3>(Row["position"].ToString());
                    List<Vector3> spawnPos = JsonConvert.DeserializeObject<List<Vector3>>(Row["spawnpositions"].ToString());
                    List<Vector3> spawnRot = JsonConvert.DeserializeObject<List<Vector3>>(Row["spawnrotations"].ToString());
                    Dictionary<string, int> vehicles = JsonConvert.DeserializeObject<Dictionary<string, int>>(Row["vehicles"].ToString());
                    RentArea area = new RentArea(Convert.ToInt32(Row["id"]), Convert.ToInt32(Row["type"]), position, spawnPos, spawnRot);
                    area.LoadVehicles(vehicles);
                    _rentAreas.Add(area);
                }
                Log.Write($"Загружено {_rentAreas.Count} площадок аренды транспорта");
            }
            catch (Exception e)
            {
                Log.Write(e.Message, nLog.Type.Error);
            }
        }

        [ServerEvent(Event.PlayerEnterVehicle)]
        public void Event_OnPlayerEnterVehicle(Player player, Vehicle vehicle, sbyte seatid)
        {
            try
            {
                if (!vehicle.HasData("ACCESS") || vehicle.GetData<string>("ACCESS") != "RENT" || seatid != 0) return;///seatid != -1
                if (vehicle.GetData<Player>("DRIVER") != null && vehicle.GetData<Player>("DRIVER") != player)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Этот транспорт уже арендован", 3000);
                    VehicleManager.WarpPlayerOutOfVehicle(player);
                    return;
                }
            }
            catch (Exception e) { Log.Write("PlayerEnterVehicle: " + e.Message, nLog.Type.Error); }
        }

        [ServerEvent(Event.PlayerExitVehicle)]
        public void Event_OnPlayerExitVehicle(Player player, Vehicle vehicle)
        {
            try
            {
                if (!player.HasData("CARROOMID")) return;
                if (player.HasData("CARROOMTEST"))
                {
                    player.ResetData("CARROOMTEST");
                    return;
                }
                if (!vehicle.HasData("ACCESS") || vehicle.GetData<string>("ACCESS") != "RENT" || vehicle.GetData<Player>("DRIVER") != player) return;
            }
            catch (Exception e) { Log.Write("PlayerExitVehicle: " + e.Message, nLog.Type.Error); }
        }
        public static void OpenMenuRentVehicles(Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (!player.HasData("RENTAREAID")) return;
                RentArea area = _rentAreas.Find(x => x.ID == player.GetData<int>("RENTAREAID"));
                if (area == null) return;
                area.OpenMenuForPlayer(player);
            }
            catch (Exception e)
            {
                Log.Write(e.Message, nLog.Type.Error);
            }
        }

        [RemoteEvent("SERVER:::RENT::BUY_RENT_CAR")]
        public static void RentCar(Player player, string vname, int minutes = 10)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (!player.HasData("RENTAREAID")) return;
            if (player.HasData("RENTED_TIME") || player.HasData("RENTED_CAR"))
            {
                Notify.Error(player, "У вас уже есть арендованный транспорт", 4500);
                return;
            }

            RentArea area = _rentAreas.Find(x => x.ID == player.GetData<int>("RENTAREAID"));
            if (area == null) return;

            if (!area.ContainsVehicle(vname)) return;

            int price = area.GetPriceThisRentCar(vname, minutes);
            if (!MoneySystem.Wallet.Change(player, -price))
            {
                Notify.Error(player, "Недостаточно средств", 3500);
                return;
            }
            area.SpawnRentCar(player, vname, minutes);
        }

        public static Vector3 GetNearestRentArea(Vector3 position)
        {
            Vector3 nearesetArea = _rentAreas[0].Position;
            foreach (var area in _rentAreas)
            {
                if (position.DistanceTo(area.Position) < position.DistanceTo(nearesetArea))
                    nearesetArea = area.Position;
            }
            return nearesetArea;

        }
    }

    class RentArea
    {
        #region Properties
        public int ID { get; set; }
        private int Type { get; set; }
        public Vector3 Position { get; set; }
        [JsonIgnore] private Blip _blip { get; set; }
        [JsonIgnore] private ColShape _shape { get; set; }
        [JsonIgnore] private Marker _marker { get; set; }
        [JsonIgnore] private TextLabel _textLabel { get; set; }
        private Dictionary<string, int> _vehicles = new Dictionary<string, int>();
        private List<Vector3> _spawnPositions { get; set; }
        private List<Vector3> _spawnRotations { get; set; }

        private int _lastSpawnPointIndex = 0;
        #endregion

        public RentArea(int iD, int type, Vector3 position, List<Vector3> spawnPositions, List<Vector3> spawnRotations)
        {
            ID = iD;
            Position = position;
            Type = type;
            _spawnPositions = spawnPositions;
            _spawnRotations = spawnRotations;

            _blip = NAPI.Blip.CreateBlip(RentAreaBlipSprites[(RentAreaType)Type], Position, 1f, 84, RentAreaBlipNames[(RentAreaType)Type], 255, 0, true, 0, 0);
            _shape = NAPI.ColShape.CreateCylinderColShape(Position, 1, 2, 0);
            _marker = NAPI.Marker.CreateMarker(1, Position - new Vector3(0, 0, 1.5f), new Vector3(), new Vector3(), 1, new Color(255, 255, 255, 140), false, 0);
            _textLabel = NAPI.TextLabel.CreateTextLabel(RentAreaBlipNames[(RentAreaType)Type], Position, 10f, 5f, 4, new Color(255, 255, 255), false, 0);

            _shape.OnEntityEnterColShape += (s, e) =>
            {
                NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 808);
                NAPI.Data.SetEntityData(e, "RENTAREAID", ID);
            };
            _shape.OnEntityExitColShape += (s, e) =>
            {
                NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 0);
                NAPI.Data.ResetEntityData(e, "RENTAREAID");
            };
        }
        public void OpenMenuForPlayer(Player player)
        {
            try
            {
                if (_vehicles.Count <= 0)
                {
                    Notify.Warn(player, "В данный момент площадка для аренды не работает", 2500);
                    return;
                }
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"header", RentAreaBlipNames[(RentAreaType)Type] },
                    {"money", Main.Players[player].Money },
                    {"vehicles", _vehicles }
                };
                string json = JsonConvert.SerializeObject(data);
                Trigger.ClientEvent(player, "RENT::OPEN_RENT_MENU", json);
            }
            catch (Exception) { }
        }
        private int LastSpawnPointIndex()
        {
            try
            {
                _lastSpawnPointIndex++;
                if (_lastSpawnPointIndex >= _spawnPositions.Count) _lastSpawnPointIndex = 0;
                return _lastSpawnPointIndex;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        private Vector3[] GetSpawnPosition()
        {
            Vector3[] pos = new Vector3[2];
            int index = this.LastSpawnPointIndex();
            pos[0] = _spawnPositions[index];
            pos[1] = _spawnRotations[index];
            return pos;
        }
        public void LoadVehicles(Dictionary<string, int> vehnames)
        {
            try
            {
                foreach (var item in vehnames)
                {
                    if (!isRentAreaTypeClassEqualsToVehicleClass(item.Key)) continue;
                    _vehicles.Add(item.Key, item.Value);
                }
            }
            catch (Exception) { }
        }
        public bool isRentAreaTypeClassEqualsToVehicleClass(string vehname)
        {
            try
            {
                VehicleHash vhash = (VehicleHash)NAPI.Util.GetHashKey(vehname);
                int vclass = NAPI.Vehicle.GetVehicleClass(vhash);
                if ((RentAreaType)Type != (RentAreaType)vclass) return false;
                else return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ContainsVehicle(string vname)
        {
            return this._vehicles.ContainsKey(vname);
        }
        public int GetPriceThisRentCar(string vname, int minutes)
        {
            return _vehicles[vname] * minutes / 10;
        }
        public void SpawnRentCar(Player player, string vname, int minutes)
        {
            VehicleHash vhash = (VehicleHash)NAPI.Util.GetHashKey(vname);
            Vector3[] pos = GetSpawnPosition();
            Vehicle vehicle = NAPI.Vehicle.CreateVehicle(vhash, pos[0], pos[1].Z, 0, 0, "RENT", 255, false, false, 0);
            NAPI.Entity.SetEntityRotation(vehicle, pos[1]);
            NAPI.Data.SetEntityData(vehicle, "ACCESS", "RENT");
            NAPI.Data.SetEntityData(vehicle, "DRIVER", player);
            NAPI.Data.SetEntitySharedData(vehicle, "PETROL", 50);
            Core.VehicleStreaming.SetEngineState(vehicle, false);
            Core.VehicleStreaming.SetLockStatus(vehicle, false);

            player.SetData("RENTED_CAR", vehicle);
            player.SetData("RENTED_TIME", DateTime.Now.AddMinutes(minutes));
            player.SetIntoVehicle(vehicle, 0);
            Trigger.ClientEvent(player, "RENT::CLOSE_RENT_MENU");
        }

        #region IEnum
        private enum RentAreaType : int
        {
            Boats = 14,
            Compacts = 0,
            Coupes = 3,
            Cycles = 13,
            Helicopters = 15,
            Motorcycles = 8,
            Muscle = 4,
            OffRoad = 9,
            Planes = 16,
            SUVs = 2,
            Sedans = 1,
            Sports = 6,
            Super = 7
        }
        private static Dictionary<RentAreaType, uint> RentAreaBlipSprites = new Dictionary<RentAreaType, uint>()
        {
            { RentAreaType.Boats, 427},
            { RentAreaType.Helicopters, 43},
            { RentAreaType.Motorcycles, 522},
            { RentAreaType.Cycles, 559},
            { RentAreaType.Planes, 579},
            { RentAreaType.Sedans, 523},
            { RentAreaType.Sports, 669},
            { RentAreaType.OffRoad, 757}
        };
        private static Dictionary<RentAreaType, string> RentAreaBlipNames = new Dictionary<RentAreaType, string>()
        {
            { RentAreaType.Boats, "Аренда лодок" },
            { RentAreaType.Helicopters, "Аренда вертолетов" },
            { RentAreaType.Motorcycles, "Аренда мотоциклов" },
            { RentAreaType.Cycles, "Аренда велосипедов" },
            { RentAreaType.Planes, "Аренда самолетов" },
            { RentAreaType.Sedans, "Аренда машин" },
            { RentAreaType.Sports, "Аренда люкс-авто" },
            { RentAreaType.OffRoad, "Аренда внедорожников" },
        };
        #endregion
    }
}

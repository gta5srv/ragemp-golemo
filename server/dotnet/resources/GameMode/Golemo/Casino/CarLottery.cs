using System;
using System.Data;
using System.Collections.Generic;
using Newtonsoft.Json;
using GTANetworkAPI;
using Golemo.Core;
using GolemoSDK;
using static Golemo.Core.VehicleManager;

namespace Golemo.Casino
{
    class CarLottery : Script
    {
        private static nLog Log = new nLog("CarRandom");
        private static bool CompleteFlag = false;
        public static string vModel;
        //Цена за участие в лотерее
        private static int _price = 5000;
        //Минимальное количество участников, которое необходимо для проведения лотереи
        private static int _minCountMembers = 5;
        //Координаты колшейпа взаимодействия
        private static Vector3 _mainShapePosition = new Vector3(1105.8865, 220.15826, -48.99499);

        private static GTANetworkAPI.ColShape _mainShape;
        private static GTANetworkAPI.ColShape _podiumShape;
        private static GTANetworkAPI.Marker _mainShapeMarker;

        public static List<int> MemberUUIDs = new List<int>();
        //Название моделей розыгрываемых машин
        private static List<string> CarsfoGive = new List<string>() {
            "zentorno",
            "t20",
            "sultan",
            "banshee",
        };

        [ServerEvent(Event.ResourceStart)]
        public static void onResourceStart()
        {
            try
            {
                Randomcar();
                _mainShapeMarker = NAPI.Marker.CreateMarker(1, _mainShapePosition - new Vector3(0, 0, 1.5), new Vector3(), new Vector3(), 1f, new Color(66, 170, 255, 150), false, 0);
                _mainShape = NAPI.ColShape.CreateCylinderColShape(_mainShapePosition, 1, 2, 0);
                _podiumShape = NAPI.ColShape.CreateCylinderColShape(new Vector3(1100.077, 219.9723, -50.07865), 50, 50, 0);
                _mainShape.OnEntityEnterColShape += (s, ent) =>
                {
                    try
                    {
                        NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 806);
                    }
                    catch (Exception ex) { Console.WriteLine("shape.OnEntityEnterColShape: " + ex.Message); }
                };
                _mainShape.OnEntityExitColShape += (s, ent) =>
                {
                    NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 0);
                };

                _podiumShape.OnEntityEnterColShape += (s, ent) =>
                {
                    try
                    {
                        Trigger.ClientEvent(ent, "CAR_LOTTERY::PODIUM_LOAD_CAR_MODEL", vModel);
                    }
                    catch (Exception ex) { Console.WriteLine("podiumcolshape.OnEntityEnterColShape: " + ex.Message); }
                };
                Log.Write("Loaded", nLog.Type.Info);

            }
            catch (Exception e) { Log.Write(e.ToString(), nLog.Type.Error); }
        }
        public static void CallBackShape(Player player)
        {
            if (!isAccessToTakePart(player)) return;
            Trigger.ClientEvent(player, "openDialog", "RANDOMMEMBER_ADD",
                $"Сегодня разыгрывается {VehicleHandlers.VehiclesName.GetRealVehicleName(vModel)}. Стоимость участия: {_price}$. Учавствовать?");
        }
        public static void Randomcar()
        {
            try
            {
                int rand = new Random().Next(0, CarsfoGive.Count);
                vModel = CarsfoGive[rand];
            }
            catch (Exception e) { Log.Write("Randomcar: " + e.Message, nLog.Type.Error); }
        }
        public static void FinishCompetition(bool isSendAdmin = false)
        {
            try
            {
                if (DateTime.Now.Hour != 22 && !isSendAdmin && !CompleteFlag) return;
                if(MemberUUIDs.Count < _minCountMembers)
                {
                    NAPI.Chat.SendChatMessageToAll("!{#fc4626} [Казино]: !{#ffffff}" + $"Из-за недостатка участников, розыгрыш автомобиля {VehicleHandlers.VehiclesName.GetRealVehicleName(vModel)}, отменяется! Следующий розыгрыш завтра!");
                    MemberUUIDs.Clear();
                    CompleteFlag = true;
                    return;
                }
                int rnd = new Random().Next(0, MemberUUIDs.Count);
                int memberuuid = MemberUUIDs[rnd];
                if (Main.PlayerNames[memberuuid] != null)
                {
                    var vNumber = Core.VehicleManager.Create(Main.PlayerNames[memberuuid], $"{vModel}", new Color(0, 0, 0), new Color(0, 0, 0), new Color(0, 0, 0));
                    var house = Houses.HouseManager.GetHouse(NAPI.Player.GetPlayerFromName(Main.PlayerNames[memberuuid]), false);
                    if (house != null)
                    {
                        if (house.GarageID != 0)
                        {
                            var garage = Houses.GarageManager.Garages[house.GarageID];
                            if (Core.VehicleManager.getAllPlayerVehicles(Main.PlayerNames[memberuuid]).Count < Houses.GarageManager.GarageTypes[garage.Type].MaxCars)
                            {
                                garage.SpawnCar(vNumber);
                            }
                        }
                    }
                    NAPI.Chat.SendChatMessageToAll("!{#fc4626} [Казино]: !{#ffffff}" + $"В розыгрыше автомобиля выиграл {Main.PlayerNames[memberuuid]} и забрал {VehicleHandlers.VehiclesName.GetRealVehicleName(vModel)} Поздравим! Следующий розыгрыш завтра!");
                }
                else
                {
                    var result = MySQL.QueryRead($"SELECT firstname AND lastname FROM characters WHERE uuid = '{memberuuid}'");
                    string FirstName = Convert.ToString(result.Rows[0]);
                    string LastName = Convert.ToString(result.Rows[1]);
                    string name = FirstName + '_' + LastName;
                    VehicleData data = new VehicleData();
                    VehicleHash vhash = (VehicleHash)NAPI.Util.GetHashKey(vModel);
                    string Holder = name;
                    string Model = vModel;
                    int Health = 1000;
                    int Fuel = VehicleTank.ContainsKey(NAPI.Vehicle.GetVehicleClass(vhash)) ? VehicleTank[NAPI.Vehicle.GetVehicleClass(vhash)] : 120;
                    int Price = (BusinessManager.ProductsOrderPrice.ContainsKey(vModel)) ? Convert.ToInt32(BusinessManager.ProductsOrderPrice[vModel] * 0.5) : 0;
                    data.Components = new VehicleCustomization();
                    data.Components.PrimColor = new Color(0, 0, 0);
                    data.Components.SecColor = new Color(0, 0, 0);
                    data.Components.NeonColor = new Color(0, 0, 0);
                    data.Items = new List<nItem>();
                    data.Dirt = 0.0F;

                    string Number = GenerateNumber();
                    Vehicles.Add(Number, data);
                    MySQL.Query("INSERT INTO `vehicles`(`number`, `holder`, `model`, `health`, `fuel`, `price`, `components`, `items`)" +
                        $" VALUES ('{Number}','{Holder}','{Model}',{Health},{Fuel},{Price},'{JsonConvert.SerializeObject(data.Components)}','{JsonConvert.SerializeObject(data.Items)}')");
                    Log.Write("Created new vehicle with number: " + Number);
                    NAPI.Chat.SendChatMessageToAll("!{#fc4626} [Казино]: !{#ffffff}" + $"В розыгрыше автомобиля выиграл {name} и забрал {VehicleHandlers.VehiclesName.GetRealVehicleName(vModel)} Поздравим! Следующий розыгрыш завтра!");
                }
                MemberUUIDs.Clear();
                CompleteFlag = true;
            }
            catch (Exception e) { Log.Write("RandomWinner: " + e.Message, nLog.Type.Error); }
        }
        public static void AcceptTakePart(Player player)
        {
            if (!isAccessToTakePart(player)) return;
            if (!MoneySystem.Wallet.Change(player, -_price))
            {
                Notify.Error(player, "У вас недостаточно средств");
                return;
            }
            Casino.CarLottery.MemberUUIDs.Add(Main.Players[player].UUID);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Вы приняли участие в розыгрыше!", 2500);
        }
        private static bool isAccessToTakePart(Player player)
        {
            if (DateTime.Now.Hour > 22)
            {
                Notify.Error(player, "Розыгрыш уже прошёл. Приходите завтра.");
                return false;
            }
            if (MemberUUIDs.Contains(Main.Players[player].UUID))
            {
                Notify.Error(player, "Вы уже учавствуете в розыгрыше");
                return false;
            }
            if (CompleteFlag)
            {
                Notify.Error(player, "Розыгрыш на сегодня закончен");
                return false;
            }
            return true;
        }
    }
}
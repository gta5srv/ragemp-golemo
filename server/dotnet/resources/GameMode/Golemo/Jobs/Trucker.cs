using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using GTANetworkAPI;
using GolemoSDK;
using System.Data;
using Newtonsoft.Json;

namespace Golemo.Jobs
{
    class Trucker : Script
    {
        private static nLog Log = new nLog("Trucker");
        private static Random rnd = new Random();

        //Время через которое игрок будет уволен с работы за выход из машины
        private const int _timerSeconds = 60;
        //Время ожидания при загрузке груза (в секундах)
        private const int _waitLoadProcessSeconds = 20;
        //Время ожидания при разгрузке груза (в секундах)
        private const int _waitUnloadProcessSeconds = 30;
        //Максимальный коэффициент суммы зарплаты
        private const int _maxRandomPayment = 5000;
        //Минимальный коэффициент суммы зарплаты
        private const int _minRandomPayment = 1500;
        //Список всех мест трудоустройства на дальнобойщика
        private static List<TruckerEmployment> _employments = new List<TruckerEmployment>();

        //Словарь грузовиков, число это уровень, который необходим, а текст = название моделей грузовика
        private static Dictionary<int, string> _trucks = new Dictionary<int, string>()
        {
            { 1, "boxville4" },
            { 2, "mule" },
            { 3, "benson" },
            { 4, "pounder" },
        };

        [ServerEvent(Event.ResourceStart)]
        public static void OnResourceStart()
        {
            var result = MySQL.QueryRead($"SELECT * FROM `truckeremployments`");
            if (result == null || result.Rows.Count == 0)
            {
                Log.Write("DB truckeremployments return null result.", nLog.Type.Warn);
                return;
            }
            foreach (DataRow Row in result.Rows)
            {
                TruckerEmployment employment = new TruckerEmployment(Convert.ToInt32(Row["id"]),
                    JsonConvert.DeserializeObject<Vector3>(Row["position"].ToString()),
                    JsonConvert.DeserializeObject<Vector3>(Row["finishworkposition"].ToString()),
                    JsonConvert.DeserializeObject<Vector3>(Row["finishworkrotation"].ToString()),
                    JsonConvert.DeserializeObject<List<Vector3>>(Row["truckspawnpositions"].ToString()),
                    JsonConvert.DeserializeObject<List<Vector3>>(Row["truckspawnrotations"].ToString())
                    );
                _employments.Add(employment);
            }
        }

        #region Открытие меню устройства
        public static void OpenTruckerMenu(Player player)
        {
            //Доступно ли открытия меню игроку
            if (!CanWorkingTrucker(player)) return;
            //Создаем список объекта, для того, чтобы отправить данные на клиент
            List<object> data = new List<object>()
            {
                //Загружаем словарь наших грузовиков
                _trucks,
                //Загружаем текущий уроень игрока на данной работе
                Main.Players[player].GetLevelAtThisWork()
            };
            //Формируем json объект в строку
            string json = JsonConvert.SerializeObject(data);
            //Отправляем строку на клиент, для открытия меню
            Trigger.ClientEvent(player, "JOBS::TRUCKER_OPEN_MENU", json);
        }
        #endregion

        #region Remote Events
        [RemoteEvent("SERVER::JOBS::TRUCKER_TAKE_ORDER")]
        public static void TakeOrder(Player player, int orderType, string truck)
        {
            //Может ли игрок начать работу дальнобойщиком
            if (!CanWorkingTrucker(player, truck)) return;
            //На всякий случай, если игрок с клиента отправит название грузовика, которого нет у нас на сервере
            if (_trucks.Values.FirstOrDefault(x => x == truck) == null) return;

            //Создаем список для загрузки в них данных о наших бизнесах
            List<Core.Business> bizlist = new List<Core.Business>();
            //Заполняем наш список бизнесами под наш тип заказа
            LoadRequiredBusinessToList(bizlist, orderType);

            //Если у нас нет бизнеса под тип заказа, который выбрал игрок
            if (bizlist.Count <= 0 || bizlist == null)
            {
                Notify.Error(player, "На данный момент заказы такой продукции недоступны", 3500);
                return;
            }
            if (player.HasData("TRUCKER_EMPLOYMENT_ID"))
            {
                player.SetData("TRUCKEREMPLOYMENTNUMBER", player.GetData<int>("TRUCKER_EMPLOYMENT_ID"));
            }

            //Находим место, где устраивался на работу игрок
            TruckerEmployment employment = GetTruckerEmployment(player);
            if(employment == null)
            {
                Notify.Error(player, "Вы не находитесь на месте трудоустройства", 4000);
                return;
            }
            //Если игрок ещё не брал грузовика, то спавним ему грузовик
            if (!(player.HasData("WORK") && player.GetData<Vehicle>("WORK") != null))
            {
                //Заспавнить грузовик у данного места трудоустройства
                employment.CreateTruck(player, truck);
            }
            //Запоминаем тип заказа, который взял игрок
            player.SetData("ORDERTYPE", orderType);
            //Задаем значение, что игрок ещё не загрузил груз
            player.SetData("ORDER_LOADED", false);
            //Создаем блип и путь к погрузке товара игроку
            employment.SetBlipShortRange(player, true);
            Trigger.ClientEvent(player, "JOBS::TRUCKER_CREATE_LOADPOINT_BLIP", orderType, "Точка погрузки товара");
            Trigger.ClientEvent(player, "JOBS::TRUCKER_START_WORK", true, employment.ExitWorkPosition, employment.ExitWorkRotation);
        }
        [RemoteEvent("SERVER::JOBS::TRUCKER_ENTER_LOADSHAPE")]
        public static void onEnterLoadProductColshape(Player player)
        {
            if (!player.IsInVehicle) return;
            if (player.HasData("ORDER_LOADED") && !player.GetData<bool>("ORDER_LOADED") && player.Vehicle == player.GetData<Vehicle>("WORK"))
            {
                LoadingProducts(player);
            }
        }
        [RemoteEvent("SERVER::JOBS::TRUCKER_STOP_FINISHING_PROCESS")]
        public static void FinishWork(Player player)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (player.HasData("ORDER_BIZ") || player.HasData("ORDER_LOADED") && player.GetData<bool>("ORDER_LOADED"))
            {
                return;
            }
            StopWorkingAndResetData(player);

            Notify.Succ(player, "Вы сдали грузовик и закончили работу", 3500);
        }
        #endregion

        #region Load Products Method
        private static void LoadingProducts(Player player)
        {
            if (player.HasData("ORDER_LOADED"))
            {
                Vehicle truck = player.Vehicle;
                int waitSeconds = _waitLoadProcessSeconds;

                Trigger.ClientEvent(player, "JOBS::TRUCKER_FREEZE", truck, true);
                TextLabel timerText = NAPI.TextLabel.CreateTextLabel($"Осталось {waitSeconds} секунд", truck.Position + new Vector3(0, 0, 2f), 5f, 5f, 4, new Color(255, 255, 255), false, 0);
                Notify.Alert(player, "Началась загрузка товара, подождите..", 4500);

                player.SetData("ORDER_LOAD_TIMER", Timers.Start(1000, () =>
                {
                    if (waitSeconds <= 0)
                    {
                        NAPI.Task.Run(() =>
                        {
                            timerText.Delete();

                            List<Core.Business> bizlist = new List<Core.Business>();
                            LoadRequiredBusinessToList(bizlist, player.GetData<int>("ORDERTYPE"));

                            int index = rnd.Next(0, bizlist.Count);
                            GetRequiredBusinessIndex(bizlist, ref index, player.Position);
                            Core.Business biz = bizlist[index];

                            player.SetData("ORDER_BIZ", biz.ID);
                            player.SetData("ORDER_LOADED", true);

                            Trigger.ClientEvent(player, "JOBS::TRUCKER_FREEZE", truck, false);
                            Trigger.ClientEvent(player, "JOBS::TRUCKER_CREATE_LOADPOINT_BLIP", -1, "Доставить товар", biz.UnloadPoint);
                        });
                        Timers.Stop(player.GetData<string>("ORDER_LOAD_TIMER"));
                        player.ResetData("ORDER_LOAD_TIMER");
                        return;
                    }
                    timerText.Position = truck.Position + new Vector3(0, 0, 2f);
                    timerText.Text = $"Осталось {waitSeconds} секунд";
                    waitSeconds--;
                }));
            }
        }
        #endregion

        #region Player Enter UnloadPoint Business
        public static void onEntityEnterDropTrailer(ColShape shape, Player player)
        {
            try
            {
                if (!player.HasData("ORDERTYPE")) return;
                if (player.GetData<int>("ORDER_BIZ") != shape.GetData<int>("BIZID")) return;
                if (!player.HasData("ORDER_LOADED")) return;
                if (player.GetData<bool>("ORDER_LOADED") == false) return;
                if (!player.IsInVehicle)
                {
                    Notify.Error(player, "Вы должны находится в вашем рабочем грузовике", 3500);
                    return;
                }
                Vehicle truck = player.GetData<Vehicle>("WORK");
                if (truck != player.Vehicle)
                {
                    Notify.Error(player, "Вы должны находится в вашем рабочем грузовике", 3500);
                    return;
                }
                UnLoadingProducts(player);
            }
            catch (Exception e) { Log.Write("onEntityDropTrailer_ATTENTION: " + e.Message, nLog.Type.Error); }
        }
        #endregion

        #region Unload Products Method
        private static void UnLoadingProducts(Player player)
        {
            if (player.HasData("ORDER_LOADED") && player.GetData<bool>("ORDER_LOADED"))
            {
                Vehicle truck = player.Vehicle;
                int waitSeconds = _waitUnloadProcessSeconds;

                Notify.Alert(player, "Началась выгрузка товара, подождите..", 3500);
                Trigger.ClientEvent(player, "JOBS::TRUCKER_FREEZE", truck, true);
                TextLabel timerText = NAPI.TextLabel.CreateTextLabel($"Осталось {waitSeconds} секунд", truck.Position + new Vector3(0, 0, 2f), 5f, 5f, 4, new Color(255, 255, 255), false, 0);
                
                player.SetData("ORDER_LOAD_TIMER", Timers.Start(1000, () =>
                {
                    if (waitSeconds <= 0)
                    {
                        NAPI.Task.Run(() =>
                        {
                            PayForShipping(player);
                            timerText.Delete();
                            //player.ResetData("ORDERTYPE");
                            player.ResetData("ORDER_BIZ");
                            //player.ResetData("ORDER_LOADED");
                            player.SetData("ORDER_LOADED", false);
                            Trigger.ClientEvent(player, "JOBS::TRUCKER_REMOVE_LOADPOINT_BLIP", true);
                            Trigger.ClientEvent(player, "JOBS::TRUCKER_FREEZE", truck, false);
                            //test
                            Trigger.ClientEvent(player, "JOBS::TRUCKER_CREATE_LOADPOINT_BLIP", player.GetData<int>("ORDERTYPE"), "Точка погрузки товара");
                        });
                        Timers.Stop(player.GetData<string>("ORDER_LOAD_TIMER"));
                        player.ResetData("ORDER_LOAD_TIMER");
                        return;
                    }
                    timerText.Position = truck.Position + new Vector3(0, 0, 2f);
                    timerText.Text = $"Осталось {waitSeconds} секунд";
                    waitSeconds--;
                }));
            }
        }
        #endregion

        #region Payment For Shipping
        private static void PayForShipping(Player player)
        {
            int max = Convert.ToInt32(_maxRandomPayment * Core.Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
            int min = Convert.ToInt32(_minRandomPayment * Core.Group.GroupPayAdd[Main.Accounts[player].VipLvl] * Main.oldconfig.PaydayMultiplier);
            int payment = Convert.ToInt32(rnd.Next(min, max));
            //надбавка с учетом уровня игрока на данной работе
            payment += Jobs.WorkManager.PaymentIncreaseAmount[Main.Players[player].WorkID] * Main.Players[player].GetLevelAtThisWork();

            //добавление опыта
            if (Main.Players[player].AddExpForWork(Main.oldconfig.PaydayMultiplier))
                Notify.Alert(player, $"Поздравляем с повышением уровня! Текущий уровень теперь: {Main.Players[player].GetLevelAtThisWork()}");

            MoneySystem.Wallet.Change(player, payment);
            Notify.Succ(player, $"Вы доставили заказ. Вам заплатили ${payment}", 3000);
            Notify.Alert(player, "Загрузите следующий товар для доставки", 4000);
        }
        #endregion

        #region Stop Work
        public static void StopWorkingAndResetData(Player player)
        {
            try
            {
                if (player.HasData("WORK") && player.GetData<Vehicle>("WORK") != null)
                {
                    if (player.IsInVehicle && player.Vehicle == player.GetData<Vehicle>("WORK"))
                    {
                        Core.VehicleManager.WarpPlayerOutOfVehicle(player);
                    }
                    NAPI.Task.Run(() => {
                        if (player.HasData("WORK"))
                        {
                            Vehicle veh = player.GetData<Vehicle>("WORK");
                            if (veh != null) veh.Delete();
                        }
                        player.ResetData("TRUCKNAME");
                        player.ResetData("WORK");
                    }, 1000);
                }
                TruckerEmployment employment = GetTruckerEmployment(player);
                if (employment != null) employment.SetBlipShortRange(player, false);
				if (player.HasData("ORDER_LOAD_TIMER") && !String.IsNullOrEmpty(player.GetData<string>("ORDER_LOAD_TIMER")))
					Timers.Stop(player.GetData<string>("ORDER_LOAD_TIMER"));

                player.ResetData("ORDERTYPE");
                player.ResetData("ORDER_BIZ");
                player.ResetData("ORDER_LOADED");
				player.ResetData("ORDER_LOAD_TIMER");
                player.SetData("ON_WORK", false);
                player.SetData("IN_WORK_CAR", false);
                player.ResetData("TRUCKEREMPLOYMENTNUMBER");

                Trigger.ClientEvent(player, "JOBS::TRUCKER_START_WORK", false);
            }
            catch (Exception e)
            {
                Log.Write(e.Message, nLog.Type.Error);
            }
        }

        [ServerEvent(Event.PlayerExitVehicle)]
        public void onPlayerExitVehicleHandler(Player player, Vehicle vehicle)
        {
            try
            {
                if (NAPI.Data.GetEntityData(vehicle, "TYPE") == "TRUCKER" &&
                Main.Players[player].WorkID == 6 &&
                NAPI.Data.GetEntityData(player, "ON_WORK") &&
                NAPI.Data.GetEntityData(player, "WORK") == vehicle)
                {
                    Notify.Send(player, NotifyType.Warning, NotifyPosition.BottomCenter, $"Если Вы не сядете в транспорт через {_timerSeconds} секунд, то рабочий день закончится", 3000);
                    NAPI.Data.SetEntityData(player, "IN_WORK_CAR", false);
                    if (player.HasData("WORK_CAR_EXIT_TIMER"))
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                    NAPI.Data.SetEntityData(player, "CAR_EXIT_TIMER_COUNT", 0);
                    NAPI.Data.SetEntityData(player, "WORK_CAR_EXIT_TIMER", Timers.Start(1000, () => timer_playerExitWorkVehicle(player, vehicle)));
                }
            }
            catch (Exception e) { Log.Write("PlayerExitVehicle: " + e.Message, nLog.Type.Error); }
        }

        private void timer_playerExitWorkVehicle(Player player, Vehicle vehicle)
        {
            NAPI.Task.Run(() =>
            {
                try
                {
                    if (!player.HasData("WORK_CAR_EXIT_TIMER")) return;
                    if (NAPI.Data.GetEntityData(player, "IN_WORK_CAR"))
                    {
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                        NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                        return;
                    }
                    if (NAPI.Data.GetEntityData(player, "CAR_EXIT_TIMER_COUNT") > _timerSeconds)
                    {
                        Timers.Stop(NAPI.Data.GetEntityData(player, "WORK_CAR_EXIT_TIMER"));
                        NAPI.Data.ResetEntityData(player, "WORK_CAR_EXIT_TIMER");
                        StopWorkingAndResetData(player);
                        Notify.Alert(player, "Вы были уволены с работы", 4500);
                        return;
                    }
                    NAPI.Data.SetEntityData(player, "CAR_EXIT_TIMER_COUNT", NAPI.Data.GetEntityData(player, "CAR_EXIT_TIMER_COUNT") + 1);

                }
                catch (Exception e)
                {
                    Log.Write("Timer_PlayerExitWorkVehicle_Truckers: \n" + e.ToString(), nLog.Type.Error);
                }
            });
        }
        #endregion

        [ServerEvent(Event.PlayerEnterVehicle)]
        public static void onPlayerEnterVehicleHandler(Player player, Vehicle truck, sbyte seatid)
        {
            if (NAPI.Data.GetEntityData(truck, "TYPE") != "TRUCKER") return;
            if (player.VehicleSeat == 0)
            {
                if(player.HasData("WORK") && player.GetData<Vehicle>("WORK") == truck)
                {
                    player.SetData("IN_WORK_CAR", true);
                }
                else
                {
                    Core.VehicleManager.WarpPlayerOutOfVehicle(player);
                    Notify.Error(player, "Вы не являетесь водителем данного грузовика", 3500);
                }
            }
        }

        #region Utils
        private static void LoadRequiredBusinessToList(List<Core.Business> data, int orderType)
        {
            foreach (var item in Core.BusinessManager.BizList.Values)
            {
                if (item.Type == orderType)
                    data.Add(item);
            }
        }
        private static void GetRequiredBusinessIndex(List<Core.Business> data, ref int index, Vector3 pos)
        {
            int i = 0;
            do
            {
                if (i >= data.Count) break;
                index = rnd.Next(0, data.Count);
                i++;
            } while (pos.DistanceTo(data[index].EnterPoint) < 200);
        }
        private static TruckerEmployment GetTruckerEmployment(Player player)
        {
            TruckerEmployment employment = null;
            if (player.HasData("TRUCKEREMPLOYMENTNUMBER"))
            {
                employment = _employments.Find(x => x.Number == player.GetData<int>("TRUCKEREMPLOYMENTNUMBER"));
            }
            return employment;
        }
        private static bool CanWorkingTrucker(Player player, string truck = null)
        {
            bool canWork = false;
            if (player != null)
            {
                if (!Main.Players.ContainsKey(player)) canWork = false;
                else if (Main.Players[player].WorkID != 6)
                {
                    Notify.Error(player, "Вы не работаете дальнобойщиком", 3500);
                    canWork = false;
                }
                else if (player.IsInVehicle)
                {
                    Notify.Error(player, "Выйдете из машины", 3500);
                    canWork = false;
                }
                else if (player.HasData("WORK") && player.GetData<Vehicle>("WORK") != null && truck != null)
                {
                    VehicleHash hash = (VehicleHash)NAPI.Util.GetHashKey(truck);
                    Vehicle ptruck = player.GetData<Vehicle>("WORK");
                    if((VehicleHash)ptruck.Model == hash) canWork = true;
                    else
                    {
                        Notify.Error(player, "Прежде чем взять другой грузовик, вам надо сдать прежний", 3500);
                        canWork = false;
                    }
                }
                else if (player.HasData("ORDER_BIZ"))
                {
                    Notify.Error(player, "Вам нужно доставить товар, чтобы взять другой заказ", 3500);
                    canWork = false;
                }
                else canWork = true;
            }
            return canWork;
        }
        private enum OrderType : int
        {
            Tools = 0,
            Fuel = 1,
            Vehicle = 2,
            Weapons = 6,
            Cloth = 7,
            Food = 8,
            TattooPaint = 9,
            SpareParts = 12,
        }
        #endregion
    }

    class TruckerEmployment
    {
        public int Number { get; }
        public Vector3 Position { get; }
        public Vector3 ExitWorkPosition { get; }
        public Vector3 ExitWorkRotation { get; }

        public List<Vector3> TruckSpawnPositions = new List<Vector3>();
        public List<Vector3> TruckSpawnRotations = new List<Vector3>();
        private int _lastSpawnIndex = 0;

        #region GTA Elements
        private GTANetworkAPI.Blip _blip;
        private GTANetworkAPI.Marker _marker;
        private GTANetworkAPI.ColShape _shape;
        private GTANetworkAPI.TextLabel _label;
        private GTANetworkAPI.ColShape _exitShape;
        #endregion
        public TruckerEmployment(int number, Vector3 position, Vector3 finishWorkPosition, Vector3 finishWorkRotation, List<Vector3> spawnPositions, List<Vector3> spawnRotations)
        {
            Number = number;
            Position = position;
            ExitWorkPosition = finishWorkPosition;
            ExitWorkRotation = finishWorkRotation;
            TruckSpawnPositions = spawnPositions;
            TruckSpawnRotations = spawnRotations;

            _blip = NAPI.Blip.CreateBlip(477, Position, 1, 6, "Работа: Дальнобойщик", 255, 0, true, 0, 0);
            _marker = NAPI.Marker.CreateMarker(1, Position - new Vector3(0, 0, 1.7f), new Vector3(), new Vector3(), 1f, new Color(111, 111, 111, 111), false, 0);
            _label = NAPI.TextLabel.CreateTextLabel("[Дальнобойщик]", Position, 3f, 5f, 4, new Color(252, 70, 38), false, 0);
            _shape = NAPI.ColShape.CreateCylinderColShape(Position, 1f, 2f, 0);
            _shape.OnEntityEnterColShape += (s, e) =>
            {
                if (e.IsInVehicle) return;
                NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 807);
                NAPI.Data.SetEntityData(e, "TRUCKER_EMPLOYMENT_ID", Number);
            };
            _shape.OnEntityExitColShape += (s, e) =>
            {
                NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 0);
                NAPI.Data.ResetEntityData(e, "TRUCKER_EMPLOYMENT_ID");
            };
            CreateFinishingWorkShape();
        }
        public void CreateTruck(Player player, string truck)
        {
            VehicleHash vhash = (VehicleHash)NAPI.Util.GetHashKey(truck);
            Vehicle veh = NAPI.Vehicle.CreateVehicle(vhash, TruckSpawnPositions[_lastSpawnIndex], new Vector3(), 1, 1, "TRUCKER", 255, false, false, 0);
            NAPI.Entity.SetEntityRotation(veh, TruckSpawnRotations[_lastSpawnIndex]);
            NAPI.Data.SetEntityData(veh, "ACCESS", "WORK");
            NAPI.Data.SetEntityData(veh, "WORK", 6);
            NAPI.Data.SetEntityData(veh, "TYPE", "TRUCKER");
            NAPI.Data.SetEntityData(veh, "ON_WORK", true);
            NAPI.Data.SetEntityData(veh, "DRIVER", player);
            veh.SetSharedData("PETROL", Core.VehicleManager.VehicleTank[veh.Class]);
            player.SetData("WORK", veh);
            player.SetData("TRUCKNAME", truck);
            player.SetData("ON_WORK", true);
            player.SetData("IN_WORK_CAR", true);
            player.SetIntoVehicle(veh, 0);
            _lastSpawnIndex++;
            if (_lastSpawnIndex >= TruckSpawnPositions.Count) 
                _lastSpawnIndex = 0;
        }
        private void CreateFinishingWorkShape()
        {
            _exitShape = NAPI.ColShape.CreateCylinderColShape(ExitWorkPosition, 3f, 5f, 0);
            NAPI.Data.SetEntitySharedData(_exitShape, "TRUCKER_WORK_FINISH_SHAPE", true);
            _exitShape.OnEntityEnterColShape += (s, e) =>
            {
                if (e.IsInVehicle)
                {
                    if (e.HasData("WORK") && e.GetData<Vehicle>("WORK") != null)
                    {
                        if (e.HasData("ORDER_BIZ") || e.HasData("ORDER_LOADED") && e.GetData<bool>("ORDER_LOADED")) return;
                        if(e.GetData<Vehicle>("WORK") == e.Vehicle)
                        {
                            Trigger.ClientEvent(e, "JOBS::TRUCKER_START_FINISHING_PROCESS", ExitWorkPosition, ExitWorkRotation, true);
                        }
                    }
                }
            };
            _exitShape.OnEntityExitColShape += (s, e) =>
            {
                if (!Main.Players.ContainsKey(e)) return;
                if (Main.Players[e].WorkID != 6) return;
                if (!(e.HasData("WORK") && e.GetData<Vehicle>("WORK") != null)) return;
                Trigger.ClientEvent(e, "JOBS::TRUCKER_START_FINISHING_PROCESS", ExitWorkPosition, ExitWorkRotation, false);
            };
        }

        public void SetBlipShortRange(Player player, bool state)
        {
            Trigger.ClientEvent(player, "JOBS::TRUCKER_BLIP_SHORT_RANGE", _blip, state);
        }
    }
}

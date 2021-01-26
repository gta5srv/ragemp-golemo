using GTANetworkAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Golemo.GUI;
using Golemo.MoneySystem;
using GolemoSDK;
using System.Threading;
using Golemo.Houses;
using Golemo.Core;
using static Golemo.Core.VehicleManager;
using Golemo;

namespace Golemo.Houses

{
    class ParkManager : Script
    {

        private static nLog Log = new nLog("ParkManager");

        [ServerEvent(Event.ResourceStart)]
        public void onResourceStart()
        {
            try
            {
                var colsp = new ParkBuy(new Vector3(-1167.416, -700.0884, 21.29));
                Log.Write("Create ParkFine point.", nLog.Type.Success);
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"PARKMAMAGER\":\n" + e.ToString(), nLog.Type.Error);
            }
        }

        // Реальные названия на серверной части(телефон, при продаже, где угодно) TODO
        public static Dictionary<string, string> ModelList = new Dictionary<string, string>()
        {	
           
              {"deluxo", "Golemo-Mode model" }, // пример
              //modelname  //Realname
        };

        public static string GetNormalName(string model)
        {
            if (!ModelList.ContainsKey(model))
            {
                return model;
            }
            else
            {
                return ModelList[model];
            }
        }

        public static List<Vector3> ParkList = new List<Vector3>()
        {
            new Vector3(-1183.132, -679.4572, 25.98754), // 1
            new Vector3(-1186.957, -675.3882, 25.9874), // 2
            new Vector3(-1191.759, -669.8932, 25.98754), // 3
            new Vector3(-1199.971, -659.928, 25.98724), // 4
            new Vector3(-1204.201, -654.2341, 25.988), // 5
            new Vector3(-1217.204, -688.3877, 25.98716), // 6
            new Vector3(-1220.613, -684.1316, 25.98973), // 7
            new Vector3(-1227.106, -676.4997, 25.98831), // 8
        };
        public static void BuyParkPlace(Player player)
        {
            var costcar = 350;
            if (Main.Players[player].Money < costcar)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Недостаточно средств, цена: [{costcar}$]", 3000);
                return;
            }
            Houses.House house = Houses.HouseManager.GetHouse(player, true);
            if (house != null)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас уже есть дом!", 3000);
                return;
            }
            var targetVehicles = VehicleManager.getAllPlayerVehicles(player.Name.ToString());
            var vehicle = "";
            foreach (var num in targetVehicles)
            {
                vehicle = num;
                break;
            }
            if (vehicle == "" || vehicle == null)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У вас нет машины", 3000);
                return;
            }
            foreach (var v in NAPI.Pools.GetAllVehicles())
            {
                if (v.HasData("ACCESS") && v.GetData<string>("ACCESS") == "PERSONAL" && NAPI.Vehicle.GetVehicleNumberPlate(v) == vehicle)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Машина была уже вызвана ранее!", 3000);
                    return;
                }
            }
            MoneySystem.Wallet.Change(player, -costcar);
            SetCarInFreeParkPlace(player, vehicle);
        }

        public static string FindFirstCarNum(Player player)
        {
            var targetVehicles = VehicleManager.getAllPlayerVehicles(player.Name.ToString());
            var vehicle = "";
            foreach (string num in targetVehicles)
            {
                vehicle = num;
                break;
            }
            return vehicle;
        }
        public static void interactionPressed(Player player, int id)
        {
            try
            {
                switch (id)
                {
                    case 556:
                        BuyParkPlace(player);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Write("EXCEPTION AT \"PARK_INTERACTION\":\n" + e.ToString(), nLog.Type.Error);
            }
        }
        public static void SetCarInFreeParkPlace(Player player, string number)
        {
            var rnd = new Random();
            var id = rnd.Next(1, ParkList.Count);
            var vehdata = VehicleManager.Vehicles[number];
            var veh = NAPI.Vehicle.CreateVehicle((VehicleHash)NAPI.Util.GetHashKey(vehdata.Model), ParkList[id], new Vector3(0, 0, 309.6527), 0, 0);

            VehicleStreaming.SetEngineState(veh, false);
            VehicleStreaming.SetLockStatus(veh, true);
            vehdata.Holder = player.Name;
            veh.SetData("ACCESS", "PERSONAL");
            veh.SetData("ITEMS", vehdata.Items);
            veh.SetData("OWNER", player);
            veh.SetSharedData("PETROL", vehdata.Fuel);
            NAPI.Vehicle.SetVehicleNumberPlate(veh, number);
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Парковочное место: №{id+1}", 3000);
            VehicleManager.ApplyCustomization(veh);
        }

        internal class ParkBuy
        {
            public Vector3 Position { get; }

            [JsonIgnore]
            private Blip blip = null;
            [JsonIgnore]
            private ColShape shape = null;
            [JsonIgnore]
            private TextLabel label = null;
            [JsonIgnore]
            private Marker marker = null;

            public ParkBuy(Vector3 pos)
            {
                Position = pos;
                blip = NAPI.Blip.CreateBlip(50, pos, 1, 47, "Парковка", 255, 0, true);
                shape = NAPI.ColShape.CreateCylinderColShape(pos, 2f, 47, 0);
                shape.OnEntityEnterColShape += (s, entity) =>
                {
                    try
                    {
                        entity.SetData("INTERACTIONCHECK", 556);
                    }
                    catch (Exception e) { Console.WriteLine("shape.OnEntityEnterColshape: " + e.Message); }
                };
                shape.OnEntityExitColShape += (s, entity) =>
                {
                    try
                    {
                        entity.SetData("INTERACTIONCHECK", 0);
                    }
                    catch (Exception e) { Console.WriteLine("shape.OnEntityEnterColshape: " + e.Message); }
                };
                label = NAPI.TextLabel.CreateTextLabel("~o~Парковка", new Vector3(pos.X, pos.Y, pos.Z), 20F, 0.5F, 0, new Color(255, 255, 255), true, 0);
                marker = NAPI.Marker.CreateMarker(1, pos - new Vector3(0, 0, 1f), new Vector3(), new Vector3(), 1f, new Color(0, 175, 250, 220), false, 0);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #1", new Vector3(-1183.132, -679.4572, 25.98754), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #2", new Vector3(-1186.957, -675.3882, 25.9874), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #3", new Vector3(-1191.759, -669.8932, 25.98754), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #4", new Vector3(-1199.971, -659.928, 25.98724), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #5", new Vector3(-1204.201, -654.2341, 25.988), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #6", new Vector3(-1217.204, -688.3877, 25.98716), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #7", new Vector3(-1220.613, -684.1316, 25.98973), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~b~Место #8", new Vector3(-1227.106, -676.4997, 25.98831), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~w~Парковщик", new Vector3(-1167.388, -700.1123, 23.0), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
                label = NAPI.TextLabel.CreateTextLabel("~w~Заказать авто", new Vector3(-928.6824, -2037.048, 9.3), 5f, 0.3f, 0, new Color(255, 255, 255), true, NAPI.GlobalDimension);
            }
        }



        internal class ParkPlace
        {
            public int ID { get; set; }
            public Vector3 Position { get; }
            public double Heading { get; }
            public bool Using { get; }

            public ParkPlace(int id, Vector3 pos, double rot, bool usin)
            {
                Position = pos;
                Heading = rot;
                ID = id;
                Using = usin;
            }
        }

        public static void OpenMenu(Player player)
        {
            Menu menu = new Menu("parkcars", false, false);
            menu.Callback = callback_cars;

            Menu.Item menuItem = new Menu.Item("header", Menu.MenuItem.Header);
            menuItem.Text = "Машина[Парковка]";
            menu.Add(menuItem);

            foreach (var v in VehicleManager.getAllPlayerVehicles(player.Name))
            {
                menuItem = new Menu.Item(v, Menu.MenuItem.Button);
                menuItem.Text = $"{ParkManager.GetNormalName(VehicleManager.Vehicles[v].Model)} - {v}";
                menu.Add(menuItem);
                break;
            }

            menuItem = new Menu.Item("close", Menu.MenuItem.Button);
            menuItem.Text = "Закрыть";
            menu.Add(menuItem);

            menu.Open(player);
        }

        private static void callback_cars(Player player, Menu menu, Menu.Item item, string eventName, dynamic data)
        {
            NAPI.Task.Run(() =>
            {
                try
                {
                    MenuManager.Close(player);
                    if (item.ID == "close") return;
                    OpenSelectedCarMenu(player, item.ID);
                }
                catch (Exception e) { Log.Write("callback_cars: " + e.Message + e.Message, nLog.Type.Error); }
            });
        }

        public static void OpenSelectedCarMenu(Player player, string number)
        {
            Menu menu = new Menu("selectedcar", false, false);
            menu.Callback = callback_selectedcar;

            var vData = VehicleManager.Vehicles[number];

            Menu.Item menuItem = new Menu.Item("header", Menu.MenuItem.Header);
            menuItem.Text = number;
            menu.Add(menuItem);

            menuItem = new Menu.Item("model", Menu.MenuItem.Card);
            menuItem.Text = ParkManager.GetNormalName(vData.Model);
            menu.Add(menuItem);

            var vClass = NAPI.Vehicle.GetVehicleClass(NAPI.Util.VehicleNameToModel(vData.Model));

            menuItem = new Menu.Item("repair", Menu.MenuItem.Button);
            menuItem.Text = $"Восстановить {VehicleManager.VehicleRepairPrice[vClass]}$";
            menu.Add(menuItem);

            menuItem = new Menu.Item("key", Menu.MenuItem.Button);
            menuItem.Text = $"Получить дубликат ключа";
            menu.Add(menuItem);

            menuItem = new Menu.Item("changekey", Menu.MenuItem.Button);
            menuItem.Text = $"Сменить замки";
            menu.Add(menuItem);

            menuItem = new Menu.Item("evac", Menu.MenuItem.Button);
            menuItem.Text = $"Эвакуировать машину";
            menu.Add(menuItem);


            int price = 0;
            if (BusinessManager.ProductsOrderPrice.ContainsKey(vData.Model))
            {
                switch (Main.Accounts[player].VipLvl)
                {
                    case 0: // None
                        price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.5);
                        break;
                    case 1: // Bronze
                        price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.6);
                        break;
                    case 2: // Silver
                        price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.7);
                        break;
                    case 3: // Gold
                        price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.8);
                        break;
                    case 4: // Platinum
                        price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.9);
                        break;
                    default:
                        price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.5);
                        break;
                }
            }

            menuItem = new Menu.Item("close", Menu.MenuItem.Button);
            menuItem.Text = "Закрыть";
            menu.Add(menuItem);

            menu.Open(player);
        }
        private static void callback_selectedcar(Player player, Menu menu, Menu.Item item, string eventName, dynamic data)
        {
            MenuManager.Close(player);
            switch (item.ID)
            {
                case "sell":
                    player.SetData("CARSELLGOV", menu.Items[0].Text);
                    VehicleManager.VehicleData vData = VehicleManager.Vehicles[menu.Items[0].Text];
                    int price = 0;
                    if (BusinessManager.ProductsOrderPrice.ContainsKey(vData.Model))
                    {
                        switch (Main.Accounts[player].VipLvl)
                        {
                            case 0: // None
                                price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.5);
                                break;
                            case 1: // Bronze
                                price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.6);
                                break;
                            case 2: // Silver
                                price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.7);
                                break;
                            case 3: // Gold
                                price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.8);
                                break;
                            case 4: // Platinum
                                price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.9);
                                break;
                            default:
                                price = Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.5);
                                break;
                        }
                    }
                    Trigger.ClientEvent(player, "openDialog", "CAR_SELL_TOGOV", $"Вы действительно хотите продать государству {ParkManager.GetNormalName(vData.Model)} ({menu.Items[0].Text}) за ${price}?");
                    MenuManager.Close(player);
                    return;
                case "repair":
                    vData = VehicleManager.Vehicles[menu.Items[0].Text];
                    if (vData.Health > 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Машина не нуждается в восстановлении", 3000);
                        return;
                    }

                    var vClass = NAPI.Vehicle.GetVehicleClass(NAPI.Util.VehicleNameToModel(vData.Model));
                    if (!MoneySystem.Wallet.Change(player, -VehicleManager.VehicleRepairPrice[vClass]))
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "У Вас недостаточно средств", 3000);
                        return;
                    }
                    vData.Items = new List<nItem>();
                    GameLog.Money($"player({Main.Players[player].UUID})", $"server", VehicleManager.VehicleRepairPrice[vClass], $"carRepair({vData.Model})");
                    vData.Health = 1000;
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы восстановили {ParkManager.GetNormalName(vData.Model)} ({menu.Items[0].Text})", 3000);
                    return;
                case "evac":
                    if (!Main.Players.ContainsKey(player)) return;

                    var number = menu.Items[0].Text;

                    if (Main.Players[player].Money < 200)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Недостаточно средств (не хватает {200 - Main.Players[player].Money}$)", 3000);
                        return;
                    }


                    var targetVehicles = VehicleManager.getAllPlayerVehicles(player.Name.ToString());
                    var vehicle = "";
                    foreach (var num in targetVehicles)
                    {
                        vehicle = num;
                        break;
                    }


                    foreach (var v in NAPI.Pools.GetAllVehicles())
                    {
                        if (v.HasData("ACCESS") && v.GetData<string>("ACCESS") == "PERSONAL" && NAPI.Vehicle.GetVehicleNumberPlate(v) == vehicle)
                        {
                            var veh = v;
                            if (veh == null) return;
                            //VehicleManager.Vehicles[number].Fuel = (!NAPI.Data.HasEntitySharedData(veh, "PETROL")) ? VehicleManager.VehicleTank[veh.Class] : NAPI.Data.GetEntitySharedData(veh, "PETROL");
                            VehicleManager.Vehicles[number].Fuel = (!NAPI.Data.HasEntityData(veh, "PETROL")) ? VehicleManager.VehicleTank[veh.Class] : NAPI.Data.GetEntityData(veh, "PETROL");
                            NAPI.Entity.DeleteEntity(veh);

                            MoneySystem.Wallet.Change(player, -200);
                            GameLog.Money($"player({Main.Players[player].UUID})", $"server", 200, $"carEvac");
                            Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Ваша машина была отогнана на стоянку", 3000);
                            break;
                        }
                    }
                    return;
                case "key":
                    if (!Main.Players.ContainsKey(player)) return;

                    var tryAdd = nInventory.TryAdd(player, new nItem(ItemType.CarKey));
                    if (tryAdd == -1 || tryAdd > 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Недостаточно места в инвентаре", 3000);
                        return;
                    }

                    nInventory.Add(player, new nItem(ItemType.CarKey, 1, $"{menu.Items[0].Text}_{VehicleManager.Vehicles[menu.Items[0].Text].KeyNum}"));
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы получили ключ от машины с номером {menu.Items[0].Text}", 3000);
                    return;
                case "changekey":
                    if (!Main.Players.ContainsKey(player)) return;

                    if (!MoneySystem.Wallet.Change(player, -1000))
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Смена замков стоит $1000", 3000);
                        return;
                    }

                    VehicleManager.Vehicles[menu.Items[0].Text].KeyNum++;
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы сменили замки на машине {menu.Items[0].Text}. Теперь старые ключи не могут быть использованы", 3000);
                    return;

            }
        }

    }
}
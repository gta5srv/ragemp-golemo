using GTANetworkAPI;
using System;
using Golemo.GUI;
using Golemo.Houses;
using System.Collections.Generic;
using System.Linq;
using GolemoSDK;
using Golemo.Core;

namespace Golemo.Houses
{
    class AutoSelltoGov : Script
    {
       
        private static Dictionary<int, ColShape> Cols = new Dictionary<int, ColShape>();
        private static Vector3 svalkaCheckpoints = new Vector3(1512.705, -2097.8896, 75.6941);
        private static nLog Log = new nLog("SVALKA");

        [ServerEvent(Event.ResourceStart)]
        public void onResourceStart()
        {
            try
            {
                NAPI.Marker.CreateMarker(1, svalkaCheckpoints - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 3, new Color(0, 255, 255));
                NAPI.Blip.CreateBlip(527, svalkaCheckpoints, 1f, 84, "Свалка", 255, 0, true, 0, 0);
                Cols.Add(0, NAPI.ColShape.CreateCylinderColShape(svalkaCheckpoints, 1, 3, 0));
                Cols[0].SetData("INTERACT", 101);
                Cols[0].OnEntityEnterColShape += svalkaShape_onEntityEnterColShape;
                Cols[0].OnEntityExitColShape += svalkaShape_onEntityExitColShape;
                NAPI.TextLabel.CreateTextLabel(Main.StringToU16("~b~Свалка"), svalkaCheckpoints + new Vector3(0, 0, 0.7), 5F, 0.3F, 0, new Color(255, 255, 255));
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        private void svalkaShape_onEntityEnterColShape(ColShape shape, Player entity)
        {
            try
            {
                NAPI.Data.SetEntityData(entity, "INTERACTIONCHECK", shape.GetData<int>("INTERACT"));
            }
            catch (Exception ex) { Log.Write("svalkaShape_onEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
        }

        private void svalkaShape_onEntityExitColShape(ColShape shape, Player entity)
        {
            try
            {
                NAPI.Data.SetEntityData(entity, "INTERACTIONCHECK", 0);
            }
            catch (Exception ex) { Log.Write("svalkaShape_onEntityExitColShape: " + ex.Message, nLog.Type.Error); }
        }
        public static void OpenCarsSellMenu(Player player)
        {
            Menu menu = new Menu("carsell", false, false);
            menu.Callback = callback_carsell;

            Menu.Item menuItem = new Menu.Item("header", Menu.MenuItem.Header);
            menuItem.Text = "Продажа автомобилей";
            menu.Add(menuItem);

            menuItem = new Menu.Item("label", Menu.MenuItem.Card);
            menuItem.Text = "Выберите машину, которую хотите продать";
            menu.Add(menuItem);

            foreach (var v in VehicleManager.getAllPlayerVehicles(player.Name))
            {
                var vData = VehicleManager.Vehicles[v];
                var price = (BusinessManager.ProductsOrderPrice.ContainsKey(vData.Model)) ? Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.5) : 0;
                menuItem = new Menu.Item(v, Menu.MenuItem.Button);
                menuItem.Text = $"{vData.Model} - {v} ({price}$)";
                menu.Add(menuItem);
            }

            menuItem = new Menu.Item("close", Menu.MenuItem.Button);
            menuItem.Text = "Закрыть";
            menu.Add(menuItem);

            menu.Open(player);
        }
        private static void callback_carsell(Player player, Menu menu, Menu.Item item, string eventName, dynamic data)
        {

            if (item.ID == "close")
            {
                MenuManager.Close(player);
                return;
            }
            var vData = VehicleManager.Vehicles[item.ID];
            var price = (BusinessManager.ProductsOrderPrice.ContainsKey(vData.Model)) ? Convert.ToInt32(BusinessManager.ProductsOrderPrice[vData.Model] * 0.5) : 0;
            MoneySystem.Wallet.Change(player, price);
            GameLog.Money($"server", $"player({Main.Players[player].UUID})", price, $"carSellgov({vData.Model})");
            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы продали государству {vData.Model} ({item.ID}) за {price}$", 3000);
            VehicleManager.Remove(item.ID);
            MenuManager.Close(player);
        }
    }
}

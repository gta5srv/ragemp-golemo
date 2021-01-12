using GTANetworkAPI;
using Golemo.Core;
using GolemoSDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace Golemo.Core
{
    class Сhangenum : Script
    {
        private static nLog RLog = new nLog("cangenum");
        private static GTANetworkAPI.ColShape shape;
        private static GTANetworkAPI.Marker intmarker;
        private static GTANetworkAPI.Blip blip;
        private static GTANetworkAPI.TextLabel lable;
        private static Vector3 Changenum = new Vector3(415.1404, -1014.025, 28.34782);
        private static int Price = 12500;

        [ServerEvent(Event.ResourceStart)]
        public static void EnterShapeRealtor()
        {
            try
            {
                #region Creating Marker & Colshape & Blip
                blip = NAPI.Blip.CreateBlip(678, Changenum, 0.6f, 58, "Смена номеров", shortRange: true, dimension: 0);
                intmarker = NAPI.Marker.CreateMarker(1, Changenum, new Vector3(), new Vector3(), 2f, new Color(66, 170, 255, 150), false, 0);
                shape = NAPI.ColShape.CreateCylinderColShape(Changenum, 1, 3, 0);
                lable = NAPI.TextLabel.CreateTextLabel("~b~Смена номеров", Changenum + new Vector3(0, 0, 0.85), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                NAPI.TextLabel.CreateTextLabel("~w~Цена:12500$\n" + "~r~Нaжмите E", Changenum + new Vector3(0, 0, 0.65), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                shape.OnEntityEnterColShape += (s, ent) =>
                {
                    try
                    {
                        NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 599);
                        Trigger.ClientEvent(ent, "Нажмите Е", true);
                    }
                    catch (Exception ex) { Console.WriteLine("shape.OnEntityEnterColShape: " + ex.Message); }
                };
                shape.OnEntityExitColShape += (s, ent) =>
                {
                    try
                    {
                        NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 0);
                        Trigger.ClientEvent(ent, "Нажмите Е", false);
                    }
                    catch (Exception ex) { Console.WriteLine("shape.OnEntityExitColShape: " + ex.Message); }
                };
                #endregion

                RLog.Write("Loaded", nLog.Type.Info);
            }
            catch (Exception e) { RLog.Write(e.ToString(), nLog.Type.Error); }
        }
        public static void Сhangeplate(Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (player.VehicleSeat != -1)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Вы должны быть в водительском месте", 3000);
                    return;
                }
                var veh = player.Vehicle;
                var oldNum = player.Vehicle.NumberPlate;
                var holder = VehicleManager.Vehicles[oldNum].Holder;
                Core.Character.Character acc = Main.Players[player];
                var firs = acc.FirstName;
                var famil = acc.LastName;
                var pl = firs + "_" + famil;
                var vData = VehicleManager.Vehicles[oldNum];
                if (pl != holder)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Это не ваша машина", 3000);
                    return;
                }
                if (Main.Players[player].Money < Price)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств", 3000);
                    return;
                }  
                MoneySystem.Wallet.Change(player, -Price);
                string newNum = VehicleManager.GenerateNumber();
                veh.NumberPlate = newNum;
                VehicleManager.Vehicles.Remove(oldNum);
                VehicleManager.Vehicles.Add(newNum, vData);
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Новый номер для {oldNum} = {newNum}", 3000);
                MySQL.Query($"UPDATE vehicles SET number='{newNum}' WHERE number='{oldNum}'");
                VehicleManager.Save(newNum);
                //nInventory.Remove(player, ItemType.CarKey, 1);
                //nInventory.Add(player, new nItem(ItemType.CarKey, 1, $"{newNum}_{VehicleManager.Vehicles[newNum].KeyNum}"));

            }
            catch (Exception e) { RLog.Write("changenum: " + e.Message, nLog.Type.Error); }
        }
    }
}
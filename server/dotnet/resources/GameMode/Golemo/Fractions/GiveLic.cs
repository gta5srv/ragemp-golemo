using GTANetworkAPI;
using Golemo.Core;
using GolemoSDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace Golemo.Fractions
{
    class GiveLic : Script
    {
        private static nLog RLog = new nLog("ChangerNumberPlate");
        private static GTANetworkAPI.ColShape shapeMed;
        private static GTANetworkAPI.Marker markerMed;
        private static GTANetworkAPI.TextLabel lableMed;
        private static Vector3 Med = new Vector3(267.29532, -1356.6094, 23.41779);
        private static GTANetworkAPI.ColShape shapeGun;
        private static GTANetworkAPI.Marker markerGun;
        private static GTANetworkAPI.TextLabel lableGun;
        private static Vector3 Gun = new Vector3(436.98013, -993.9815, 29.569595);
        private static int PriceMed = 12500; // цена на мед.карту
        private static int PriceGun = 12500; // цена на лицензию на оружие 

        [ServerEvent(Event.ResourceStart)]
        public static void EnterShapeRealtor()
        {
            try
            {
                #region Creating Marker & Colshape
                //мед.карта
                markerMed = NAPI.Marker.CreateMarker(1, Med, new Vector3(), new Vector3(), 1f, new Color(66, 170, 255, 150), false, 0);
                shapeMed = NAPI.ColShape.CreateCylinderColShape(Med + new Vector3(0, 0, 0.65), 1, 1, 0);
                lableMed = NAPI.TextLabel.CreateTextLabel("~b~Получение мед.карты", Med + new Vector3(0, 0, 0.85), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                NAPI.TextLabel.CreateTextLabel("~w~Цена:12500$\n" + "~r~Нaжмите E", Med + new Vector3(0, 0, 0.65), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                shapeMed.OnEntityEnterColShape += (s, ent) =>
                {
                    try
                    {
                        NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 228);
                        Trigger.ClientEvent(ent, "Нажмите Е", true);
                    }
                    catch (Exception ex) { Console.WriteLine("shape.OnEntityEnterColShape: " + ex.Message); }
                };
                shapeMed.OnEntityExitColShape += (s, ent) =>
                {
                    try
                    {
                        NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 0);
                        Trigger.ClientEvent(ent, "Нажмите Е", false);
                    }
                    catch (Exception ex) { Console.WriteLine("shape.OnEntityExitColShape: " + ex.Message); }
                };
                //Лицензия на оружие
                markerGun = NAPI.Marker.CreateMarker(1, Gun, new Vector3(), new Vector3(), 1f, new Color(66, 170, 255, 150), false, 0);
                shapeGun = NAPI.ColShape.CreateCylinderColShape(Gun + new Vector3(0, 0, 0.65), 1, 1, 0);
                lableGun = NAPI.TextLabel.CreateTextLabel("~b~Получение лицензии на оружие", Gun + new Vector3(0, 0, 0.85), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                NAPI.TextLabel.CreateTextLabel("~w~Цена:12500$\n" + "~r~Нaжмите E", Gun + new Vector3(0, 0, 0.65), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                shapeGun.OnEntityEnterColShape += (s, ent) =>
                {
                    try
                    {
                        NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 229);
                        Trigger.ClientEvent(ent, "Нажмите Е", true);
                    }
                    catch (Exception ex) { Console.WriteLine("shape.OnEntityEnterColShape: " + ex.Message); }
                };
                shapeGun.OnEntityExitColShape += (s, ent) =>
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
        public static void MedLic(Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (Main.Players[player].Licenses[7])
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть мед.карта.", 3000);
                    return;
                }
                if (!MoneySystem.Wallet.Change(player, -PriceMed))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас недостаточно средств.", 3000);
                    return;
                }
                if (Manager.countOfFractionMembers(8) > 2)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"В штате есть медики, обратитесь к ним.", 3000);
                    return;
                }
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы купили мед.карту", 3000);
                Main.Players[player].Licenses[7] = true;
                GUI.Dashboard.sendStats(player);
            }
            catch (Exception e) { RLog.Write("GiveLic: " + e.Message, nLog.Type.Error); }
        }
        public static void GunLic(Player player)
        {
            try
            {

                if (!Main.Players.ContainsKey(player)) return;
                if (Main.Players[player].Licenses[6])
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас уже есть лицензия на оружие.", 3000);
                    return;
                }
                if (!MoneySystem.Wallet.Change(player, -PriceGun))
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"У Вас недостаточно средств.", 3000);
                    return;
                }
                if (Manager.countOfFractionMembers(7) > 2)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"В штате есть полицейсике, обратитесь к ним.", 3000);
                    return;
                }
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы купили лицензию на оружие.", 3000);
                Main.Players[player].Licenses[6] = true;
                GUI.Dashboard.sendStats(player);
            }
            catch (Exception e) { RLog.Write("GiveLic: " + e.Message, nLog.Type.Error); }
        }
    }
}
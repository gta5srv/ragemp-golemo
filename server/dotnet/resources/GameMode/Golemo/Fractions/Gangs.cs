﻿using System.Collections.Generic;
using GTANetworkAPI;
using Golemo.Core;
using GolemoSDK;
using System;

namespace Golemo.Fractions
{
    class Gangs : Script
    {
        private static nLog Log = new nLog("Gangs");

        public static Dictionary<int, Vector3> EnterPoints = new Dictionary<int, Vector3>()
        {
            { 1, new Vector3(-223.069, -1616.918, 33.74932) },
            { 2, new Vector3(85.79318, -1958.851, 20.0017) },
            { 3, new Vector3(1408.579, -1486.897, 59.53736) },
            { 4, new Vector3(892.2407, -2172.888, 31.16626) },
            { 5, new Vector3(485.3334, -1528.692, 28.18008) },
        };
        public static Dictionary<int, Vector3> ExitPoints = new Dictionary<int, Vector3>()
        {
            { 1, new Vector3(-201.7147, -1627.962, -38.664788) },
            { 2, new Vector3(82.57095, -1958.607, -23.41236) },
            { 3, new Vector3(1420.487, -1497.264, -107.8639) },
            { 4, new Vector3(892.4592, -2168.068, 0.921189) },
            { 5, new Vector3(484.9963, -1536.083, -33.22089) },
        };

        public static List<Vector3> DrugPoints = new List<Vector3>()
        {
            new Vector3(8.621573, 3701.914, 39.51624),
            new Vector3(3804.169, 4444.753, 3.977164),
        };

        private static TextLabel _priceDrugLabel;
        private static Random rnd = new Random();
        private static int[] _currency = new int[] { 40, 120 };
        private static int _pricePerDrug;

        public static void UpdateMultiplier()
        {
            _pricePerDrug = rnd.Next(_currency[0], _currency[1] + 1);
            if(_priceDrugLabel != null)
                _priceDrugLabel.Text = $"~g~Купить наркоту ({_pricePerDrug}$/грамм)";
            Log.Write($"Обновлена цена закупки наркоты: {_pricePerDrug}");
        }

        [ServerEvent(Event.ResourceStart)]
        public void Event_OnResourceStart()
        {
            try
            {
                UpdateMultiplier();

                foreach (var pos in DrugPoints)
                {
                    NAPI.Marker.CreateMarker(1, pos - new Vector3(0, 0, 1.12), new Vector3(), new Vector3(), 4, new Color(255, 0, 0), false, 0);
                    _priceDrugLabel = NAPI.TextLabel.CreateTextLabel($"~g~Купить наркоту ({_pricePerDrug}$/грамм)", pos + new Vector3(0, 0, 0.7), 5f, 0.3f, 0, new Color(255, 255, 255), true, 0);
                    NAPI.Blip.CreateBlip(140, pos, 1f, 4, "Закупка наркоты", 255, 0, true, 0, 0);

                    var col = NAPI.ColShape.CreateCylinderColShape(pos - new Vector3(0, 0, 1.12), 4, 5, 0);
                    col.OnEntityEnterColShape += (s, e) =>
                    {
                        try
                        {
                            e.SetData("INTERACTIONCHECK", 47);
                        }
                        catch (Exception ex) { Log.Write("OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                    };
                    col.OnEntityExitColShape += (s, e) =>
                    {
                        try
                        {
                            e.SetData("INTERACTIONCHECK", -1);
                        }
                        catch (Exception ex) { Log.Write("OnEntityExitColShape: " + ex.Message, nLog.Type.Error); }
                    };
                }

                foreach (var point in EnterPoints)
                {
                    NAPI.Marker.CreateMarker(1, point.Value - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(0, 255, 255), false, NAPI.GlobalDimension);

                    var col = NAPI.ColShape.CreateCylinderColShape(point.Value, 1.2f, 2, NAPI.GlobalDimension);
                    col.SetData("FRAC", point.Key);

                    col.OnEntityEnterColShape += (s, e) =>
                    {
                        if (!Main.Players.ContainsKey(e)) return;
                        e.SetData("FRACTIONCHECK", s.GetData<object>("FRAC"));
                        e.SetData("INTERACTIONCHECK", 64);
                    };
                    col.OnEntityExitColShape += (s, e) =>
                    {
                        if (!Main.Players.ContainsKey(e)) return;
                        e.SetData("INTERACTIONCHECK", -1);
                    };
                }

                foreach (var point in ExitPoints)
                {
                    NAPI.Marker.CreateMarker(1, point.Value - new Vector3(0, 0, 0.7), new Vector3(), new Vector3(), 1, new Color(0, 255, 255), false, NAPI.GlobalDimension);

                    var col = NAPI.ColShape.CreateCylinderColShape(point.Value, 1.2f, 2, NAPI.GlobalDimension);
                    col.SetData("FRAC", point.Key);

                    col.OnEntityEnterColShape += (s, e) =>
                    {
                        if (!Main.Players.ContainsKey(e)) return;
                        e.SetData("FRACTIONCHECK", s.GetData<object>("FRAC"));
                        e.SetData("INTERACTIONCHECK", 65);
                    };
                    col.OnEntityExitColShape += (s, e) =>
                    {
                        if (!Main.Players.ContainsKey(e)) return;
                        e.SetData("INTERACTIONCHECK", -1);
                    };
                }
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        public static void InteractPressed(Player player)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (!player.IsInVehicle || !player.Vehicle.HasData("CANDRUGS"))
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы должны находиться в машине, которая может перевозить наркотики", 3000);
                return;
            }
            if (Fractions.Manager.FractionTypes[Main.Players[player].FractionID] != 1)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете закупать наркотики", 3000);
                return;
            }
            if (!Fractions.Manager.canUseCommand(player, "buydrugs")) return;
            Trigger.ClientEvent(player, "openInput", "Закупить наркотики", $"Введите кол-во:", 4, "buy_drugs");
        }

        public static void BuyDrugs(Player player, int amount)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (!player.IsInVehicle || !player.Vehicle.HasData("CANDRUGS"))
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы должны находиться в машине, которая может перевозить наркотики", 3000);
                return;
            }
            if (Fractions.Manager.FractionTypes[Main.Players[player].FractionID] != 1)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вы не можете закупать наркотики", 3000);
                return;
            }
            if (!Fractions.Manager.canUseCommand(player, "buydrugs")) return;

            var tryAdd = VehicleInventory.TryAdd(player.Vehicle, new nItem(ItemType.Drugs, amount));
            if (tryAdd == -1 || tryAdd > 0)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Недостаточно места в машине", 3000);
                return;
            }
            if (Fractions.Stocks.fracStocks[Main.Players[player].FractionID].Money < amount * _pricePerDrug)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств на складе банды", 3000);
                return;
            }

            VehicleInventory.Add(player.Vehicle, new nItem(ItemType.Drugs, amount));
            Fractions.Stocks.fracStocks[Main.Players[player].FractionID].Money -= amount * _pricePerDrug;

            Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы закупили {amount}г наркотиков", 3000);
        }
    }
}

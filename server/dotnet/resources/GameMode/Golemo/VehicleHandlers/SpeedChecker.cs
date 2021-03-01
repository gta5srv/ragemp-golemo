﻿using GolemoSDK;
using GTANetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Golemo.VehicleHandlers
{
    class SpeedChecker : Script
    {
        private static nLog RLog = new nLog("SpeedChecker");
        private static GTANetworkAPI.ColShape _ShapePolicePay;
        private static GTANetworkAPI.Vector3 _paycords = new Vector3(440.95834, -976.21063, 29.569593);
        private static List<Vector3> CameraPosition = new List<Vector3>() {

            new Vector3(956.71265, 137.17805, 80.90612),
            new Vector3(256.74335, -589.912, 43.277744),
            new Vector3(223.62695, -1040.3018, 29.440355),
            new Vector3(-254.45065, -665.5185, 33.33108),
            new Vector3(-516.23126, -277.11868, 35.56219),
            new Vector3(-503.7054, -836.2421, 30.560585),

        };
        private static List<Vector3> PropPosition = new List<Vector3>() {

            new Vector3(245, -1029, 28.2f),
            new Vector3(964, 131, 79.96f),
            new Vector3(276, -595, 42.18f),
            new Vector3(-237, -651, 32.28f),
            new Vector3(-506, -288, 34.44f),
            new Vector3(-485, -823, 29.35f),

        };

        private static int maxspeed = 90;

        [ServerEvent(Event.ResourceStart)]
        public static void onResourceStart()
        {
            for (int i = 0; i < CameraPosition.Count; i++)
            {
                ColShape _ShapeCam = NAPI.ColShape.CreateCylinderColShape(CameraPosition[i], 30, 30, 0);
                NAPI.Blip.CreateBlip(629, CameraPosition[i], 0.4f, 4, Main.StringToU16("Камера"), 150, 10, true, 0, 0);

                _ShapeCam.OnEntityEnterColShape += (s, e) =>
                {
                    if (e.VehicleSeat == 0)
                    {
                        Trigger.ClientEvent(e, "sc:check", maxspeed);
                    }
                };
            }

            for (int i = 0; i < PropPosition.Count; i++)
            {
                NAPI.Object.CreateObject(NAPI.Util.GetHashKey("prop_cctv_pole_04"), PropPosition[i], new Vector3(0, 0, 0), 255, 0);
            }

            _ShapePolicePay = NAPI.ColShape.CreateCylinderColShape(_paycords, 1, 2, 0);
            NAPI.Marker.CreateMarker(1, _paycords, new Vector3(), new Vector3(), 1f, new Color(255, 0, 0), false, 0);
            NAPI.TextLabel.CreateTextLabel(Main.StringToU16("~b~Оплата штрафов"), _paycords + new Vector3(0, 0, 0.7), 5f, 0.3f, 4, new Color(0, 0, 255), true, 0);
            _ShapePolicePay.OnEntityEnterColShape += (s, e) =>
            {
                if (Main.Players[e].Fines > 0)
                {
                    NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 908);
                }
                else
                {
                    Notify.Info(e, $"У вас нет штрафов!", 2500);
                }
            };
            _ShapePolicePay.OnEntityExitColShape += (s, ent) =>
            {
                NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 0);
            };
            RLog.Write("SpeedChecker Loaded", nLog.Type.Info);

        }
        [RemoteEvent("sc:check")]
        public static void SpeedCheck(Player player, float playerspeed, int maxspeed)
        {
            if (playerspeed > maxspeed)
            {
                if ((Main.Players[player].FractionID != 6 || Main.Players[player].FractionID != 7 || Main.Players[player].FractionID != 8 || Main.Players[player].FractionID != 9) && !NAPI.Data.GetEntityData(player, "ON_DUTY"))
                {
                    int amount = 500;
                    int difference = Convert.ToInt32(Math.Abs(maxspeed - playerspeed));
                    amount += difference * 10;
                    Main.Players[player].Fines += amount;
                    Notify.Info(player, $"Вы получили штраф за превышение допустимой скорости:{maxspeed}, в размере {amount}$.", 2500);
                    if (Main.Players[player].Fines >= 50000)
                    {
                        if (Main.Players[player].Licenses[1] == true)
                        {
                            Main.Players[player].Licenses[1] = false;
                            Notify.Error(player, "У вас отобрали лицензию за неоплаченные штрафы.", 2500);
                        }
                    }
                }
            }
        }

    }
}

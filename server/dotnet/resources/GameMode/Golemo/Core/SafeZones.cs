using System;
using System.Collections.Generic;
using System.Text;
using GTANetworkAPI;
using GolemoSDK;

namespace Golemo.Core
{
    class SafeZones : Script
    {
        private static nLog Log = new nLog("SafeZones");
        public static void CreateSafeZone(Vector3 position, int height, int width)
        {
            var colShape = NAPI.ColShape.Create2DColShape(position.X, position.Y, height, width, 0);
            colShape.OnEntityEnterColShape += (shape, player) =>
            {
                try
                {
                    Trigger.ClientEvent(player, "safeZone", true);
                    player.SendNotification("~g~Вы вошли в зеленую зону", false);
                }
                catch (Exception e) { Log.Write($"SafeZoneEnter: {e.Message}", nLog.Type.Error); }

            };
            colShape.OnEntityExitColShape += (shape, player) =>
            {
                try
                {
                    Trigger.ClientEvent(player, "safeZone", false);
                    player.SendNotification("~r~Вы покинули зеленую зону", false);
                }
                catch (Exception e) { Log.Write($"SafeZoneExit: {e.Message}", nLog.Type.Error); }
            };
        }

        [ServerEvent(Event.ResourceStart)]
        public void Event_onResourceStart()
        {
            CreateSafeZone(new Vector3(301.5305, -596.4332, 42.16399), 100, 100); //ems
            CreateSafeZone(new Vector3(-561.2012, -194.0378, 37.10239), 150, 150); //major
            CreateSafeZone(new Vector3(113.6499, -752.8422, 44.63474), 85, 85); //fib
            CreateSafeZone(new Vector3(-371.19, -236.16, 35.90), 50, 50); // spawn
            CreateSafeZone(new Vector3(-570.4495, -394.9133, 33.9366), 50, 50);
            CreateSafeZone(new Vector3(1216.343, -2989.767, 4.769878), 200, 200);//container
        }

        [ServerEvent(Event.PlayerEnterColshape)]
        public static void SetEnterInteractionCheck(ColShape shape, Player player)
        {
            if (!Main.Players.ContainsKey(player)) return;
            if (player.HasData("INTERACTIONCHECK") && player.GetData<int>("INTERACTIONCHECK") <= 0) return;
            if (player.HasData("CUFFED") && player.GetData<bool>("CUFFED")) return;
            if (player.HasData("IS_DYING") || player.HasData("FOLLOWING")) return;

            if (player.HasData("GARAGEID"))
            {
                Houses.House house = Houses.HouseManager.GetHouse(player);
                if (house == null) return;
                if (player.GetData<int>("GARAGEID") != house.GarageID) return;
            }
            Trigger.ClientEvent(player, "playerInteractionCheck", true);
        }

        [ServerEvent(Event.PlayerExitColshape)]
        public static void SetExitInteractionCheck(ColShape shape, Player player)
        {
            if (!Main.Players.ContainsKey(player)) return;
            Trigger.ClientEvent(player, "playerInteractionCheck", false);
        }
    }
}

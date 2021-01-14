using GTANetworkAPI;
using System;
using System.Linq;
using GolemoSDK;

namespace Golemo.Core
{
    class EatManager : Script
    {

        private static nLog Log = new nLog("EatManager");

        [ServerEvent(Event.ResourceStart)]

        public void onResourceStart()
        {
            try
            {
                Timers.StartTask("checkwater", 360000, () => CheckWater()); //320000
                Timers.StartTask("checkeat", 340000, () => CheckEat());
                Log.Write("Timers started.", nLog.Type.Success);
            }
            catch (Exception e)
            {
                Log.Write($"Error started timers. \n Exeption: {e}", nLog.Type.Error);
            }
        }

        [ServerEvent(Event.PlayerDeath)]
        public void OnPlayerDeath(Player player, Player killer, uint reason)
        {
            SetEat(player, 30);
            SetWater(player, 30);
        }

        public static void SetEat(Player player, int change)
        {
            try
            {
                Main.Players[player].Eat = change;
                Trigger.ClientEvent(player, "UpdateEat", Main.Players[player].Eat, Convert.ToString(change));
                GUI.Dashboard.sendStats(player);
            }
            catch { Log.Write("ERROR SET EAT", nLog.Type.Error); }
        }
        public static void AddEat(Player player, int change)
        {
            if (Main.Players[player].Eat + change > 100)
            {
                Main.Players[player].Eat = 100;
            }
            else if (Main.Players[player].Eat + change < 0)
            {
                Main.Players[player].Eat = 0;
            }
            else
            {
                Main.Players[player].Eat += change;
            }
            Trigger.ClientEvent(player, "UpdateEat", Main.Players[player].Eat, Convert.ToString(change));
            GUI.Dashboard.sendStats(player);
        }
        public static void SetWater(Player player, int change)
        {
            try
            {
                Main.Players[player].Water = change;
                Trigger.ClientEvent(player, "UpdateWater", Main.Players[player].Water, Convert.ToString(change));
                GUI.Dashboard.sendStats(player);
            }
            catch { Log.Write("ERROR SET WATER", nLog.Type.Error); }
        }
        public static void AddWater(Player player, int change)
        {
            if (Main.Players[player].Water + change > 100)
            {
                Main.Players[player].Water = 100;
            }
            else if (Main.Players[player].Water + change < 0)
            {
                Main.Players[player].Water = 0;
            }
            else
            {
                Main.Players[player].Water += change;
            }
            Trigger.ClientEvent(player, "UpdateWater", Main.Players[player].Water, Convert.ToString(change));
            GUI.Dashboard.sendStats(player);
        }

        public static void CheckEat()
        {

            Log.Write("Check Eat.", nLog.Type.Info);
            foreach (Player player in Main.Players.Keys.ToList())
            {
                try
                {
                    if (player.Health > 0)
                    {
                        if (Main.Players[player].Eat > 0 && Main.Players[player].Water > 0)
                        {
                            if (player.IsInVehicle)
                            {
                                AddEat(player, -1);
                            }
                            else
                            {
                                AddEat(player, -2);
                            }
                        }
                        else
                        {
                            if (Main.Players[player].Eat == 0 && Main.Players[player].Water == 0)
                            {
                                player.Health -= 4;
                            }
                            else
                            {
                                if (Main.Players[player].Water == 0)
                                {
                                    AddEat(player, -4);
                                }
                                if (Main.Players[player].Eat == 0)
                                {
                                    player.Health -= 2;
                                }
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
        }
        public static void CheckWater()
        {
            Log.Write("Check Water.", nLog.Type.Info);
            foreach (Player player in Main.Players.Keys.ToList())
            {
                try
                {
                    if (player.Health > 0)
                    {
                        if (Main.Players[player].Eat > 0 && Main.Players[player].Water > 0)
                        {
                            if (player.IsInVehicle)
                            {
                                AddWater(player, -1);
                            }
                            else
                            {
                                AddWater(player, -2);
                            }
                        }
                        else
                        {
                            if (Main.Players[player].Water != 0 && Main.Players[player].Eat == 0)
                            {
                                AddWater(player, -4);
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
        }

    }
}

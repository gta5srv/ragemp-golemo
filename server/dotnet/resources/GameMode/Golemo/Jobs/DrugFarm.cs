using GTANetworkAPI;
using System.Collections.Generic;
using System;
using Golemo.GUI;
using Golemo.Core;
using GolemoSDK;

namespace Golemo.Jobs
{
    class DrugFarm : Script
    {
        private static nLog Log = new nLog("DrugFarm");
        private static Dictionary<int, ColShape> Cols = new Dictionary<int, ColShape>();
        public static int drugsmultiplier;
        private static Random rnd = new Random();
        private static int _minMultiplier = 15; // минимальный коеффициент
        private static int _maxMultiplier = 51; // максимальный коеффициеннт

        private void cf_onEntityEnterColShape(ColShape shape, Player entity)
        {
            try
            {
                NAPI.Data.SetEntityData(entity, "INTERACTIONCHECK", shape.GetData<int>("INTERACT"));
                UpdateLabel();
            }
            catch (Exception ex) { Log.Write("gp_onEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
        }
        private void cf_onEntityExitColShape(ColShape shape, Player entity)
        {
            try
            {
                NAPI.Data.SetEntityData(entity, "INTERACTIONCHECK", 0);
            }
            catch (Exception ex) { Log.Write("gp_onEntityExitColShape: " + ex.Message, nLog.Type.Error); }
        }
        public TextLabel label = null;
        [ServerEvent(Event.ResourceStart)]
        public void onResourceStart()
        {
            try
            {
                NAPI.Blip.CreateBlip(469, new Vector3(2931.998, 4624.349, 47.60344), 1.5f, 2, Main.StringToU16("Сборщик травы"), 255, 0, true, 0, 0);
               
                Cols.Add(1, NAPI.ColShape.CreateCylinderColShape(new Vector3(2931.998, 4624.349, 47.60344) , 10, 2, 0)); // get clothes
                Cols[1].OnEntityEnterColShape += cf_onEntityEnterColShape;
                Cols[1].OnEntityExitColShape += cf_onEntityExitColShape;
                Cols[1].SetData("INTERACT", 381);
                NAPI.TextLabel.CreateTextLabel(Main.StringToU16("~w~Приступить собирать"), new Vector3(2931.998, 4624.349, 49) + new Vector3(0, 0, 1), 10F, 0.6F, 0, new Color(0, 180, 0));
                label = NAPI.TextLabel.CreateTextLabel(Main.StringToU16($"~w~Курс еще не известен."), new Vector3(2931.998, 4624.349, 48.6) + new Vector3(0, 0, 1), 10F, 0.6F, 0, new Color(0, 180, 0));
                NAPI.TextLabel.CreateTextLabel(Main.StringToU16("~r~Наркодиллер"), new Vector3(2931.998, 4624.349, 49.2) + new Vector3(0, 0, 1), 10F, 0.6F, 0, new Color(0, 180, 0));
                UpdateMultiplier();
                UpdateLabel();


                Cols.Add(2, NAPI.ColShape.CreateCylinderColShape(new Vector3(-1897.205, 2079.9526, 139.87746), 10, 2, 0)); // drug seller
                Cols[2].OnEntityEnterColShape += cf_onEntityEnterColShape;
                Cols[2].OnEntityExitColShape += cf_onEntityExitColShape;
                Cols[2].SetData("INTERACT", 382);
                int i = 0;
                foreach (var Check in Checkpoints)
                {
                    var col = NAPI.ColShape.CreateCylinderColShape(Check.Position, 1, 2, 0);
                    col.SetData("NUMBER", i);
                    col.OnEntityEnterColShape += PlayerEnterCheckpoint;
                    i++;
                }

            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        public void UpdateLabel()
        {
            string text = $"~w~Курс {60 * drugsmultiplier} за 1 травку"; // если надо, тут меняем цену в TextLable
            label.Text = Main.StringToU16(text);

        }
        public static void UpdateMultiplier()
        {
            drugsmultiplier = rnd.Next(_minMultiplier, _maxMultiplier);
            Log.Write($"Обновлен коэффициент на: {drugsmultiplier}");
            
        }
        public static void StartWorkDay(Player player)
        {

            if (player.GetData<bool>("ON_WORK"))
            {
                Customization.ApplyCharacter(player);
                player.SetData("ON_WORK", false);

                Trigger.ClientEvent(player, "deleteCheckpoint", 15);
                Trigger.ClientEvent(player, "deleteWorkBlip");
                int UUID = Main.Players[player].UUID;
                var drugs = nInventory.Items[UUID].Find(t => t.Type == ItemType.Drugs);
                if (drugs != null)
                {
                    if (Main.Players[player].FractionID != 0)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Вам нельзя сдавать травку", 3000);
                        return;
                    }
                    nInventory.Remove(player, drugs.Type, drugs.Count);
                    GUI.Dashboard.sendItems(player);
                    int payment = (int)(drugs.Count * 60 * drugsmultiplier); // количество * fix-price * коеффициент
                    MoneySystem.Wallet.Change(player, payment);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы продали {drugs.Count} травы  за {payment}$", 3000);
                    

                }

                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, $"Вы закончили рабочий день", 3000);
                return;
            }
            else
            {
                Customization.ClearClothes(player, Main.Players[player].Gender);
                if (Main.Players[player].Gender)
                {
                    player.SetClothes(3, 85, 0);
                    player.SetClothes(11, 42, 0);
                    player.SetClothes(4, 41, 0);
                    player.SetClothes(6, 37, 0);
                }
                else
                {
                    player.SetClothes(3, 17, 0);
                    player.SetClothes(11, 150, 1);
                    player.SetClothes(4, 1, 5);
                    player.SetClothes(6, 52, 0);
                }

                var check = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                player.SetData("WORKCHECK", check);
                Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoints[check].Position, 1, 0, 255, 0, 0);
                Trigger.ClientEvent(player, "createWorkBlip", Checkpoints[check].Position);

                player.SetData("ON_WORK", true);
                Notify.Send(player, NotifyType.Info, NotifyPosition.BottomCenter, "Вы начали рабочий день", 3000);
                player.SendChatMessage("~r~Наркодиллер:Пс.... чел, собирай быстрее пока мусора не накрыли");

                return;
            }
        }
        public static void interactPressed(Player client, int id)
        {
            switch (id)
            {
                case 381:
                    try
                    {
                        if (!Main.Players.ContainsKey(client)) return;
                        StartWorkDay(client);
                    }
                    catch (Exception e) { Log.Write("PlayerExitVehicle: " + e.Message, nLog.Type.Error); }
                    return;
                case 382:
                    try
                    {

                        if (!Main.Players.ContainsKey(client)) return;
                        int UUID = Main.Players[client].UUID;
                        var drugs= nInventory.Items[UUID].Find(t => t.Type == ItemType.Drugs);                      
                        if (drugs == null)
                        {
                            Notify.Send(client, NotifyType.Error, NotifyPosition.BottomCenter, "У вас нет травы", 3000);
                            return;
                        }
                        if (Main.Players[client].FractionID != 0)
                        {
                            Notify.Send(client, NotifyType.Error, NotifyPosition.BottomCenter, "Вам нельзя сдавать травку", 3000);
                            return;
                        }
                        nInventory.Remove(client, drugs.Type, drugs.Count);
                        GUI.Dashboard.sendItems(client);
                        int payment = (int)(drugs.Count * 60 * drugsmultiplier); // количество * fix-price * коеффициент
                        MoneySystem.Wallet.Change(client, payment);
                        Notify.Send(client, NotifyType.Success, NotifyPosition.BottomCenter, $"Вы продали {drugs.Count} травы  за {payment}$", 3000);

                    }
                    catch (Exception e) { Log.Write("PlayerExitVehicle: " + e.Message, nLog.Type.Error); }
                    return;
            }

        }
        private static void PlayerEnterCheckpoint(ColShape shape, Player player)
        {
            try
            {
                if (!Main.Players.ContainsKey(player)) return;
                if (!player.GetData<bool>("ON_WORK") || shape.GetData<int>("NUMBER") != player.GetData<int>("WORKCHECK")) return;

                if (Checkpoints[(int)shape.GetData<int>("NUMBER")].Position.DistanceTo(player.Position) > 3) return;



                NAPI.Entity.SetEntityPosition(player, Checkpoints[shape.GetData<int>("NUMBER")].Position + new Vector3(0, 0, 1.2));
                NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, Checkpoints[shape.GetData<int>("NUMBER")].Heading));
                Main.OnAntiAnim(player);
                player.PlayAnimation("anim@mp_snowball", "pickup_snowball", 39);
                player.SetData("WORKCHECK", -1);
                NAPI.Task.Run(() => {
                    try
                    { 
                        if (player != null && Main.Players.ContainsKey(player))
                        {
                            player.StopAnimation();
                            Main.OffAntiAnim(player);
                            var tryAdd = nInventory.TryAdd(player, new nItem(ItemType.Drugs, 1));
                            if (tryAdd == -1 || tryAdd > 0) Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно места в инвентаре", 3000);                       
                            else nInventory.Add(player, new nItem(ItemType.Drugs, 1, ""));
                            var nextCheck = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                            while (nextCheck == shape.GetData<int>("NUMBER")) nextCheck = WorkManager.rnd.Next(0, Checkpoints.Count - 1);
                            player.SetData("WORKCHECK", nextCheck);
                            Trigger.ClientEvent(player, "createCheckpoint", 15, 1, Checkpoints[nextCheck].Position, 1, 0, 255, 0, 0);
                            Trigger.ClientEvent(player, "createWorkBlip", Checkpoints[nextCheck].Position);
                        }
                    }
                    catch { }
                }, 4000);

            }
            catch (Exception e) { Log.Write("PlayerEnterCheckpoint: " + e.Message, nLog.Type.Error); }
        }

        private static List<Checkpoint> Checkpoints = new List<Checkpoint>()
        {
            new Checkpoint(new Vector3(2906.992, 4672.376, 48.08558), 338.0567),
            new Checkpoint(new Vector3(2928.021, 4669.851, 48.02123), 338.1701),
            new Checkpoint(new Vector3(2911.434, 4679.981, 48.24757), 338.3819),
            new Checkpoint(new Vector3(2943.281, 4675.077, 48.2809), 338.4535),
            new Checkpoint(new Vector3(2917.888, 4660.626, 48.08864), 338.01),
            new Checkpoint(new Vector3(2904.49, 4654.597, 48.06182), 346.9411),
            new Checkpoint(new Vector3(2943.357, 4694.266, 50.18837), 345.3615),
            new Checkpoint(new Vector3(2918.11, 4686.844, 48.76686), 345.2057),
            new Checkpoint(new Vector3(2898.486, 4683.106, 47.92973), 345.2057),
            new Checkpoint(new Vector3(2932.073, 4678.818, 48.89362), 345.2057),
        };
        internal class Checkpoint
        {
            public Vector3 Position { get; }
            public double Heading { get; }

            public Checkpoint(Vector3 pos, double rot)
            {
                Position = pos;
                Heading = rot;
            }
        }
    }
}

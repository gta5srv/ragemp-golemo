using System;
using System.Collections.Generic;
using System.Text;
using GTANetworkAPI;
using GolemoSDK;
using System.Data;
using Newtonsoft.Json;
using System.Linq;

namespace Golemo.Core
{
    class ContainerSystem : Script
    {
        private static nLog Log = new nLog("ContainerSystem");

        public static List<Container> containers = new List<Container>();

        [ServerEvent(Event.ResourceStart)]
        public static void OnResourceStart()
        {
            try
            {
                Blip blip = NAPI.Blip.CreateBlip(50, new Vector3(1216.3, -2980, 0), 0.7f, 6, Main.StringToU16("Аукцион контейнеров"), 255, 0, true, 0, 0);

                var result = MySQL.QueryRead($"SELECT * FROM `containers`");
                if (result == null || result.Rows.Count == 0)
                {
                    Log.Write("DB containers return null result.", nLog.Type.Warn);
                    return;
                }
                foreach (DataRow Row in result.Rows)
                {
                    Vector3 pos = JsonConvert.DeserializeObject<Vector3>(Row["position"].ToString());
                    Vector3 rot = JsonConvert.DeserializeObject<Vector3>(Row["rotation"].ToString());

                    Container data = new Container(Convert.ToInt32(Row["id"]), Convert.ToString(Row["name"]), pos, rot, Convert.ToInt32(Row["price"]), Convert.ToBoolean(Row["donate"]));
                    Dictionary<string, int> items = JsonConvert.DeserializeObject<Dictionary<string, int>>(Row["loot"].ToString());

                    foreach (var item in items)
                    {
                        for (int i = 0; i < item.Value; i++)
                        {
                            data.Loots.Add(item.Key);
                        }
                    }
                    containers.Add(data);
                }

                Log.Write($"Загружено {containers.Count} контейнера");
            }
            catch (Exception e)
            {
                Log.Write(e.Message, nLog.Type.Error);
            }
        }

        [Command("statebox")]
        public static void ChangeStateContainers(Player player = null, bool state = true)
        {
            if (player != null)
            {
                if (!Core.Group.CanUseCmd(player, "statebox")) return;
            }
            foreach (var item in containers)
            {
                item.Visible(state);
            }
            if (state)
                NAPI.Chat.SendChatMessageToAll("!{#fc4626} [Порт]: !{#ffffff}" + "В штат привезли новую партию контейнеров!");
        }

        [Command("openallcontainers")] //для админа, чтобы проверить одновременное открытие всех контейнеров
        public static void OpenAllContainers(Player player)
        {
            if (!Core.Group.CanUseCmd(player, "openallcontainers")) return;
            foreach (var item in containers)
            {
                item.OpenDoor();
                item.GenerateLoot(player);
                item.Visible(false);
            }
        }

        public static void OpenMenuContainer(Player player)
        {
            if (!player.HasData("ContainerID")) return;
            Container container = containers[player.GetData<int>("ContainerID")];
            if (!container.State) return;
            Trigger.ClientEvent(player, "openContainerMenu", container);
        }

        [RemoteEvent("openContainer")]
        public static void OpenContainer(Player player)
        {
            try
            {
                if (!player.HasData("ContainerID")) return;
                Container container = containers[player.GetData<int>("ContainerID")];
                if (!container.State) return;
                if (container.DoorState) return;
                if (container.Donate)
                {
                    if (Main.Accounts[player].RedBucks < container.Price)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств", 2500);
                        return;
                    }
                    MoneySystem.Wallet.ChangeDonateBalance(player, -container.Price);
                }
                else
                {
                    if (Main.Players[player].Money < container.Price)
                    {
                        Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств", 2500);
                        return;
                    }
                    MoneySystem.Wallet.Change(player, -container.Price);
                }
                container.OpenDoor();
                container.GenerateLoot(player);
                container.Visible(false);
            }
            catch (Exception e)
            {
                Log.Write(e.Message, nLog.Type.Error);
            }
        }
    }

    public class Container
    {
        public static Random rnd = new Random();
        public int ID { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public bool Donate { get; set; }
        public bool State { get; set; } = false;
        public bool DoorState { get; set; } = false;

        public List<string> Loots = new List<string>();

        public GTANetworkAPI.Object Model;
        public GTANetworkAPI.Object Door_l;
        public GTANetworkAPI.Object Door_R;
        public GTANetworkAPI.Object Fence;

        public GTANetworkAPI.ColShape shape;
        public GTANetworkAPI.Marker marker;
        public GTANetworkAPI.TextLabel label;
        public Container(int id, string name, Vector3 pos, Vector3 rot, int price, bool donate = false, string model = "prop_container_02a", string door_l = "prop_cntrdoor_ld_l", string door_r = "prop_cntrdoor_ld_r", string fence = "prop_fncsec_01b")
        {
            ID = id;
            Name = name;
            Price = price;
            Donate = donate;
            Model = NAPI.Object.CreateObject(NAPI.Util.GetHashKey(model), pos, rot, 255, 0);
            Door_l = NAPI.Object.CreateObject(NAPI.Util.GetHashKey(door_l), pos + new Vector3(1.3, 6.08, 1.4), rot, 255, 0);
            Door_R = NAPI.Object.CreateObject(NAPI.Util.GetHashKey(door_r), pos + new Vector3(-1.3, 6.08, 1.4), rot, 255, 0);
            Fence = NAPI.Object.CreateObject(NAPI.Util.GetHashKey(fence), pos + new Vector3(-1.25, 6.05, 0.5), rot, 0, 0);

            shape = NAPI.ColShape.CreateCylinderColShape(pos + new Vector3(-2, 6.7, 0), 1f, 2f, 0);
            marker = NAPI.Marker.CreateMarker(27, pos + new Vector3(-2, 6.7, 0.1), new Vector3(), new Vector3(), 1f, new Color(28, 90, 19, 0), false, 0);
            label = NAPI.TextLabel.CreateTextLabel("Неактивен", pos + new Vector3(-2, 6.7, 1), 10f, 0.2f, 0, new Color(255, 255, 255), true, 0);

            shape.OnEntityEnterColShape += (s, ent) =>
            {
                if (!State) return;
                NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 803);
                NAPI.Data.SetEntityData(ent, "ContainerID", ID);
            };
            shape.OnEntityExitColShape += (s, ent) => 
            {
                if (!State) return;
                NAPI.Data.SetEntityData(ent, "INTERACTIONCHECK", 0);
                NAPI.Data.ResetEntityData(ent, "ContainerID");
            };
        }

        public void Visible(bool state)
        {
            if(state)
            {
                label.Text = $"{Name} ID:~b~{ID} \nЦена: ~g~{Price}{(Donate ? "RB" : "$")}";
                marker.Color = new Color(28, 90, 19, 180);
            }
            else
            {
                label.Text = "Неактивен";
                marker.Color = new Color(28, 90, 19, 0);
            }
            State = state;
        }

        public void OpenDoor()
        {
            int i = 0;
            DoorState = true;
            NAPI.Task.Run(() => {
                Timers.Start($"openDoorContainer{ID}", 1, () =>
                {
                    ++i;
                    if (i >= 120)
                    {
                        Timers.Stop($"openDoorContainer{ID}");
                    }
                    NAPI.Task.Run(() => { Door_l.Rotation -= new Vector3(0, 0, 1); });
                    NAPI.Task.Run(() => { Door_R.Rotation -= new Vector3(0, 0, -1); });
                });
            });
        }
        public void CloseDoor(Vehicle veh)
        {
            int i = 0;
            NAPI.Task.Run(() => {
                Timers.Start($"closeDoorContainer{ID}", 1, () =>
                {
                    ++i;
                    if (i >= 120)
                    {
                        DoorState = false;
                        NAPI.Task.Run(() => { veh.Delete(); });
                        Timers.Stop($"closeDoorContainer{ID}");
                    }
                    NAPI.Task.Run(() => { Door_l.Rotation += new Vector3(0, 0, 1); });
                    NAPI.Task.Run(() => { Door_R.Rotation += new Vector3(0, 0, -1); });
                });
            });
        }

        public void Moveloots()
        {
            for (int i = 0; i < Loots.Count; i++)
            {
                Random rnd = new Random();
                int index = rnd.Next(0, Loots.Count);
                string elem = Loots[index];
                Loots.RemoveAt(index);
                Loots.Add(elem);
            }
        }

        public void GenerateLoot(Player player)
        {
            Moveloots();
            string vName = Loots[rnd.Next(0, Loots.Count)];
            Vehicle veh = NAPI.Vehicle.CreateVehicle((VehicleHash)NAPI.Util.GetHashKey(vName), Model.Position, Model.Rotation.Z + 180, 0, 0);
            veh.Dimension = 0;
            veh.NumberPlate = "AUCTION";
            veh.PrimaryColor = 0;
            veh.SecondaryColor = 0;
            veh.Health = 1000;
            veh.Locked = true;
            VehicleStreaming.SetEngineState(veh, false);

            string vNumber = "none";
            vNumber = VehicleManager.Create(player.Name, vName, new Color(0, 0, 0), new Color(0, 0, 0), new Color(0, 0, 0), 1000, 100);
            var house = Houses.HouseManager.GetHouse(player, true);
            if (house == null || house.GarageID == 0)
                Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Ваш приз - {vName}", 2500);
            else
            {
                var garage = Houses.GarageManager.Garages[house.GarageID];
                if (vNumber != "none")
                {
                    garage.SpawnCar(vNumber);
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Ваш приз - {vName} будет доставлен в гараж", 2500);
                }
            }
            NAPI.Task.Run(() => { CloseDoor(veh); }, 10000);
        }
    }
}

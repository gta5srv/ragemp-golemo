using System;
using GTANetworkAPI;
using GolemoSDK;

namespace Golemo.Casino
{
    class CasinoManager : Script
    {
        private static nLog Log = new nLog("Casino");
        private static int _priceForAdmission = 5000;
        private static Vector3 _entrancePosition = new Vector3(935.7294, 46.61844, 81.2);
        private static Vector3 _exitPosition = new Vector3(1089.695, 206.015, -49);
        [ServerEvent(Event.ResourceStart)]
        public void onResourceStart()
        {
            try
            {
                NAPI.TextLabel.CreateTextLabel($"~b~Вход в Казино", _entrancePosition, 5f, 0.3f, 4, new Color(255, 255, 255), true, 0);
                NAPI.TextLabel.CreateTextLabel($"~r~Выход", _exitPosition, 5f, 0.3f, 4, new Color(255, 255, 255), true, 0);

                var colShapeEnter = NAPI.ColShape.CreateCylinderColShape(_entrancePosition, 1f, 2, 0);
                var colShapeExit = NAPI.ColShape.CreateCylinderColShape(_exitPosition, 1f, 2, 0);

                NAPI.Marker.CreateMarker(1, _entrancePosition - new Vector3(0, 0, 1.5), new Vector3(), new Vector3(), 1, new Color(0, 255, 255), false, 0);
                NAPI.Marker.CreateMarker(1, _exitPosition - new Vector3(0, 0, 1.5), new Vector3(), new Vector3(), 1, new Color(0, 255, 255), false, 0);

                NAPI.Blip.CreateBlip(681, _entrancePosition, 1, 67, "Diamond Casino", 255, 0, true);

                colShapeEnter.OnEntityEnterColShape += (s, e) =>
                {
                    try
                    {
                        if (!e.IsInVehicle)
                        {
                            NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 805);
                            NAPI.Data.SetEntityData(e, "CASINO_MAIN_SHAPE", "ENTER");
                        }
                    }
                    catch (Exception ex) { Log.Write("EnterCasino_OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                colShapeEnter.OnEntityExitColShape += OnEntityExitCasinoMainShape;

                colShapeExit.OnEntityEnterColShape += (s, e) =>
                {
                    try
                    {
                        if (!e.IsInVehicle)
                        {
                            NAPI.Data.SetEntityData(e, "INTERACTIONCHECK", 805);
                            NAPI.Data.SetEntityData(e, "CASINO_MAIN_SHAPE", "EXIT");
                        }
                    }
                    catch (Exception ex) { Log.Write("ExitCasino_OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                };
                colShapeExit.OnEntityExitColShape += OnEntityExitCasinoMainShape;
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }
        public static void OnEntityExitCasinoMainShape(ColShape shape, Player player)
        {
            NAPI.Data.SetEntityData(player, "INTERACTIONCHECK", 0);
            NAPI.Data.ResetEntityData(player, "CASINO_MAIN_SHAPE");
        }
        public static void CallBackShape(Player player)
        {
            if (!player.HasData("CASINO_MAIN_SHAPE")) return;
            string data = player.GetData<string>("CASINO_MAIN_SHAPE");
            if (data == "ENTER")
            {
                Trigger.ClientEvent(player, "openDialog", "ENTER_CASINO", $"Стоимость входа: {_priceForAdmission}$. Зайти?");
                return;
            }
            if(data == "EXIT")
            {
                NAPI.Entity.SetEntityPosition(player, _entrancePosition);
                NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, 113.5));
            }
        }
        public static void EnterCasino(Player player)
        {
            if (!MoneySystem.Wallet.Change(player, -_priceForAdmission))
            {
                Notify.Error(player, "У вас недостаточно средств");
                return;
            }
            NAPI.Entity.SetEntityPosition(player, _exitPosition);
            NAPI.Entity.SetEntityRotation(player, new Vector3(0, 0, -27.5));
        }
    }
}

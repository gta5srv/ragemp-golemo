using System;
using System.Collections.Generic;
using System.Text;
using GTANetworkAPI;
using GolemoSDK;

namespace Golemo.Buildings
{
    public static class Dummies
    {
        private static nLog Log = new nLog("Dummies");

        private static Dictionary<string, Tuple<int, int, Vector3, Vector3>> _vehicleDummies = new Dictionary<string, Tuple<int, int, Vector3, Vector3>>();
        private static void VehicleDummy(string vehicleName, Vector3 position, Vector3 rotation, int a = 0, int b = 0)
        {
            if(vehicleName != null && position != null && rotation != null)
            {
                _vehicleDummies.Add(vehicleName, new Tuple<int, int, Vector3, Vector3>(a, b, position, rotation));
            }
        }

        #region Init VehicleList
        public static void OnResourceStart()
        {                //model    //position                  //rotation                        color color
            VehicleDummy("buffalo", new Vector3(-47.9, -1116.4, 26.5), new Vector3(0.1, -0.006, 3), 25, 83);
            VehicleDummy("comet2", new Vector3(-50.8, -1116.1, 26.5), new Vector3(0.07, -0.01, 2.8), 83, 21);
            VehicleDummy("elegy2", new Vector3(-53.7, -1116.4, 26.5), new Vector3(0.08, -0.002, 2.9), 120, 120);

            foreach (var item in _vehicleDummies)
            {
                VehicleHash vh = (VehicleHash)NAPI.Util.GetHashKey(item.Key);
                var vehicle = NAPI.Vehicle.CreateVehicle(vh, item.Value.Item3, item.Value.Item4, item.Value.Item1, item.Value.Item2, "DUMMY", 255, true, false, 0);
                vehicle.SetSharedData("ACCESS", "DUMMY");
                Core.SafeZones.CreateSafeZone(vehicle.Position, 10, 10, false); //если вам не нужно Зеленая зона у машины, то удалите эту строку
            }

            Log.Write("Заспавнено " + _vehicleDummies.Count + " выставочного транспорта", nLog.Type.Info);
        }
        #endregion
    }
}

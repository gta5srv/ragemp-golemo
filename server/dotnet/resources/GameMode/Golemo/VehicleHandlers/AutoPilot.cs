using System;
using System.Collections.Generic;
using System.Text;
using GTANetworkAPI;
using GolemoSDK;

namespace Golemo.VehicleHandlers
{
    public class AutoPilot
    {
        private static nLog Log = new nLog("AutoPilot");
        public static List<string> accessVehicle = new List<string>()
        {
            "comet2",
            "buffalo",
            "elegy2"
        };

        public static bool HasAccessToAutopilot(VehicleHash hash)
        {
            try
            {
                bool access = false;
                foreach (var item in accessVehicle)
                {
                    if (NAPI.Util.VehicleNameToModel(item) == hash)
                    {
                        access = true;
                        break;
                    }
                }
                return access;
            }
            catch (Exception e)
            {
                Log.Write(e.Message, nLog.Type.Error);
                return false;
            }
        }
    }
}

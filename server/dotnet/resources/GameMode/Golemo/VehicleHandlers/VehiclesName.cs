using System.Collections.Generic;
using System.Linq;

namespace Golemo.VehicleHandlers
{
    public static class VehiclesName
    {
        //реальные названия авто
        public static Dictionary<string, string> ModelList = new Dictionary<string, string>()
        {

              {"deluxo", "Golemo-Mode model" }, // пример
              //modelname  //Realname
        };

        public static string GetRealVehicleName(string model)
        {
            if (ModelList.ContainsKey(model))
            {
                return ModelList[model];
            }
            else
            {
                return model;
            }
        }

        public static string GetVehicleModelName(string name)
        {
            if (ModelList.ContainsValue(name))
            {
                return ModelList.FirstOrDefault(x => x.Value == name).Key;
            }
            else
            {
                return name;
            }
        }
    }
}

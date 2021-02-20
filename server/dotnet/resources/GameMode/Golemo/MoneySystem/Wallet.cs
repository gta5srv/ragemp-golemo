using System;
using System.Collections.Generic;
using System.Text;
using GTANetworkAPI;
using Golemo.Core;
using GolemoSDK;

namespace Golemo.MoneySystem
{
    class Wallet : Script
    {
        private static nLog Log = new nLog("Wallet");

        public static bool Change(Player player, int Amount)
        {
            if (!Main.Players.ContainsKey(player)) return false;
            if (Main.Players[player] == null) return false;
            int temp = Convert.ToInt32(Main.Players[player].Money + Amount);
            if (temp < 0) return false;
            Main.Players[player].Money = temp;
            Trigger.ClientEvent(player, "UpdateMoney", temp, Convert.ToString(Amount));
            MySQL.Query($"UPDATE characters SET money={Main.Players[player].Money} WHERE uuid={Main.Players[player].UUID}");
            return true;
        }
        public static bool ChangeBank(Player player, int Amount)
        {
            if (!Main.Players.ContainsKey(player)) return false;
            if (Main.Players[player] == null) return false;
            int bankid = Main.Players[player].Bank;
            int temp = Convert.ToInt32(Bank.Accounts[bankid].Balance + Amount);
            if (temp < 0) return false;
            else
            {
                Bank.Accounts[bankid].Balance = temp;
                Trigger.ClientEvent(player, "UpdateBank", temp, Convert.ToString(Amount));
                MySQL.Query($"UPDATE money SET balance={temp} WHERE id={bankid}");
                return true;
            }
        }
        public static bool ChangeDonateBalance(Player player, int Amount)
        {
            if (!Main.Players.ContainsKey(player)) return false;
            if (Main.Players[player] == null) return false;
            int temp = Convert.ToInt32(Main.Accounts[player].RedBucks + Amount);
            if (temp < 0) return false;
            else
            {
                Main.Accounts[player].RedBucks = temp;
                MySQL.Query($"UPDATE `accounts` SET redbucks={temp} WHERE login='{Main.Accounts[player].Login}'");
                Trigger.ClientEvent(player, "redset", Main.Accounts[player].RedBucks);
                return true;
            }
        }
        public static bool ChangeLuckyWheelSpins(Player player, int Amount)
        {
            if (!Main.Players.ContainsKey(player)) return false;
            if (Main.Players[player] == null) return false;
            int temp = Convert.ToInt32(Main.Players[player].LuckyWheelSpins + Amount);
            if (temp < 0) return false;
            Main.Players[player].LuckyWheelSpins = temp;
            Trigger.ClientEvent(player, "UpdateLuckyWheelSpins", temp, Convert.ToString(Amount));
            MySQL.Query($"UPDATE characters SET `luckywheelspins`={Main.Players[player].LuckyWheelSpins} WHERE uuid={Main.Players[player].UUID}");
            return true;
        }
        public static void Set(Player player, long Amount)
        {
            var data = Main.Players[player];
            if (data == null) return;
            data.Money = Amount;
            Trigger.ClientEvent(player, "UpdateMoney", data.Money);
        }
    }
}

using GTANetworkAPI;
using System;
using System.Collections.Generic;
using Golemo.Core;
using GolemoSDK;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Golemo.GUI;
using System.Threading.Tasks;
using MySqlConnector;

namespace Golemo.MoneySystem
{
    class Bank : Script
    {
        private static nLog Log = new nLog("BankSystem");
        private static Random Rnd = new Random();

        public static Dictionary<int, Data> Accounts = new Dictionary<int, Data>();
        public static ICollection<int> BankAccKeys = Accounts.Keys;

        public enum BankNotifyType
        {
            PaySuccess,
            PayIn,
            PayOut,
            PayError,
            InputError,
        }
        public Bank()
        {
            Log.Write("Loading Bank Accounts...");
            var result = MySQL.QueryRead("SELECT * FROM `money`");
            if (result == null || result.Rows.Count == 0)
            {
                Log.Write("DB return null result.", nLog.Type.Warn);
                return;
            }
            foreach (DataRow Row in result.Rows)
            {
                Data data = new Data();
                data.ID = Convert.ToInt32(Row["id"]);
                data.Type = Convert.ToInt32(Row["type"]);
                data.Holder = Row["holder"].ToString();
                data.Balance = Convert.ToInt64(Row["balance"]);
                Accounts.Add(Convert.ToInt32(Row["id"]), data);
            }
        }

        #region Changing account balance
        public static bool Change(int accountID, long amount, bool notify = true)
        {
            lock (Accounts)
            {
                if (!Accounts.ContainsKey(accountID)) return false;
                if (Accounts[accountID].Balance + amount < 0) return false;
                Accounts[accountID].Balance += amount;
                if (Accounts[accountID].Type == 1 || amount >= 10000) MySQL.Query($"UPDATE `money` SET balance={Accounts[accountID].Balance} WHERE id={accountID}");
                if (Accounts[accountID].Type == 1 && NAPI.Player.GetPlayerFromName(Accounts[accountID].Holder) != null)
                {
                    NAPI.Task.Run(() =>
                    {
                        try
                        {
                            if (notify)
                            {
                                if (amount > 0)
                                    BankNotify(NAPI.Player.GetPlayerFromName(Accounts[accountID].Holder), BankNotifyType.PayIn, amount.ToString());
                                else
                                    BankNotify(NAPI.Player.GetPlayerFromName(Accounts[accountID].Holder), BankNotifyType.PayOut, amount.ToString());
                            }
                            NAPI.Player.GetPlayerFromName(Accounts[accountID].Holder).TriggerEvent("UpdateBank", Accounts[accountID].Balance);
                        }
                        catch { }
                    });
                }
                return true;
            }
        }
        #endregion Changing account balance
        #region Transfer money from 1-Acc to 2-Acc
        public static bool Transfer(int firstAccID, int lastAccID, long amount)
        {
            if (firstAccID == 0 || lastAccID == 0)
            {
                Log.Write($"Account ID error [{firstAccID}->{lastAccID}]", nLog.Type.Error);
                return false;
            }
            Data firstAcc = Accounts[firstAccID];
            if (!Accounts.ContainsKey(lastAccID))
            {
                if (firstAcc.Type == 1)
                    BankNotify(NAPI.Player.GetPlayerFromName(firstAcc.Holder), BankNotifyType.InputError, "Такого счета не существует!");
                Log.Write($"Transfer with error. Account does not exist! [{firstAccID.ToString()}->{lastAccID.ToString()}:{amount.ToString()}]", nLog.Type.Warn);
                return false;
            }
            if (!Change(firstAccID, -amount))
            {
                if (firstAcc.Type == 1)
                    BankNotify(NAPI.Player.GetPlayerFromName(firstAcc.Holder), BankNotifyType.PayError, "Недостаточно средств!");
                Log.Write($"Transfer with error. Insufficient funds! [{firstAccID.ToString()}->{lastAccID.ToString()}:{amount.ToString()}]", nLog.Type.Warn);
                return false;
            }
            Change(lastAccID, amount);
            GameLog.Money($"bank({firstAccID})", $"bank({lastAccID})", amount, "bankTransfer");
            return true;
        }
        #endregion Transfer money from 1-Acc to 2-Acc
        #region Save Acc
        public static void Save(int AccID)
        {
            if (!Accounts.ContainsKey(AccID)) return;
            Data acc = Accounts[AccID];
            MySQL.Query($"UPDATE `money` SET `balance`={acc.Balance}, `holder`='{acc.Holder}' WHERE id={AccID}");
        }
        #endregion Save Acc

        public static void BankNotify(Player player, BankNotifyType type, string info)
        {
            switch (type)
            {
                case BankNotifyType.InputError:
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Ошибка ввода", 3000);
                    return;
                case BankNotifyType.PayError:
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Ошибка списания средств", 3000);
                    return;
                case BankNotifyType.PayIn:
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Поступление средств ({info}$)", 3000);
                    return;
                case BankNotifyType.PayOut:
                    Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, $"Списание средств ({info}$)", 3000);
                    return;
            }
        }

        public static int Create(string holder, int type = 1, long balance = 0)
        {
            int id = GenerateUUID();
            Data data = new Data();
            data.ID = id;
            data.Type = type;
            data.Holder = holder;
            data.Balance = balance;
            Accounts.Add(id, data);
            MySQL.Query($"INSERT INTO `money`(`id`, `type`, `holder`, `balance`) VALUES ({id},{type},'{holder}',{balance})");
            Log.Write("Created new Bank Account! ID:" + id.ToString(), nLog.Type.Success);
            return id;
        }
        public static void Remove(int id, string holder)
        {
            if (!Accounts.ContainsKey(id)) return;
            Accounts.Remove(id);
            MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = "DELETE FROM `money` WHERE holder=@pn";
            cmd.Parameters.AddWithValue("@pn",holder);
            MySQL.Query(cmd);
            Log.Write("Bank account deleted! ID:" + id, nLog.Type.Warn);
        }
        public static void RemoveByID(int id)
        {
            if (!Accounts.ContainsKey(id)) return;
            Accounts.Remove(id);
            MySqlCommand cmd = new MySqlCommand();
            cmd.CommandText = "DELETE FROM `money` WHERE id=@pn";
            cmd.Parameters.AddWithValue("@pn",id);
            MySQL.Query(cmd);
            Log.Write("Bank account deleted! ID:" + id, nLog.Type.Warn);
        }
        public static bool isAccExist(int id)
        {
            return Accounts.ContainsKey(id);
        }

        public static Data Get(string holder)
        {
            return Accounts.FirstOrDefault(A => A.Value.Holder == holder).Value;
        }

        public static Data Get(int id)
        {
            return Accounts.FirstOrDefault(A => A.Value.ID == id).Value;
        }

        public static void Update(Player client)
        {
            NAPI.Task.Run(() =>
            {
                Trigger.ClientEvent(client, "UpdateBank", Get(client.Name).Balance);
            });
        }

        private static int GenerateUUID()
        {
            var result = 0;
            while (true)
            {
                result = Rnd.Next(000001, 999999);
                if (!BankAccKeys.Contains(result)) break;
            }
            return result;
        }

        public static void changeHolder(string oldName, string newName)
        {
            List<int> toChange = new List<int>();
            lock (Accounts)
            {
                foreach (KeyValuePair<int, Data> bank in Accounts)
                {
                    if (bank.Value.Holder != oldName) continue;
                    Log.Debug($"The bank was found! [{bank.Key}]");
                    toChange.Add(bank.Key);
                }
                foreach (int id in toChange)
                {
                    Accounts[id].Holder = newName;
                    Save(id);
                }
            }
        }

        internal class Data
        {
            public int ID { get; set; }
            public int Type { get; set; }
            public string Holder { get; set; }
            public long Balance { get; set; }
        }
    }

    class ATM : Script
    {
        private static nLog Log = new nLog("ATMs");

        public static Dictionary<int, ColShape> ATMCols = new Dictionary<int, ColShape>();

        #region ATMs List
        public static List<Vector3> ATMs = new List<Vector3>()
        {
            new Vector3(-301.657257,-829.5886,31.4197655),
            new Vector3(-303.2257,-829.3121,31.4197655),
            new Vector3(-204.0193,-861.0091,29.2713318),
            new Vector3(118.641563,-883.569458,30.13945),
            new Vector3(24.5933,-945.543,28.333046),
            new Vector3(5.686035,-919.9551,28.48088),
            new Vector3(296.175629,-896.2318,28.2901459),
            new Vector3(296.8775,-894.3196,28.2614784),
            new Vector3(147.473053,-1036.21753,28.3677826),
            new Vector3(145.839218,-1035.62537,28.3677826),
            new Vector3(112.476143,-819.808044,30.3395519),
            new Vector3(111.388557,-774.840149,30.4376583),
            new Vector3(114.547417,-775.9721,30.4173641),
            new Vector3(-256.6386,-715.889832,32.7883),
            new Vector3(-259.276733,-723.2652,32.7015457),
            new Vector3(-254.521851,-692.8869,32.5782547),
            new Vector3(-27.8903427,-724.108948,43.22287),
            new Vector3(-30.0995712,-723.2863,43.22287),
            new Vector3(228.03244,337.850128,104.501328),
            new Vector3(158.796539,234.745163,105.643265),
            new Vector3(527.777649,-160.660858,56.1367149),
            new Vector3(-57.1702881,-92.37918,56.7506866),
            new Vector3(89.813385,2.880325,67.35214),
            new Vector3(285.3485,142.975067,103.162323),
            new Vector3(357.128448,174.083618,102.059654),
            new Vector3(1137.81128,-468.862549,65.6986542),
            new Vector3(1167.06,-455.6541,65.8185654),
            new Vector3(1077.77856,-776.966431,57.2565155),
            new Vector3(289.529968,-1256.7876,28.4405746),
            new Vector3(289.267853,-1282.32043,28.65519),
            new Vector3(-165.584427,234.765869,93.92897),
            new Vector3(-165.584427,232.695465,93.92897),
            new Vector3(-1044.466,-2739.64136,8.12406),
            new Vector3(-1205.3783,-326.5286,36.85104),
            new Vector3(-1206.14172,-325.031647,36.85104),
            new Vector3(-846.6537,-341.509033,37.6685028),
            new Vector3(-847.204,-340.429077,37.6793),
            new Vector3(-720.6288,-415.524323,33.97996),
            new Vector3(-867.013,-187.992783,36.8821754),
            new Vector3(-867.974548,-186.341934,36.8821754),
            new Vector3(-1415.4801,-212.332443,45.49542),
            new Vector3(-1430.66333,-211.358673,45.47162),
            new Vector3(-1410.73572,-98.9278946,51.39701),
            new Vector3(-1410.183,-100.645393,51.3965225),
            new Vector3(-1282.09827,-210.559921,41.43031),
            new Vector3(-1286.70374,-213.782745,41.43031),
            new Vector3(-1289.742,-227.164978,41.43031),
            new Vector3(-1285.13647,-223.942154,41.43031),
            new Vector3(-712.935669,-818.4827,22.7406578),
            new Vector3(-710.082764,-818.4756,22.7363358),
            new Vector3(-617.803467,-708.8591,29.0432129),
            new Vector3(-617.803467,-706.8521,29.0432129),
            new Vector3(-614.5187,-705.5981,30.223999),
            new Vector3(-611.8581,-705.5981,30.223999),
            new Vector3(-660.676331,-854.488159,23.4566345),
            new Vector3(-537.8052,-854.935669,28.2754288),
            new Vector3(-594.614441,-1160.85193,21.3335114),
            new Vector3(-596.125061,-1160.85034,21.3336),
            new Vector3(-526.7791,-1223.37366,17.45272),
            new Vector3(-1569.8396,-547.0309,33.9321632),
            new Vector3(-1570.76526,-547.7035,33.9321632),
            new Vector3(-1305.70776,-706.6881,24.3144684),
            new Vector3(-1315.416,-834.431,15.9523277),
            new Vector3(-1314.466,-835.6913,15.9523277),
            new Vector3(-2071.92847,-317.2862,12.3180847),
            new Vector3(-821.893555,-1081.55457,10.1366386),
            new Vector3(-1110.22839,-1691.15381,3.378483),
            new Vector3(-2956.84814,487.215759,14.4780006),
            new Vector3(-2958.977,487.307129,14.4780006),
            new Vector3(-2974.58643,380.126923,14.0),
            new Vector3(-1091.88745,2709.05347,17.9194145),
            new Vector3(-2295.85254,357.934753,173.601425),
            new Vector3(-2295.06934,356.2556,173.601425),
            new Vector3(-2294.2998,354.6056,173.601425),
            new Vector3(-3144.88745,1127.811,19.8380356),
            new Vector3(-3043.83472,594.16394,6.732796),
            new Vector3(-3241.45459,997.9085,11.5483694),
            new Vector3(2563.99951,2584.553,37.06807),
            new Vector3(2558.32422,350.988,107.597466),
            new Vector3(156.188629,6643.2,30.59372),
            new Vector3(173.8246,6638.21729,30.59372),
            new Vector3(-282.7141,6226.43,30.4964752),
            new Vector3(-95.870285,6457.462,30.473938),
            new Vector3(-97.63721,6455.732,30.4679337),
            new Vector3(-132.66629,6366.87646,30.47258),
            new Vector3(-386.4596,6046.41064,30.4739914),
            new Vector3(1687.39514,4815.9,41.0064659),
            new Vector3(1700.69409,6426.762,31.6329651),
            new Vector3(1822.97144,3682.57715,33.2674522),
            new Vector3(1171.523,2703.1394,37.1476974),     //90
            new Vector3(1172.45728,2703.1394,37.1476974),   //91
            //new Vector3(238.267792,217.109177,105.406151),  //92 в мерии
            //new Vector3(238.697815,216.186981,105.406151),  //93 в мерии
            new Vector3(237.837753,218.031372,105.406151),  //94
            //new Vector3(237.40773,218.953583,105.406151),   //95 в мерии
            //new Vector3(236.9777,219.875778,105.406151),    //96 в мерии
            //new Vector3(264.868958,209.948639,105.406151),  //97 в мерии
            //new Vector3(265.216949,210.9048,105.406151),    //98 в мерии
            //new Vector3(265.564972,211.860977,105.406151),  //99 в мерии
            //new Vector3(265.913,212.817139,105.406151),     //100 в мерии
            //new Vector3(266.261017,213.773315,105.406151),  //101 в мерии
            new Vector3(380.655762,322.8424,102.566338),    //102
            new Vector3(1153.11108,-326.901855,68.20503),   //103
            new Vector3(33.19432,-1348.80579,28.49696),     //104
            new Vector3(130.579117,-1292.36877,28.2714214), //105
            new Vector3(130.15036,-1291.6261,28.2714214),   //106
            new Vector3(129.697525,-1290.8418,28.2714214),  //107
            new Vector3(-57.4022369,-1751.74707,28.4209366),//108
            new Vector3(-718.261353,-915.712769,18.21553),  //109
            new Vector3(-273.366547,-2024.20789,29.1696434),//110
            new Vector3(-262.360779,-2012.054,29.1696434),  //111
            new Vector3(-1391.34448,-589.862732,29.315836), //112
            new Vector3(-1827.68872,784.465,137.315216),    //113
            new Vector3(-3040.20459,593.296936,6.908859),   //114
            new Vector3(-3240.028,1008.54529,11.8306389),   //115
            new Vector3(2559.05225,389.474426,107.62291),   //116
            new Vector3(1703.31519,4934.05273,41.0635872),  //117
            new Vector3(1735.0105,6410.01,34.03717),        //118
            new Vector3(2683.592,3286.3,54.2410774),        //119
            new Vector3(1968.39233,3743.07837,31.343689),   //120
            new Vector3(540.220642,2671.683,41.15644)       //121
        };
        // Игнор список индексов листа ATMs, которым не нужно создавать блипы, или же отправлять на работе инкасстора
        public static readonly List<int> ignoreAtmIndex = new List<int>() { 91 };
        #endregion ATMs List

        [ServerEvent(Event.ResourceStart)]
        public void onResourceStart()
        {
            try
            {
                Log.Write("Loading ATMs...");
                for (int i = 0; i < ATMs.Count; i++)
                {
                    if(!ignoreAtmIndex.Contains(i)) NAPI.Blip.CreateBlip(500, ATMs[i], 0.35f, 27, "Банкомат", shortRange: true, dimension: 0);
                    ATMCols.Add(i, NAPI.ColShape.CreateCylinderColShape(ATMs[i], 1, 2, 0));
                    ATMCols[i].SetData("NUMBER", i);
                    ATMCols[i].OnEntityEnterColShape += (s, e) => {
                        try
                        {
                            e.SetData("INTERACTIONCHECK", 13);
                            Jobs.Collector.CollectorEnterATM(e, s);
                        }
                        catch (Exception ex) { Log.Write("ATMCols.OnEntityEnterColShape: " + ex.Message, nLog.Type.Error); }
                    };
                    ATMCols[i].OnEntityExitColShape += (s, e) => {
                        try
                        {
                            e.SetData("INTERACTIONCHECK", 0);
                        }
                        catch (Exception ex) { Log.Write("ATMCols.OnEntityExitrColShape: " + ex.Message, nLog.Type.Error); }
                    };
                }
            }
            catch (Exception e) { Log.Write("ResourceStart: " + e.Message, nLog.Type.Error); }
        }

        public static Vector3 GetNearestATM(Player player)
        {
            Vector3 nearesetATM = ATMs[0];
            foreach (var v in ATMs)
            {
                if (v == new Vector3(237.3785, 217.7914, 106.2868)) continue;
                if (player.Position.DistanceTo(v) < player.Position.DistanceTo(nearesetATM))
                    nearesetATM = v;
            }
            return nearesetATM;
        }

        public static void OpenATM(Player player)
        {
            var acc = Main.Players[player];
            if (acc.Bank == 0)
            {
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Зарегистрируйте счет в ближайшем отделении банка", 3000);
                return;
            }
            long balance = Bank.Accounts[acc.Bank].Balance;
            Trigger.ClientEvent(player, "setatm", acc.Bank.ToString(), player.Name, balance.ToString(), "");
            Trigger.ClientEvent(player, "openatm");
            return;
        }

        public static void AtmBizGen(Player player)
        {
            var acc = Main.Players[player];
            Log.Debug("Biz count : " + acc.BizIDs.Count);
            if (acc.BizIDs.Count > 0)
            {
                List<string> data = new List<string>();
                foreach (int key in acc.BizIDs)
                {
                    Business biz = BusinessManager.BizList[key];
                    string name = BusinessManager.BusinessTypeNames[biz.Type];
                    data.Add($"{name}");
                }
                Trigger.ClientEvent(player, "atmOpenBiz", JsonConvert.SerializeObject(data), "");
            }
            else
            {
                Trigger.ClientEvent(player, "atmOpen", "[1,0,0]");
                Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "У вас нет бизнеса!", 3000);
            }
        }

        [RemoteEvent("atmVal")]
        public static void ClientEvent_ATMVAL(Player player, params object[] args)
        {
            try
            {
                if (Admin.IsServerStoping)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Сервер сейчас не может принять это действие", 3000);
                    return;
                }
                var acc = Main.Players[player];
                int type = NAPI.Data.GetEntityData(player, "ATMTYPE");
                string data = Convert.ToString(args[0]);
                int amount;
                if (!Int32.TryParse(data, out amount))
                    return;
                switch (type)
                {
                    case 0:
                        Trigger.ClientEvent(player, "atmClose");
                        if (Wallet.Change(player, -Math.Abs(amount)))
                        {
                            Bank.Change(acc.Bank, +Math.Abs(amount));
                            GameLog.Money($"player({Main.Players[player].UUID})", $"bank({acc.Bank})", Math.Abs(amount), $"atmIn");
                            Trigger.ClientEvent(player, "setbank", Bank.Accounts[acc.Bank].Balance.ToString(), "");
                        }
                        break;
                    case 1:
                        if (Bank.Change(acc.Bank, -Math.Abs(amount)))
                        {
                            Wallet.Change(player, +Math.Abs(amount));
                            GameLog.Money($"bank({acc.Bank})", $"player({Main.Players[player].UUID})", Math.Abs(amount), $"atmOut");
                            Trigger.ClientEvent(player, "setbank", Bank.Accounts[acc.Bank].Balance.ToString(), "");
                        }
                        break;
                    case 2:
                        var house = Houses.HouseManager.GetHouse(player, true);
                        if (house == null) return;

                        var maxMoney = Convert.ToInt32(house.Price / 100 * 0.013) * 24 * 7;
                        if (Bank.Accounts[house.BankID].Balance + Math.Abs(amount) > maxMoney)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно перевести столько средств на счет дома.", 3000);
                            return;
                        }
                        if (!Wallet.Change(player, -Math.Abs(amount)))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств.", 3000);
                            return;
                        }
                        Bank.Change(house.BankID, +Math.Abs(amount));
                        GameLog.Money($"player({Main.Players[player].UUID})", $"bank({house.BankID})", Math.Abs(amount), $"atmHouse");
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Успешный перевод.", 3000);
                        Trigger.ClientEvent(player,
                                "atmOpen", $"[2,'{Bank.Accounts[house.BankID].Balance}/{Convert.ToInt32(house.Price / 100 * 0.013) * 24 * 7}$','Сумма внесения наличных']");
                        break;
                    case 3:
                        int bid = NAPI.Data.GetEntityData(player, "ATMBIZ");

                        Business biz = BusinessManager.BizList[acc.BizIDs[bid]];

                        maxMoney = Convert.ToInt32(biz.SellPrice / 100 * 0.013) * 24 * 7;
                        if (Bank.Accounts[biz.BankID].Balance + Math.Abs(amount) > maxMoney)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно перевести столько средств на счет бизнеса.", 3000);
                            return;
                        }
                        if (!Wallet.Change(player, -Math.Abs(amount)))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств.", 3000);
                            return;
                        }
                        Bank.Change(biz.BankID, +Math.Abs(amount));
                        GameLog.Money($"player({Main.Players[player].UUID})", $"bank({biz.BankID})", Math.Abs(amount), $"atmBiz");
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Успешный перевод.", 3000);
                        Trigger.ClientEvent(player, "atmOpen", $"[2,'{Bank.Accounts[biz.BankID].Balance}/{Convert.ToInt32(biz.SellPrice / 100 * 0.013) * 24 * 7}$','Сумма зачисления']");
                        break;
                    case 4:
                        if (!Bank.Accounts.ContainsKey(amount) || amount <= 0)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Счет не найден!", 3000);
                            Trigger.ClientEvent(player, "closeatm");
                            return;
                        }
                        NAPI.Data.SetEntityData(player, "ATM2ACC", amount);
                        Trigger.ClientEvent(player,
                                "atmOpen", "[2,0,'Сумма для перевода']");
                        NAPI.Data.SetEntityData(player, "ATMTYPE", 44);
                        break;
                    case 44:
                        if (Main.Players[player].LVL < 1)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Перевод денег доступен после первого уровня", 3000);
                            return;
                        }
                        if (player.HasData("NEXT_BANK_TRANSFER") && DateTime.Now < player.GetData<DateTime>("NEXT_BANK_TRANSFER"))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, $"Следующая транзакция будет возможна в течение минуты", 3000);
                            return;
                        }
                        int bank = NAPI.Data.GetEntityData(player, "ATM2ACC");
                        if (!Bank.Accounts.ContainsKey(bank) || bank <= 0)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Счет не найден!", 3000);
                            Trigger.ClientEvent(player, "closeatm");
                            return;
                        }
                        if(Bank.Accounts[bank].Type != 1 && Main.Players[player].AdminLVL == 0) {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Счет не найден!", 3000);
                            Trigger.ClientEvent(player, "closeatm");
                            return;
                        }
                        if(acc.Bank == bank) {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Операция отменена.", 3000);
                            Trigger.ClientEvent(player, "closeatm");
                            return;
                        }
                        Bank.Transfer(acc.Bank, bank, Math.Abs(amount));
                        Trigger.ClientEvent(player, "closeatm");
                        if(Main.Players[player].AdminLVL == 0) player.SetData("NEXT_BANK_TRANSFER", DateTime.Now.AddMinutes(1));
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Write(e.ToString(), nLog.Type.Error);
            }
        }
        [RemoteEvent("atmDP")]
        public static void ClientEvent_ATMDupe(Player client)
        {
            foreach (var p in Main.Players.Keys.ToList())
            {
                if (!Main.Players.ContainsKey(p)) continue;
                if (Main.Players[p].AdminLVL >= 3)
                {
                    p.SendChatMessage($"!{{#d35400}}[ATM-FLOOD] {client.Name} ({client.Value})");
                }
            }
        }

        [RemoteEvent("atmCB")]
        public static void ClientEvent_ATMCB(Player player, params object[] args)
        {
            try
            {
                var acc = Main.Players[player];
                int type = Convert.ToInt32(args[0]);
                int index = Convert.ToInt32(args[1]);
                if (index == -1)
                {
                    Trigger.ClientEvent(player, "closeatm");
                    return;
                }
                switch (type)
                {
                    case 1:
                        switch (index)
                        {
                            case 0:
                                Trigger.ClientEvent(player,
                                    "atmOpen", "[2,0,'Сумма внесения наличных']");
                                NAPI.Data.SetEntityData(player, "ATMTYPE", index);
                                break;
                            case 1:
                                Trigger.ClientEvent(player,
                                    "atmOpen", "[2,0,'Сумма для снятия']");
                                NAPI.Data.SetEntityData(player, "ATMTYPE", index);
                                break;
                            case 2:
                                if (Houses.HouseManager.GetHouse(player, true) == null)
                                {
                                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "У вас нет дома!", 3000);
                                    return;
                                }
                                var house = Houses.HouseManager.GetHouse(player, true);
                                Trigger.ClientEvent(player,
                                    "atmOpen", $"[2,'{Bank.Accounts[house.BankID].Balance}/{Convert.ToInt32(house.Price / 100 * 0.013) * 24 * 7}$','Сумма внесения наличных']");
                                Trigger.ClientEvent(player, "setatm", "Дом", $"Дом #{house.ID}", Bank.Accounts[house.BankID].Balance, "");
                                NAPI.Data.SetEntityData(player, "ATMTYPE", index);
                                break;
                            case 3:
                                AtmBizGen(player);
                                NAPI.Data.SetEntityData(player, "ATMTYPE", index);
                                break;
                            case 4:
                                Trigger.ClientEvent(player,
                                    "atmOpen", "[2,0,'Счет зачисления']");
                                NAPI.Data.SetEntityData(player, "ATMTYPE", index);
                                break;

                        }
                        break;
                    case 2:
                        Trigger.ClientEvent(player, "atmOpen", "[1,0,0]");
                        Trigger.ClientEvent(player, "setatm", acc.Bank, player.Name, Bank.Accounts[acc.Bank].Balance, "");
                        break;
                    case 3:
                        if (index == -1)
                        {
                            Trigger.ClientEvent(player, "atmOpen", "[1,0,0]");
                            Trigger.ClientEvent(player, "setatm", acc.Bank, player.Name, Bank.Accounts[acc.Bank].Balance, "");
                            return;
                        }
                        Business biz = BusinessManager.BizList[acc.BizIDs[index]];
                        NAPI.Data.SetEntityData(player, "ATMBIZ", index);
                        Trigger.ClientEvent(player, "atmOpen", $"[2,'{Bank.Accounts[biz.BankID].Balance}/{Convert.ToInt32(biz.SellPrice / 100 * 0.013) * 24 * 7}$','Сумма зачисления']");
                        Trigger.ClientEvent(player, "setatm",
                            "Бизнес",
                            BusinessManager.BusinessTypeNames[biz.Type],
                            Bank.Accounts[biz.BankID].Balance, "");
                        break;

                }
            }
            catch (Exception e) { Log.Write("atmCB: " + e.Message, nLog.Type.Error); }
        }

        [RemoteEvent("atm")]
        public static void ClientEvent_ATM(Player player, params object[] args)
        {
            try
            {
                if (Admin.IsServerStoping)
                {
                    Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Сервер сейчас не может принять это действие", 3000);
                    return;
                }
                int act = Convert.ToInt32(args[0]);
                string data1 = Convert.ToString(args[1]);
                var acc = Main.Players[player];
                int amount;
                if (!Int32.TryParse(data1, out amount))
                    return;
                Log.Debug($"{player.Name} : {data1}");
                switch (act)
                {
                    case 0: //put money
                        if (Wallet.Change(player, -Math.Abs(amount)))
                        {
                            Bank.Change(acc.Bank, amount);
                            GameLog.Money($"player({Main.Players[player].UUID})", $"bank({acc.Bank})", Math.Abs(amount), $"atmIn");
                            Trigger.ClientEvent(player, "setbank", Bank.Accounts[acc.Bank].Balance.ToString(), "");
                        }
                        break;
                    case 1: //take money
                        if (Bank.Change(acc.Bank, -Math.Abs(amount)))
                        {
                            Wallet.Change(player, amount);
                            GameLog.Money($"bank({acc.Bank})", $"player({Main.Players[player].UUID})", Math.Abs(amount), $"atmOut");
                            Trigger.ClientEvent(player, "setbank", Bank.Accounts[acc.Bank].Balance.ToString(), "");
                        }
                        break;
                    case 2: //put house tax
                        var house = Houses.HouseManager.GetHouse(player, true);
                        if (house == null) return;

                        var maxMoney = Convert.ToInt32(house.Price / 100 * 0.013) * 24 * 7;
                        if (Bank.Accounts[house.BankID].Balance + Math.Abs(amount) > maxMoney)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно перевести столько средств на счет дома.", 3000);
                            return;
                        }
                        if (!Wallet.Change(player, -Math.Abs(amount)))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств.", 3000);
                            return;
                        }
                        Bank.Change(house.BankID, Math.Abs(amount));
                        GameLog.Money($"player({Main.Players[player].UUID})", $"bank({house.BankID})", Math.Abs(amount), $"atmHouse");
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Успешный перевод.", 3000);
                        break;
                    case 3: //put biz tax
                        var check = NAPI.Data.GetEntityData(player, "bizcheck");
                        if (check == null) return;
                        if (acc.BizIDs.Count != check)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Возникла ошибка! Попробуйте еще раз.", 3000);
                            return;
                        }
                        int bid = 0;
                        if (!Int32.TryParse(Convert.ToString(args[2]), out bid))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Возникла ошибка! Попробуйте еще раз.", 3000);
                            return;
                        }

                        Business biz = BusinessManager.BizList[acc.BizIDs[bid]];

                        maxMoney = Convert.ToInt32(biz.SellPrice / 100 * 0.01) * 24 * 7;
                        if (Bank.Accounts[biz.BankID].Balance + Math.Abs(amount) > maxMoney)
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Невозможно перевести столько средств на счет бизнеса.", 3000);
                            return;
                        }
                        if (!Wallet.Change(player, -Math.Abs(amount)))
                        {
                            Notify.Send(player, NotifyType.Error, NotifyPosition.BottomCenter, "Недостаточно средств.", 3000);
                            return;
                        }
                        Bank.Change(biz.BankID, Math.Abs(amount));
                        GameLog.Money($"player({Main.Players[player].UUID})", $"bank({biz.BankID})", Math.Abs(amount), $"atmBiz");
                        Notify.Send(player, NotifyType.Success, NotifyPosition.BottomCenter, "Успешный перевод.", 3000);
                        break;
                    case 4: //transfer to
                        int num = 0;
                        if (!Int32.TryParse(Convert.ToString(args[2]), out num))
                            return;
                        Bank.Transfer(acc.Bank, num, +Math.Abs(amount));
                        break;
                }
            }
            catch (Exception e) { Log.Write("atm: " + e.Message, nLog.Type.Error); }
        }
    }
}

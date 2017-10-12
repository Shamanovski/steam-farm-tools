using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Management;
using System.Text;
using System.Net;
using System.ComponentModel;
using System.Globalization;
using SteamAuth;
using System.Drawing;

namespace Shatulsky_Farm {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, EventArgs e) {
            BlockAll();

            #region Настройки Database
            await Task.Run(() => {
                Database.BOT_LIST = new List<Bot>();
                Database.BOTS_LOADING = new List<bool>();
                Database.WASTED_MONEY = 0;
                Database.ALL_GAMES_LIST = new List<Game>();
                Database.COUPONS = new Dictionary<string, Tuple<string, int>>();
                #region Загрузка купонов
                if (Program.GetForm.MyMainForm.CouponsKeyBox.Text != "") {
                    var couponResponse = Request.getResponse($"http://steamkeys.ovh/user.php?key={Program.GetForm.MyMainForm.CouponsKeyBox.Text}");
                    var splCoupons = couponResponse.Split(new[] { "<div style='min-weight:", }, StringSplitOptions.None);
                    for (int i = 1; i < splCoupons.Count(); i++) {
                        var coupon = splCoupons[i];

                        var shop = coupon.Split(new[] { "<div title=" }, StringSplitOptions.None)[1];
                        shop = "http://" + shop.Split('>')[1].Split('<')[0];
                        var text = "";
                        try { text = coupon.Split(new[] { "Купон'value='" }, StringSplitOptions.None)[1].Split('\'')[0]; } catch { }
                        int percent = 0;
                        try { percent = int.Parse(coupon.Split(new[] { "Скидка'value='" }, StringSplitOptions.None)[1].Split('%')[0]); } catch { }
                        Database.COUPONS.Add(shop, new Tuple<string, int>(text, percent));
                    }
                }
                #endregion
            });
            #endregion

            #region Загрузка VDS
            var VDSs = ServersRichTextBox.Text.Split('\n').ToList();
            #region удалить пустые строки
            for (int i = 0; i < VDSs.Count; i++) {
                if (VDSs[i] == "" || VDSs[i] == "\n")
                    VDSs.RemoveAt(i--);
            }
            #endregion

            for (int i = 0; i < VDSs.Count; i++) {
                var VDS = VDSs[i];

                if (VDS != string.Empty) {
#pragma warning disable CS4014 
                    Task.Run(() => {
                        AddLog($"{VDS} - bots loading started");
                        Bot.AllBotsToDatabase(VDS);
                        Database.BOTS_LOADING.Add(true);
                        AddLog($"{VDS} - bots loading done");
                    });
#pragma warning restore CS4014 
                }
            }

            await Task.Run(() => {
                bool done = false;
                while (!done) {
                    if (Database.BOTS_LOADING.Count == VDSs.Count)
                        break;
                    Thread.Sleep(1000);
                }
            });
            #endregion

            if (Program.GetForm.MyMainForm.LogBox.Text.Contains("Bot parse error")) {
                Program.GetForm.MyMainForm.AddLogBold("Fix erroneous bots and restart Steam Farm Tools");
                Thread.Sleep(Timeout.Infinite);
            }

            await Task.Run(async () => {

                #region Обработка Json каталога
                Program.GetForm.MyMainForm.AddLog("All suitable games loading.");

                var response = Request.GetCatalog();
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                foreach (var item in json) {
                    string appid = item.Path;
                    string lequeshop_id = item.Value.lequeshop_id.Value;
                    double price = item.Value.price.Value;
                    double amount = (item.Value.amount.Value.ToString() == "N/A") ? 0 : item.Value.amount.Value;
                    string store = item.Value.store.Value;
                    string game_name = item.Value.game_name.Value;
                    Database.ALL_GAMES_LIST.Add(new Game(appid, lequeshop_id, price, amount, store, game_name));
                }
                #endregion

                #region Поиск нужных игр
                foreach (var game in Database.ALL_GAMES_LIST) {
                    foreach (var bot in Database.BOT_LIST) {
                        if (!bot.gamesHave.Contains(game.appid))
                            game.count += 1;
                    }
                }

                double maxGamePrice = double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text);
                for (int i = 0; i < Database.ALL_GAMES_LIST.Count; i++) {//удаляем из списка игры которые не нужны и которые дороже разрешонного
                    var game = Database.ALL_GAMES_LIST[i];
                    if (game.count == 0 || game.price > maxGamePrice || Database.BLACKLIST.Contains(game.appid) || game.store.Contains("akens.ru")) {
                        Database.ALL_GAMES_LIST.Remove(game);
                        i--;
                    }
                }

                Database.ALL_GAMES_LIST.Sort(); //сортируем от минимальной цены
                #endregion

                #region Покупка игр
                Program.GetForm.MyMainForm.AddLog($"Found {Database.ALL_GAMES_LIST.Count()} games satisfying the condition (<={Program.GetForm.MyMainForm.MaxGameCostBox.Text})");
                Program.GetForm.MyMainForm.AddLog("Game buying process started");

                foreach (var game in Database.ALL_GAMES_LIST) {

                    #region Пост запрос в магазин
                    string[] setCookies;

                    var postData = "email=" + Program.GetForm.MyMainForm.EmailBox.Text.Replace("@", "%40");
                    postData += "&count=" + game.count;
                    postData += "&type=" + game.lequeshop_id;
                    postData += "&forms=%7B%7D&fund=4";
                    postData += "&copupon=";
                    if (Program.GetForm.MyMainForm.CouponsKeyBox.Text != "")
                        postData += Database.COUPONS[game.store].Item1;

                    var order = Request.POST(game.store + "/order", postData, out setCookies);

                    dynamic jsonOrder = null;
                    try {
                        jsonOrder = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(order);
                    } catch {
                        Program.GetForm.MyMainForm.AddLogBold($"Error getting payment information {game.game_name} in {game.store}. Moving on to the next product.");
                        continue;
                    }
                    #endregion

                    #region Если товара не хватает
                    try {
                        if (jsonOrder.error.Value.Contains("Такого количества товара нет в наличии.")) {
                            var keysLeft = jsonOrder.error.Value.Split(new[] { "Доступно: " }, StringSplitOptions.None)[1].Split(new[] { " Шт" }, StringSplitOptions.None)[0];
                            Program.GetForm.MyMainForm.AddLog($"{game.store} does notenought keys fot {game.game_name}. Buying {keysLeft} remaining keys.");
                            postData = postData.Replace($"count={game.count}", $"count={keysLeft}");
                            order = Request.POST(game.store + "/order", postData, out setCookies);
                            jsonOrder = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(order);
                        }
                    } catch { }
                    #endregion

                    #region Данные заказа
                    var allPrice = "";
                    var reciever = "";
                    var comment = "";
                    var buyLink = "";
                    double count = 0;
                    var appid = "";
                    try {
                        allPrice = jsonOrder.price.Value.Split(new[] { " QIWI" }, StringSplitOptions.None)[0];
                        reciever = jsonOrder.fund.Value.Split('>')[1].Split('<')[0];
                        comment = jsonOrder.bill.Value.Split('>')[1].Split('<')[0];
                        buyLink = jsonOrder.check_url.Value.Replace("\\", "");
                        count = jsonOrder.count.Value;
                        appid = game.appid;
                    } catch {
                        Program.GetForm.MyMainForm.AddLogBold($"Error getting payment information {game.game_name} in {game.store}. Proceed to the next product.");
                        continue;
                    }
                    #endregion
                    double allPreciDouble = 0;
                    var separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    switch (separator) {
                        case ".":
                            allPreciDouble = double.Parse(allPrice.Replace(',', '.')); break;
                        case ",":
                            allPreciDouble = double.Parse(allPrice.Replace('.', ',')); break;
                        default: throw new Exception($"Custom fractional separator - \"{separator}\"");
                    }
                    #region Проверки
                    if (Database.WASTED_MONEY + allPreciDouble > double.Parse(Program.GetForm.MyMainForm.MaxMoneyBox.Text)) { //заканчиваем цикл если достигли лимит по деньгам
                        Program.GetForm.MyMainForm.AddLogBold($"You have reached the purchase limit. Spent {Database.WASTED_MONEY}");
                        break;
                    }

                    var oneItemPrice = allPreciDouble / count;
                    double maxAllowedPrice = 0;
                    switch (separator) {
                        case ".":
                            maxAllowedPrice = double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text.Replace(',', '.')); break;
                        case ",":
                            maxAllowedPrice = double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text.Replace('.', ',')); break;
                        default: throw new Exception($"Custom fractional separator - \"{separator}\"");
                    }

                    if (oneItemPrice >= maxAllowedPrice) {
                        Program.GetForm.MyMainForm.AddLog($"We skip the game {game.game_name} ({game.appid}) since its price has become higher than permissible - {Math.Round(oneItemPrice, 2)}");
                        continue; //пропускаем элемент если его цена увеличилась выше допустимой
                    }
                    #endregion

                    #region Оплата
                    Program.GetForm.MyMainForm.AddLog($"Buying {count} games {game.game_name} ({appid}) by {Math.Round(oneItemPrice, 2)} for the amount of {allPrice}");
                    var totalPrice = allPrice.ToString().Replace(',', '.');
                    Qiwi qiwiAccount = new Qiwi(Program.GetForm.MyMainForm.QiwiTokenBox.Text);
                    var paymentDone = await qiwiAccount.SendMoneyToWallet(reciever, totalPrice, comment);
                    if (!paymentDone) throw new Exception($"Failed to pay {reciever} {comment} {appid} {totalPrice} RUB. {buyLink}");
                    File.AppendAllText("buylinks.txt", $"{DateTime.Now} - {buyLink}\n");
                    Database.WASTED_MONEY += allPreciDouble;
                    UpdateWastedMoney();
                    Program.GetForm.MyMainForm.AddLog($"Paid {totalPrice} RUB, to the number {reciever}");
                    Thread.Sleep(5000);
                    #endregion

                    #region Загрузка файла

                    #region Получить куки
                    string cookies = "";
                    if (setCookies.Count() > 0) {
                        var cookiesDictionary = new Dictionary<string, string>();
                        var setCookiesString = "";
                        foreach (var item in setCookies) {
                            setCookiesString += item;
                        }
                        foreach (var item in setCookiesString.Split(';')) {
                            try {
                                var splItem = item.Split('=');
                                cookiesDictionary.Add(splItem[0].Replace(" ", ""), splItem[1].Replace(" ", ""));
                            } catch { }
                        }
                        cookies = "PHPSESSID=" + cookiesDictionary["PHPSESSID"];
                    }

                    #endregion

                    try { File.Delete("downloaded.txt"); } catch { };
                    response = Request.getResponse(buyLink, cookies).ToString();
                    var fileDownloaded = Request.DownloadFile(buyLink.Replace("/order/", "/order/get/") + "/saved/", cookies, "downloaded.txt");
                    if (!fileDownloaded) throw new Exception($"Failed to download file {buyLink}");
                    Thread.Sleep(1000);
                    var fileName = $"{appid} {game.game_name} - {DateTime.Now}";
                    fileName = fileName.Replace('.', '-');
                    fileName = fileName.Replace(':', '-');
                    fileName = fileName.Replace('/', '-');
                    fileName = fileName.Replace('\\', '-');
                    Directory.CreateDirectory("keys");
                    File.Move("downloaded.txt", $"keys\\{fileName}.txt");
                    Program.GetForm.MyMainForm.AddLog($"File {fileName}.txt saved.");
                    Thread.Sleep(1000);
                    #endregion

                    #region Активация ключей
                    var keysList = File.ReadAllLines($"keys\\{fileName}.txt");
                    Program.GetForm.MyMainForm.AddLog($"Activation {keysList.Count()} keys {game.game_name} ({appid})");
                    foreach (var line in keysList) {
                        foreach (var bot in Database.BOT_LIST) {
                            if (!bot.gamesHave.Contains(appid)) {
                                Regex regex = new Regex(@"\w{5}-\w{5}-\w{5}");
                                var key = regex.Match(line);
                                var command = $"http://{bot.vds}/IPC?command=";
                                command += $"!redeem^ {bot.login} SD,SF {key}";
                                var keysResponse = Request.getResponse(command);
                                File.AppendAllText($"responses.txt", $"\n{DateTime.Now} {bot.vds} {bot.login} {appid} {buyLink} - {keysResponse}");

                                if (keysResponse.Contains("Timeout")) {
                                    Thread.Sleep(10000);
                                    var botResponse = Request.getResponse($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={Program.GetForm.MyMainForm.ApikeyBox.Text}&steamid={bot.steamID}&format=json");
                                    if (botResponse.Contains(appid))
                                        keysResponse += "False timout. OK/NoDetail";
                                }

                                if (keysResponse.Contains("OK/NoDetail") == false) {
                                    Program.GetForm.MyMainForm.AddLogBold($"Bad activation for {bot.vds},{bot.login},{key},{keysResponse.Replace('\r', ' ').Replace('\n', ' ')}");
                                    File.AppendAllText("UNUSEDKEYS.TXT", $"{bot.vds},{bot.login},{key},{keysResponse.Replace('\r', ' ').Replace('\n', ' ')}\n");
                                }
                                bot.gamesHave.Add(appid);
                                break;
                            }
                        }
                    }


                    Program.GetForm.MyMainForm.AddLog($"Waiting 30 seconds until next purchase");
                    Thread.Sleep(30000);
                    Program.GetForm.MyMainForm.AddLogNoDate($"----------------------------------------------------------------------------------------");
                    #endregion
                }
                #endregion

            });
            Program.GetForm.MyMainForm.AddLog($"Purchases complete");
            UnblockAll();
        }
        public void IncreaseBotsCount() {
            if (InvokeRequired)
                Invoke((Action)IncreaseBotsCount);
            else {
                Program.GetForm.MyMainForm.BotsLoadedCountLable.Text = (Database.BOT_LIST.Count + 1).ToString();
            }
        }
        public void UpdateWastedMoney() {
            if (InvokeRequired)
                Invoke((Action)UpdateWastedMoney);
            else {
                Program.GetForm.MyMainForm.WastedManeyCountLable.Text = Database.WASTED_MONEY.ToString();
            }
        }
        public void AddLog(string text) {
            if (InvokeRequired)
                Invoke((Action<string>)AddLog, text);
            else {
                Program.GetForm.MyMainForm.LogBox.AppendText(DateTime.Now + " - " + text + "\n");
                File.AppendAllText("log.txt", "\n" + DateTime.Now + " - " + text);
            }
        }
        public void AddLogBold(string text) {
            if (InvokeRequired)
                Invoke((Action<string>)AddLogBold, text);
            else {
                Program.GetForm.MyMainForm.LogBox.SelectionFont = new Font(Program.GetForm.MyMainForm.LogBox.Font, FontStyle.Bold);
                Program.GetForm.MyMainForm.LogBox.AppendText(DateTime.Now + " - " + text + "\n");
                Program.GetForm.MyMainForm.LogBox.SelectionFont = new Font(Program.GetForm.MyMainForm.LogBox.Font, FontStyle.Regular);
                File.AppendAllText("log.txt", "\n" + DateTime.Now + " - " + text);
            }
        }
        public void AddLogNoDate(string text) {
            if (InvokeRequired)
                Invoke((Action<string>)AddLogNoDate, text);
            else {
                Program.GetForm.MyMainForm.LogBox.AppendText(text + "\n");
                File.AppendAllText("log.txt", "\n" + text);
            }
        }
        public void BlockAll() {
            if (InvokeRequired)
                Invoke((Action)BlockAll);
            Program.GetForm.MyMainForm.IPC1GruopBox.Enabled = false;
            Program.GetForm.MyMainForm.PaymentGroupBox.Enabled = false;
            Program.GetForm.MyMainForm.IPC2GruopBox.Enabled = false;
            Program.GetForm.MyMainForm.ScanGroupBox.Enabled = false;
            Program.GetForm.MyMainForm.CommandsGroupBox.Enabled = false;
            Program.GetForm.MyMainForm.ManualCommandsGroupBox.Enabled = false;
            Program.GetForm.MyMainForm.BuyGamesButton.Enabled = false;
            Program.GetForm.MyMainForm.ActivateKeysButton.Enabled = false;
            Program.GetForm.MyMainForm.ActivateUnusedKeysButton.Enabled = false;
            Program.GetForm.MyMainForm.QIWIGroupBox.Enabled = false;
            Program.GetForm.MyMainForm.QIWIStartButton.Enabled = false;
            Program.GetForm.MyMainForm.QIWILoginsBox.Enabled = false;
            Program.GetForm.MyMainForm.QIWITextBox.Enabled = false;
        }
        public void UnblockAll() {
            if (InvokeRequired)
                Invoke((Action)UnblockAll);
            Program.GetForm.MyMainForm.IPC1GruopBox.Enabled = true;
            Program.GetForm.MyMainForm.PaymentGroupBox.Enabled = true;
            Program.GetForm.MyMainForm.IPC2GruopBox.Enabled = true;
            Program.GetForm.MyMainForm.ScanGroupBox.Enabled = true;
            Program.GetForm.MyMainForm.CommandsGroupBox.Enabled = true;
            Program.GetForm.MyMainForm.ManualCommandsGroupBox.Enabled = true;
            Program.GetForm.MyMainForm.BuyGamesButton.Enabled = true;
            Program.GetForm.MyMainForm.ActivateKeysButton.Enabled = true;
            Program.GetForm.MyMainForm.ActivateUnusedKeysButton.Enabled = true;
            Program.GetForm.MyMainForm.QIWIGroupBox.Enabled = true;
            Program.GetForm.MyMainForm.QIWIStartButton.Enabled = true;
            Program.GetForm.MyMainForm.QIWILoginsBox.Enabled = true;
            Program.GetForm.MyMainForm.QIWITextBox.Enabled = true;
        }
        private class DescendingComparer : IComparer<string> {
            int IComparer<string>.Compare(string a, string b) {
                return StringComparer.InvariantCulture.Compare(b, a);
            }
        }
        private void LogBox_TextChanged(object sender, EventArgs e) {
            LogBox.SelectionStart = LogBox.TextLength;
            LogBox.ScrollToCaret();
        }
        private async void LootButton_Click(object sender, EventArgs e) {
            BlockAll();
            await Task.Run(() => {
                var unusedKeys = File.ReadAllLines("UNUSEDKEYS.TXT").ToList<string>();
                for (int i = 0; i < unusedKeys.Count(); i++) {
                    var line = unusedKeys[i];
                    var data = line.Split(',');
                    var vds = data[0];
                    var login = data[1];
                    var key = data[2];
                    var command = $"http://{vds}/IPC?command=";
                    command += $"!redeem^ {login} SD,SF {key}";
                    var response = Request.getResponse(command);
                    Program.GetForm.MyMainForm.AddLog(response);
                    if (response.Contains("OK/NoDetail")) {
                        unusedKeys.Remove(line);
                        File.WriteAllLines("UNUSEDKEYS.TXT", unusedKeys);
                        Thread.Sleep(1000);
                    }
                    else {
                        File.WriteAllText("UNUSEDKEYS.TXT", $"{vds},{login},{key},{response.Replace('\r', ' ').Replace('\n', ' ')}");
                    }
                }
            });
            UnblockAll();
        }
        private void MainForm_Load(object sender, EventArgs e) {
            var settings = File.ReadAllText("settings.txt").Replace('\\', '/');
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(settings);
            ApikeyBox.Text = json.SteamAPI;
            CouponsKeyBox.Text = json.SteamkeysAPI;
            MaxGameCostBox.Text = json.MaxGameCost;
            MaxMoneyBox.Text = json.MaxMoneySpent;
            EmailBox.Text = json.Email;
            CouponsKeyBox.Text = json.CouponsKey;
            QiwiTokenBox.Text = json.QiwiToken;
            QiwiTokenBox2.Text = json.QiwiToken;
            MafilePathBox.Text = json.MafilesPath.ToString().Replace("/", "\\");
            Database.KEY = json.LicenseKey;
            for (int i = 0; i < json.VDSs.Count; i++) {
                ServersRichTextBox.AppendText(json.VDSs[i].Value + "\n");
                ServersRichTextBox2.AppendText(json.VDSs[i].Value + "\n");
            }

            Database.BLACKLIST = new List<string>();
            for (int i = 0; i < json.BlacklistAppids.Count; i++) {
                Database.BLACKLIST.Add(json.BlacklistAppids[i].Value);
            }
            LogBox.Text = $"The program is started {System.DateTime.Now}\n";

            string uid = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc) {
                uid = mo.Properties["processorID"].Value.ToString();
                break;
            }
            try {
                ManagementObject dsk = new ManagementObject(
                    @"win32_logicaldisk.deviceid=""" + "C" + @":""");
                dsk.Get();
                uid += dsk["VolumeSerialNumber"].ToString();
            } catch { }

            Database.UID = uid;

            string check = $"uid={uid}&key={Database.KEY}";
            string[] ok;
            var postResponse = Request.POST("https://shamanovski.pythonanywhere.com/check_license", check, out ok);
            var postJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(postResponse);
            if (postJson.success == false) {
                DialogResult res = new DialogResult();
                res = MessageBox.Show("License check failed!",
                                                 "License Error",
                                                 MessageBoxButtons.OK,
                                                 MessageBoxIcon.Error);
                if (res == DialogResult.OK) { Close(); }
                else { Close(); }
            }

        }
        private async void ActivateKeysButton_Click(object sender, EventArgs e) {
            BlockAll();

            #region Обнуление Database
            await Task.Run(() => {
                Database.BOT_LIST = new List<Bot>();
                Database.BOTS_LOADING = new List<bool>();
                Database.WASTED_MONEY = 0;
                Database.ALL_GAMES_LIST = new List<Game>();
            });
            #endregion

            #region Загрузка VDS
            var VDSs = ServersRichTextBox.Text.Split('\n').ToList();
            #region удалить пустые строки
            for (int i = 0; i < VDSs.Count; i++) {
                if (VDSs[i] == "" || VDSs[i] == "\n")
                    VDSs.RemoveAt(i--);
            }
            #endregion

            for (int i = 0; i < VDSs.Count; i++) {
                var VDS = VDSs[i];

                if (VDS != string.Empty) {
#pragma warning disable CS4014
                    Task.Run(() => {
                        AddLog($"{VDS} - bots loading started");
                        Bot.AllBotsToDatabase(VDS);
                        Database.BOTS_LOADING.Add(true);
                        AddLog($"{VDS} - bots loading done");
                    });
#pragma warning restore CS4014
                }
            }

            await Task.Run(() => {
                bool done = false;
                while (!done) {
                    if (Database.BOTS_LOADING.Count == VDSs.Count)
                        break;
                }
            });
            #endregion


            #region Активация
            await Task.Run(() => {
                Directory.CreateDirectory("activate");
                var files = Directory.GetFiles("activate");
                int botCount = 0;
                foreach (var file in files) {
                    Program.GetForm.MyMainForm.AddLog($"Processing {file}");
                    var appid = file.Split('\\')[1].Split('.')[0];
                    var keys = File.ReadAllLines(file).ToList<string>();
                    for (int i = 0; i < keys.Count(); i++) {
                        if (keys[i] != String.Empty) {
                            for (; botCount < Database.BOT_LIST.Count(); botCount++) {
                                var bot = Database.BOT_LIST[botCount];

                                if (!bot.gamesHave.Contains(appid)) {

                                    Regex regex = new Regex(@"\w{5}-\w{5}-\w{5}");
                                    var key = regex.Match(keys[i]);
                                    var command = $"http://{bot.vds}/IPC?command=";
                                    command += $"!redeem^ {bot.login} SD,SF {key}";
                                    var response = Request.getResponse(command);

                                    File.AppendAllText($"responses.txt", $"\n{DateTime.Now} {bot.vds} {bot.login} {appid} - {response}");

                                    if (response.Contains("Timeout")) {
                                        Thread.Sleep(10000);
                                        var botResponse = Request.getResponse($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={Program.GetForm.MyMainForm.ApikeyBox.Text}&steamid={bot.steamID}&format=json");
                                        if (botResponse.Contains(appid))
                                            response += "False timeout. OK/NoDetail";
                                    }

                                    if (response.Contains("OK/NoDetail")) {
                                        Program.GetForm.MyMainForm.AddLog($"{keys[i]} - OK");
                                        keys.Remove(keys[i--]);
                                        bot.gamesHave.Add(appid);
                                        botCount++;
                                        break;
                                    }

                                    if (response.Contains("BadActivationCode") || response.Contains("DuplicateActivationCode")) {
                                        Program.GetForm.MyMainForm.AddLogBold($"{keys[i]} - DuplicateActivationCode");
                                        keys.Remove(keys[i--]);
                                        botCount++;
                                        break;
                                    }

                                }
                            }
                        }
                    }
                    File.WriteAllLines(file, keys);
                }
            });
            #endregion

            UnblockAll();
        }
        private async void button1_Click(object sender, EventArgs e) {
            BlockAll();
            await Task.Run(async () => {
                string text = "";
                Invoke((Action)(() => {
                    text = Program.GetForm.MyMainForm.QIWILoginsBox.Text.Clone().ToString();
                }));

                List<string> inputBots = text.Replace("\r", "").Split('\n').ToList<string>();
                for (int i = 0; i < inputBots.Count; i++) {
                    var item = inputBots[i];
                    if (item == string.Empty || item == "") {
                        inputBots.Remove(item);
                        i--;
                    }
                }

                int processStatus = 0;
                Qiwi qiwiAccount = new Qiwi(Program.GetForm.MyMainForm.QiwiTokenBox2.Text);
                var money = Program.GetForm.MyMainForm.QIWIDonateBox.Text.Replace(',', '.');
                foreach (var bot in inputBots) {
                    if (bot != String.Empty) {
                        var paymentDone = await qiwiAccount.SendMoneyToSteam(bot, money);
                        if (!paymentDone) {
                            Program.GetForm.MyMainForm.AddLog($"[{++processStatus}/{inputBots.Count}] {bot} ERROR in the process of replenishment!");
                            break;
                        }
                        Program.GetForm.MyMainForm.AddLog($"[{++processStatus}/{inputBots.Count}] {bot} replenishment for the amount of {money} RUB successfully conducted.");
                        File.AppendAllText("QIWI.txt", $"{DateTime.Now} - {bot},{money}");
                        Thread.Sleep(1111);
                    }
                }
            });
            UnblockAll();
        }
        private async void InventoryItemsButton_Click(object sender, EventArgs e) {
            BlockAll();

            #region Загрузка VDS
            var VDSs = ServersRichTextBox2.Text.Split('\n').ToList();
            #region удалить пустые строки
            for (int i = 0; i < VDSs.Count; i++) {
                if (VDSs[i] == "" || VDSs[i] == "\n")
                    VDSs.RemoveAt(i--);
            }
            #endregion

            var InvId = "";
            var InvAssetsId = Program.GetForm.MyMainForm.InventoryToScanBox2.Text;

            switch (InventoryToScanBox.Text) {
                case "Steam": InvId = "753"; break;
                case "CS GO": InvId = "730"; break;
                case "PUBG": InvId = "578080"; break;
                case "TF": InvId = "440"; break;
                default: InvId = Program.GetForm.MyMainForm.InventoryToScanBox.Text; break;
            }

            #endregion
            await Task.Run(() => {
                var totalCount = 0;
                foreach (var VDS in VDSs) {
                    var command = $"http://{VDS}/IPC?command=";
                    var response = Request.getResponse(command + "!api asf");
                    var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                    var bots = json["Bots"];
                    Database.ALL_BOTS_STEAMID_LOGIN = new Dictionary<string, string>();
                    foreach (var bot in bots) {
                        Database.ALL_BOTS_STEAMID_LOGIN.Add(bot.Value.SteamID.ToString(), bot.Name);
                    }
                    foreach (var bot in Database.ALL_BOTS_STEAMID_LOGIN) {
                        var invResponse = Request.getResponse($"http://steamcommunity.com/inventory/{bot.Key}/{InvId}/{InvAssetsId}?l=english&count=1");
                        var inventoryJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(invResponse);
                        totalCount += inventoryJson.total_inventory_count.Value;
                        Program.GetForm.MyMainForm.AddLog($"{bot.Value} - {inventoryJson.total_inventory_count}");
                    }
                }
                Program.GetForm.MyMainForm.AddLog($"Total items - {totalCount}.");
            });
            UnblockAll();

        }
        private async void FarmStatusButton_Click(object sender, EventArgs e) {
            BlockAll();

            #region Загрузка VDS
            var VDSs = ServersRichTextBox2.Text.Split('\n').ToList();
            #region удалить пустые строки
            for (int i = 0; i < VDSs.Count; i++) {
                if (VDSs[i] == "" || VDSs[i] == "\n")
                    VDSs.RemoveAt(i--);
            }
            #endregion

            #endregion
            await Task.Run(() => {
                int farmingCount = 0;
                int notFarmingCount = 0;
                int cardsLeft = 0;

                foreach (var VDS in VDSs) {
                    var statusResponse = Request.getResponse($"http://{VDS}/IPC?command=!sa").Replace("\r", "");
                    foreach (var line in statusResponse.Split('\n')) {
                        if (line == "") continue;

                        if (!line.Contains("There are ")) {
                            var botName = line.Split('<')[1].Split('>')[0];
                            var status = line.Split('>')[1];
                            if (line.Contains("not idling")) {
                                status = "Not idling";
                                notFarmingCount++;
                            }
                            if (line.Contains("is idling")) {
                                status = (new Regex(@"(\(\~[^&]*)remaining\)")).Match(line).ToString().Replace("~", "");
                                farmingCount++;
                            }
                            Program.GetForm.MyMainForm.AddLogNoDate($"{botName} - {status}");
                        }
                        else {
                            cardsLeft += int.Parse(line.Split('(')[1].Split(' ')[0]);
                        }
                    }
                }
                Program.GetForm.MyMainForm.AddLogNoDate("----------------------------------------------------------------------------------------");
                Program.GetForm.MyMainForm.AddLog($"Not farming {notFarmingCount}");
                Program.GetForm.MyMainForm.AddLog($"Farming {farmingCount}");
                Program.GetForm.MyMainForm.AddLog($"Cards left to idle {cardsLeft}");
            });

            UnblockAll();
        }
        private void InventoryToScanBox_SelectedIndexChanged(object sender, EventArgs e) {
            switch (InventoryToScanBox.Text) {
                case "Steam": InventoryToScanBox2.Text = "6"; break;
                case "CS GO": InventoryToScanBox2.Text = "2"; break;
                case "PUBG": InventoryToScanBox2.Text = "2"; break;
                case "TF": InventoryToScanBox2.Text = "2"; break;
            }
        }
        private async void LootAllFarmButton_Click(object sender, EventArgs e) {
            BlockAll();
            #region Загрузка VDS
            var VDSs = ServersRichTextBox.Text.Split('\n').ToList();
            #region удалить пустые строки
            for (int i = 0; i < VDSs.Count; i++) {
                if (VDSs[i] == "" || VDSs[i] == "\n")
                    VDSs.RemoveAt(i--);
            }
            #endregion
            #endregion
            Database.BOTS_LOADING = new List<bool>();

            foreach (var VDS in VDSs) {
#pragma warning disable CS4014
                Task.Run(() => {
                    var command = $"http://{VDS}/IPC?command=";
                    var response = Request.getResponse(command + "!api asf");
                    var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                    var bots = json["Bots"];
                    var allBots = new List<string>();
                    foreach (var bot in bots) {
                        allBots.Add(bot.Name);
                    }
                    foreach (var bot in allBots) {
                        var lootResponse = Request.getResponse($"http://{VDS}/IPC?command=!loot {bot}");
                        Program.GetForm.MyMainForm.AddLog(VDS + lootResponse);
                        Thread.Sleep(15000);
                    }
                    Database.BOTS_LOADING.Add(true);
                });
#pragma warning restore CS4014
            }

            await Task.Run(() => {
                bool done = false;
                while (!done) {
                    if (Database.BOTS_LOADING.Count == VDSs.Count)
                        break;
                    Thread.Sleep(1000);
                }
            });
            UnblockAll();
        }

        private async void ManualCommandButton_Click(object sender, EventArgs e) {
            BlockAll();
            #region Загрузка VDS
            var VDSs = ServersRichTextBox.Text.Split('\n').ToList();
            #region удалить пустые строки
            for (int i = 0; i < VDSs.Count; i++) {
                if (VDSs[i] == "" || VDSs[i] == "\n")
                    VDSs.RemoveAt(i--);
            }
            #endregion
            #endregion
            var commandFromBox = Program.GetForm.MyMainForm.ManualCommandBox.Text;

            await Task.Run(() => {
                foreach (var VDS in VDSs) {
                    var command = $"http://{VDS}/IPC?command=!" + commandFromBox;
                    var response = Request.getResponse(command);
                    Program.GetForm.MyMainForm.AddLog(response);
                }
            });
            UnblockAll();

        }

        private async void SteamBuyButton_Click(object sender, EventArgs e) {
            BlockAll();
            var allBotsInfo = new Dictionary<string, string>();
            #region Загрузка мафайлов
            await Task.Run(() => {
                Program.GetForm.MyMainForm.AddLog("Mafiles processing started.");
                var mafiles = Directory.GetFiles(Program.GetForm.MyMainForm.MafilePathBox.Text);
                foreach (var mafile in mafiles) {
                    try {
                        var mafileJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(mafile));
                        allBotsInfo.Add(mafileJson.account_name.Value, mafileJson.shared_secret.Value);
                    } catch { }
                }
                Program.GetForm.MyMainForm.AddLog($"{mafiles.Count()} mafiles processed.");
            });
            #endregion

            var accounts = Program.GetForm.MyMainForm.SteamBuyAccounts.Text.Split('\n');
            await Task.Run(() => {
                int count = 0;
                foreach (var account in accounts) {
                    #region Данные аккаунта
                    var accSpl = account.Split(':');
                    var login = accSpl[0];
                    var password = accSpl[1];
                    var sharedSecret = allBotsInfo[login];
                    #endregion

                    #region Steam login
                    var steamLogin = new UserLogin(login, password);
                    var bot = new SteamGuardAccount();
                    bot.SharedSecret = sharedSecret;
                    steamLogin.TwoFactorCode = bot.GenerateSteamGuardCode();
                    steamLogin.DoLogin();
                    if (!steamLogin.LoggedIn) throw new Exception($"Cant login - {login}.");

                    var steamCookies = new Dictionary<string, string>();
                    steamCookies.Add("sessionid", steamLogin.Session.SessionID);
                    steamCookies.Add("steamLogin", steamLogin.Session.SteamLogin);
                    steamCookies.Add("steamLoginSecure", steamLogin.Session.SteamLoginSecure);
                    #endregion

                    #region Что покупать
                    var buyLink = "";
                    if (Program.GetForm.MyMainForm.radioButtonTF.Checked) buyLink = "https://store.steampowered.com/buyitem/440/5021/2";
                    if (Program.GetForm.MyMainForm.radioButtonPubg.Checked) buyLink = "http://store.steampowered.com/buyitem/578080/35100001/2";
                    #endregion

                    #region Покупка
                    CookieCollection storeCookies = new CookieCollection();
                    var response = Request.getSteamResponse(buyLink, steamCookies, out storeCookies);

                    Match m1 = new Regex(@"name=""returnurl"" value=""(.+)""").Match(response);
                    var returnUrl = m1.Groups[1];

                    Match m2 = new Regex(@"name=""transaction_id"" value=""(.+)""").Match(response);
                    var transId = m2.Groups[1];

                    var postData = "transaction_id=" + transId;
                    postData += "&returnurl=" + returnUrl.ToString().Replace(";", "%2F&");
                    postData += "&sessionid=" + storeCookies[2].Value.ToString();
                    postData += "&approved=1";

                    var postResponse = Request.SendPostRequest("https://store.steampowered.com/checkout/approvetxnsubmit", postData, storeCookies);
                    if (postResponse.Contains("произошла непредвиденная ошибка") || postResponse.Contains("произошла ошибка"))
                        throw new Exception($"Cant buy items for {login}");
                    Program.GetForm.MyMainForm.AddLog($"{login} - DONE [{++count}/{account.Count()}]");
                    #endregion
                }
            });
            UnblockAll();
        }

        private void BrowseFolderButton_Click(object sender, EventArgs e) {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Chose Mafiles folder.";
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                Program.GetForm.MyMainForm.MafilePathBox.Text = folderBrowserDialog.SelectedPath;
            }
        }
    }
}
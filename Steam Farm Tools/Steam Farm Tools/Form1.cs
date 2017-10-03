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
                        AddLog($"{VDS} - загрузка ботов начата");
                        Bot.AllBotsToDatabase(VDS);
                        Database.BOTS_LOADING.Add(true);
                        AddLog($"{VDS} - загрузка ботов завершена");
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


            await Task.Run(async () => {

                #region Обработка Json каталога
                Program.GetForm.MyMainForm.AddLog("Загрузка списка всех доступных игр");
                var response = Request.getResponse("https://shamanovski.pythonanywhere.com/catalogue", $"uid={Database.UID}", $"key={Database.KEY}");
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

                double maxGamePrice = double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text.Replace('.', ','));
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
                Program.GetForm.MyMainForm.AddLog($"Найдено {Database.ALL_GAMES_LIST.Count()} игр удовлетворяющих условию (<={Program.GetForm.MyMainForm.MaxGameCostBox.Text})");
                Program.GetForm.MyMainForm.AddLog("Начата покупка игр");

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

                    var jsonOrder = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(order);
                    #endregion

                    #region Если товара не хватает
                    try {
                        if (jsonOrder.error.Value.Contains("Такого количества товара нет в наличии.")) {
                            var keysLeft = jsonOrder.error.Value.Split(new[] { "Доступно: " }, StringSplitOptions.None)[1].Split(new[] { " Шт" }, StringSplitOptions.None)[0];
                            postData = postData.Replace($"count={game.count}", $"count={keysLeft}");
                            order = Request.POST(game.store + "/order", postData, out setCookies);
                            jsonOrder = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(order);
                        }
                    } catch { }
                    #endregion

                    #region Данные заказа
                    double allPrice = 0;
                    var reciever = "";
                    var comment = "";
                    var buyLink = "";
                    double count = 0;
                    var appid = "";
                    try {
                        allPrice = double.Parse(jsonOrder.price.Value.Split(new[] { " QIWI" }, StringSplitOptions.None)[0].Replace('.', ','));
                        reciever = jsonOrder.fund.Value.Split('>')[1].Split('<')[0];
                        comment = jsonOrder.bill.Value.Split('>')[1].Split('<')[0];
                        buyLink = jsonOrder.check_url.Value.Replace("\\", "");
                        count = jsonOrder.count.Value;
                        appid = game.appid;
                    } catch {
                        continue;
                    }
                    #endregion

                    #region Проверки
                    if (Database.WASTED_MONEY + allPrice > double.Parse(Program.GetForm.MyMainForm.MaxMoneyBox.Text)) { //заканчиваем цикл если достигли лимит по деньгам
                        Program.GetForm.MyMainForm.AddLog($"Достигнуто ограничение на покупку. Потрачено {Database.WASTED_MONEY}");
                        break;
                    }

                    var oneItemPrice = allPrice / count;
                    var maxAllowedPrice = double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text.Replace('.', ','));
                    if (oneItemPrice >= maxAllowedPrice) {
                        Program.GetForm.MyMainForm.AddLog($"Пропускаем игру {game.game_name} ({game.appid}) так как ее цена стала выше допустимой - {Math.Round(oneItemPrice, 2)}");
                        continue; //пропускаем элемент если его цена увеличилась выше допустимой
                    }
                    #endregion

                    #region Оплата
                    Program.GetForm.MyMainForm.AddLog($"Покупка {count} игр {game.game_name} ({appid}) по {Math.Round(oneItemPrice, 2)} на сумму {allPrice}");
                    var totalPrice = allPrice.ToString().Replace(',', '.');
                    Qiwi qiwiAccount = new Qiwi(Program.GetForm.MyMainForm.QiwiTokenBox.Text);
                    var paymentDone = await qiwiAccount.SendMoneyToWallet(reciever, totalPrice, comment);
                    if (!paymentDone) throw new Exception($"Не удалось оплатить {reciever} {comment} {appid} {totalPrice} руб. {buyLink}");
                    File.AppendAllText("buylinks.txt", $"{DateTime.Now} - {buyLink}");
                    Database.WASTED_MONEY += allPrice;
                    UpdateWastedMoney();
                    Program.GetForm.MyMainForm.AddLog($"Оплачено {totalPrice} руб, на номер {reciever}");
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
                    Request.getResponse(buyLink, cookies);
                    var fileDownloaded = Request.DownloadFile(buyLink.Replace("/order/", "/order/get/") + "/saved/", cookies, "downloaded.txt");
                    //if (!fileDownloaded) throw new Exception($"Не удалось скачать файл {downloadLink}");
                    Thread.Sleep(1000);
                    var fileName = $"{appid} {game.game_name} - {DateTime.Now}";
                    fileName = fileName.Replace('.', '-');
                    fileName = fileName.Replace(':', '-');
                    Directory.CreateDirectory("keys");
                    File.Move("downloaded.txt", $"keys\\{fileName}.txt");
                    Program.GetForm.MyMainForm.AddLog($"Файл {fileName}.txt сохранен.");
                    Thread.Sleep(1000);
                    #endregion

                    #region Активация ключей
                    var keysList = File.ReadAllLines($"keys\\{fileName}.txt");
                    Program.GetForm.MyMainForm.AddLog($"Активация {keysList.Count()} ключей {game.game_name} ({appid})");
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
                                        keysResponse += "Ложный таймаут. OK/NoDetail";
                                }

                                if (keysResponse.Contains("OK/NoDetail") == false) {
                                    Program.GetForm.MyMainForm.AddLog($"Ошибка при активации ключей для {bot.vds},{bot.login},{key},{keysResponse.Replace('\r', ' ').Replace('\n', ' ')}");
                                    File.AppendAllText("UNUSEDKEYS.TXT", $"{bot.vds},{bot.login},{key},{keysResponse.Replace('\r', ' ').Replace('\n', ' ')}\n");
                                }
                                else {
                                    bot.gamesHave.Add(appid);
                                }
                                break;
                            }
                        }
                    }
                    Program.GetForm.MyMainForm.AddLog($"Ожидание 30 секунд до следующей покупки");
                    Thread.Sleep(30000);
                    Program.GetForm.MyMainForm.AddLog($"-----------------------------------");
                    #endregion
                }
                #endregion

            });
            Program.GetForm.MyMainForm.AddLog($"Покупки завершены");
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
                File.AppendAllText("log.txt", "\n" + text);
            }
        }
        public void BlockAll() {
            if (InvokeRequired)
                Invoke((Action)BlockAll);
            Program.GetForm.MyMainForm.groupBox1.Enabled = false;
            Program.GetForm.MyMainForm.groupBox2.Enabled = false;
            Program.GetForm.MyMainForm.BuyGamesButton.Enabled = false;
            Program.GetForm.MyMainForm.ActivateKeysButton.Enabled = false;
            Program.GetForm.MyMainForm.ActivateUnusedKeysButton.Enabled = false;
            Program.GetForm.MyMainForm.QIWIGroupBox.Enabled = false;
            Program.GetForm.MyMainForm.QIWIStartButton.Enabled = false;
            Program.GetForm.MyMainForm.QIWILoginsBox.Enabled = false;

        }
        public void UnblockAll() {
            if (InvokeRequired)
                Invoke((Action)UnblockAll);
            Program.GetForm.MyMainForm.groupBox1.Enabled = true;
            Program.GetForm.MyMainForm.groupBox2.Enabled = true;
            Program.GetForm.MyMainForm.BuyGamesButton.Enabled = true;
            Program.GetForm.MyMainForm.ActivateKeysButton.Enabled = true;
            Program.GetForm.MyMainForm.ActivateUnusedKeysButton.Enabled = true;
            Program.GetForm.MyMainForm.QIWIGroupBox.Enabled = true;
            Program.GetForm.MyMainForm.QIWIStartButton.Enabled = true;
            Program.GetForm.MyMainForm.QIWILoginsBox.Enabled = true;

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

        private void LootButton_Click(object sender, EventArgs e) {
            var unusedKeys = File.ReadAllLines("UNUSEDKEYS.TXT");
            foreach (var line in unusedKeys) {
                var data = line.Split(',');
                //{ bot.vds},{ bot.login},{ key},{ response.Replace('\r', ' ').Replace('\n', ' ')}
                var vds = data[0];
                var login = data[1];
                var key = data[1];
                var command = $"http://{vds}/IPC?command=!redeem {login} {key}";
                var response = Request.getResponse(command);
                Program.GetForm.MyMainForm.AddLog(response);
                if (response.Contains("OK/NoDetail") == false || response.Contains("RateLimited")) {
                    File.WriteAllText("UNUSEDKEYS.TXT", $"{vds},{login},{key},{response.Replace('\r', ' ').Replace('\n', ' ')}");
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            var settings = File.ReadAllText("settings.txt");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(settings);
            ApikeyBox.Text = json.SteamAPI;
            CouponsKeyBox.Text = json.SteamkeysAPI;
            MaxGameCostBox.Text = json.MaxGameCost;
            MaxMoneyBox.Text = json.MaxMoneySpent;
            EmailBox.Text = json.Email;
            CouponsKeyBox.Text = json.CouponsKey;
            QiwiTokenBox.Text = json.QiwiToken;
            QiwiTokenBox2.Text = json.QiwiToken;
            Database.KEY = json.LicenseKey;
            for (int i = 0; i < json.VDSs.Count; i++) {
                ServersRichTextBox.AppendText(json.VDSs[i].Value + "\n");
            }
            Database.BLACKLIST = new List<string>();
            for (int i = 0; i < json.BlacklistAppids.Count; i++) {
                Database.BLACKLIST.Add(json.BlacklistAppids[i].Value);
            }
            LogBox.Text = $"Программа запущена {System.DateTime.Now}\n";

            var uid = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                       where nic.OperationalStatus == OperationalStatus.Up
                       select nic.GetPhysicalAddress().ToString()).FirstOrDefault();
            uid = "0x485ab6c24e8e";
            Database.UID = uid;

            string check = $"uid={uid}&key={Database.KEY}";
            string[] ok;
            var postResponse = Request.POST("https://shamanovski.pythonanywhere.com", check, out ok);
            var postJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(postResponse);
            if (postJson.success == false) {
                DialogResult res = new DialogResult();
                res = MessageBox.Show("Проверка лицензии не пройдена!",
                                                 "Ошибка лицензии",
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
                        AddLog($"{VDS} - загрузка ботов начата");
                        Bot.AllBotsToDatabase(VDS);
                        Database.BOTS_LOADING.Add(true);
                        AddLog($"{VDS} - загрузка ботов завершена");
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
            Directory.CreateDirectory("activate");
            var files = Directory.GetFiles("activate");
            foreach (var file in files) {
                var appid = file.Split('\\')[1].Split('.')[0];
                var keys = File.ReadAllLines(file);
                for (int i = 0; i < keys.Count(); i++) {
                    if (keys[i] != String.Empty) {
                        foreach (var bot in Database.BOT_LIST) {
                            if (!bot.gamesHave.Contains(appid)) {

                                Regex regex = new Regex(@"\w{5}-\w{5}-\w{5}");
                                var key = regex.Match(keys[i]);
                                var command = $"http://{bot.vds}/IPC?command=";
                                command += $"!redeem {bot.login} {key}";
                                var response = Request.getResponse(command);
                                Program.GetForm.MyMainForm.AddLog($"{bot.vds} - {response}\n");
                                File.AppendAllText($"responses.txt", $"\n{DateTime.Now} {bot.vds} {bot.login} {appid} - {response}");
                                if (response.Contains("Timeout")) {
                                    Thread.Sleep(10000);
                                    var botResponse = Request.getResponse($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={Program.GetForm.MyMainForm.ApikeyBox.Text}&steamid={bot.steamID}&format=json");
                                    if (botResponse.Contains(appid))
                                        response += "Ложный таймаут. OK/NoDetail";
                                }
                                if (response.Contains("OK/NoDetail") == false || response.Contains("RateLimited")) {
                                    Program.GetForm.MyMainForm.AddLog($"Ошибка при активации ключей для {bot.vds} из {file}.txt\n{response}");
                                    //Thread.Sleep(Timeout.Infinite);
                                }
                                else {
                                    keys[i] = string.Empty;
                                    File.WriteAllText(file, keys.ToString());
                                    bot.gamesHave.Add(appid);
                                }
                                break;
                            }
                        }
                    }

                }
            }
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

                var inputBots = text.Replace("\r", "").Split('\n');

                int processStatus = 0;
                Qiwi qiwiAccount = new Qiwi(Program.GetForm.MyMainForm.QiwiTokenBox2.Text);
                var money = Program.GetForm.MyMainForm.QIWIDonateBox.Text.Replace(',', '.');
                foreach (var bot in inputBots) {
                    if (bot != String.Empty) {
                        var paymentDone = await qiwiAccount.SendMoneyToSteam(bot, money);
                        if (!paymentDone) {
                            Program.GetForm.MyMainForm.AddLog($"[{++processStatus}/{inputBots.Count()}] {bot} ОШИБКА ПОПОЛНЕНИЯ!");
                            break;
                        }
                        Program.GetForm.MyMainForm.AddLog($"[{++processStatus}/{inputBots.Count()}] {bot} пополнение на сумму {money} руб успешно проведено.");
                        Thread.Sleep(1111);
                    }
                }
            });
            UnblockAll();
        }

    }
}


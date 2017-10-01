using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System.Xml;
using System.Xml.XPath;
using OpenQA.Selenium.PhantomJS;
using System.Net;
using System.Text.RegularExpressions;

namespace Shatulsky_Farm {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        private async void StartButton_Click(object sender, EventArgs e) {
            BlockAll();

            #region Для Database
            await Task.Run(() => {
                Database.ALL_GAMES = Database.GetAllGamesList();
                Database.ALL_NEEDS_FOR_SHOP = new System.Collections.Hashtable();
                foreach (var item in Database.ALL_GAMES) {
                    Database.ALL_NEEDS_FOR_SHOP.Add(item, 0);
                }
                AddLog("Загрузка списка всех игр завершена");
                Database.BOT_LIST = new List<Bot>();
                Database.BOTS_LOADING = new List<bool>();
                Database.GAMES_LINKS_TO_BUY_UNSORTED = new Dictionary<string, double>();
                Database.WASTED_MONEY = 0;
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

            #region КАТАЛОГ
            await Task.Run(async () => {
                Program.GetForm.MyMainForm.AddLog("Начата обработка каталога");
                string games = "";
                foreach (DictionaryEntry item in Database.ALL_NEEDS_FOR_SHOP) {
                    if (item.Value.ToString() != "0")
                        games += $"{item.Key}:{item.Value};";
                }
                string CatalogKey = "";
                Invoke((ThreadStart)delegate () {
                    CatalogKey = Program.GetForm.MyMainForm.GotoCatalogBox.Text;
                });
                string linkToCalalog = $"http://steamkeys.ovh/?key={CatalogKey}&str=1&farm=SHATULSKY-FARM&app={games}";
                var CatalogPage = Request.getResponse(linkToCalalog);

                #region Настройка Browser
                var driverService = PhantomJSDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;
                var Browser = new OpenQA.Selenium.PhantomJS.PhantomJSDriver(driverService);
                #endregion

                Program.GetForm.MyMainForm.AddLog("Получение списка игр к покупке");
                Browser.Navigate().GoToUrl(linkToCalalog);
                var elements = Browser.FindElements(By.XPath(".//*[@id='DataTables_Table_0']/tbody/tr[*]/td[4]//*[@class='qckp']"));

                #region Ссылки на покупку игры
                foreach (var item in elements) {
                    var href = item.GetAttribute("href");
                    var gamePriceText = href.Split(new[] { "&price=" }, StringSplitOptions.None)[1].Split('&')[0].Replace('.', ',');
                    double gamePrice = double.Parse(gamePriceText);
                    if (gamePrice <= double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text.Replace('.', ','))) {
                        href = href.Replace("cupp", "rcupp");
                        href = href.Replace("count", "rcount");
                        href += "&ordernow=ordernow&typetype=4&remail=";
                        href += Program.GetForm.MyMainForm.EmailBox.Text;
                        Database.GAMES_LINKS_TO_BUY_UNSORTED.Add(href, gamePrice);
                    }
                }
                Database.GAMES_LINKS_TO_BUY = Database.GAMES_LINKS_TO_BUY_UNSORTED.OrderBy(x => x.Value);
                Program.GetForm.MyMainForm.AddLog($"Найдено {Database.GAMES_LINKS_TO_BUY.Count()} игр удовлетворяющих условию (<={Program.GetForm.MyMainForm.MaxGameCostBox.Text})");
                #endregion

                #region Покупка игр
                if (Database.GAMES_LINKS_TO_BUY.Count() > 0) {
                    Program.GetForm.MyMainForm.AddLog("Начата покупка игр");
                    foreach (var gameLink in Database.GAMES_LINKS_TO_BUY) {
                        Browser.Navigate().GoToUrl(gameLink.Key);

                        #region Если в магазине не хватило ключей
                        bool notEnought = false;
                        try { notEnought = Browser.FindElement(By.XPath("//*[text()[contains(.,'количества товара нет в наличии')]]")).Displayed; } catch { }
                        if (notEnought) {
                            var keysLeft = Browser.FindElement(By.XPath(".//*[@id='block']/p")).Text;
                            keysLeft = keysLeft.Split(new[] { "Доступно: " }, StringSplitOptions.None)[1].Split(' ')[0];
                            var newGameLink = gameLink.Key;
                            var oldGamecount = newGameLink.Split(new[] { "&rcount=" }, StringSplitOptions.None)[1].Split('&')[0];
                            newGameLink = newGameLink.Replace(oldGamecount, keysLeft);
                            Browser.Navigate().GoToUrl(newGameLink);
                        }
                        #endregion
                        bool blocked = false;

                        try {
                            blocked = Browser.FindElement(By.XPath("//*[text()[contains(.,'Товар заблокирован')]]")).Displayed;
                        } catch { }

                        try {
                            var appidForBlacklist = Browser.FindElement(By.XPath(".//*[@id='block']/h1/span[2]/span/b")).Text;
                            if (Database.BLACKLIST.Contains(appidForBlacklist)) blocked = true;
                        } catch { }

                        if (!blocked) {
                            var allPrice = double.Parse(Browser.FindElement(By.XPath(".//*[@id='mi']/p[2]/span")).Text.Split(' ')[0].Replace('.', ','));

                            if (Database.WASTED_MONEY + allPrice > double.Parse(Program.GetForm.MyMainForm.MaxMoneyBox.Text)) {
                                Program.GetForm.MyMainForm.AddLog($"Достигнуто ограничение на покупку. Потрачено {Database.WASTED_MONEY}");
                                break;
                            }
                            var count = double.Parse(Browser.FindElement(By.XPath(".//*[@id='mi']/p[5]/span")).Text);
                            var price = allPrice / count;
                            if (price < double.Parse(Program.GetForm.MyMainForm.MaxGameCostBox.Text.Replace('.', ','))) {
                                var appid = Browser.FindElement(By.XPath(".//*[@id='block']/h1/span[2]/span/b")).Text;
                                var reciever = Browser.FindElement(By.XPath(".//*[@id='mi']/p[6]/span/b")).Text;
                                var comment = Browser.FindElement(By.XPath(".//*[@id='mi']/p[7]/span/b")).Text;
                                Program.GetForm.MyMainForm.AddLog($"Покупка {count} игр ({appid}) по {Math.Round(price, 2)}");

                                #region Оплата
                                var totalPrice = allPrice.ToString().Replace(',', '.');
                                Qiwi qiwiAccount = new Qiwi(Program.GetForm.MyMainForm.QiwiTokenBox.Text);
                                var paymentDone = await qiwiAccount.SendMoneyToWallet(reciever, totalPrice, comment);
                                if (!paymentDone) throw new Exception($"Не удалось оплатить {reciever} {comment} {appid} {totalPrice}RUB {gameLink}");
                                Database.WASTED_MONEY += allPrice;
                                UpdateWastedMoney();
                                Program.GetForm.MyMainForm.AddLog($"Оплачено {totalPrice} руб, на номер {reciever}");
                                Thread.Sleep(5000);
                                #endregion

                                #region Загрузка файла
                                var downloadLink = GetDownloadLink(Browser);

                                try { File.Delete("downloaded.txt"); } catch { };
                                var fileDownloaded = Request.DownloadFile(downloadLink, Browser, "downloaded.txt");
                                if (!fileDownloaded) throw new Exception($"Не удалось скачать файл {downloadLink}");
                                Thread.Sleep(1000);
                                var fileName = $"{appid} - {DateTime.Now}";
                                fileName = fileName.Replace('.', '-');
                                fileName = fileName.Replace(':', '-');
                                File.Move("downloaded.txt", $"keys\\{fileName}.txt");
                                Program.GetForm.MyMainForm.AddLog($"Файл {fileName}.txt сохранен.");
                                Thread.Sleep(1000);
                                #endregion

                                #region Активация ключей
                                var keysList = File.ReadAllLines($"keys\\{fileName}.txt");
                                Program.GetForm.MyMainForm.AddLog($"Активация {keysList.Count()} ключей {appid}");
                                foreach (var line in keysList) {
                                    foreach (var bot in Database.BOT_LIST) {
                                        if (bot.gamesNeed.Contains(appid)) {
                                            Regex regex = new Regex(@"\w{5}-\w{5}-\w{5}");
                                            var key = regex.Match(line);
                                            var command = $"http://{bot.vds}/IPC?command=";
                                            command += $"!redeem^ {bot.login} SD,SF {key}";
                                            var response = Request.getResponse(command);
                                            File.AppendAllText($"responses.txt", $"\n{DateTime.Now} {bot.vds} {bot.login} {appid} {downloadLink} - {response}");

                                            if (response.Contains("Timeout")) {
                                                Thread.Sleep(10000);
                                                var botResponse = Request.getResponse($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={Program.GetForm.MyMainForm.ApikeyBox.Text}&steamid={bot.steamID}&format=json");
                                                if (botResponse.Contains(appid))
                                                    response += "Ложный таймаут. OK/NoDetail";
                                            }

                                            if (response.Contains("OK/NoDetail") == false) {
                                                Program.GetForm.MyMainForm.AddLog($"Ошибка при активации ключей для {bot.vds},{bot.login},{key},{response.Replace('\r', ' ').Replace('\n', ' ')}");
                                                File.AppendAllText("UNUSEDKEYS.TXT", $"{bot.vds},{bot.login},{key},{response.Replace('\r',' ').Replace('\n', ' ')}\n");
                                                //Thread.Sleep(Timeout.Infinite);
                                            }
                                            else {
                                                bot.gamesNeed.Remove(appid);
                                            }
                                            //Thread.Sleep(1500);
                                            break;
                                        }
                                    }
                                }
                                Program.GetForm.MyMainForm.AddLog($"Ожидание 30 секунд до следующей покупки");
                                Thread.Sleep(30000);
                                Program.GetForm.MyMainForm.AddLog($"-----------------------------");
                            }
                        }
                        else {
                            AddLog($"Пропускаем заблокированный товар {gameLink}");
                        }
                        #endregion
                    }
                }
                #endregion


            });
            #endregion

            UnblockAll();
        }

        private void MafileFolderButton_Click(object sender, EventArgs e) {
            FolderBrowserDialog MafileFolderBrowserDialog = new FolderBrowserDialog();

            if (MafileFolderBrowserDialog.ShowDialog() == DialogResult.OK) {
                ApikeyBox.Text = MafileFolderBrowserDialog.SelectedPath;
            }
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

        }
        public void UnblockAll() {
            if (InvokeRequired)
                Invoke((Action)UnblockAll);
            Program.GetForm.MyMainForm.groupBox1.Enabled = true;
            Program.GetForm.MyMainForm.groupBox2.Enabled = true;
            Program.GetForm.MyMainForm.BuyGamesButton.Enabled = true;
            Program.GetForm.MyMainForm.ActivateKeysButton.Enabled = true;
            Program.GetForm.MyMainForm.ActivateUnusedKeysButton.Enabled = true;
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

        public static string GetDownloadLink(PhantomJSDriver Browser) {
            int type = 0;
            string downloadLink = "";

            #region Тип страницы
            try {
                var test = Browser.FindElement(By.XPath(".//*[@id='dwnld']")).Displayed;
                type = 1;
            } catch { }

            try {
                var test = Browser.FindElement(By.XPath("//*[text()[contains(.,'К сожалению IP адрес покупателя и Ваш IP адрес не совпадают.')]]")).Displayed;
                type = 2;
            } catch { }

            #endregion

            switch (type) {
                case 0: {
                        throw new Exception("Неопознанная страницы оплаты");
                    }
                case 1: {
                        var doneLink = Browser.FindElement(By.XPath(".//*[@id='dwnld']")).GetAttribute("href");
                        Browser.Navigate().GoToUrl(doneLink);
                        File.AppendAllText("buylinks.txt", "\n" + doneLink);
                        var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(15));
                        try { wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("form-control"))); } catch {
                            Browser.Navigate().GoToUrl(doneLink);
                            wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("form-control")));
                        }

                        var emailField = Browser.FindElementByClassName("form-control");
                        emailField.SendKeys(Program.GetForm.MyMainForm.EmailBox.Text);
                        var nextBtn = Browser.FindElement(By.XPath(".//*[@class='input-group-btn']/input"));
                        nextBtn.Click();
                        downloadLink = Browser.FindElement(By.XPath(".//table/tbody/tr[6]/td[2]/a")).GetAttribute("href");
                        break;
                    }
                case 2: {
                        downloadLink = $"{Browser.Url}?email={Program.GetForm.MyMainForm.EmailBox.Text}";
                        File.AppendAllText("buylinks.txt", "\n" + downloadLink);
                        break;
                    }
            }

            return downloadLink;
        }

        private void MainForm_Load(object sender, EventArgs e) {
            LogBox.Text = $"Программа запущена {System.DateTime.Now}\n";
            var settings = File.ReadAllText("settings.txt");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(settings);
            ApikeyBox.Text = json.SteamAPI;
            KeysShopKey.Text = json.SteamkeysAPI;
            GotoCatalogBox.Text = json.GoToCatalogAPI;
            MaxGameCostBox.Text = json.MaxGameCost;
            MaxMoneyBox.Text = json.MaxMoneySpent;
            EmailBox.Text = json.Email;
            QiwiTokenBox.Text = json.QiwiToken;
            for(int i=0;i < json.VDSs.Count; i++) {
                ServersRichTextBox.AppendText(json.VDSs[i].Value+"\n");
            }
            Database.BLACKLIST = new List<string>();
            for (int i = 0; i < json.BlacklistAppids.Count; i++) {
                Database.BLACKLIST.Add(json.BlacklistAppids[i].Value);
            }
        }

        private async void ActivateKeysButton_Click(object sender, EventArgs e) {
            BlockAll();

            #region Для Database
            await Task.Run(() => {
                Database.ALL_GAMES = Database.GetAllGamesList();
                Database.ALL_NEEDS_FOR_SHOP = new System.Collections.Hashtable();
                foreach (var item in Database.ALL_GAMES) {
                    Database.ALL_NEEDS_FOR_SHOP.Add(item, 0);
                }
                AddLog("Загрузка списка всех игр завершена");
                Database.BOT_LIST = new List<Bot>();
                Database.BOTS_LOADING = new List<bool>();
                Database.GAMES_LINKS_TO_BUY_UNSORTED = new Dictionary<string, double>();
                Database.WASTED_MONEY = 0;
                Database.BLACKLIST = new List<string>();
                Database.BLACKLIST.Add("506670");
                Database.BLACKLIST.Add("581670");
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
            var files = Directory.GetFiles("activate");
            foreach (var file in files) {
                var appid = file.Split('\\')[1].Split('.')[0];
                var keys = File.ReadAllLines(file);
                for(int i=0; i< keys.Count(); i++) {
                    if (keys[i] != String.Empty) {
                        foreach (var bot in Database.BOT_LIST) {
                            if (bot.gamesNeed.Contains(appid)) {

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
    }
}




/*
 string Servers="";
 Invoke((ThreadStart)delegate () {
    Servers = Program.GetForm.MyMainForm.ServersRichTextBox.Text;
 });  
 */

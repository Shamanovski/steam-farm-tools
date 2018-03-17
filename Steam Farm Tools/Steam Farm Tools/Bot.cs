using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shatulsky_Farm {
    public class Bot {
        public string login;
        public string steamID;
        public string vds;
        public List<string> gamesHave;

        public Bot() {
            Program.GetForm.MyMainForm.IncreaseBotsCount();
        }

        public Bot(string login, string steamID, string VDS) {
            this.login = login;
            this.steamID = steamID;
            vds = VDS;

            #region Игры которые есть
            var response = Request.getResponse($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={Program.GetForm.MyMainForm.ApikeyBox.Text}&steamid={steamID}&format=json");
            if (response == "{\n\t\"response\": {\n\n\t}\n}") {
                Program.GetForm.MyMainForm.AddLog($"Bot parse error - {login} {VDS}");
            }
            else {
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                var games = json.response.games;
                gamesHave = new List<string>();
                if (games != null) {
                    foreach (var item in games) {
                        gamesHave.Add(item.appid.ToString());
                    }
                }
                #endregion
                Program.GetForm.MyMainForm.IncreaseBotsCount();
            }
            File.WriteAllText($"bots/{steamID}.json", JsonConvert.SerializeObject(this));
        }

        public Bot(string login, string VDS) {
            this.login = login;
            this.vds = VDS;
        }

        public static void AllBotsToDatabase(string VDS, bool loadFromFileAllowed = true) {
            Directory.CreateDirectory("bots");
            var botsFiles = new List<string>(Directory.GetFileSystemEntries("bots"));

            var command = $"http://{VDS}/";
            var response = Request.getResponse(command + "Api/Bot/asf");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            var bots = json["Result"];
            foreach (var bot in bots) {
                if (bot.SteamID.ToString() == "0") {
                    Program.GetForm.MyMainForm.AddLogBold($"SteamID=0 - {VDS} {bot.Name}");
                    continue;
                }

                if (botsFiles.Contains($"bots\\{bot.SteamID.Value.ToString()}.json") && loadFromFileAllowed) {
                    Bot botFromFile = JsonConvert.DeserializeObject<Bot>(File.ReadAllText($"bots/{bot.SteamID.Value.ToString()}.json"));
                    Database.BOT_LIST.Add(botFromFile);
                }
                else {
                    Database.BOT_LIST.Add(new Bot(bot.BotName.Value, bot.SteamID.Value.ToString(), VDS));
                }
            }
        }


        public static void AllBotsToDatabaseNoSteamID(string VDS) {
            var command = $"http://{VDS}/";
            var response = Request.getResponse(command + "Api/Bot/asf");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            var bots = json["Result"];
            foreach (var bot in bots) {
                Database.BOT_LIST.Add(new Bot(bot.Name, VDS));
            }
        }

        internal void UpdateFile() {
            string output = JsonConvert.SerializeObject(this);
            File.WriteAllText($"bots/{steamID}.json", JsonConvert.SerializeObject(this));
        }
    }
}

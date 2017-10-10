using System.Collections.Generic;

namespace Shatulsky_Farm {
    public class Bot {
        public string login;
        public string steamID;
        public string vds;
        public List<string> gamesHave;

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
                foreach (var item in games) {
                    gamesHave.Add(item.appid.ToString());
                }
                #endregion
                Program.GetForm.MyMainForm.IncreaseBotsCount();
            }
        }

        public Bot(string login, string VDS) {
            this.login = login;
            this.vds = VDS;
        }



        public static void AllBotsToDatabase(string VDS) {
            var command = $"http://{VDS}/IPC?command=";
            var response = Request.getResponse(command + "!api asf");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            var bots = json["Bots"];
            foreach (var bot in bots) {
                Database.BOT_LIST.Add(new Bot(bot.Name, bot.Value.SteamID.ToString(), VDS));
            }
        }

        public static void AllBotsToDatabaseNoSteamID(string VDS) {
            var command = $"http://{VDS}/IPC?command=";
            var response = Request.getResponse(command + "!api asf");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            var bots = json["Bots"];
            foreach (var bot in bots) {
                Database.BOT_LIST.Add(new Bot(bot.Name, VDS));
            }
        }
    }
}

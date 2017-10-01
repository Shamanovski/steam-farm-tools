using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shatulsky_Farm {
    public class Bot {
        public string login;
        public string steamID;
        public string vds;
        public List<string> gamesHave;
        public List<string> gamesNeed;

        public Bot(string login, string steamID, string VDS) {
            this.login = login;
            this.steamID = steamID;
            vds = VDS;

            #region Игры которые есть
            var response = Request.getResponse($"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={Program.GetForm.MyMainForm.ApikeyBox.Text}&steamid={steamID}&format=json");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            var games = json.response.games;
            gamesHave = new List<string>();
            foreach (var item in games) {
                gamesHave.Add(item.appid.ToString());
            }
            #endregion

            #region Игры которых нет
            gamesNeed = new List<string>();
            foreach (var game in Database.ALL_GAMES) {
                if (gamesHave.Contains(game) == false)
                    gamesNeed.Add(game);
            }
            #endregion

            #region Запись в общий список игр которых нет
            foreach (var game in gamesNeed) {
                Database.ALL_NEEDS_FOR_SHOP[game] = int.Parse(Database.ALL_NEEDS_FOR_SHOP[game].ToString()) + 1;
            }
            #endregion
            Program.GetForm.MyMainForm.IncreaseBotsCount();
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

        public static void AllBotsToDatabase(string VDS, string fl) {
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

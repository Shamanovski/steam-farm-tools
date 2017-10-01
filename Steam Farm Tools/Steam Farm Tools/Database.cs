using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Shatulsky_Farm {
    public static class Database {
        public static List<Bot> BOT_LIST;
        public static List<string> ALL_GAMES;
        public static List<bool> BOTS_LOADING;
        public static Hashtable ALL_NEEDS_FOR_SHOP;
        public static Dictionary<string, double> GAMES_LINKS_TO_BUY_UNSORTED;
        public static System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<string,double>> GAMES_LINKS_TO_BUY;
        public static double WASTED_MONEY;
        public static List<string> BLACKLIST;
        public static List<string> GetAllGamesList() {
            var allGamesList = new List<string>();
            var keyShopApi = Program.GetForm.MyMainForm.KeysShopKey.Text;
            var response = Request.getResponse($"http://217.182.170.100/JHukl9/api.php?key={keyShopApi}");
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
            foreach (var game in json.games) {
                allGamesList.Add(game.appid.ToString());
            }
            return allGamesList;
        }
    }
}

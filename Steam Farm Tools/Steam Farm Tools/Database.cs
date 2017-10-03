using System;
using System.Collections.Generic;

namespace Shatulsky_Farm {
    public static class Database {
        public static string KEY;
        public static List<Bot> BOT_LIST;
        public static List<bool> BOTS_LOADING;

        public static double WASTED_MONEY;
        public static List<string> BLACKLIST;

        public static Dictionary<string,Tuple<string,int>> COUPONS;
        public static List<Game> ALL_GAMES_LIST;

    }
    public class Game :IComparable<Game> {
        public string appid;
        public string lequeshop_id;
        public double price;
        public double amount;
        public string store;
        public string game_name;
        public int count = 0;

        public Game(string appid, string lequeshop_id, double price, double amount, string store, string game_name) {
            this.appid = appid;
            this.lequeshop_id = lequeshop_id;
            this.price = price;
            this.amount = amount;
            this.store = store;
            this.game_name = game_name;
        }

        public int CompareTo(Game other) {
            return price.CompareTo(other.price);
        }
    }
}

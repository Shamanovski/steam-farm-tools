using OpenQA.Selenium;
using QiwiApi.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shatulsky_Farm {
    static class Request {
        public static string FilePath { get; private set; }

        public static string getResponse(string uri) {
            System.Net.WebClient web = new System.Net.WebClient();
            web.Encoding = UTF8Encoding.UTF8;
            //string userAgentString = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            //web.Headers.Add("user-agent", userAgentString);
            string html = web.DownloadString(uri);
            return html;
        }
        public static bool DownloadFile(string url, IWebDriver driver, string filename) {
            try {
                // Construct HTTP request to get the file
               
                var client = new WebClient();
                for (int i = 0; i < driver.Manage().Cookies.AllCookies.Count; i++) {
                    client.Headers.Add(HttpRequestHeader.Cookie, $"{driver.Manage().Cookies.AllCookies[i].Name}={driver.Manage().Cookies.AllCookies[i].Value}");
                }
                client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
                client.DownloadFile(url, $"{filename}");

                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        public static string POST(string Url, string postData) {
            var request = (HttpWebRequest)WebRequest.Create(Url);

            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream()) {
                stream.Write(data, 0, data.Length);
            }

            var response1 = (HttpWebResponse)request.GetResponse();

            return new StreamReader(response1.GetResponseStream()).ReadToEnd();
        }
    }
}

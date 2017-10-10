using SteamAuth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Shatulsky_Farm {
    static class Request {

        public static string getResponse(string uri, string cookies1 = "") {
            System.Net.WebClient web = new System.Net.WebClient();
            web.Encoding = UTF8Encoding.UTF8;
            web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
            if (cookies1 != "")
                web.Headers.Add(HttpRequestHeader.Cookie, cookies1);
            string html = "";
            try { html = web.DownloadString(uri); } catch {
                System.Threading.Thread.Sleep(10000);
                html = getResponse(uri);
            }
            return html;
        }

        public static bool DownloadFile(string url, string cookies, string filename) {
            try {
                // Construct HTTP request to get the file

                var client = new WebClient();

                client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
                client.Headers.Add(HttpRequestHeader.Cookie, cookies);

                client.DownloadFile(url, $"{filename}");

                return true;
            } catch {
                return false;
            }
        }

        public static string POST(string Url, string postData, out string[] setCookies ) {
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

            var returnValue = new StreamReader(response1.GetResponseStream()).ReadToEnd();
            setCookies = response1.Headers.GetValues("Set-Cookie");


            return returnValue;
        }

        public static string cookiesPOST(string Url, string postData, out string[] setCookies, string cookies1 = "") {
            System.Net.WebClient web = new System.Net.WebClient();
            web.Encoding = UTF8Encoding.UTF8;
            web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
            if (cookies1 != "")
                web.Headers.Add(HttpRequestHeader.Cookie, cookies1);
            string response = "";
            try { response = web.UploadString(Url, postData); } catch {
                System.Threading.Thread.Sleep(10000);
                response = cookiesPOST(Url, postData, out setCookies, cookies1 = "");
            }
            setCookies = web.Headers.GetValues("Set-Cookie");
            return response;
        }

        public static string GetCatalog() {
            System.Net.WebClient web = new System.Net.WebClient();
            web.Encoding = UTF8Encoding.UTF8;
            web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
            web.Headers.Add("uid", Database.UID);
            web.Headers.Add("key", Database.KEY);
            string html = web.DownloadString("http://shamanovski.pythonanywhere.com/catalogue");
            return html;
        }
        
        public static string getSteamResponse(string url, IDictionary<string, string> cookieNameValues) {
            var encoding = Encoding.UTF8;
            using (var webClient = new WebClient()) {
                var uri = new Uri(url);
                var webRequest = WebRequest.Create(uri);
                foreach (var nameValue in cookieNameValues) {
                    webRequest.TryAddCookie(new Cookie(nameValue.Key, nameValue.Value, "/", uri.Host));
                }
                var response = webRequest.GetResponse();
                var receiveStream = response.GetResponseStream();
                var readStream = new StreamReader(receiveStream, encoding);
                var htmlCode = readStream.ReadToEnd();
                return htmlCode;
            }
        }
        public static bool TryAddCookie(this WebRequest webRequest, Cookie cookie) {
            HttpWebRequest httpRequest = webRequest as HttpWebRequest;
            if (httpRequest == null) {
                return false;
            }

            if (httpRequest.CookieContainer == null) {
                httpRequest.CookieContainer = new CookieContainer();
            }

            httpRequest.CookieContainer.Add(cookie);
            return true;
        }

    }
}

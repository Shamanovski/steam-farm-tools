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
            try { html = web.DownloadString(uri); } catch(Exception ex){
                addRequestLog(uri);
                System.Threading.Thread.Sleep(10000);
                html = getResponse(uri);
            }
            return html;
        }

        public static void addRequestLog(string uri) {
            try {
                File.AppendAllText("requests.txt", DateTime.Now + " - " + uri + "\n");
            } catch {
                addRequestLog(uri);
            }
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

        public static string POST(string Url, string postData, out string[] setCookies) {
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

        public static string GetCatalog() {
            System.Net.WebClient web = new System.Net.WebClient();
            web.Encoding = UTF8Encoding.UTF8;
            web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
            web.Headers.Add("uid", Database.UID);
            web.Headers.Add("key", Database.KEY);
            var catalogKey = Program.GetForm.MyMainForm.CatalogLicenseTextBox.Text;
            web.Headers.Add("catalogue-key", catalogKey);
            string html = web.DownloadString("http://shamanovski.pythonanywhere.com/catalogue");
            return html;
        }

        public static string getSteamResponse(string url, IDictionary<string, string> cookieNameValues, out CookieCollection storeCookies) {
            var encoding = Encoding.UTF8;
            using (var webClient = new WebClient()) {
                var uri = new Uri(url);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                foreach (var nameValue in cookieNameValues) {
                    webRequest.TryAddCookie(new Cookie(nameValue.Key, nameValue.Value, "/", uri.Host));
                }
                var response = webRequest.GetResponse();
                var receiveStream = response.GetResponseStream();
                var readStream = new StreamReader(receiveStream, encoding);
                var htmlCode = readStream.ReadToEnd();
                storeCookies = webRequest.CookieContainer.GetCookies(new Uri("https://store.steampowered.com"));
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


        public static string SendPostRequest(string url, string data, CookieCollection cookieNameValues) {
            string responseFromServer = "";
            try {
                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";
                string postData = data;
                request.ContentType = "application/x-www-form-urlencoded";
                foreach (var item in cookieNameValues) {
                    request.TryAddCookie((Cookie)item);
                }
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] postByteArray = encoding.GetBytes(postData);
                request.ContentLength = postByteArray.Length;

                System.IO.Stream postStream = request.GetRequestStream();
                postStream.Write(postByteArray, 0, postByteArray.Length);
                postStream.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine("Response Status Description: " + response.StatusDescription);
                Stream dataSteam = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataSteam);
                responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataSteam.Close();
                response.Close();
            } catch (Exception ex) {
                //Если что-то пошло не так, выводим ошибочку о том, что же пошло не так.
                Console.WriteLine("ERROR: " + ex.Message);
            }
            return responseFromServer;

        }



    }
}

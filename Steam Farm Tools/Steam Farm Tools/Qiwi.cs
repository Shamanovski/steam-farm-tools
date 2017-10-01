using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QiwiApi.Misc;
using QiwiApi.Requests;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shatulsky_Farm {
    public class Qiwi {
        private const string BASE = "https://edge.qiwi.com/";
        private string _token;
        private readonly WebClient _webClient;
        private FixedSizedQueue<long> _handledTransactions = new FixedSizedQueue<long>(50);

        public Qiwi(string token) {
            _token = token;
            _webClient = new WebClient {
                Encoding = Encoding.UTF8
            };
        }

        public async Task<bool> SendMoneyToWallet(string phone, string amount, string comment = null) {
            var request = new MoneyTransfer {
                Id = (1000 * DateTimeOffset.Now.ToUnixTimeSeconds()).ToString(),
                Sum = new Sum {
                    Amount = amount,
                    Currency = "643"
                },
                Source = "account_643",
                PaymentMethod = new PaymentMethod {
                    Type = "Account",
                    AccountId = "643"
                },
                Comment = comment,
                Fields = new Fields {
                    Account = "+" + phone
                }
            };
            _webClient.Headers["Authorization"] = $"Bearer {_token}";
            _webClient.Headers["Content-Type"] = "application/json";
            var url = BuildUrl("sinap/terms/99/payments");
            try {
                var response = await _webClient.UploadStringTaskAsync(url, JsonConvert.SerializeObject(request, new JsonSerializerSettings {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }));
                return response.Contains("Accepted");
            } catch (Exception ex) {
                return false;
            }
        }

        private static Uri BuildUrl(string additionalUrl, Dictionary<string, string> parameters = null) {
            var urlBuilder = new UriBuilder(BASE + additionalUrl);
            if (parameters != null) {
                urlBuilder.Query = string.Join("&", parameters.Select(kvp =>
                    string.Format("{0}={1}", kvp.Key, kvp.Value)));
            }
            return urlBuilder.Uri;
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace OpenTrader
{
    public class Sharesies
    {
        Cookie cookie;
        CookieCollection cookieCollection;
        JObject details;
        JObject check;
        JToken user;
        string cookieString;
        ArrayList cookieList;
        public const double epsilon = 0.00001;

        string SessionCookie { get => (string) cookieList[1];  }

        string DistillToken { get; set; }

        const string authUrl = "https://app.sharesies.nz/api/identity/login";
        const string checkUrl = "https://app.sharesies.nz/api/identity/check";
        const string statsUrl = "https://app.sharesies.nz/api/accounting/stats-v3";
        const string fundListUrl = "https://app.sharesies.nz/api/fund/list";
        const string transHistUrl = "https://app.sharesies.nz/api/accounting/transaction-history";
        const string fundHistUrl = "https://app.sharesies.nz/api/fund/price-history?first=0001-01-01";
        const string fundSellUrl = "https://app.sharesies.nz/api/fund/sell";
        const string instrumentsUrl = "https://data.sharesies.nz/api/v1/instruments";

        public Sharesies()
        {
        }

        string email, password;

        [DataContract]
        public class Credentials
        {
            [DataMember]
            public string email { get; set; }
            [DataMember]
            public string password { get; set; }
            [DataMember]
            public bool remember { get; set; }
        }

        public class Fund
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
        }

        [DataContract]
        public class Sell
        {
            [DataMember]
            public string fund_id { get; set; }
            [DataMember]
            public double shares { get; set; }
            [DataMember]
            public string acting_as_id { get; set; }
        }

        public List<Fund> funds;

        public class Transaction
        {
            public string Code { get; set; }
            public double Price { get; set; }
            public double Fee { get; set; }
            public double Quantity { get; set; }
            public string Contract { get; set; }
            public DateTime Date { get; set; }
        }

        Cookie ParseCookie(string cookieString)
        {
            string domain;
            var elements = cookieString.Split(';');
            var cookie = new Cookie();
            for (int i = 0; i < elements.Length; i++)
            {
                var keyvalue = elements[i].Split('=');
                if (i == 0)
                {
                    cookie.Name = keyvalue[0];
                    cookie.Value = keyvalue[1];
                }
                else
                {
                    switch (keyvalue[0].ToLower().Trim())
                    {
                        case "expires": cookie.Expires = DateTime.Parse(keyvalue[1]); break;
                        case "path": cookie.Path = keyvalue[1]; break;
                        case "domain":
                            cookie.Domain = keyvalue[1];
                            break;
                        case "secure": cookie.Secure = true; break;
                        case "httponly": cookie.HttpOnly = true; break; 
                    }
                }
            }
            return cookie;
        }


        private static ArrayList ConvertCookieHeaderToArrayList(string strCookHeader)
        {
            strCookHeader = strCookHeader.Replace("\r", "");
            strCookHeader = strCookHeader.Replace("\n", "");
            string[] strCookTemp = strCookHeader.Split(',');
            ArrayList al = new ArrayList();
            int i = 0;
            int n = strCookTemp.Length;
            while (i < n)
            {
                if (strCookTemp[i].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    al.Add(strCookTemp[i] + "," + strCookTemp[i + 1]);
                    i = i + 1;
                }
                else
                {
                    al.Add(strCookTemp[i]);
                }
                i = i + 1;
            }
            return al;
        }

        public async Task Reauthenticate()
        {

        }

        public async Task<string> Authenticate( string email, string password )
        {
            this.email = email;
            this.password = password;

            var request = System.Net.WebRequest.Create(authUrl);
            request.ContentType = "application/json";
            request.Method = "post";

            Credentials credentials = new Credentials()
            {
                email = email,
                password = password,
                remember = true
            };

            string json;
            DataContractJsonSerializer serializer;
            serializer = new DataContractJsonSerializer(typeof(Credentials));
            using (var memory = new MemoryStream())
            {
                serializer.WriteObject(memory, credentials);
                json = System.Text.Encoding.ASCII.GetString(memory.ToArray());
            }

            var encoding = new ASCIIEncoding();
            byte[] bytes = encoding.GetBytes(json);
            Stream stream = await request.GetRequestStreamAsync();
            stream.Write(bytes, 0, bytes.Length);
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch(Exception e)
            {
                DebugHelper.WriteLine(e);
            }
            cookieString = response.Headers["set-cookie"];
            cookieList = ConvertCookieHeaderToArrayList(cookieString);

            cookieCollection = new CookieCollection();
            foreach(var item in cookieList)
            {
                var trimmed = (item as string).TrimStart();
                cookieCollection.Add(ParseCookie((string) trimmed));
            }

            var responseStream = response.GetResponseStream();
            using (var reader = new StreamReader(responseStream))
            {
                json = await reader.ReadToEndAsync();
            }
            details = JObject.Parse(json);
            _ = details.TryGetValue("user", out user);
            return json;
        }

        public async Task<string> Check()
        {
            var id = user["id"].ToString();
            string url = checkUrl;
            var request = WebRequest.Create(url);
            request.Method = "get";
            request.Headers.Add(HttpRequestHeader.Cookie, (string) cookieList[1]);

            (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";

            WebResponse response = null;
            try
            {
                response = await request.GetResponseAsync();
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine(e);
            }
            string json = "";
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                json = await reader.ReadToEndAsync();
            }

            check = JObject.Parse(json);
            JToken _distill_token, _ga_id;
            _ = check.TryGetValue("distill_token", out _distill_token);
            DistillToken = (string)_distill_token;
            _ = check.TryGetValue("ga_id", out _ga_id);

            return json;
        }

        public async Task<List<Fund>> FundList()
        {
            var id = user["id"].ToString();
            string url = fundListUrl;
            var request = WebRequest.Create(url);
            request.Method = "get";


            (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";

            WebResponse response = null;
            try
            {
                response = await request.GetResponseAsync();
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine(e);
            }
            string json = "";
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                try
                {
                    json = await reader.ReadToEndAsync();
                }
                catch (Exception e)
                {
                    DebugHelper.WriteLine(e);
                }
            }

            details = JObject.Parse(json);
            JToken _funds;
            _ = details.TryGetValue("funds", out _funds);
            funds = new List<Fund>();
            foreach(var _fund in _funds)
            {
                Fund fund = new Fund()
                {
                    Id = (string)_fund["id"],
                    Name = (string)_fund["name"],
                    Code = (string)_fund["code"],
                };
                funds.Add(fund);
            }
            return funds;
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            return epoch.AddMilliseconds(unixTime);
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public string CookieString
        {
            get {
                if (cookieList == null || cookieList.Count<2)
                    return null;
                else
                    return (string)cookieList[1];
            }
        }

        /*
        {
	        "fund_id": "cfb37118-e4b8-4333-b199-47ea7d4592d5",
	        "shares": "125.949367",
	        "acting_as_id": "c1721cae-a96a-49f0-9107-e7dea53cce03"
        }
        */

        public async Task FundSell(string fundId, double shares)
        {
            string url = fundSellUrl;
            var request = WebRequest.Create(url);
            request.Method = "post";

            request.Headers.Add(HttpRequestHeader.Cookie, (string)cookieList[1]);

            var sell = new Sell()
            {
                fund_id = fundId,
                shares = shares,
                acting_as_id = user["id"].ToString()
            };

            string json;
            DataContractJsonSerializer serializer;
            serializer = new DataContractJsonSerializer(typeof(Sell));
            using (var memory = new MemoryStream())
            {
                serializer.WriteObject(memory, sell);
                json = System.Text.Encoding.ASCII.GetString(memory.ToArray());
            }

            var encoding = new ASCIIEncoding();
            byte[] bytes = encoding.GetBytes(json);
            Stream stream = await request.GetRequestStreamAsync();
            stream.Write(bytes, 0, bytes.Length);
            var response = (HttpWebResponse)await request.GetResponseAsync();
        }

        public async Task<List<OpenTrader.Data.Trade>> TransactionHistory(int limit=50)
        {
            var id = user["id"].ToString();
            string url = transHistUrl + "?&acting_as_id=" + id +"&since=0&limit=50";
            var request = WebRequest.Create(url);
            request.Method = "get";

            request.Headers.Add(HttpRequestHeader.Cookie, (string)cookieList[1] ) ;

            (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";

            WebResponse response = null;
            try
            {
                response = await request.GetResponseAsync();
            }
            catch(Exception e)
            {
                DebugHelper.WriteLine(e);
            }
            string json = "";
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                try
                {
                    json = await reader.ReadToEndAsync();
                }
                catch(Exception e)
                {
                    DebugHelper.WriteLine(e);
                }
            }

            details = JObject.Parse(json);
            JToken _transactions;
            _ = details.TryGetValue("transactions", out _transactions);

            var trades = new List<OpenTrader.Data.Trade>();

            // int i = -1;
            foreach(var _transaction in _transactions)
            {
                string jsonT = _transaction.ToString();
                try
                {
                    Fund fund = null;
                    double amount = 0;
                    double sign = 0;
                    JToken jTrade = null;
                    JToken jFundId = null;
                    JToken jTrades;
                    _ = (_transaction as JObject).TryGetValue("trades", out jTrades);
                    foreach (var pair in (JObject)_transaction)
                    {
                        string key = pair.Key;
                        var value = pair.Value;
                        switch (key)
                        {
                            case "fund_id":
                                jFundId = value;
                                fund = funds.Find(f => f.Id == (string) value);
                                break;
                            case "amount":
                                amount = double.Parse((string)value);
                                break;
                            case "balance":
                            case "csn_transfer_order":
                            case "currency":
                            case "description":
                            case "fx_order":
                            case "memo":
                            case "order_id":
                            case "reason":
                            case "timestamp":
                            case "transaction_id":
                            case "withdrawal_order":
                            case "line_number":
                                break;
                            case "buy_order":
                                if (value.HasValues)
                                {
                                    jTrades = value["trades"];
                                    sign = 1;
                                }
                                break;
                            case "sell_order":
                                if (value.HasValues)
                                {
                                    jTrades = value["trades"];
                                    sign = -1;
                                }
                                break;
                            case "trades":
                                break;
                            case "trade":
                                jTrade = value;
                                break;
                            default:
                                DebugHelper.WriteLine(key);
                                break;
                        }
                    }

                    if (fund == null || sign == 0 || (jTrade == null && jTrades == null) )
                        continue;
                    

                    if (jTrade != null && jTrade.HasValues)
                    {
                        var _trade = jTrade;
                        var secondsString = (string)_trade["trade_datetime"]["$quantum"];
                        var quantity = double.Parse((string)_trade["volume"]);
                        long seconds = long.Parse(secondsString);
                        var date = FromUnixTime(seconds).ToLocalTime();
                        var trade = new OpenTrader.Data.Trade()
                        {
                            Fee = double.Parse((string)_trade["corporate_fee"]),
                            Price = double.Parse((string)_trade["share_price"]),
                            Quantity = quantity * sign,
                            YahooCode = fund.Code + ".NZ",
                            Date = date
                        };
                        trades.Add(trade);
                    }

                    if (jTrades != null && (jTrade == null || ! jTrade.HasValues))
                    {
                        foreach (var _trade in jTrades)
                        {
                            var secondsString = (string)_trade["trade_datetime"]["$quantum"];
                            var quantity = double.Parse((string)_trade["volume"]);
                            long seconds = long.Parse(secondsString);
                            var date = FromUnixTime(seconds).ToLocalTime();
                            var trade = new OpenTrader.Data.Trade()
                            {
                                Fee = double.Parse((string)_trade["corporate_fee"]),
                                Price = double.Parse((string)_trade["share_price"]),
                                Quantity = quantity * sign,
                                YahooCode = fund.Code + ".NZ",
                                Date = date
                            };
                            trades.Add(trade);
                        }
                    }
                }
                catch(Exception e)
                {
                    DebugHelper.WriteLine(e);
                }
            }


           return trades;
        }

        public class Instrument
        {
            public string Symbol { get; set; }
            public string Description { get; set; }
            public string Name { get; set; }
            public string Exchange { get; set; }
        }

        public async Task<List<Instrument>> InstrumentList()
        {
            List<Instrument> instruments = new List<Instrument>();

            var id = user["id"].ToString();

            int page = 1;
            int pages = 0;
            int total;
            do
            {
                string url = instrumentsUrl + "?Page=" + page.ToString() + "&Sort=marketCap&PriceChangeTime=1y&Query=";
                var request = WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + DistillToken);
                var cfduid = (string)cookieList[0];
                request.Headers.Add(HttpRequestHeader.Cookie, cfduid);

                (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                (request as HttpWebRequest).Referer = "https://app.sharesies.nz/invest/search";

                WebResponse response = null;
                try
                {
                    response = await request.GetResponseAsync();
                }
                catch (Exception e)
                {
                    DebugHelper.WriteLine(e);
                }
                string json = "";
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    try
                    {
                        json = await reader.ReadToEndAsync();
                    }
                    catch (Exception e)
                    {
                        DebugHelper.WriteLine(e);
                    }
                }

                var record = JObject.Parse(json);
                JToken _instruments, _total, _pages;
                _ = record.TryGetValue("instruments", out _instruments);
                _ = record.TryGetValue("total", out _total);
                total = (int)_total.Value<int>();
                _ = record.TryGetValue("numberOfPages", out _pages);
                pages = (int)_pages.Value<int>();

                foreach (var _instrument in _instruments)
                {
                    Instrument instrument = new Instrument();
                    foreach (var pair in (JObject)_instrument)
                    {
                        string key = pair.Key;
                        var value = pair.Value;
                        switch (key)
                        {
                            case "symbol":
                                instrument.Symbol = (string)value;
                                break;
                            case "name":
                                instrument.Name = (string)value; ;
                                break;
                            case "description":
                                instrument.Description = (string)value;
                                break;
                            case "exchange":
                                instrument.Exchange = (string)value;
                                break;
                        }
                    }
                    instruments.Add(instrument);
                }

                page++;
            } while (page <= pages);
            return instruments;
        }
    }
}

using OpenTrader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenTrader
{
    /// <summary>
    /// Used to process soap calls to cloud storage
    /// </summary>
    class SoapClient
    {
        static string user, password, machine;

        public static string User { get => user; set { user = value; } }
        public static string Password { get => password; set { password = value; } }
        public static string Machine { get => machine; set { machine = value; } }

        static System.Net.WebRequest CreateRequest()
        {
            var request = (System.Net.WebRequest)System.Net.WebRequest.Create(@"https://mikaelaldridge.com/soap/opentrader.php");
            request.ContentType = "text/xml;charset=\"utf-8\"";
            // request.Accept = "text/xml";
            request.Method = "POST";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls; // | SecurityProtocolType.Ssl3;

            return request;
        }


        static string CreateSoapString(string function, Dictionary<string, string> parameters)
        {
            string xmlString = @"<?xml version=""1.0"" encoding=""utf-8""?>
                            <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Body>
                    <" + function + @">  
                      <user>" + user + @"</user>
                      <password>" + password + @"</password>
                      <machine>" + machine + @"</machine>";
            foreach (var parameter in parameters)
            {
                xmlString += @"<" + parameter.Key + ">" + parameter.Value + @"</" + parameter.Key + ">";
            }
            xmlString += @"</" + function + @">
                  </soap:Body>
                </soap:Envelope> ";
            return xmlString;
        }

        static XDocument CreateDocument(string function, XElement[] parameters)
        {
            XElement[] fullParameters = new XElement[parameters.Length + 3];
            fullParameters[0] = new XElement("user", user);
            fullParameters[1] = new XElement("password", password);
            fullParameters[2] = new XElement("machine", machine);
            for (int i = 0; i < parameters.Length; i++)
            {
                fullParameters[i + 3] = parameters[i];
            }

            XDocument document = null;
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

            try
            {
                document = new XDocument(
                    new XDeclaration("1.0", "utf-8", String.Empty),
                    new XElement(soapenv + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                        new XElement(soapenv + "Header"),
                        new XElement(soapenv + "Body",
                            new XElement(function, fullParameters))
                    )
                 );
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return document;
        }

        static XDocument GetResponse(WebRequest request, string body)
        {
            // byte[] buf = System.Text.Encoding.UTF8.GetBytes(body.ToCharArray());
            if (body != null && body != "")
            {
                request.ContentLength = body.Length;
                try
                {
                    using (StreamWriter writer = new(request.GetRequestStream(), System.Text.Encoding.ASCII))
                    {
                        writer.Write(body);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            HttpWebResponse response = null;
            string content = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                content = new StreamReader(response.GetResponseStream()).ReadToEnd();
                content = content.Replace("\n", "");
            }
            catch (WebException wex)
            {
                content = new StreamReader(wex.Response.GetResponseStream())
                      .ReadToEnd();
                throw (wex);
            }
            var pos = content.IndexOf("<soap:Envelope");
            // content = content.Substring(pos);
            XDocument xml = XDocument.Parse(content);
            return xml;
        }

        async static Task<XDocument> GetResponse(WebRequest request, XDocument document)
        {
            using (var stream = request.GetRequestStream())
            {
                document.Save(stream);
            }

            string content = "";
            try
            {
                using WebResponse response = request.GetResponse();
                using var rd = new StreamReader(response.GetResponseStream());
                //reading stream  
                content = rd.ReadToEnd();
                content = content.Replace("\n", "");
                //writting stream result on console  

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            XDocument xml = XDocument.Parse(content);
            return xml;
        }

        static XDocument SoapRequest(string function, Dictionary<string, string> parameters)
        {
            var request = CreateRequest();
            // request.ServerCertificateValidationCallback = delegate { return true; };
            string body = CreateSoapString(function, parameters);
            XDocument xml = GetResponse(request, body);
            return xml;
        }

        async static Task<XDocument> SoapRequest(string function, XElement[] parameters)
        {
            var request = CreateRequest();
            // request.ServerCertificateValidationCallback = delegate { return true; };
            var document = CreateDocument(function, parameters);
            XDocument xml = await GetResponse(request, document);
            return xml;
        }

        public static List<Transaction> GetTransactions(string id, string limit)
        {
            /*
            var parameters = new XElement[] {
                new XElement("id", id),
                new XElement("limit", limit)
            };
            */

            var parameters = new Dictionary<string, string>() { { "id", id }, { "limit", limit } };
            XDocument xml = SoapRequest("getTransactions", parameters);
            // XDocument xml = await SoapRequest("getTransactions", parameters);

            var _transactions = xml.Descendants().Where(x => x.Name.LocalName == "item").Select(x => new Transaction()
            {
                Id = int.Parse((string)x.Element("id")),
                RecordId = int.Parse((string)x.Element("recordId")),
                TimeStamp = DateTime.Parse((string)x.Element("timestamp")),
                FileName = (string)x.Element("tableName"),
                Machine = (string)x.Element("machine"),
                Method = (string)x.Element("method"),
                Data = (string)x.Element("data"),
            });

            List<Transaction> transactions = new();


            try
            {
                foreach (Transaction transaction in _transactions)
                {

                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }


            return transactions;
        }


        public static List<Transaction> GetTransactionsFromTime(string timeStamp, string limit)
        {
            /*
            var parameters = new XElement[] {
                new XElement("id", id),
                new XElement("limit", limit)
            };
            */

            var parameters = new Dictionary<string, string>() { { "time", timeStamp }, { "limit", limit } };
            XDocument xml = SoapRequest("getTransactionsFromTime", parameters);
            // XDocument xml = await SoapRequest("getTransactions", parameters);

            var _transactions = xml.Descendants().Where(x => x.Name.LocalName == "item").Select(x => new Transaction()
            {
                Id = int.Parse((string)x.Element("id")),
                TimeStamp = DateTime.Parse((string)x.Element("timestamp")),
                FileName = (string)x.Element("tableName"),
                Machine = (string)x.Element("machine"),
                Method = (string)x.Element("method"),
                Data = (string)x.Element("data"),
            });

            List<Transaction> transactions = _transactions.ToList();

            return transactions;
        }

        public static System.Collections.Generic.IEnumerable<(int affectedRows, string update, bool success)> Count()
        {
            var lastTimeStamp = Preference.Get(Preference.LastTimeStamp);
            var request = (System.Net.WebRequest)System.Net.WebRequest.Create(@"https://mikaelaldridge.com/api/lifeforce/count.php?machine=" + machine + "&timestamp=" + System.Web.HttpUtility.UrlEncode(lastTimeStamp.Value));
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Headers.Add("user", user);
            request.Headers.Add("password", password);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

            XDocument xml = GetResponse(request, "");
            var result = new List<(int affectedRows, string update, bool success)>();
            int affectedRows = 0;
            string update = "";
            bool success = false;
            var first = xml.FirstNode as XElement;
            if (first != null)
            {
                first = first.FirstNode as XElement;
            }
            else
            {
                return result;
            }
            var descendants = first.Descendants();
            foreach (var node in descendants)
            {
                if (node is XElement element)
                {
                    if (element.Name == "result")
                    {
                        var children = element.Nodes();
                        foreach (XElement child in children.Cast<XElement>())
                        {
                            switch (child.Name.LocalName)
                            {
                                case "affectedRows":
                                    int.TryParse(child.Value, out affectedRows);
                                    break;
                                case "update":
                                    update = child.Value;
                                    break;
                                case "success":
                                    bool.TryParse(child.Value, out success);
                                    break;
                            }
                        }
                        result.Add((affectedRows, update, success));
                        return result;
                    }
                    break;
                }
            }

            if (result.Count == 0)
            {
                result.Add((affectedRows, update, success));
            }

            return result;
        }

        public static List<(int affectedRows, string update, bool success)> AddTransactions(List<Transaction> transactions) // async static Task<System.Collections.Generic.IEnumerable<
        {
            var request = (System.Net.WebRequest)System.Net.WebRequest.Create(@"https://mikaelaldridge.com/api/lingvo/transactions.php?machine=" + machine);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Headers.Add("user", user);
            request.Headers.Add("password", password);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls; // | SecurityProtocolType.Ssl3;

            // request.ServerCertificateValidationCallback = delegate { return true; };

            string body = @"[";
            bool isNext = false;
            foreach (var transaction in transactions)
            {
                if (isNext)
                {
                    body += ",";
                }
                else
                {
                    isNext = true;
                }

                var data = System.Text.Json.JsonEncodedText.Encode(transaction.Data);

                body += "{\"tableName\":\"" + transaction.FileName + "\"," +
                 "\"method\":\"" + transaction.Method + "\"," +
                 "\"data\":\"" + data + "\"}";
            }
            body += @"]";

            XDocument xml = GetResponse(request, body);

            var results = xml.Descendants().Where(x => x.Name.LocalName == "result").Select(x => (
                 affectedRows: int.Parse((string)x.Element("affectedRows")),
                 update: (string)x.Element("update"),
                 success: bool.Parse((string)x.Element("success"))
            ));

            results.First();
            return results.ToList();
        }

        async public static Task<System.Collections.Generic.IEnumerable<(int affectedRows, string update, bool success)>> AddTransaction(Transaction transaction)
        {
            var parameters = new XElement[]
            {
                new XElement("tableName", transaction.FileName),
                new XElement("method", transaction.Method),
                new XElement("data", transaction.Data)
            };

            XDocument xml = await SoapRequest("addTransaction", parameters);

            var results = xml.Descendants().Where(x => x.Name.LocalName == "result").Select(x => (
                 affectedRows: int.Parse((string)x.Element("affectedRows")),
                 update: (string)x.Element("update"),
                 success: bool.Parse((string)x.Element("success"))
            ));

            results.First();
            return results;
        }

        async public static Task<string> GetRecord(string tableName, string id)
        {
            var parameters = new XElement[]
            {
                new XElement("tableName", tableName),
                new XElement("id", id)
            };

            XDocument xml = await SoapRequest("getRecord", parameters);

            // var _transactions = xml.Where(x => x.Name.LocalName == "data").Select(x => (string)x.Element("data"));
            // var transactions = _transactions.ToList();

            var reader = xml.DescendantNodes().ElementAt(3).CreateReader();
            reader.MoveToContent();

            var data = reader.ReadInnerXml();
            return data;
        }
    }
}

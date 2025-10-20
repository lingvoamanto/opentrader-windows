using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.IO;
using System.Windows.Markup;
using System.Text.RegularExpressions;
using OpenCompiler;
using Newtonsoft.Json.Linq;
using static OpenTrader.Sharesies;
using System.Diagnostics;
using Accord.Audio;
using Newtonsoft.Json;
#if __MACOS__
using Mono.Data.Sqlite;
#endif
#if __WINDOWS__
using System.Data.SQLite;
#endif

namespace OpenTrader.Data
{
    [DataContract]
    public partial class Dividend : CloudObject<Dividend>, ICloudRepository<Dividend>
    {
        public Dividend()
        {
            Payable = DateTime.Now;
        }

        #region properties
        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string YahooCode { get; set; }
        [DataMember]
        public DateTime ExDividend { get; set; }
        [DataMember]
        public string Period { get; set; }
        [DataMember]
        public double Amount { get; set; }
        [DataMember]
        public double Supp { get; set; }
        [DataMember]
        public double Imputation { get; set; }
        [DataMember]
        public DateTime Payable { get; set; }

#endregion properties

#region database
        static string tableName = "dividends";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
            new Repository.Column( "Guid", "guid", "guid", "" ),
            new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
            new Repository.Column( "ExDividend", "ex_dividend", "DateTime", "" ),
            new Repository.Column( "Period", "period", "string", "(7)" ),
            new Repository.Column( "Amount", "amount", "double", "" ),
            new Repository.Column( "Supp", "supp", "double", "" ),
            new Repository.Column( "Imputation", "imputation", "double", "" ),
            new Repository.Column( "Payable", "payable", "DateTime", "" ),
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
#endregion database

#region required methods
        public bool Save(bool syncing=false) =>
           Save(tableName, tableColumns, syncing);
        public void Remove(bool syncing = false) =>
            Repository.Remove(this, tableName, syncing);
        static public Dividend Get(int id) =>
            Repository.Get(typeof(Dividend), tableName, tableColumns, id) as Dividend;
        static public List<Dividend> GetAll() => GetAll(tableName, tableColumns);

        override public string Serialise()
        {
            var sb = new System.Text.StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("Guid");
                writer.WriteValue(Guid.ToString());
                writer.WritePropertyName("YahooCode");
                writer.WriteValue(YahooCode);
                writer.WritePropertyName("ExDividend");
                writer.WriteValue((ExDividend - DateTime.UnixEpoch).TotalSeconds);
                writer.WritePropertyName("Period");
                writer.WriteValue(Period);
                writer.WritePropertyName("Amount");
                writer.WriteValue(Amount);
                writer.WritePropertyName("Supp");
                writer.WriteValue(Supp);
                writer.WritePropertyName("Imputation");
                writer.WriteValue(Imputation);
                writer.WritePropertyName("Payable");
                writer.WriteValue((Payable - DateTime.UnixEpoch).TotalSeconds);

                writer.WriteEndObject();

                // { "Amount":1.5,
                // "ExDividend":"\/Date(1583924400000+1300)\/",
                // "Id":27,"Imputation":0,
                // "Payable":"\/Date(1601377200000+1300)\/",
                // "Period":"Interim",
                // "Supp":0,
                // "YahooCode":"MHJ.NZ"}

            }
            return sb.ToString();
        }

        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }
#endregion required methods

#region additional methods

        internal static List<Dividend> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, string value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<Dividend> results = new List<Dividend>();
#if __MACOS__
            SqliteCommand command = Repository.Connection.CreateCommand();
#endif
#if __WINDOWS__
            SQLiteCommand command = Repository.Connection.CreateCommand();
#endif
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "'";
            // DebugHelper.WriteLine("CommandText = " + command.CommandText);
#if __MACOS__
            SqliteDataReader reader = command.ExecuteReader();
#endif
#if __WINDOWS__
            SQLiteDataReader reader = command.ExecuteReader();
#endif

            while (reader.Read())
            {
                Dividend result = new Dividend();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }

        static public List<Dividend> GetYahooCode(string value) 
        {
            List<Dividend> dividends = GetEquals(tableName, tableColumns, "YahooCode", value);
            dividends.OrderByDescending(x=>x.Supp);
            return dividends;
        }

        public static List<Dividend> ReadFromDividendDates(string yahooCode)
        {
            if (string.IsNullOrEmpty(yahooCode) || yahooCode.Length <= 3)
                return new List<Dividend>();
            if (yahooCode.Substring(yahooCode.Length - 3) != ".AX")
                return new List<Dividend>();

            var name = yahooCode.Substring(0, yahooCode.Length - 3);

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionOutputAsXml = true;

            using (var client = new System.Net.WebClient())
            {
                try
                {
                    using (var reader = client.OpenRead("https://www.dividenddates.com.au/" + name.ToLower() + "-dividend-history"))
                    {
                        htmlDoc.Load(reader);
                    }
                }
                catch
                {
                    return new List<Dividend>();
                }
            }

            var bodyNodes = htmlDoc.DocumentNode.SelectNodes("//tbody");
            if( bodyNodes.Count < 2)
                return new List<Dividend>();

            var bodyNode = bodyNodes[1];

            var doc = new System.Xml.XmlDocument();
            var settings = new System.Xml.XmlReaderSettings()
            {
                DtdProcessing = System.Xml.DtdProcessing.Parse
            };

            string content = bodyNode.OuterHtml;
            try
            {
                doc.LoadXml(content);
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine(e);
            }

            var dividends = Dividend.GetYahooCode(yahooCode);

            var nTables = doc.GetElementsByTagName("tbody").Count;

            for (int iTable = 0; iTable < nTables; iTable++)
            {
                var node = doc.GetElementsByTagName("tbody")[iTable];

                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    var dividend = ParseDividendDatesNode(node, i);
                    dividend.YahooCode = yahooCode;

                    var found = dividends.Find(d => d.Payable == dividend.Payable);
                    if (found == null)
                    {
                        dividend.Save();
                        dividends.Add(dividend);
                    }
                }
            }

            return dividends;
        }

        static Dividend ParseDividendDatesNode(System.Xml.XmlNode node, int i)
        {
            var exDividend = DateTime.ParseExact(node.ChildNodes[i].ChildNodes[2].InnerText, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            
            var amountString = node.ChildNodes[i].ChildNodes[0].InnerText.Split('c')[0];

            double amount = 0;
            if (amountString[^1] == 'p')
                double.TryParse(amountString.Substring(0, amountString.Length - 2), out amount);
            else
                double.TryParse(amountString, out amount);

            double imputation = 0;
            var imputationString = node.ChildNodes[i].ChildNodes[1].InnerText.Split('%')[0];
            double.TryParse(imputationString, out imputation);

            var payable = DateTime.ParseExact(node.ChildNodes[i].ChildNodes[3].InnerText, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

            var dividend = new Dividend()
            {
                ExDividend = exDividend,
                Amount = amount,
                Imputation = imputation,
                Payable = payable
            };

            return dividend;
        }

        public static List<Dividend> ReadFromNZX(string yahooCode)
        {
            if (string.IsNullOrEmpty(yahooCode) || yahooCode.Length <= 3)
                return new List<Dividend>();
            if (yahooCode.Substring(yahooCode.Length-3) != ".NZ")
                return new List<Dividend>();

            var name = yahooCode.Substring(0, yahooCode.Length - 3);

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionOutputAsXml = true;

            using (var client = new System.Net.WebClient())
            {
                try
                {
                    using (var reader = client.OpenRead("https://www.nzx.com/instruments/" + name + "/dividends"))
                    {
                        htmlDoc.Load(reader);
                    }
                }
                catch
                {
                    return new List<Dividend>();
                }
            }

            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("tbody");

            var doc = new System.Xml.XmlDocument();
            var settings = new System.Xml.XmlReaderSettings()
            {
                DtdProcessing = System.Xml.DtdProcessing.Parse
            };

            string content = bodyNode.OuterHtml;
            try
            {
                doc.LoadXml(content);
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine(e);
            }

            var dividends = Dividend.GetYahooCode(yahooCode);

            var nTables = doc.GetElementsByTagName("tbody").Count;

            for (int iTable = 0; iTable < nTables; iTable++)
            {
                var node = doc.GetElementsByTagName("tbody")[iTable];

                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    var dividend = ParseNZXNode(node,i);

                    var found = dividends.Find(d => d.Payable == dividend.Payable);
                    if (found == null)
                    {
                        dividend.Save();
                        dividends.Add(dividend);
                    }
                }
            }

            return dividends;
        }


        public static void UpdateFromNZX()
        {
            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.OptionOutputAsXml = true;

            string s = "";

            using (var client = new System.Net.WebClient())
            {
                using (var data = client.OpenRead("https://www.nzx.com/markets/NZSX/dividends"))
                {
                    using StreamReader reader = new StreamReader(data);
                    s = reader.ReadToEnd();
                }
            }


            Match jsonMatch = null;
            try
            {
                Regex jsonRegex = new Regex(".*?<script id=\"__NEXT_DATA__\".*?>(.*)<\\/script.*?>.*?/");
                jsonMatch = jsonRegex.Match(s);
            }
            catch (Exception ex)
            {

            }

            JObject jObject = JObject.Parse(jsonMatch.Groups[1].Value);
            var props = jObject["props"];
            var pageProps = props["pageProps"];
            var marketData = pageProps["data"];
            var marketInstruments = marketData["marketInstruments"];
            var marketDividends = marketData["marketDividends"];

            foreach (var marketDividend in marketDividends)
            {
                string isinDividend = (string) marketDividend["isin"];

                foreach (var marketInstrument in marketInstruments)
                {
                    string isinInstrument = (string) marketInstrument["isin"];
                    if (isinInstrument == isinDividend)
                    {
                       
                        var yahooCode = (string)marketInstrument["code"];

                        var dividends = Dividend.GetYahooCode(yahooCode);


                        var expectedSeconds = (long)marketDividend["expectedDate"];
                        var date = new DateTime(1970, 1, 1);
                        var expected = date.AddSeconds(expectedSeconds);
                        var localTime = date.ToLocalTime();
                        var period = (string) marketDividend["type"];
                        var amount = (double) marketDividend["amount"];
                        var supp = (double) marketDividend["supplementaryAmount"];
                        var imputation = (double) marketDividend["imputationCreditAmount"];
                        var payableSeconds = (long) marketDividend["payableDate"];
                        date = new DateTime(1970, 1, 1);
                        var payable = date.AddSeconds(payableSeconds);
                        localTime = date.ToLocalTime();
                        var currency = (string)marketDividend["currencyCode"];

                        var found = dividends.Find(d => d.Payable == payable);
                        Dividend dividend = found == null ? new Dividend() : found;

                        dividend.YahooCode = yahooCode;
                        dividend.ExDividend = expected;
                        dividend.Period = period;
                        dividend.Amount = amount;
                        dividend.Supp = supp;
                        dividend.Imputation = imputation;
                        dividend.Payable = payable;

                        dividend.Save();
                    }
                }
            }

        }

        static Dividend ParseNZXNode(System.Xml.XmlNode node, int i)
        {
            var yahooCode = node.ChildNodes[i].ChildNodes[0].InnerText + ".NZ" ?? string.Empty;
            var exDividend = DateTime.ParseExact(node.ChildNodes[i].ChildNodes[1].InnerText, "dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var amountString = node.ChildNodes[i].ChildNodes[3].InnerText.Split('c')[0];

            double amount = 0;
            if (amountString[^1] == 'p')
                double.TryParse(amountString.Substring(0, amountString.Length - 2), out amount);
            else
                double.TryParse(amountString, out amount);
            var suppString = node.ChildNodes[i].ChildNodes[4].InnerText.Split('c')[0];
            double supp = 0;

            if (suppString[^1] == 'p')
                double.TryParse(suppString.Substring(0, suppString.Length - 2), out supp);
            else
                double.TryParse(suppString, out supp);

            double imputation = 0;
            var imputationString = node.ChildNodes[i].ChildNodes[5].InnerText.Split('c')[0];
            if (imputationString[^1] == 'p')
                double.TryParse(imputationString.Substring(0, imputationString.Length - 2), out imputation);
            else
                double.TryParse(imputationString, out imputation);



            var payable = DateTime.ParseExact(node.ChildNodes[i].ChildNodes[6].InnerText, "dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

            var dividend = new Dividend()
            {
                YahooCode = yahooCode,
                ExDividend = exDividend,
                Period = node.ChildNodes[i].ChildNodes[2].InnerText ?? string.Empty,
                Amount = amount,
                Supp = supp,
                Imputation = imputation,
                Payable = payable
            };

            return dividend;
        }

        public static void UpdateGuids() => UpdateGuids(tableName, tableColumns);

        #endregion additional methods
    }
}

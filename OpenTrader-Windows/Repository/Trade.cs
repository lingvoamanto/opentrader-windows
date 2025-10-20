using System;
using System.Collections.Generic;
#if __IOS__
using Mono.Data.Sqlite;
using UIKit;
#endif
#if __MACOS__
using Mono.Data.Sqlite;
using AppKit;
#endif
using OpenTrader;
using System.Runtime.Serialization; // used for data contracts
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace OpenTrader.Data
{
    public partial class Trade : CloudObject<Trade>, ICloudRepository<Trade>
    {
        public Trade()
        {
            Date = DateTime.Now;
        }

        #region data contract
        private DateTime date;
        public DateTime Date { get { return date; } set { date = value; } }
        public double Quantity { get; set; }
        public double Price { get; set; }
        public double Fee { get; set; }
        volatile private string signal;
        public string Signal { get { return signal; } set { signal = value; } }
        public string Strategy { get; set; }
        public string YahooCode { get; set; }
        public double? Stop { get; set; }
        public double? Target { get; set; }
#endregion data contract

#region database structure
        static string tableName = "trades";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
            new Repository.Column( "Guid", "guid", "guid", "" ),
            new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
            new Repository.Column( "Signal", "signal", "string", "(25)" ),
            new Repository.Column( "Strategy", "strategy", "string", "(25)" ),
            new Repository.Column( "Quantity", "quantity", "double", "" ),
            new Repository.Column( "Date", "date", "DateTime", "" ),
            new Repository.Column( "Fee", "fee", "double", "" ),
            new Repository.Column( "Price", "price", "double", "" ),
            new Repository.Column( "Stop", "stop", "double", "null" ),
            new Repository.Column( "Target", "target", "double", "null" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
        #endregion database structure

        #region required methods

        public bool Save(bool syncing = false) => Save(tableName,tableColumns,syncing);

        public void Remove(bool syncing = false) =>
            Repository.Remove( this, tableName,syncing );
        static public Trade Get(int id) =>
            Repository.Get(typeof(Trade), tableName, tableColumns, id) as Trade;
        static public List<Trade> GetAll() => GetAll(tableName, tableColumns);

        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }

        #endregion required methods

        #region additional methods
        internal static List<Trade> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, string value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<Trade> results = new List<Trade>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "'";
            // DebugHelper.WriteLine("CommandText = " + command.CommandText);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Trade result = new Trade();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }

        static public List<Trade> GetYahooCode(string value)
        {
            var list = GetEquals(tableName, tableColumns, "YahooCode", value);
            var ordered = list.OrderBy(t => t.Date);
            var trades = new List<Trade>();
            var epoch = new DateTime(1970, 1, 1, 0, 0, 1);
            foreach(var trade in ordered)
            {
                if (trade.Date < epoch)
                    trade.Remove();
                else if (trade.Quantity == 0)
                    trade.Remove();
                else
                    trades.Add(trade);
            }

            return trades;
        }
#endregion additional methods

        static public double GetCurrentQuantity(string yahooCode)
        {
            if (string.IsNullOrEmpty(yahooCode))
            {
                return 0;
            }

            var trades = Trade.GetYahooCode(yahooCode);
            double currentQuantity = 0;
            foreach (Trade trade in trades)
            {
                currentQuantity += trade.Quantity;
            }

            return currentQuantity;
        }

        static public void AddHistory(List<Trade> history)
        {
            foreach(var @event in history)
            {
                var trades = Trade.GetYahooCode(@event.YahooCode);
                var trade = trades.Find(t => t.Date == @event.Date && Math.Abs(t.Quantity - @event.Quantity) < 0.0001);
                if( trade == null)
                {
                    @event.Save();
                }
            }
        }

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
                writer.WritePropertyName("Signal");
                writer.WriteValue(Signal);
                writer.WritePropertyName("Strategy");
                writer.WriteValue(Strategy);
                writer.WritePropertyName("Quantity");
                writer.WriteValue(Quantity);
                writer.WritePropertyName("Date");
                writer.WriteValue((Date - DateTime.UnixEpoch).TotalSeconds.ToString());
                writer.WritePropertyName("Fee");
                writer.WriteValue(Fee);
                writer.WritePropertyName("Price");
                writer.WriteValue(Price);
                if (Stop != null)
                {
                    writer.WritePropertyName("Stop");
                    writer.WriteValue(Stop);
                }
                if (Target != null)
                {
                    writer.WritePropertyName("Target");
                    writer.WriteValue(Target);
                }

                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        static public Trade Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var trade = new Trade();
            object? propertyName = "";

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        propertyName = reader.Value;
                        break;
                    case JsonToken.String:
                        if (reader.Value != null && propertyName != null)
                        {
                            long seconds;
                            switch (propertyName)
                            {
                                case "Guid":
                                    trade.Guid = new Guid((string)reader.Value);
                                    break;
                                case "YahooCode":
                                    trade.YahooCode = (string)reader.Value;
                                    break;
                                case "Signal":
                                    trade.signal = (string)reader.Value;
                                    break;
                                case "Strategy":
                                    trade.Strategy = (string)reader.Value;
                                    break;
                                case "Quantity":
                                    trade.Quantity = (double)reader.Value;
                                    break;
                                case "Date":
                                    seconds = (long)reader.Value;
                                    trade.Date = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                                case "Fee":
                                    trade.Fee = (double)reader.Value;
                                    break;
                                case "Price":
                                    trade.Price = (double)reader.Value;
                                    break;
                                case "Stop":
                                    trade.Stop = (double)reader.Value;
                                    break;
                                case "Target":
                                    trade.Target = (double)reader.Value;
                                    break;
                            }
                        }
                        break;
                }
            }

            return trade;
        }

        public static void UpdateGuids() => UpdateGuids(tableName, tableColumns);
    }
}
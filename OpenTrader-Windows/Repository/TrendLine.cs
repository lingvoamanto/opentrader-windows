using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
#if __MACOS__
using Mono.Data.Sqlite;
#endif
#if __WINDOWS__
using System.Data.SQLite;
using System.Windows.Threading;
using Newtonsoft.Json;
using OpenCompiler;
using static OpenTrader.Sharesies;
using System.Collections;
using ILGPU.Frontend;
using System.Transactions;
using System.Windows.Markup;
using System.Xml.XPath;
#endif

namespace OpenTrader.Data
{
    public enum TrendKind
    {
        Linear = 0,
        Quadratic = 1,
        Measure = 2
    }

    [DataContract]
    public partial class TrendLine : IRepository<TrendLine>
    {
        public TrendLine()
        {
            StartDate = new DateTime(1900, 1, 1);
            EndDate = new DateTime(1900, 1, 1);
            MidDate = new DateTime(1900, 1, 1);
        }


        #region data contract
        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public DateTime StartDate { get; set; }
        [DataMember]
        public double StartPrice { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }
        [DataMember]
        public double EndPrice { get; set; }
        [DataMember]
        public string YahooCode { get; set; }
        [DataMember]
        public TrendKind TrendKind { get; set; }
        [DataMember]
        public double MidPrice { get; set; }
        [DataMember]
        public DateTime MidDate { get; set; }
        #endregion data contract

        #region database structure
        static string tableName = "trend_lines";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "Guid", "guid", "guid", "" ),
                new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
                new Repository.Column( "StartDate", "start_date",  "DateTime", "" ),
                new Repository.Column( "StartPrice", "start_price", "double", "" ),
                new Repository.Column( "EndDate", "end_date",  "DateTime", "" ),
                new Repository.Column( "EndPrice", "end_price", "double", "" ),
                new Repository.Column( "MidDate", "mid_date",  "DateTime", "" ),
                new Repository.Column( "MidPrice", "mid_price", "double", "" ),
                new Repository.Column( "TrendKind", "trend_kind", "enum", "" )
        };


        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
        #endregion database structure

        #region required methods
        public bool Save(bool syncing = false) => Save(this, tableName, tableColumns, syncing);
        // public bool Save(bool syncing = false) => TestSave(this, tableName, tableColumns, syncing);
        public void Remove(bool syncing = false) =>
            Repository.Remove(this, tableName, syncing);
        static public TrendLine Get(int id) =>
            Repository.Get(typeof(TrendLine), tableName, tableColumns, id) as TrendLine;
        public bool ShouldSync() => true;
        // public string Serialise() => Serialise(this);
        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }
        #endregion required methods

        #region additional methods
        static public List<TrendLine> GetYahooCode(string value) =>
            GetEquals(tableName, tableColumns, "YahooCode", value);
        #endregion additional methods

        internal static List<TrendLine> GetAll()
        {
            SQLiteCommand command = Repository.Connection.CreateCommand();

            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText;
            DebugHelper.WriteLine("CommandText = " + command.CommandText);
            var reader = command.ExecuteReader();

            var results = new List<TrendLine>();
            while (reader.Read())
            {
                var result = new TrendLine();
                Repository.ReadValues(reader, tableColumns, result, true);
                results.Add(result);
            }

            reader.Close();
            return results;
        }

        internal static void UpdateGuids()
        {
            var results = TrendLine.GetAll();

            foreach (var o in results)
            {
                SQLiteCommand command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.UpdateCommand(tableName, tableColumns);
                Repository.AddParameters(command, tableColumns, o, o.Id != 0);
                command.ExecuteNonQuery();
            }
        }

        internal static List<TrendLine> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, string value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<TrendLine> results = new List<TrendLine>();
#if __MACOS__
            SqliteCommand command = Repository.Connection.CreateCommand();
#endif
#if __WINDOWS__
            SQLiteCommand command = Repository.Connection.CreateCommand();
#endif
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "'";
            DebugHelper.WriteLine("CommandText = " + command.CommandText);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                TrendLine result = new TrendLine();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }

            reader.Close();
            return results;
        }

        internal static bool Save(TrendLine o, string tableName, List<Repository.Column> tableColumns, bool syncing = false)
        {
            int id = o.Id;
            if (o.Guid == Guid.Empty)
            {
                o.Guid = Guid.NewGuid();
            }
            bool inserted = false;
            var command = Repository.Connection.CreateCommand();
            command.CommandText = Repository.SelectCommand(tableName, tableColumns, true) + " WHERE guid='" + o.Guid.ToString() + "'";
            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.UpdateCommand(tableName, tableColumns);
                Repository.AddParameters(command, tableColumns, o, true);
                command.ExecuteNonQuery();
                if (o.ShouldSync())
                {
                    string json = o.Serialise();
                    Transaction.Add("update", tableName, o.Id, json);
                }
            }
            else
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.InsertCommand(tableName, tableColumns, o.Id != 0);
                Repository.AddParameters(command, tableColumns, o, o.Id != 0);
                command.ExecuteNonQuery();

                if (o.Id == 0)
                {
                    command = Repository.Connection.CreateCommand();
                    command.CommandText = "SELECT last_insert_rowid() AS ID";
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        o.Id = (int)(long)reader["ID"];
                        inserted = true;
                    }
                    reader.Close();
                }
                if (o.ShouldSync() && !syncing)
                {
#if __MACOS__
                    var obj = new Foundation.NSObject();
                    obj.InvokeOnMainThread(() => {
                        string json = o.Serialise();
                        DebugHelper.WriteLine(json);
                        Transaction.Add("insert", tableName, o.Id, json);
                    });
#endif
#if __WINDOWS__
                    string json = o.Serialise();
                    DebugHelper.WriteLine(json);
                    Transaction.Add("insert", tableName, o.Id, json);
#endif
                }
            }

            return inserted;
        }

        static public TrendLine Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var trendLine = new TrendLine();
            object? propertyName = "";
            // {
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
                                    trendLine.Guid = new Guid((string)reader.Value);
                                    break;
                                case "StartDate":
                                    seconds = (long)reader.Value;
                                    trendLine.StartDate = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                                case "StartPrice":
                                    trendLine.StartPrice = (double) reader.Value;
                                    break;
                                case "EndDate":
                                    seconds = (long)reader.Value;
                                    trendLine.EndDate = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                                case "EndPrice":
                                    trendLine.EndPrice = (double)reader.Value;
                                    break;
                                case "YahooCode":
                                    trendLine.YahooCode = (string)reader.Value;
                                    break;
                                case "TrendKind":
                                    trendLine.TrendKind = (TrendKind) (int) reader.Value;
                                    break;
                                case "MidPrice":
                                    trendLine.MidPrice = (double)reader.Value;
                                    break;
                                case "MidDate":
                                    seconds = (long)reader.Value;
                                    trendLine.MidDate = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                            }
                        }
                        break;
                }
            }

            return trendLine;
        }

        public string Serialise()
        {
            var sb = new System.Text.StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("Guid");
                writer.WriteValue(Guid.ToString());
                writer.WritePropertyName("EndDate");
                writer.WriteValue((EndDate - DateTime.UnixEpoch).TotalSeconds.ToString());
                writer.WritePropertyName("EndPrice");
                writer.WriteValue(EndPrice.ToString());
                writer.WritePropertyName("MidDate");
                writer.WriteValue((MidDate - DateTime.UnixEpoch).TotalSeconds.ToString());
                writer.WritePropertyName("MidPrice");
                writer.WriteValue(MidPrice.ToString());
                writer.WritePropertyName("StartDate");
                writer.WriteValue((StartDate - DateTime.UnixEpoch).TotalSeconds.ToString());
                writer.WritePropertyName("StartPrice");
                writer.WriteValue(StartPrice.ToString());
                writer.WritePropertyName("TrendKind");
                writer.WriteValue((int)TrendKind);
                writer.WritePropertyName("YahooCode");
                writer.WriteValue(YahooCode);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }
    }
}

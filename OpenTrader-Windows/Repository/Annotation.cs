using System;
using System.Collections.Generic;
#if __MACOS__
using Mono.Data.Sqlite;
using AppKit;
#endif
#if __WINDOWS__
using System.Data.SQLite;
#endif
using OpenTrader;
using System.Runtime.Serialization; // used for data contracts
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using Accord.Audio;
using Newtonsoft.Json;
using System.Windows.Documents;

namespace OpenTrader.Data
{
    public class Annotation : CloudObject<Annotation>, ICloudRepository<Annotation>
    {
        public Annotation()
        {
            Date = DateTime.Now;
        }

        #region data contract
        public Guid Guid { get; set; }
        public int Id { get; set; }
        private DateTime date;
        public DateTime Date { get { return date; } set { date = value; } }
        public double Price { get; set; }
        volatile private string text;
        public string Text { get { return text; } set { text = value; } }
        public string YahooCode { get; set; }
#endregion data contract

#region database structure
        static string tableName = "annotations";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "Guid", "guid", "guid", "" ),
                new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
                new Repository.Column( "Text", "text", "string", "(25)" ),
                new Repository.Column( "Date", "date", "DateTime", "" ),
                new Repository.Column( "Price", "price", "double", "" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
#endregion database structure

#region required methods
        public bool Save(bool syncing=false) => Save(tableName, tableColumns, syncing);
        public void Remove(bool syncing = false) =>
            Repository.Remove(this, tableName,syncing);
        static public Annotation Get(int id) =>
            Repository.Get(typeof(Annotation), tableName, tableColumns, id) as Annotation;
        static public List<Annotation> GetAll() => GetAll(tableName, tableColumns);
        public bool ShouldSync() => true;
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
                writer.WritePropertyName("Text");
                writer.WriteValue(Text);
                writer.WritePropertyName("Date");
                writer.WriteValue((Date-DateTime.UnixEpoch).TotalSeconds);
                writer.WritePropertyName("Price");
                writer.WriteValue(Price);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }


        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }

        static public Annotation Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var annotation = new Annotation();
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
                                    annotation.Guid = new Guid((string)reader.Value);
                                    break;
                                case "YahooCode":
                                    annotation.YahooCode = (string)reader.Value;
                                    break;
                                case "Text":
                                    annotation.text = (string)reader.Value;
                                    break;
                                case "Date":
                                    seconds = (long)reader.Value;
                                    annotation.Date = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                                case "Price":
                                    annotation.Price = (double)reader.Value;
                                    break;
                            }
                        }
                        break;
                }
            }

            return annotation;
        }

        #endregion required methods

        #region additional methods
        static public List<Annotation> GetYahooCode(string value) =>
            GetEquals(tableName, tableColumns, "YahooCode", value);

        internal static List<Annotation> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, string value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<Annotation> results = new List<Annotation>();
#if __MACOS__
            SqliteCommand command = Repository.Connection.CreateCommand();
#endif
#if __WINDOWS__
            SQLiteCommand command = Repository.Connection.CreateCommand();
#endif
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "'";
            DebugHelper.WriteLine("CommandText = " + command.CommandText);
#if __MACOS__
            SqliteDataReader reader = command.ExecuteReader();
#endif
#if __WINDOWS__
            SQLiteDataReader reader = command.ExecuteReader();
#endif

            while (reader.Read())
            {
                var result = new Annotation();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }


        public static void UpdateGuids() => UpdateGuids(tableName, tableColumns);

        #endregion additional methods
        #region test methods
        #endregion test methods
    }
}
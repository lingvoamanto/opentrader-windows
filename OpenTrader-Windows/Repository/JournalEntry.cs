using System;
using System.Collections.Generic;
#if __APPLEOS__
using Mono.Data.Sqlite;
using AppKit;
#endif
using OpenTrader;
using System.Runtime.Serialization; // used for data contracts
using System.Linq;
using Accord.Audio;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Navigation;

namespace OpenTrader.Data
{
    [DataContract]
    public partial class JournalEntry : CloudObject<JournalEntry>, ICloudRepository<JournalEntry>
    {
        public JournalEntry()
        {
            Date = DateTime.Now;
        }

        #region data contract
        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public int Id { get; set; }
        private DateTime date;
        private string notes;
        [DataMember]
        public DateTime Date { get { return date; } set { date = value; } }
        [DataMember]
        public string Notes { get { return notes; } set { notes = value; } }
        [DataMember]
        public string YahooCode { get; set; }
#endregion data contract

#region database structure
        static string tableName = "journal_entries";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "Guid", "guid", "guid", "" ),
                new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
                new Repository.Column( "Date", "date", "DateTime", "" ),
                new Repository.Column( "Notes", "notes", "string", "" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }

        #endregion database structure

        #region required methods
        public bool Save(bool syncing = false) => Save(tableName, tableColumns, syncing);
        public void Remove(bool syncing = false) =>
            Repository.Remove( this, tableName, syncing );
        static public JournalEntry Get(int id) =>
            Repository.Get(typeof(JournalEntry), tableName, tableColumns, id ) as JournalEntry;

        static public List<JournalEntry> GetAll() => GetAll(tableName, tableColumns);
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
                writer.WritePropertyName("Date");
                writer.WriteValue((Date - DateTime.UnixEpoch).TotalSeconds.ToString());
                writer.WritePropertyName("Notes");
                writer.WriteValue(Notes);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        static public JournalEntry Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var journalEntry = new JournalEntry();
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
                                    journalEntry.Guid = new Guid((string)reader.Value);
                                    break;
                                case "YahooCode":
                                    journalEntry.YahooCode = (string)reader.Value;
                                    break;
                                case "Notes":
                                    journalEntry.notes = (string)reader.Value;
                                    break;
                                case "Date":
                                    seconds = (long)reader.Value;
                                    journalEntry.Date = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                            }
                        }
                        break;
                }
            }

            return journalEntry;
        }


        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }
#endregion required methods

        static public List<JournalEntry> GetYahooCode(string value)
        {
            var list = Repository.GetEquals(typeof(JournalEntry), tableName, tableColumns, "YahooCode", value, "date DESC");
            var yc = new List<JournalEntry>();
            foreach(object o in list)
            {
                yc.Add(o as JournalEntry);
            }
            return yc;
        }

        public static void UpdateGuids() => UpdateGuids(tableName, tableColumns);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
#if __IOS__
using Mono.Data.Sqlite;
#endif
#if __MACOS__
using Mono.Data.Sqlite;
#endif
#if __WINDOWS__
using System.Data.SQLite;
#endif

namespace OpenTrader.Data
{
    public class Bar : IRepository<Bar>
    {

#region properties
        public int Id { get; set; }
        public string YahooCode { get; set; }
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public bool Interim { get; set; }
#endregion properties

#region database
        static string tableName = "bars";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
            new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
            new Repository.Column( "Date", "date", "DateTime", "" ),
            new Repository.Column( "Open", "open", "double", "" ),
            new Repository.Column( "High", "high", "double", "" ),
            new Repository.Column( "Low", "low", "double", "" ),
            new Repository.Column( "Close", "close", "double", "" ),
            new Repository.Column( "Volume","volume", "double", "" ),
            new Repository.Column( "Interim", "interim", "bool", "" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }

#if __IOS__
        static public void ReadValues(SqliteDataReader reader, Bar bar, bool readsId = false)
#endif
#if __MACOS__
        static public void ReadValues(SqliteDataReader reader, Bar bar, bool readsId = false)
#endif
#if __WINDOWS__
        static public void ReadValues(SQLiteDataReader reader, Bar bar, bool readsId = false)
#endif
        {
            if (reader.IsDBNull(0))
                bar.YahooCode = "";
            else
                bar.YahooCode = reader.GetString(0);
            if (reader.IsDBNull(1))
                bar.Date = new DateTime(1900,1,1);
            else
            {
                long longValue;
                if (reader.GetFieldType(1) == typeof(long))
                    longValue = reader.GetInt64(1);
                else if (reader.GetFieldType(1) == typeof(int))
                    longValue = (long)reader.GetInt32(1);
                else
                    longValue = new DateTime(1900, 1, 1).Ticks;
                DateTime dateTime = new DateTime(longValue);
                if (dateTime < new DateTime(1900, 1, 1))
                    dateTime = new DateTime(1900, 1, 1);
                bar.Date = dateTime;
            }
            if (reader.IsDBNull(2))
                bar.Open = 0;
            else
                bar.Open = reader.GetDouble(2);
            if (reader.IsDBNull(3))
                bar.High = 0;
            else
                bar.High = reader.GetDouble(3);
            if (reader.IsDBNull(4))
                bar.Low = 0;
            else
                bar.Low = reader.GetDouble(4);
            if (reader.IsDBNull(5))
                bar.Close = 0;
            else
                bar.Close = reader.GetDouble(5);
            if (reader.IsDBNull(6))
                bar.Volume = 0;
            else
                bar.Volume = reader.GetDouble(6);
            if (reader.IsDBNull(7))
                bar.Interim = false;
            else
                bar.Interim = reader.GetInt32(7) != 0;
            if( readsId )
                bar.Id = reader.GetInt32(8);
        }
        #endregion database


        #region interface methods
        public bool Save(bool syncing = false)
        {
            return Repository.Save(this, tableName, tableColumns, syncing);
        }

        public void Remove(bool syncing = false) => Repository.Remove(this, tableName, syncing);
        public Bar Get(int id) => Repository.Get(typeof(Bar),tableName, tableColumns, id) as Bar;

        public bool ShouldSync() => false;
        public string Serialise() => Repository.Serialise(typeof(Bar),this);
        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }

        public static List<Bar> GetEquals(string field, string value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<Bar> results = new List<Bar>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "' ORDER BY date";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var result = new Bar();
                ReadValues(reader, result, true);
                results.Add(result);
            }
            return results;
        }

        public static void GetYahooCode(string yahooCode, Bars bars, List<bool> interim, List<int> barId)
        {
            var command = Repository.Connection.CreateCommand();
            command.CommandText = "SELECT yahoo_code,date,open,high,low,close,volume,interim,id FROM bars WHERE yahoo_code='" + yahooCode + "' ORDER BY date";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var bar = new Bar();
                if (reader.IsDBNull(0))
                    bar.YahooCode = "";
                else
                    bar.YahooCode = reader.GetString(0);
                if (reader.IsDBNull(1))
                    bar.Date = new DateTime(1900, 1, 1);
                else
                {
                    long longValue;
                    if (reader.GetFieldType(1) == typeof(long))
                        longValue = reader.GetInt64(1);
                    else if (reader.GetFieldType(1) == typeof(int))
                        longValue = (long)reader.GetInt32(1);
                    else
                        longValue = new DateTime(1900, 1, 1).Ticks;
                    DateTime dateTime = new DateTime(longValue);
                    if (dateTime < new DateTime(1900, 1, 1))
                        dateTime = new DateTime(1900, 1, 1);
                    bar.Date = dateTime;
                }
                if (reader.IsDBNull(2))
                    bar.Open = 0;
                else
                    bar.Open = reader.GetDouble(2);
                if (reader.IsDBNull(3))
                    bar.High = 0;
                else
                    bar.High = reader.GetDouble(3);
                if (reader.IsDBNull(4))
                    bar.Low = 0;
                else
                    bar.Low = reader.GetDouble(4);
                if (reader.IsDBNull(5))
                    bar.Close = 0;
                else
                    bar.Close = reader.GetDouble(5);
                if (reader.IsDBNull(6))
                    bar.Volume = 0;
                else
                    bar.Volume = reader.GetDouble(6);
                if (reader.IsDBNull(7))
                    bar.Interim = false;
                else
                    bar.Interim = reader.GetInt32(7) != 0;
                bar.Id = reader.GetInt32(8);

                bars.Open.Add(bar.Open);
                bars.High.Add(bar.High);
                bars.Low.Add(bar.Low);
                bars.Close.Add(bar.Close);
                bars.Volume.Add(bar.Volume);
                bars.Date.Add(bar.Date);
                interim.Add(bar.Interim);
                barId.Add(bar.Id);
            }
        }

        internal static int Save(string yahooCode, DateTime date, double open, double high, double low, double close, double volume, bool interim)
        {
            Bar bar = new Bar()
            {
                YahooCode = yahooCode,
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
                Interim = interim
            };
            // bool inserted = false;

            var command = Repository.Connection.CreateCommand();

            command.CommandText = "SELECT yahoo_code,date,open,high,low,close,volume,interim,id FROM bars WHERE yahoo_code = @YahooCode AND date = @Date";
            command.Parameters.AddWithValue("@YahooCode", yahooCode ?? "");
            command.Parameters.AddWithValue("@Date", date.Ticks);
            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                bar.Id = (int)(long)reader["id"];
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = "UPDATE bars SET yahoo_code = @YahooCode,date = @Date,open = @Open,high = @High,low = @Low,close = @Close,volume = @Volume,interim = @Interim WHERE id = @Id";
                // command.CommandText = Repository.UpdateCommand(tableName, tableColumns);
                try
                {
                    command.Parameters.AddWithValue("@YahooCode", yahooCode ?? "");
                    command.Parameters.AddWithValue("@Date", date.Ticks);
                    command.Parameters.AddWithValue("@Open", open);
                    command.Parameters.AddWithValue("@High", high);
                    command.Parameters.AddWithValue("@Low", low);
                    command.Parameters.AddWithValue("@Close", close);
                    command.Parameters.AddWithValue("@Volume", volume);
                    command.Parameters.AddWithValue("@Interim", interim ? 1 : 0);
                    command.Parameters.AddWithValue("@Id", bar.Id);
                    // Repository.AddParameters(command, tableColumns, bar, true);
                }
                catch (Exception e)
                {
                    string message = e.Message;
                }
                command.ExecuteNonQuery();
            }
            else
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                // command.CommandText = Repository.InsertCommand(tableName, tableColumns);
                command.CommandText = "INSERT INTO bars (yahoo_code,date,open,high,low,close,volume,interim) VALUES (@YahooCode,@Date,@Open,@High,@Low,@Close,@Volume,@Interim)";
                try
                {
                    // Repository.AddParameters(command, tableColumns, bar);
                    command.Parameters.AddWithValue("@YahooCode", yahooCode ?? "");
                    command.Parameters.AddWithValue("@Date", date.Ticks);
                    command.Parameters.AddWithValue("@Open", open);
                    command.Parameters.AddWithValue("@High", high);
                    command.Parameters.AddWithValue("@Low", low);
                    command.Parameters.AddWithValue("@Close", close);
                    command.Parameters.AddWithValue("@Volume", volume);
                    command.Parameters.AddWithValue("@Interim", interim ? 1 : 0);
                }
                catch( Exception e)
                {
                    string message = e.Message;
                }
                command.ExecuteNonQuery();

                command = Repository.Connection.CreateCommand();
                command.CommandText = "SELECT last_insert_rowid() AS ID";
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    bar.Id = (int)(long)reader["ID"];
                    // inserted = true;
                }
                reader.Close();
            }

            return bar.Id;
        }

#endregion

    }
}

using System;
using System.Collections.Generic;

#if __IOS__
using UIKit;
using Mono.Data.Sqlite;
#endif
#if __MACOS__
using AppKit;
using Mono.Data.Sqlite;
#endif
#if __WINDOWS__
using System.Data.SQLite;
using System.Windows;
#endif

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;

namespace OpenTrader.Data
{
    public partial class DataSet : CloudObject<DataSet>, ICloudRepository<DataSet>
    {
        public DataSet()
        {

        }
        #region database structure

        static string tableName = "datasets";

        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "Guid", "guid", "guid", "" ),
                new Repository.Column( "Name", "name", "string", "(25)" ),
                new Repository.Column( "YahooPrefix", "yahooprefix", "string", "(255)" ),
                new Repository.Column( "YahooSuffix", "yahoosuffix", "string", "(255)" ),
                new Repository.Column( "Liquidity", "liquidity", "double", "" ),
                new Repository.Column( "Exchange", "exchange", "string", "(255)" ),
                new Repository.Column( "YahooIndex", "yahoo_index", "string", "(255)" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
#endregion database structure

#region interface methods

        static public DataSet Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var dataSet = new DataSet();
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
                                    dataSet.Guid = new Guid((string)reader.Value);
                                    break;
                                case "Name":
                                    dataSet.Name = (string)reader.Value;
                                    break;
                                case "YahooPrefix":
                                    dataSet.YahooPrefix = (string)reader.Value;
                                    break;
                                case "YahooSuffix":
                                    dataSet.YahooSuffix = (string)reader.Value;
                                    break;
                                case "Liquidity":
                                    dataSet.Liquidity = (double) reader.Value;
                                    break;
                                case "Exchange":
                                    dataSet.Exchange = (string) reader.Value;
                                    break;
                                case "YahooIndex":
                                    dataSet.YahooIndex = (string)reader.Value;
                                    break;
                            }
                        }
                        break;
                }
            }

            return dataSet;
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
                writer.WritePropertyName("Name");
                writer.WriteValue(Name);
                writer.WritePropertyName("YahooPrefix");
                writer.WriteValue(YahooPrefix);
                writer.WritePropertyName("YahooSuffix");
                writer.WriteValue(YahooSuffix);
                writer.WritePropertyName("Liquidity");
                writer.WriteValue(Liquidity);
                writer.WritePropertyName("Exchange");
                writer.WriteValue(Exchange);
                writer.WritePropertyName("YahooIndex");
                writer.WriteValue(YahooIndex);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }
#endregion

        public static HashTable<DataSet> Hash = new HashTable<DataSet>();

        static public void CreateTable()
        {
            // Set up datasets
            try
            {
                var dbCommand = Repository.Connection.CreateCommand();
                dbCommand.CommandText = "CREATE TABLE IF NOT EXISTS 'datasets' ("
                                    + "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "
                                    + "'name' TEXT(25), "
                                    + "'yahooprefix' TEXT(255), "
                                    + "'yahoosuffix' TEXT(255), "
                                    + "'liquidity' TEXT(255), "
                                    + "'exchange' TEXT(255), "
                                    + "'yahooindex' TEXT(255) "
                                    + ")";
                dbCommand.ExecuteNonQuery();

            }
            catch (Exception debugException)
            {
#region debug message						
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(debugException, true);
                string stringLineNumber = stack.GetFrame(0).GetFileLineNumber().ToString();
                string message = "(" + stringLineNumber + ") " + debugException.Message;

#if __IOS__
                UIKit.UIAlertController alertController = UIAlertController.Create("CreateTable", message, UIAlertControllerStyle.Alert);
                alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, alert => Console.WriteLine("CreateTable: " + message)));
                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alertController, true, null);
#endif
#if __MACOS__
                AppKit.NSAlert alert = new AppKit.NSAlert();
                alert.MessageText = message;
                alert.RunSheetModal(NSApplication.SharedApplication.MainWindow);
#endif
#if __WINDOWS__
                MessageBox.Show(message);
#endif
                #endregion
            }
        }

        // A private constructor, just for GetAll()
        private DataSet(DataSets dataSets, int id, string path, string name, string yahooPrefix, string yahooSuffix, double liquidity, string? exchange=null)
        {
            this.datasets = dataSets;
            this.Id = id;
            this.Path = path;
            this.mName = name;
            this.mYahooPrefix = yahooPrefix;
            this.mYahooSuffix = yahooSuffix;
            if (exchange == null)
                mExchange = yahooSuffix;
            else
                mExchange = exchange;
            Liquidity = liquidity;
        }

        static public DataSets GetAll()
        {
            DataSets dataSets;

            dataSets = new DataSets();

            // Get each dataset				
            var dbDataSetsCommand = Repository.Connection.CreateCommand();
            dbDataSetsCommand.CommandText = "SELECT id, path, name, yahooprefix, yahoosuffix, liquidity,exchange, yahoo_index, guid FROM datasets";
            var dbReader = dbDataSetsCommand.ExecuteReader();

            while (dbReader.Read())
            {
                int id = dbReader.GetInt32(0);
                string path = dbReader.GetString(1);
                string name = dbReader.GetString(2);
                string yahooPrefix = dbReader.IsDBNull(3) ? "" : dbReader.GetString(3);
                string yahooSuffix = dbReader.IsDBNull(4) ? "" : dbReader.GetString(4);
                double liquidity = dbReader.IsDBNull(5) ? 10000.0 : dbReader.GetDouble(5);
                string exchange = dbReader.IsDBNull(6) ? "" : dbReader.GetString(6);
                string yahooIndex = dbReader.IsDBNull(7) ? "" : dbReader.GetString(7);
                string guidString = dbReader.IsDBNull(8) ? "" : dbReader.GetString(8);

                DataSet dataSet = new DataSet(dataSets, id, path, name, yahooPrefix, yahooSuffix, liquidity,exchange);
                dataSet.YahooIndex = yahooIndex;
                dataSet.Guid = new Guid(guidString);

                dataSets.Add(dataSet);
            }

            foreach( DataSet dataSet in dataSets)
            {
                dataSet.DataFiles = DataFile.GetAll(dataSet);
            }
            return dataSets;
        }

        public void Add( bool useExchange = false )
        {
            DirectoryInfo datasetDirectoryInfo = null;
            if (! string.IsNullOrEmpty(this.Path))
            {
                datasetDirectoryInfo = new DirectoryInfo(this.Path);
                if( string.IsNullOrEmpty(this.Name) )
                    mName = datasetDirectoryInfo.Name;
            }
            var dbCommand = Repository.Connection.CreateCommand();
            if (useExchange)
                dbCommand.CommandText = "SELECT id, name, yahooprefix, yahoosuffix, liquidity,exchange,yahoo_index FROM datasets WHERE exchange='" + this.Exchange + "'"; 
            else
                dbCommand.CommandText = "SELECT id, name, yahooprefix, yahoosuffix, liquidity,exchange,yahoo_index FROM datasets WHERE path='" + this.Path + "'";

            var dbDataSetsReader = dbCommand.ExecuteReader();


            if (!dbDataSetsReader.Read())
            {
                // this is really new so create it's entry in the database
                dbDataSetsReader.Close();

                dbCommand.CommandText = "INSERT INTO 'datasets' ('path','name','exchange') VALUES "
                                    + "('" + this.Path + "',"
                                    + "'" + mName + "'," 
                                    + "'" + mExchange + "'" 
                                    + ")";
                dbCommand.ExecuteNonQuery();

                // Pick up the id
                dbCommand.CommandText = "SELECT last_insert_rowid() AS ID";
                dbDataSetsReader = dbCommand.ExecuteReader();
                if (dbDataSetsReader.Read())
                {
                    int datasetid = (int)(long)dbDataSetsReader["ID"];
                    this.Id = datasetid;
                    Hash.Add(this, datasetid);
                }

                if (this.ShouldSync())
                {
                    Transaction.Add("insert", tableName, this.Id, this.Serialise());
                }
            }
            else
            {
                // we know about the DataSet so get what information we have
                int datasetid = dbDataSetsReader.GetInt32(0);
                string datasetname = dbDataSetsReader.GetString(1);
                string prefix;
                try { prefix = dbDataSetsReader.GetString(2); } catch { prefix = ""; }
                string suffix;
                try { suffix = dbDataSetsReader.GetString(3); } catch { suffix = ""; }
                string exchange;
                try { exchange = dbDataSetsReader.GetString(5); } catch { exchange = ""; }
                string yahooIndex;
                try { yahooIndex = dbDataSetsReader.GetString(6); } catch { yahooIndex = ""; }
                Hash.Add(this, datasetid);
                this.mName = datasetname;
                this.mYahooPrefix = prefix;
                this.mYahooSuffix = suffix;
                this.mExchange = exchange;
                this.mYahooIndex = yahooIndex;
                this.Id = datasetid;
            }
            dbDataSetsReader.Close();

            // Now we need to pick up all the txt files in the directory that belong to this dataset
            DataFiles = DataFile.GetAll(this);
            DataFiles.dataset = this;

            // Look through the DataSet's directory for any files, ignoring hidden and system files

        }


        public void Remove(bool syncing = false) => Repository.Remove( this, tableName, syncing );

        public bool Save(bool syncing = false) => Save(tableName, tableColumns, syncing);

        public static void UpdateGuids()
        {
            UpdateGuids(tableName, tableColumns);
        }
    }
}

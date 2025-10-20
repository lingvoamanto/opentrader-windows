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
#if __WINDOWS__
using System.Windows;
using System.Data.SQLite;
#endif
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Accord.Audio;
using Newtonsoft.Json;
using static OpenTrader.Sharesies;
using System.Data;
using System.ComponentModel;

namespace OpenTrader.Data
{
    public partial class DataFile : CloudObject<DataFile>, ICloudRepository<DataFile>
    {

        #region properties
        public static string tableName = "datafiles";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
            new Repository.Column( "Guid", "guid", "guid", "" ),
            new Repository.Column( "Name", "name", "string", "(25)" ),
            new Repository.Column( "DatasetGuid", "dataset_guid", "guid", "" ),
            new Repository.Column( "Description", "description", "string", "(25)" ),
            new Repository.Column( "YahooCode", "yahoo_code", "string", "(6)" ),
            new Repository.Column( "YahooStart", "yahoo_start", "DateTime", "" ),
            new Repository.Column( "Watching", "watching", "bool", "" ),
            new Repository.Column( "TradingPrompt", "trading_prompt", "string", "(50)" ),
            new Repository.Column( "TradingNotes","trading_notes", "string", "" ),
            new Repository.Column( "ZeroVolume", "zero_volume", "bool", "" ),
            new Repository.Column( "WatchAmount", "watch_amount", "double", "" ),
            new Repository.Column( "HasReadHistorical", "has_read_historical", "bool", "" ),
            new Repository.Column( "LastUpdated", "last_updated", "UnixTime", "" ),
            // new Repository.Column( "DatasetId", "dataset_id", "int", "" ),
#if __WINDOWS__
            new Repository.Column( "Image", "image", "byte[]", "" )
#endif
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }

        public string Path { get; set; }
        public string Name { get; set; }
        // public int DatasetId { get; set; }
        public Guid DatasetGuid { get; set; }

        public string Description { get; set; }

        public string YahooCode { get; set; }

        public DateTime YahooStart { get; set; }
        public bool Watching { get; set; }

        public byte[] Image { get; set; }

        public string TradingPrompt { get; set; }

        public string TradingNotes { get; set; }
        public bool ZeroVolume { get; set; }

        public double WatchAmount { get; set; }

        public bool HasReadHistorical { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UnixEpoch;

        public bool IsTrading { get; set; }

        #endregion properties


        #region interface methods
        public bool Save(bool syncing=false) => Save(tableName, tableColumns, syncing);
        public void Remove(bool syncing = false) => Repository.Remove(this, tableName, syncing);
        public DataFile Get(int id)
        {
            var dataFile = Repository.Get(typeof(DataFile), tableName, tableColumns, id) as DataFile;

            IsTrading = Trade.GetCurrentQuantity(dataFile.YahooCode) > Sharesies.epsilon;

            return dataFile;
        }
        public static List<DataFile> GetEquals(string name, string value) => GetEquals(tableName, tableColumns, name, value);

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
                writer.WritePropertyName("DatasetGuid");
                writer.WriteValue(DatasetGuid);
                writer.WritePropertyName("Name");
                writer.WriteValue(Name);
                writer.WritePropertyName("Description");
                writer.WriteValue(Description);
                writer.WritePropertyName("YahooCode");
                writer.WriteValue(YahooCode);
                writer.WritePropertyName("YahooStart");
                writer.WriteValue((YahooStart - DateTime.UnixEpoch).TotalSeconds.ToString());
                writer.WritePropertyName("Watching");
                writer.WriteValue(Watching);
                writer.WritePropertyName("TradingPrompt");
                writer.WriteValue(TradingPrompt);
                writer.WritePropertyName("TradingNotes");
                writer.WriteValue(TradingNotes);
                writer.WritePropertyName("ZeroVolume");
                writer.WriteValue(ZeroVolume);
                writer.WritePropertyName("WatchAmount");
                writer.WriteValue(WatchAmount);
                writer.WritePropertyName("HasReadHistorical");
                writer.WriteValue(HasReadHistorical);
                if (Image != null)
                {
                    writer.WritePropertyName("Image");
                    writer.WriteValue(Convert.ToBase64String(Image));
                }
                writer.WritePropertyName("LastUpdated");
                writer.WriteValue((LastUpdated - DateTime.UnixEpoch).TotalSeconds);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        static public DataFile Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var dataFile = new DataFile();
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
                                    dataFile.Guid = new Guid((string)reader.Value);
                                    break;
                                case "Name":
                                    dataFile.Name = (string)reader.Value;
                                    break;
                                case "Description":
                                    dataFile.Description = (string)reader.Value;
                                    break;
                                case "YahooCode":
                                    dataFile.YahooCode = (string)reader.Value;
                                    break;
                                case "YahooStart":
                                    seconds = (long)reader.Value;
                                    dataFile.YahooStart = DateTime.UnixEpoch.AddSeconds(seconds);
                                    break;
                                case "Watching":
                                    dataFile.Watching = (bool)reader.Value;
                                    break;
                                case "TradingPrompt":
                                    dataFile.TradingPrompt = (string)reader.Value;
                                    break;
                                case "TradingNotes":
                                    dataFile.TradingNotes = (string)reader.Value;
                                    break;
                                case "ZeroVolume":
                                    dataFile.ZeroVolume = (bool)reader.Value;
                                    break;
                                case "WatchAmount":
                                    dataFile.WatchAmount = (double)reader.Value;
                                    break;
                                case "HasReadHistorical":
                                    dataFile.HasReadHistorical = (bool)reader.Value;
                                    break;
                                case "DatasetGuid":
                                    dataFile.DatasetGuid = new Guid((string)reader.Value);
                                    break;
                                case "Image":
                                    dataFile.Image = Convert.FromBase64String((string)reader.Value);
                                    break;
                                case "LastUpdated":
                                    dataFile.LastUpdated = DateTime.UnixEpoch.AddSeconds((double)reader.Value);
                                    break;
                            }
                        }
                        break;
                }
            }

            return dataFile;
        }

        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        #endregion

        public static HashTable<DataFile> Hash = new HashTable<DataFile>();

        internal static bool Save(DataFile o, string tableName, List<Repository.Column> tableColumns, bool syncing = false)
        {
            int id = (int)o.GetType().GetProperty("Id").GetValue(o, null);
            bool inserted = false;
            var command = Repository.Connection.CreateCommand();
            command.CommandText = Repository.SelectCommand(tableName, tableColumns, true) + " WHERE id=" + id.ToString();
            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.UpdateCommand(tableName, tableColumns);
                if( o.Image != null )
                {
                    DebugHelper.WriteLine("hello");
                }
                Repository.AddParameters(command, tableColumns, o, true);
                command.ExecuteNonQuery();
                if (o.ShouldSync())
                {
                    string json = o.Serialise();
                    DebugHelper.WriteLine(json);
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
                        id = (int)(long)reader["ID"];
                        o.GetType().GetProperty("Id").SetValue(o, id);
                        inserted = true;
                    }
                    reader.Close();
                }
                if (o.ShouldSync() && !syncing)
                {
                    string json = o.Serialise();
                    Transaction.Add("insert", tableName, o.Id, json);
                }
            }
            return inserted;
        }


        // Requires a parameterless constructor

        internal static List<DataFile> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, string value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<DataFile> results = new List<DataFile>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "'";
            DebugHelper.WriteLine("CommandText = " + command.CommandText);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                DataFile result = new DataFile();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }

        public DataFile()
        {
        }

        public void Initialise()
        {
            interim = new List<bool>();
            barId = new List<int>();
            mCache = new Dictionary<string, WeakReference>();
            weekCache = new Dictionary<string, WeakReference>();
            bars = new Bars(Name, mCache);
            weekBars = new Bars(Name, weekCache);
        }

        static public DataFile GetPath(string path)
        {
            List<DataFile> files = GetEquals("Path", path);
            if (files.Count > 0)
            {
                DataFile dataFile = files[0];
                dataFile.Initialise();
                return files[0];
            }
            else
                return null;
        }

        public void Write()
        {
            System.IO.StreamWriter CodeFileStreamWriter = null;
            int lineNumber = 227;
            try
            {
                // CodeFileStreamWriter = new System.IO.StreamWriter(Path, false);

                var connection = Repository.Connection;


                var idCommand = connection.CreateCommand();
                idCommand.CommandText = "SELECT last_insert_rowid() AS ID";



                var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = "SELECT yahoo_code,date,open,high,low,close,volume,interim,id FROM bars WHERE yahoo_code = @YahooCode";
                selectCommand.Parameters.AddWithValue("@YahooCode", YahooCode);
                selectCommand.Parameters.AddWithValue("@Date", (long) 0);

                var actions = new List<(int index,bool found,long ticks, int id)>();

                var reader = selectCommand.ExecuteReader();

                var count = bars.Count;
                for(int i=0; i<count; i++ )
                {
                    actions.Add((i,false,bars.Date[i].Ticks,0));
                }


                while(reader.Read())
                {
                    long ticks = reader.GetInt64(1);
                    int id = reader.GetInt32(8);
                    int index = actions.FindIndex(a => (long) a.ticks == (long) ticks);
                    lineNumber = 260;
                    if( index >= 0 )
                        actions[index] = (actions[index].index, true, ticks,id);
                }

                lineNumber = 265;
                reader.Close();

                var transaction = connection.BeginTransaction();

                lineNumber = 270;
                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = "UPDATE bars SET yahoo_code = @YahooCode,date = @Date,open = @Open,high = @High,low = @Low,close = @Close,volume = @Volume,interim = @Interim WHERE id = @Id";
                updateCommand.Parameters.AddWithValue("@YahooCode", "Some Code");
                updateCommand.Parameters.AddWithValue("@Date", long.MaxValue);
                updateCommand.Parameters.AddWithValue("@Open", 0.1);
                updateCommand.Parameters.AddWithValue("@High", 0.1);
                updateCommand.Parameters.AddWithValue("@Low", 0.1);
                updateCommand.Parameters.AddWithValue("@Close", 0.1);
                updateCommand.Parameters.AddWithValue("@Volume", 0.1);
                updateCommand.Parameters.AddWithValue("@Interim", 0.1);
                updateCommand.Parameters.AddWithValue("@Id", (int) 1);

                lineNumber = 283;
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO bars (yahoo_code,date,open,high,low,close,volume,interim) VALUES (@YahooCode,@Date,@Open,@High,@Low,@Close,@Volume,@Interim)";
                insertCommand.Parameters.AddWithValue("@YahooCode", "Some Code");
                insertCommand.Parameters.AddWithValue("@Date", long.MaxValue);
                insertCommand.Parameters.AddWithValue("@Open", (float) 0.1);
                insertCommand.Parameters.AddWithValue("@High", (float) 0.1);
                insertCommand.Parameters.AddWithValue("@Low", (float) 0.1);
                insertCommand.Parameters.AddWithValue("@Close", (float)  0.1);
                insertCommand.Parameters.AddWithValue("@Volume", (float) 0.1);
                insertCommand.Parameters.AddWithValue("@Interim", (int) 1);

                foreach (var action in actions)
                {
                    lineNumber = 297;
                    int id = action.id;
                    int index = action.index;
                    if (action.found)
                    {
                        try
                        {
                            var testDate = bars.Date[index];
                            if (testDate > new DateTime(2025,1,1) ) {
                                var testClose = bars.Close[index];
                            }
                            updateCommand.Parameters["@YahooCode"].Value = YahooCode ?? "";
                            updateCommand.Parameters["@Date"].Value = bars.Date[index].Ticks;
                            updateCommand.Parameters["@Open"].Value = bars.Open[index];
                            updateCommand.Parameters["@High"].Value = bars.High[index];
                            updateCommand.Parameters["@Low"].Value = bars.Low[index];;
                            updateCommand.Parameters["@Close"].Value = bars.Close[index];
                            updateCommand.Parameters["@Volume"].Value = bars.Volume[index];
                            updateCommand.Parameters["@Interim"].Value = interim[index] ? 1 : 0;
                            updateCommand.Parameters["@Id"].Value = (int) id;
                            // Repository.AddParameters(command, tableColumns, bar, true);
                        }
                        catch (Exception e)
                        {
                            string message = e.Message;
                        }
                        lineNumber = 329;
                        var rowsAffected = updateCommand.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            lineNumber = 331;
                        }

                    }
                    else
                    {
                        // command.CommandText = Repository.InsertCommand(tableName, tableColumns);
                        
                        try
                        {
                            // Repository.AddParameters(command, tableColumns, bar);
                            lineNumber = 336;
                            insertCommand.Parameters["@YahooCode"].Value = YahooCode ?? "";
                            insertCommand.Parameters["@Date"].Value = bars.Date[index].Ticks; 
                            insertCommand.Parameters["@Open"].Value = bars.Open[index];
                            insertCommand.Parameters["@High"].Value = bars.High[index];
                            insertCommand.Parameters["@Low"].Value = bars.Low[index];
                            insertCommand.Parameters["@Close"].Value = bars.Close[index];
                            insertCommand.Parameters["@Volume"].Value = bars.Volume[index];
                            insertCommand.Parameters["@Interim"].Value = interim[index] ? 1 : 0;
                        }
                        catch (Exception e)
                        {
                            string message = e.Message;
                        }
                        insertCommand.ExecuteNonQuery();
                    }
                }

                lineNumber = 346;
                transaction.Commit();
                updateCommand.Dispose();
                insertCommand.Dispose();
                idCommand.Dispose();
            }
            catch (Exception exception)
            {
                System.Diagnostics.StackTrace stack = new System.Diagnostics.StackTrace(exception, true);
                string stringLineNumber = stack.GetFrame(3).GetFileLineNumber().ToString();
                string message = "(" + lineNumber + ") " + exception.Message;
#if __IOS__
                UIKit.UIAlertController alertController = UIAlertController.Create("DataFile(130)", exception.Message, UIAlertControllerStyle.Alert);
                alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, alert => Console.WriteLine("DataFile(130): " + exception.Message)));
                UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alertController, true, null);

#endif
#if __MACOS__
                AppKit.NSAlert alert = new AppKit.NSAlert();
                alert.MessageText = message;
                alert.RunSheetModal(NSApplication.SharedApplication.MainWindow);
#endif
#if __WINDOWS__
                MessageBox.Show(exception.Message);
#endif
            }
            finally
            {
                if(CodeFileStreamWriter != null)
                    CodeFileStreamWriter.Close();
            }
        }

        public static List<DataFile> GetAll()
        {
            var dataFiles = new List<DataFile>();
            var dbCommand = Repository.Connection.CreateCommand();
            dbCommand.CommandText = "SELECT id, path, name, description, yahoo_code, yahoo_start, watching, trading_prompt, trading_notes, watch_amount, has_read_historical, image, dataset_guid, last_updated FROM 'datafiles'";
            var dbReader = dbCommand.ExecuteReader();

            while (dbReader.Read())
            {
                try
                {
                    int id = dbReader.GetInt32(0);

                    long ticks = dbReader.IsDBNull(5) ? DateTime.MinValue.Ticks : dbReader.GetInt64(5);
                    string name = name = dbReader.IsDBNull(2) ? "" : dbReader.GetString(2);
                    byte[] bytes = new byte[0];
                    if (!dbReader.IsDBNull(11))
                    {
                        using (var ms = new MemoryStream())
                        {
                            using (var stream = dbReader.GetStream(11))
                            {
                                stream.CopyTo(ms);
                            }
                            bytes = ms.ToArray();
                        }
                    }

                    Guid datasetGuid = dbReader.IsDBNull(12) ? Guid.Empty : new Guid(dbReader.GetString(12));

                    var dataFile = new DataFile()
                    {
                        Id = id,
                        Name = name,
                        Path = dbReader.IsDBNull(1) ? "" : dbReader.GetString(1),
                        Description = dbReader.IsDBNull(3) ? "" : dbReader.GetString(3),
                        YahooCode = dbReader.IsDBNull(4) ? "" : dbReader.GetString(4),
                        YahooStart = new DateTime(ticks),
                        Watching = dbReader.IsDBNull(6) ? false : dbReader.GetInt32(6) == 1,
                        TradingPrompt = dbReader.IsDBNull(7) ? "" : dbReader.GetString(7),
                        TradingNotes = dbReader.IsDBNull(8) ? "" : dbReader.GetString(8),
                        // DatasetId = dbReader.IsDBNull(9) ? 0 : dbReader.GetInt32(9),
                        WatchAmount = dbReader.IsDBNull(9) ? 0 : dbReader.GetDouble(9),
                        HasReadHistorical = dbReader.IsDBNull(10) ? false : dbReader.GetInt32(10) != 0,
                        Image = bytes,
                        
                        DatasetGuid = datasetGuid,
                        LastUpdated = dbReader.IsDBNull(13) ? new DateTime(1900,1,1) : DateTime.UnixEpoch.AddSeconds(dbReader.GetDouble(13)),
                    };
                    double quantity = Trade.GetCurrentQuantity(dataFile.YahooCode);
                    bool isTrading = quantity > Sharesies.epsilon;
                    dataFile.IsTrading = isTrading;
                    dataFiles.Add(dataFile);
                }
                catch (Exception e)
                {
                    DebugHelper.WriteLine(e);
                }
            }
            dbReader.Close();

            dataFiles.Sort((x, y) => string.Compare(x.Title.ToUpper(), y.Title.ToUpper(), StringComparison.CurrentCulture));
            return dataFiles;
        }



        public static DataFiles GetAll(DataSet dataSet)
        {
            DataFiles dataFiles = new DataFiles();
            dataFiles.dataset = dataSet;

            /*
            DirectoryInfo datasetDirectoryInfo = string.IsNullOrEmpty(dataSet.Path) ? null : new DirectoryInfo(dataSet.Path);
            FileInfo[] rgFiles = datasetDirectoryInfo.GetFiles("*.txt", new EnumerationOptions());
            foreach (FileInfo fi in rgFiles)
            {
                // Set up the pathname and filename for this thing
                DataFile datafile = DataFile.GetPath(fi.FullName);
                if (datafile == null)
                {
                    datafile = new DataFile(dataFiles, fi.FullName, fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
                    datafile.DatasetId = dataSet.Id;
                    datafile.Replace();
                }
                else
                {
                    datafile.datafiles = dataFiles;
                    datafile.DatasetId = dataSet.Id; // Don't need this anymore
                    // datafile.Replace();
                    // ReaderCode.Get(datafile);
                    // dataFile.ReaderCodes.ReaderCodeChangedEvent += ReaderCodeChanged_EventHandler;
                }
                
                datafile.ReadImageFromFile();
                datafile.IsTrading = Trade.GetCurrentQuantity(datafile.YahooCode) > Sharesies.epsilon;
                // datafile.Replace();
                // Add the file in
                dataFiles.Add(datafile);
            }
            */

            var dbCommand = Repository.Connection.CreateCommand();
            dbCommand.CommandText = "SELECT id, path, name, description, yahoo_code, yahoo_start, watching, trading_prompt, trading_notes, watch_amount, has_read_historical, image, dataset_guid, last_updated FROM 'datafiles' WHERE dataset_guid='" + dataSet.Guid.ToString() + "'";
            
            var dbReader = dbCommand.ExecuteReader();

            while (dbReader.Read())
            {
                try
                {
                    int id = dbReader.GetInt32(0);
                    if (dataFiles.Find(df => df.Id == id) == null)
                    {
                        long ticks = dbReader.IsDBNull(5) ? DateTime.MinValue.Ticks : dbReader.GetInt64(5);
                        string name = name = dbReader.IsDBNull(2) ? "" : dbReader.GetString(2);
                        byte[]? bytes = null;
                        if (!dbReader.IsDBNull(11))
                        {
                            using (var ms = new MemoryStream())
                            {
                                using (var stream = dbReader.GetStream(11))
                                {
                                    stream.CopyTo(ms);
                                }
                                bytes = ms.ToArray();
                            }
                        }

                        DataFile dataFile = new DataFile()
                        {
                            Id = id,
                            Name = name,
                            Path = dbReader.IsDBNull(1) ? "" : dbReader.GetString(1),
                            Description = dbReader.IsDBNull(3) ? "" : dbReader.GetString(3),
                            YahooCode = dbReader.IsDBNull(4) ? "" : dbReader.GetString(4),
                            YahooStart = new DateTime(ticks),
                            Watching = dbReader.IsDBNull(6) ? false : dbReader.GetInt32(6) == 1,
                            TradingPrompt = dbReader.IsDBNull(7) ? "" : dbReader.GetString(7),
                            TradingNotes = dbReader.IsDBNull(8) ? "" : dbReader.GetString(8),
                            WatchAmount = dbReader.IsDBNull(9) ? 0 : dbReader.GetDouble(9),
                            HasReadHistorical = dbReader.IsDBNull(10) ? false : dbReader.GetInt32(10) != 0,
                            Image = bytes,
                            DatasetGuid = dbReader.IsDBNull(12) ? Guid.Empty : new Guid(dbReader.GetString(12)),
                            LastUpdated = dbReader.IsDBNull(13) ? DateTime.UnixEpoch : DateTime.UnixEpoch.AddSeconds(dbReader.GetDouble(13)),
                        };
                        dataFile.Initialise();
                        double quantity = Trade.GetCurrentQuantity(dataFile.YahooCode);
                        bool isTrading = quantity > Sharesies.epsilon;
                        dataFile.IsTrading = isTrading;
                        dataFile.datafiles = dataFiles;
                        dataFiles.Add(dataFile);
                    }
                }
                catch(Exception e)
                {
                    DebugHelper.WriteLine(e);
                }
            }
            dbReader.Close();

            dataFiles.Sort((x, y) => string.Compare(x.Title.ToUpper(), y.Title.ToUpper(), StringComparison.CurrentCulture));
            return dataFiles;
        }


        public void Add()
        {
            var dbCommand = Repository.Connection.CreateCommand();
            dbCommand.CommandText = "SELECT path, description, yahoo_code, name, id, yahoo_start FROM 'datafiles' WHERE path='" + this.Path + "'";
            var dbReader = dbCommand.ExecuteReader();

            if (!dbReader.Read())
            {

                dbCommand.CommandText = "INSERT INTO datafiles (path) VALUES ('" + this.Path + "')";
                dbCommand.ExecuteNonQuery();

                dbCommand.CommandText = "SELECT last_insert_rowid() AS ID";
                dbReader = dbCommand.ExecuteReader();
                if (dbReader.Read())
                {
                    this.Id = (int)(long)dbReader["ID"];
                    Hash.Add(this, this.Id);
                }
                dbReader.Close();
                Hash.Add(this, this.Id);
            }
            dbReader.Close();
        }

        public void Replace()
        {
            if( Save() )
            {
                Hash.Add(this, this.Id);
            }
        }

        void ReadImageFromFile()
        {
            string[] extensions = { ".png", ".jpg" };

            DirectoryInfo di = new DirectoryInfo(Path);
            string imagePath = Path.Substring(0, Path.Length - di.Extension.Length);

            foreach (string extension in extensions)
            {
                imagePath += ".png";
                Image = null;
                FileInfo fi = new FileInfo(imagePath);
                if (!fi.Exists)
                    continue;

                using (FileStream stream = File.Open(imagePath, FileMode.Open))
                {
                    using (System.IO.BinaryReader reader = new System.IO.BinaryReader(stream))
                    {
                        Image = reader.ReadBytes((int)fi.Length);
                    }
                }

                break;
            }
        }

        public void ReadBarsFromFile()
        {
            mCache.Clear();
            bars.Clear();
            barId.Clear();
            interim.Clear();

            Bar.GetYahooCode(this.YahooCode, bars, interim, barId);
        }

        void ConvertBarsToWeeks()
        {
            DateTime currentWeek = bars.Date[0];

            // This week starts on Monday
            if( currentWeek.DayOfWeek == DayOfWeek.Sunday )
            {
                currentWeek = currentWeek.AddDays(-6);
            }
            else
            {
                currentWeek = currentWeek.AddDays(1- (int)currentWeek.DayOfWeek);
            }

            double open=bars.Open[0], high=bars.High[0], low=bars.Low[0], close=bars.Close[0], volume=bars.Volume[0];

            for(int i=1; i<bars.Count;i++)
            {
                if((bars.Date[i] - currentWeek).TotalDays > 6)
                {
                    weekBars.Add(currentWeek, open, high, low, close, volume);
                    currentWeek = bars.Date[i];
                    if (currentWeek.DayOfWeek == DayOfWeek.Sunday)
                    {
                        currentWeek = currentWeek.AddDays(-6);
                        
                    }
                    else
                    {
                        currentWeek = currentWeek.AddDays(1-(int)currentWeek.DayOfWeek);
                        open = bars.Open[i];
                        high = bars.High[i]; 
                        low = bars.Low[i];
                        close = bars.Close[i];
                        volume = bars.Volume[i];
                    }
                }
                else
                {
                    if ( high < bars.High[i] )
                        high = bars.High[i];
                    if( low > bars.Low[i] )
                        low = bars.Low[i];
                    close = bars.Close[i];
                    volume += bars.Volume[i];
                }
            }

            weekBars.Add(currentWeek, open, high, low, close, volume);
        }

        public static void UpdateGuids()
        {
            var list = GetAll();

            foreach (var o in list)
            {
                if (o.Guid == Guid.Empty)
                {
                    o.Guid = Guid.NewGuid();
                }

                SQLiteCommand command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.UpdateCommand(tableName, tableColumns);
                Repository.AddParameters(command, tableColumns, o, o.Id != 0);
                command.ExecuteNonQuery();
            }
        }

    }
}

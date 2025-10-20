using System;
using System.Collections.Generic;
using System.IO;
#if __IOS__
using Mono.Data.Sqlite;
using UIKit;
#endif
#if __MACOS__
using Mono.Data.Sqlite;
using AppKit;
#endif
#if __WINDOWS__
using System.Data.SQLite;
using System.Windows;
using System.Linq;
#endif

namespace OpenTrader
{
    public partial class Preference : IRepository<Preference>
    {
        static string currentPath = null;
        static string documentsPath = null;
        static string lastTransaction = "";
        static string guid = "";
        static string strategyPath;

        public static string LastTimeStamp = "LastTimeStamp";
        public static string LastTransaction = "LastTransaction";
        public static string Machine = "Machine";
        public static string OpenTraderUser = "OpenTraderUser";
        public static string OpenTraderPassword = "OpenTraderPassword";

        static List<(string name, string value)> defaults = new List<(string name, string value)>();
        public static List<(string name, string value)> Defaults { get { return defaults; } }

        #region properties
        public static string tableName = "setup";
        public static string TableName { get { return "setup"; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "Name", "name", "string", "(25)" ),
                new Repository.Column( "Value", "value", "string", "(25)" ),
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        #endregion properties

        #region implementation of methods
        public bool Save(bool syncing = false) => Repository.Save(this,tableName, tableColumns,syncing);
        public void Remove(bool syncing = false) => Repository.Remove(this,tableName,syncing);
        static public Preferences Get(int id)
        {
            var o = Repository.Get(typeof(Preferences), tableName, tableColumns, id);
            return o as Preferences;
        }
        public bool ShouldSync() => false;
        public string Serialise() => Repository.Serialise(typeof(Preferences),this);

        public void Reset() {
            var dbCommand = Repository.Connection.CreateCommand();
            dbCommand.CommandText = "DELETE FROM setup";
            dbCommand.ExecuteNonQuery();
        }

        #endregion implementation of methods


        public Preference()
        {

        }

        public static string StrategyPath
        {
            get
            {
                if (strategyPath == null || strategyPath == "")
                {
                    var dbCommand = Repository.Connection.CreateCommand();
                    dbCommand.CommandText = "SELECT value FROM setup WHERE name='StrategyPath'";

                    var reader = dbCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        strategyPath = reader.GetString(0);
                        reader.Close();
                    }
                    else
                    {
                        reader.Close();

                        strategyPath = documentsPath + System.IO.Path.DirectorySeparatorChar + "OpenTrader";
                        DirectoryInfo StrategyDir = new DirectoryInfo(strategyPath);
                        if (!StrategyDir.Exists)
                            StrategyDir.Create();

                        dbCommand.CommandText = "INSERT INTO 'setup' ('name','value') VALUES ('StrategyPath','" + strategyPath + "')";
                        dbCommand.ExecuteNonQuery();
                    }
                }

                return strategyPath;
            }
            set
            {
                var dbCommand = Repository.Connection.CreateCommand();
                dbCommand.CommandText = "UPDATE 'setup' SET value='" + value.Replace("'", @"					\'") + "' WHERE name='StrategyPath'";
                dbCommand.ExecuteNonQuery();

                strategyPath = value;
            }
        }

        public static string CurrentPath
        {
            get
            {
                if (currentPath == null || currentPath == "")
                {
                    var dbCommand = Repository.Connection.CreateCommand();
                    dbCommand.CommandText = "SELECT value FROM setup WHERE name='CurrentPath'";

                    var reader = dbCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        currentPath = reader.GetString(0);
                        reader.Close();
                    }
                    else
                    {
                        reader.Close();

                        currentPath = documentsPath;

                        dbCommand.CommandText = "INSERT INTO 'setup' ('name','value') VALUES ('CurrentPath','" + currentPath + "')";
                        dbCommand.ExecuteNonQuery();
                    }
                }

                return currentPath;
            }
            set
            {
                var dbCommand = Repository.Connection.CreateCommand();
                dbCommand.CommandText = "UPDATE 'setup' SET value='" + value.Replace("'", @"					\'") + "' WHERE name='CurrentPath'";
                dbCommand.ExecuteNonQuery();

                currentPath = value;
            }
        }

        public static void CreateTable()
        {
            // Set up datasets
            try
            {
                var dbCommand = Repository.Connection.CreateCommand();
                dbCommand.CommandText = "CREATE TABLE IF NOT EXISTS 'setup' ("
                                    + "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "
                                    + "'name' TEXT(25), "
                                    + "'value' TEXT(255) "
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

        public static Preference Get(string name)
        {
            var item = new Preference();
            item.Name = name;

            var command = Repository.Connection.CreateCommand();
            command.CommandText = "SELECT value,id FROM setup WHERE name='" + name + "'";

            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                item.Value = reader.GetString(0);
                item.Id = reader.GetInt32(1);
                reader.Close();
            }
            else
            {
                reader.Close();
                var index = defaults.FindIndex(d=>d.name == name);
                item.Value = index == -1 ? "" : defaults[index].value;
                command.CommandText = "INSERT INTO '" + tableName + "' ('name','value') VALUES ('" + name + "','" + item.Value + "')";
                command.ExecuteNonQuery();

                command = Repository.Connection.CreateCommand();
                command.CommandText = "SELECT last_insert_rowid() AS ID";
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    item.Id = (int)(long)reader["ID"];
                }
                reader.Close();
            }

            return item;
        }

    }
}

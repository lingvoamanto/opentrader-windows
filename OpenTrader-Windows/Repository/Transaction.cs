using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SoapHttpClient;
using SoapHttpClient.Enums;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

#if __IOS__
using Mono.Data.Sqlite;
using UIKit;
#endif
#if __MACOS__
using Mono.Data.Sqlite;
using AppKit;
#endif

namespace OpenTrader
{
    public class Transaction : IRepository<Transaction>
    {
        public int Id { get; set; }
        public bool ShouldSync() => false;
        public void Remove(bool syncing = false) => Repository.Remove(this, tableName, syncing);
        public bool Save(bool syncing = false) => Save(this, tableName, tableColumns, syncing);
        public string FileName { get; set; }
        public string Method { get; set; }
        public string Data { get; set; }
        public bool Synced { get; set; }
        public int RecordId { get; set; }
        public string Machine { get; set; }
        public DateTime TimeStamp { get; set; }
        public Guid Guid { get; set; }

        static public List<Transaction> GetSynced(bool synced) => GetEquals(tableName, tableColumns, "Synced", false);

        #region database structure
        static string tableName = "transactions";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "FileName", "table_name", "string", "(63)" ),
                new Repository.Column( "Method", "method", "string", "(31)" ),
                new Repository.Column( "Data", "data", "string", "" ),
                new Repository.Column( "Synced", "synced", "bool", "" ),
                new Repository.Column( "Machine", "machine", "string", "" ),
                new Repository.Column( "Guid", "guid", "guid", "" ),
                new Repository.Column( "TimeStamp", "timestamp", "DateTime", "" ),
                new Repository.Column( "RecordId", "table_id", "int", "" ),
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
        #endregion database structure

        internal static List<Transaction> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, bool value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<Transaction> results = new List<Transaction>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "=" + (value ? "1" : "0");
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Transaction result = new Transaction();
                Repository.ReadValues(reader, TableColumns, result, true);
                results.Add(result);
            }
            return results;
        }


        public string Serialise() => "";

        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);

        public void Sync() { }
        

        public static void Add(string method, string tableName, int id, string json)
        {
            var machine = Preference.Get(Preference.Machine);
            Transaction transaction = new Transaction()
            {
                Method = method,
                FileName = tableName,
                Data = json,
                RecordId = id,
                Machine = machine.Value,
                TimeStamp = DateTime.Now
            };
            transaction.Save();
        }

        internal static bool Save(Transaction o, string tableName, List<Repository.Column> tableColumns, bool syncing = false)
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
                Repository.AddParameters(command, tableColumns, o, true);
                command.ExecuteNonQuery();
                if (o.ShouldSync())
                {
                    string json = o.Serialise();
                    Transaction.Add("update", tableName, id, json);
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
                    DebugHelper.WriteLine(json);
                    Transaction.Add("insert", tableName, o.Id, json);
                }
            }

            return inserted;
        }


#if __MACOS__  || __WINDOWS__
        public static async System.Threading.Tasks.Task SyncUp()
        {
            List<Transaction> transactions = Transaction.GetSynced(false);
            foreach (Transaction t in transactions)
            {
            }
        }
#endif
        static bool isSyncing = false;
        static object lockObject = new object();

        public static async System.Threading.Tasks.Task SyncDown()
        {
            bool shouldReturn = false;
            lock(lockObject)
            {
                if (isSyncing)
                    shouldReturn = true;
                isSyncing = true;
            }

            if (shouldReturn)
                return;


            var lastTransaction = Preference.Get(Preference.LastTransaction);
            var lastTimeStamp = Preference.Get(Preference.LastTimeStamp);
            var machine = Preference.Get(Preference.Machine);
            var user = Preference.Get(Preference.OpenTraderUser);
            var password = Preference.Get(Preference.OpenTraderPassword);

            SoapClient.User = user.Value;
            SoapClient.Password = password.Value;
            SoapClient.Machine = machine.Value;

            var transactions  = SoapClient.GetTransactionsFromTime(lastTimeStamp.Value, "100");

            /*
            foreach (Transaction transaction in transactions)
            {
                // We don't need to update transactions for this machine
                try
                {
                    switch(transaction.FileName)
                    {
                        case "transaction":
                            {

                            }
                            break;
                        default:
                            break;
                    }
                }
                catch(Exception debugException)
                {
                    DebugHelper.Alert(debugException);
                    return;
                }

                // Update the last transaction so we don't read it again
                Preference.LastTransaction = transaction.Id.ToString();
            }

            isSyncing = false;
            */
        }

    }
}

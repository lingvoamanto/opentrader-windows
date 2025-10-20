using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OpenTrader.Data
{
    abstract public class CloudObject<T> : RepositoryObject where T : ICloudRepository<T>, new()
    {
        public abstract string Serialise();
        public bool ShouldSync() => true;

        public Guid Guid { get; set; }
        public int Id { get; set; }

        internal bool Save(string tableName, List<Repository.Column> tableColumns, bool syncing = false)
        {
            bool inserted = false;
            var command = Repository.Connection.CreateCommand();

            if (this.Guid == Guid.Empty)
            {
                this.Guid = Guid.NewGuid();
                command.CommandText = Repository.SelectCommand(tableName, tableColumns, true) + " WHERE id=" + this.Id.ToString() + "";
            }
            else
            {
                command.CommandText = Repository.SelectCommand(tableName, tableColumns, true) + " WHERE guid='" + this.Guid.ToString() + "'";
            }

            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.UpdateCommand(tableName, tableColumns);
                Repository.AddParameters(command, tableColumns, this, true);
                command.ExecuteNonQuery();
                if (this.ShouldSync())
                {
                    string json = this.Serialise();
                    Transaction.Add("update", tableName, this.Id, json);
                }
            }
            else
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.InsertCommand(tableName, tableColumns, this.Id != 0);
                Repository.AddParameters(command, tableColumns, this, this.Id != 0);
                command.ExecuteNonQuery();

                if (this.Id == 0)
                {
                    command = Repository.Connection.CreateCommand();
                    command.CommandText = "SELECT last_insert_rowid() AS ID";
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        this.Id = (int)(long)reader["ID"];
                        inserted = true;
                    }
                    reader.Close();
                }
                if (this.ShouldSync() && !syncing)
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
                    string json = this.Serialise();
                    DebugHelper.WriteLine(json);
                    Transaction.Add("insert", tableName, this.Id, json);
#endif
                }
            }

            return inserted;
        }

        internal static List<T> GetAll(string tableName, List<Repository.Column> tableColumns)
        {
            SQLiteCommand command = Repository.Connection.CreateCommand();

            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText;
            DebugHelper.WriteLine("CommandText = " + command.CommandText);
            var reader = command.ExecuteReader();

            var results = new List<T>();
            while (reader.Read())
            {
                var result = new T();
                Repository.ReadValues(reader, tableColumns, result, true);
                results.Add(result);
            }

            reader.Close();
            return results;
        }

        public static void UpdateGuids(string tableName, List<Repository.Column> tableColumns)
        {
            var list = GetAll(tableName, tableColumns);

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

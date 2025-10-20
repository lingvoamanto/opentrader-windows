using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace OpenTrader
{
    public partial class Repository
    {
        public static void Remove(object o, string tableName, bool syncing = false)
        {
            int id = (int)o.GetType().GetProperty("Id").GetValue(o, null);
            var dbCommand = Repository.Connection.CreateCommand();
            dbCommand.CommandText = "DELETE FROM " + tableName + " WHERE id=" + id.ToString();
            dbCommand.ExecuteNonQuery();
            bool shouldSync = (bool)o.GetType().GetMethod("ShouldSync").Invoke(o, null);
            if ( shouldSync && !syncing )
            {
                var method = o.GetType().GetMethod("Serialise",0,new Type[] { } );
                string json = (string) method.Invoke(o, null);
                Transaction.Add("delete", tableName, id, json);
            }
        }

        internal static bool Save(object o, string tableName, List<Repository.Column> tableColumns, bool syncing = false)
        {
            int id = (int)o.GetType().GetProperty("Id").GetValue(o, null);
            bool shouldSync = (bool)o.GetType().GetMethod("ShouldSync").Invoke(o, null);
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
                

                if (shouldSync)
                {
                    string json = (string) o.GetType().GetMethod("Serialise").Invoke(o, null);
                    Transaction.Add("update", tableName, id, json);
                }
            }
            else
            {
                reader.Close();
                command = Repository.Connection.CreateCommand();
                command.CommandText = Repository.InsertCommand(tableName, tableColumns, id != 0);
                Repository.AddParameters(command, tableColumns, o, id != 0);
                command.ExecuteNonQuery();

                if (id == 0)
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
                if (shouldSync && !syncing)
                {
                    string json = (string)o.GetType().GetMethod("Serialise").Invoke(o, null);
                    DebugHelper.WriteLine(json);
                    Transaction.Add("insert", tableName, id, json);
                }
            }

            return inserted;
        }

        static public void CreateTable(string tableName, List<Repository.Column> columns)
        {
            var command = Repository.Connection.CreateCommand();
            string commandText = "CREATE TABLE IF NOT EXISTS '" + tableName + "' ("
            + "id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT ";
            for (int i = 0; i < columns.Count; i++)
            {
                switch (columns[i].Type)
                {
                    case "float":
                        commandText += "," + columns[i].Name + " INTEGER " + columns[i].Modifiers;
                        break;
                    case "double":
                        commandText += "," + columns[i].Name + " REAL " + columns[i].Modifiers;
                        break;
                    case "string":
                        commandText += "," + columns[i].Name + " TEXT " + columns[i].Modifiers;
                        break;
                    case "DateTime":
                        commandText += "," + columns[i].Name + " INTEGER " + columns[i].Modifiers;
                        break;
                    case "UnixTime":
                        commandText += "," + columns[i].Name + " REAL " + columns[i].Modifiers;
                        break;
                    case "int":
                        commandText += "," + columns[i].Name + " INTEGER " + columns[i].Modifiers;
                        break;
                    case "long":
                        commandText += "," + columns[i].Name + " INTEGER " + columns[i].Modifiers;
                        break;
                    case "bool":
                        commandText += "," + columns[i].Name + " INTEGER " + columns[i].Modifiers;
                        break;
                    case "enum":
                        commandText += "," + columns[i].Name + " INTEGER " + columns[i].Modifiers;
                        break;
                }

            }
            commandText += ")";
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }

        static public string Serialise(Type t, object o)
        {
            string json = "";
            try
            {
                DataContractJsonSerializer serializer;
                serializer = new DataContractJsonSerializer(t);
                var stream = new MemoryStream();
                serializer.WriteObject(stream, o);
                json = System.Text.Encoding.ASCII.GetString(stream.ToArray());
                stream.Close();
            }
            catch (Exception e)
            {
                DebugHelper.WriteLine(e);
                return "";
            }
            return json;
        }

        public static object Get(Type t, string tableName, List<Repository.Column> tableColumns, int id)
        {
            var result = t.GetConstructor(new Type[] { t }).Invoke(new object[] { });
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE id='" + id.ToString() + "'";
            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                Repository.ReadValues(reader, tableColumns, result, true);
            }

            return result;
        }

        public static List<object> GetEquals(Type t, string tableName, List<Repository.Column> tableColumns, string field, string value, string orderBy)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<object> results = new List<object>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "' ORDER BY " + orderBy;
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var x = t.GetConstructor(new Type[] {});
                var result = x.Invoke(new object[] { });
                Repository.ReadValues(reader, tableColumns, result, true);
                results.Add(result);
            }
            return results;
        }

        public static List<object> GetEquals(string field, int value) { return null; }
        internal static List<object> GetEquals(Type t, string tableName, List<Repository.Column> tableColumns, string field, int value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<object> results = new List<object>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "=" + value.ToString() + "";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var result = t.GetConstructor(new Type[] {}).Invoke(new object[] { });
                Repository.ReadValues(reader, tableColumns, result, true);
                results.Add(result);
            }
            return results;
        }

        public static List<object> GetEquals(string field, DateTime value) { return null; }
        internal static List<object> GetEquals(Type t,string tableName, List<Repository.Column> tableColumns, string field, DateTime value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<object> results = new List<object>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "=" + value.Ticks.ToString() + "";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var result = t.GetConstructor(new Type[] {}).Invoke(new object[] { });
                Repository.ReadValues(reader, tableColumns, result, true);
                results.Add(result);
            }
            return results;
        }

        public static List<object> GetEquals(string field, bool value) { return null; }
        internal static List<object> GetEquals(Type t, string tableName, List<Repository.Column> tableColumns, string field, bool value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<object> results = new List<object>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "=" + (value ? "1" : "0");
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var result = t.GetConstructor(new Type[] { }).Invoke(new object[] { });
                Repository.ReadValues(reader, tableColumns, result, true);
                results.Add(result);
            }
            return results;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
#if __IOS__
using Mono.Data.Sqlite;
#endif
#if __MACOS__
using Mono.Data.Sqlite;
using FirebaseAdmin;
#endif
#if __WINDOWS__
using System.Data.SQLite;
#endif
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace OpenTrader
{
    public interface IRepository<T> where T : IRepository<T>, new()
    {
        int Id { get; set; }

        // A method is used instead of a property so that the compiler informs the developer
        // of the need to determine whether this is synced or not.
        public bool ShouldSync();

        // This needs to be here because it doesn't work inside the Repository class


        public string Serialise();

     

        public void Remove(bool syncing = false);


        public bool Save(bool syncing=false);


        /// <summary>Method <c>GetEquals</c> returns all records where a field equals
        /// a given value</summary>
        /// <param name="field">The c# naming equivalent of the field to be searched</param>
        /// <param name="value">The value to be searched for</param>
        /// <example>GetEquals("YahooCode","ANZ.NZ")</example>
        ///
        /*
        internal static List<T> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, string value)
        {
            string fieldName = tableColumns.Find(c=>c.Property==field).Name;
            List<T> results = new List<T>();
            SqliteCommand command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value + "'";
            DebugHelper.WriteLine("CommandText = "+ command.CommandText);
            SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                T result = new T();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }
        */




#region table creation


#endregion table creation
    }
}

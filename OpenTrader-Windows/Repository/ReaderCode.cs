using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
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
#endif

using OpenTrader;

namespace OpenTrader.Data
{
    public partial class ReaderCode : IRepository<ReaderCode>
    {
        static public HashTable<ReaderCode> Hash = new HashTable<ReaderCode>();

        #region table structure
        public static string tableName = "reader_codes"; 
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "DataFileId", "data_file_id", "string", "(25)" ),
                new Repository.Column( "Reader", "reader", "string", "(25)" ),
                new Repository.Column( "Code", "code", "string", "(6)" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
        #endregion table structure

        #region interface methods
        public void Remove(bool syncing = false) => Repository.Remove(this,tableName, syncing);
        public bool Save(bool syncing=false) => Repository.Save(this, tableName, tableColumns,syncing);
        public bool Replace() => Repository.Save(this, tableName, tableColumns);
        public bool ShouldSync() => true;
        public string Serialise() => Repository.Serialise(typeof(ReaderCode),this);
        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);
        public void Initialise() { }

        #endregion interface methods

        public ReaderCode()
        {

        }


        static public List<ReaderCode> GetEquals(string field, int value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            List<ReaderCode> results = new List<ReaderCode>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(TableName, TableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "=" + value.ToString() + "";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                ReaderCode result = new ReaderCode();
                Repository.ReadValues(reader, TableColumns, result, true);
                results.Add(result);
            }
            return results;
        }

        #region additional methods
        public static void Get(DataFile dataFile)
        {
            // Get all of the readercodes for this datafile
            List<ReaderCode> readerCodes = GetEquals("DataFileId", dataFile.Id);

            foreach(ReaderCode readerCode in readerCodes)
            {
                dataFile.ReaderCodes.Add(readerCode);
            }
        }

        public void Add()
        {
            if (this.Id == 0)
            {
                if((this as IRepository<DataFile>).Save())
                {
                    Hash.Add(this, this.Id);
                }
            }
        }
        #endregion additional methods
    }
}

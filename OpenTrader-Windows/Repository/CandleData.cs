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
using OpenTrader;
using System.Runtime.Serialization; // used for data contracts
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Linq;
using Accord.Audio;
using Newtonsoft.Json;

namespace OpenTrader.Data
{
    [DataContract]
    public partial class CandleData : CloudObject<CandleData>, ICloudRepository<CandleData>
    {
        public CandleData()
        {
            Id = 0;
        }

        #region data contract
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public Guid DataSetGuid { get; set; }
        [DataMember]
        public int N { get; set; }
        [DataMember]
        public double Sum { get; set; }
        [DataMember]
        public double SumSquared { get; set; }
        [DataMember]
        public int PositiveN { get; set; }
        [DataMember]
        public double PositiveSum { get; set; }
        [DataMember]
        public double PositiveSumSquared { get; set; }
        [DataMember]
        public int NegativeN { get; set; }
        [DataMember]
        public double NegativeSum { get; set; }
        [DataMember]
        public double NegativeSumSquared { get; set; }
        [DataMember]
        public MarketType MarketType { get; set; }
        #endregion data contract

        #region database structure
        static string tableName = "candle_data";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
                new Repository.Column( "Guid", "guid", "guid", "" ),
                new Repository.Column( "Name", "name", "string", "" ),
                new Repository.Column( "DataSetGuid", "dataset_guid", "guid", "" ),
                new Repository.Column( "N", "n", "int", "" ),
                new Repository.Column( "Sum", "sum", "double", "" ),
                new Repository.Column( "SumSquared", "sum_squared", "double", "" ),
                new Repository.Column( "PositiveN", "positive_n", "int", "" ),
                new Repository.Column( "PositiveSum", "positive_sum", "double", "" ),
                new Repository.Column( "PositiveSumSquared", "positive_sum_squared", "double", "" ),
                new Repository.Column( "NegativeN", "negative_n", "int", "" ),
                new Repository.Column( "NegativeSum", "negative_sum", "double", "" ),
                new Repository.Column( "NegativeSumSquared", "negative_sum_squared", "double", "" ),
                new Repository.Column( "MarketType", "market_type", "enum", "" )
        };

        public static List<Repository.Column> TableColumns { get { return tableColumns; } }
        #endregion database structure

        public double ExpectedReturn
        {
            get
            {
                if (N == 0)
                    return 0;

                return (PositiveSum - NegativeSum) / N; ;
            }

        }


        #region required methods
        public bool Save(bool syncing = false) => Save(tableName, tableColumns, syncing);
        public void Remove(bool syncing = false) =>
            Repository.Remove(this, tableName, syncing);
        static public CandleData Get(int id) =>
            Repository.Get(typeof(CandleData), tableName, tableColumns, id) as CandleData;
        static public List<CandleData> GetAll() => GetAll(tableName, tableColumns);
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
                writer.WritePropertyName("DataSetGuid");
                writer.WriteValue(DataSetGuid);
                writer.WritePropertyName("N");
                writer.WriteValue(N);
                writer.WritePropertyName("Sum");
                writer.WriteValue(Sum);
                writer.WritePropertyName("SumSquared");
                writer.WriteValue(SumSquared);
                writer.WritePropertyName("PositiveN");
                writer.WriteValue(PositiveN);
                writer.WritePropertyName("PositiveSum");
                writer.WriteValue(PositiveSum);
                writer.WritePropertyName("PositiveSumSquared");
                writer.WriteValue(PositiveSumSquared);
                writer.WritePropertyName("NegativeN");
                writer.WriteValue(NegativeN);
                writer.WritePropertyName("NegativeSum");
                writer.WriteValue(NegativeSum);
                writer.WritePropertyName("NegativeSumSquared");
                writer.WriteValue(NegativeSumSquared);
                writer.WritePropertyName("MarketType");
                writer.WriteValue(MarketType);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        static public CandleData Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var candleData = new CandleData();
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
                                    candleData.Guid = new Guid((string)reader.Value);
                                    break;
                                case "Name":
                                    candleData.Name = (string)reader.Value;
                                    break;
                                case "DataSetGuid": 
                                    candleData.DataSetGuid = new Guid((string)reader.Value); 
                                    break;
                                case "N": 
                                    candleData.N = (int)reader.Value;
                                    break;
                                case "Sum": 
                                    candleData.Sum = (double)reader.Value; 
                                    break;
                                case "SumSquared": 
                                    candleData.SumSquared = (double)reader.Value; 
                                    break;
                                case "PositiveN":
                                    candleData.PositiveN = (int)reader.Value;
                                    break; 
                                case "PositiveSum":
                                    candleData.PositiveSum = (double)reader.Value;
                                    break;
                                case "PositiveSumSquared":
                                    candleData.PositiveSumSquared = (double)reader.Value;
                                    break;
                                case "NegativeN":
                                    candleData.NegativeN = (int)reader.Value;
                                    break;
                                case "NegativeSum":
                                    candleData.NegativeSum = (double)reader.Value;
                                    break;
                                case "NegativeSumSquared":
                                    candleData.NegativeSumSquared = (double)reader.Value;
                                    break;
                                case "MarketType": 
                                    candleData.MarketType = (MarketType)(int)reader.Value;
                                    break;
                            }
                        }
                        break;
                }
            }

            return candleData;
        }


        public void Initialise() { }
        public static void CreateTable() => Repository.CreateTable(tableName, TableColumns);

        #endregion required methods

        #region additional methods
        internal static List<CandleData> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, int value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            var results = new List<CandleData>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "=" + value;
            // DebugHelper.WriteLine("CommandText = " + command.CommandText);

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                CandleData result = new CandleData();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }

        internal static List<CandleData> GetEquals(string tableName, List<Repository.Column> tableColumns, string field, Guid value)
        {
            string fieldName = tableColumns.Find(c => c.Property == field).Name;
            var results = new List<CandleData>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE " + fieldName + "='" + value.ToString()+"'";
            // DebugHelper.WriteLine("CommandText = " + command.CommandText);

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                CandleData result = new CandleData();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results;
        }

        static public List<CandleData> GetDataSet(Guid value)
        {
            var list = GetEquals(tableName, tableColumns, "DataSetGuid", value);
            return list;
        }
        #endregion additional methods

        static public List<CandleData> GetProfitable(int dataSetId, int n, double profit, double profit_ratio)
        {
            var results = new List<CandleData>();
            var command = Repository.Connection.CreateCommand();
            string commandText = Repository.SelectCommand(tableName, tableColumns, true);
            command.CommandText = commandText + " WHERE dataset_id = " + dataSetId + " AND (positive_sum/cast(positive_n as real)) >= "+profit.ToString()+ " AND (positive_sum /(positive_sum+negative_sum)) >="+profit_ratio.ToString();

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                CandleData result = new CandleData();
                Repository.ReadValues(reader, tableColumns, result, true);
                // 
                results.Add(result);
            }
            return results; 
        }

        public static void UpdateGuids() => UpdateGuids(tableName, tableColumns);


    }
}
using Accord.Audio;
using Newtonsoft.Json;
using OpenTrader.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.Data
{
    public class ScriptFile : CloudObject<ScriptFile>, ICloudRepository<ScriptFile>
    {
        public ScriptFile()
        {
            Language = Language.OpenScript;
            Code = "";
        }

        public string Name { get; set; }
        public Language Language { get; set; }
        public string Code { get; set; }
        public DateTime Modified { get; set; }

        static string tableName = "scripts";
        public static string TableName { get { return tableName; } }

        static List<Repository.Column> tableColumns = new List<Repository.Column>() {
            new Repository.Column( "Guid", "guid", "guid", "" ),
            new Repository.Column( "Name", "name", "string", "" ),
            new Repository.Column( "Modified", "modified", "UnixTime", "" ),
            new Repository.Column( "Language", "language", "int", "" ),
            new Repository.Column( "Code", "code", "string", "" ),
        };

        public void Remove(bool syncing = false) => Repository.Remove(this, tableName, syncing);

        public bool Save(bool syncing = false) => Save(tableName, tableColumns, syncing);

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
                writer.WriteValue(Name.ToString());
                writer.WritePropertyName("Language");
                writer.WriteValue(Language);
                writer.WritePropertyName("Code");
                writer.WriteValue(Code);
                writer.WritePropertyName("Modified");
                writer.WriteValue((Modified - DateTime.UnixEpoch).TotalSeconds);
                writer.WriteEndObject();
            }
            return sb.ToString();
        }

        static public ScriptFile Deserialise(string data)
        {
            var sr = new StringReader(data);
            var reader = new JsonTextReader(sr);
            var scriptFile = new ScriptFile();
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
                            switch (propertyName)
                            {
                                case "Guid":
                                    scriptFile.Guid = new Guid((string)reader.Value);
                                    break;
                                case "Name":
                                    scriptFile.Name = (string)reader.Value;
                                    break;
                                case "Modified":
                                    scriptFile.Modified = DateTime.UnixEpoch.AddSeconds((double)reader.Value);
                                    break;
                                case "Code":
                                    scriptFile.Code = (string)reader.Value;
                                    break;
                                case "Language":
                                    scriptFile.Language = (Language) (int)reader.Value;
                                    break;
                            }
                        }
                        break;
                }
            }

            return scriptFile;
        }


        public static void CreateTable() => Repository.CreateTable(tableName, tableColumns);

        public static List<ScriptFile> GetAll()
        {
            var results = GetAll(tableName, tableColumns);
            results.Sort( (a,b) => a.Name.CompareTo(b.Name));
            return results;
        }

    }
}

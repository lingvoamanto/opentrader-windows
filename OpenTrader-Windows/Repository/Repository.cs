using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
#endif
using System.Reflection;
using SoapHttpClient;
using SoapHttpClient.Enums;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json.Converters;

namespace OpenTrader
{
    public partial class Repository
    {
        public static object lockObject = new object();

        int id;
        virtual public int Id
        {
            get { return id; }
            set { id = value; }
        }

        static public List<TableInfo> TableInfo
        {
            get { return tableInfo; }
        }

        public static object SqliteParameterCollection { get; private set; }

#region build sqllite statements
        static public string InsertCommand(string tableName, List<Column> columns, bool addId=false )
        {
            string insertString = "INSERT INTO " + tableName + " (";
            bool first;

            if (addId)
            {
                insertString += "id";
                first = false; ;
            }
            else
            {
                first = true;
            }

            for (int i = 0; i < columns.Count; i++)
            {
                if (first)
                {
                    insertString += columns[i].Name;
                    first = false;
                }
                else
                {
                    insertString += ","+ columns[i].Name;
                }
            }

            insertString += ") VALUES (";

            if (addId)
            {
                insertString += "@Id";
                first = false; ;
            }
            else
            {
                first = true;
            }

            for (int i = 0; i < columns.Count; i++)
            {
                if (first)
                {
                    insertString += "@"+ columns[i].Property;
                    first = false;
                }
                else
                {
                    insertString += ",@" + columns[i].Property;
                }
            }
            insertString += ")";
            return insertString;
        }

        static public string SelectCommand(string tableName, List<Column> columns, bool includesId=false)
        {
            string selectString = "SELECT ";
            bool first = true;
            for (int i = 0; i < columns.Count; i++)
            {
                if (first)
                {
                    selectString += columns[i].Name;
                    first = false;
                }
                else
                {
                    selectString += "," + columns[i].Name;
                }
            }

            if( includesId)
            {
                selectString += ",id";
            }

            selectString += " FROM "+tableName;
            return selectString;
        }

        static public string UpdateCommand(string tableName, List<Column> columns)
        {
            string updateString = "UPDATE "+tableName+" SET ";
            bool first = true;
            for(int i=0; i<columns.Count; i++)
            {
                if (first)
                {
                    updateString += columns[i].Name + "=@" + columns[i].Property;
                    first = false;
                }
                else
                {
                    updateString += ","+ columns[i].Name + "=@" + columns[i].Property;
                }
            }
            updateString += " WHERE id=@Id";
            return updateString;
        }
        #endregion build sqllite statements

        #region helpers to convert strings to and from underscore and camel case
        /*
        static public string UnderscoreToCamelCase(string name)
        {
            System.Diagnostics.Debug.WriteLine("UnderscoreToCamelCase start.");
            string[] array = name.Split('_');
            for (int i = 0; i < array.Length; i++)
            {
                string s = array[i];
                string first = string.Empty;
                string rest = string.Empty;
                if (s.Length > 0)
                {
                    first = Char.ToUpperInvariant(s[0]).ToString();
                }
                if (s.Length > 1)
                {
                    rest = s.Substring(1).ToLowerInvariant();
                }
                array[i] = first + rest;
            }
            string newname = string.Join("", array);
            System.Diagnostics.Debug.WriteLine("UnderscoreToCamelCase end.");
            return newname;
        }

        static public string CamelCaseToUnderscore(string name)
        {
            string newName = "";
            for(int i=0; i<name.Length; i++)
            {
                string c = name.Substring(i,1);
                if (i == 0)
                {
                    newName += c.ToLowerInvariant();
                }
                else
                {
                    if (c == c.ToUpperInvariant())
                    {
                        newName += "_" + c.ToLowerInvariant();
                    }
                    else
                    {
                        newName += c;
                    }
                }
            }

            return newName;
        }
        */
        #endregion helpers

        #region manage values

#if __IOS__
       static byte[] GetBytes(SqliteDataReader reader)
#endif
#if __MACOS__
       static byte[] GetBytes(SqliteDataReader reader)
#endif
#if __WINDOWS__
        static byte[] GetBytes(SQLiteDataReader reader)
#endif
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead=0;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                try
                {
                    bytesRead = reader.GetBytes((int) 0, fieldOffset, buffer, (int) 0, buffer.Length);
                }
                catch
                {
                    return new byte[0];
                }
                while (bytesRead > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }

#if __IOS__
        static public void ReadValues(SqliteDataReader reader, List<Column> columns, object o, bool readsId=false)
#endif
#if __MACOS__
        static public void ReadValues(SqliteDataReader reader, List<Column> columns, object o, bool readsId=false)
#endif
 #if __WINDOWS__
        static public void ReadValues(SQLiteDataReader reader, List<Column> columns, object o, bool readsId=false)
#endif
        {
            for (int i = 0; i < columns.Count; i++)
            {
                Type type = o.GetType();
                string columnName = columns[i].Name;
                string name = columns[i].Property;

                PropertyInfo info = type.GetProperty(name);
                if (info == null)
                    continue;
                var value = info.GetValue(o, null);
                double doubleValue;
                int intValue;
                string stringValue;
                long longValue;
                bool boolValue;
                float floatValue;
                byte[] image;
                DateTime dateTime;
                Guid guidValue;

                switch (columns[i].Type)
                {
                    case "byte[]":
                        if (reader.IsDBNull(i))
                        {
                            image = null;
                        }
                        else
                        {
                            image = GetBytes(reader);
                            o.GetType().GetProperty(name).SetValue(o, image);
                        }
                        break;
                    case "double":
                        if (reader.IsDBNull(i))
                        {
                            if (columns[i].Modifiers.Contains("null"))
                            {
                                o.GetType().GetProperty(name).SetValue(o, null);
                            }
                            else
                            {
                                o.GetType().GetProperty(name).SetValue(o, (double) 0);
                            }
                        }
                        else
                        {
                            doubleValue = reader.GetDouble(i);
                            o.GetType().GetProperty(name).SetValue(o, doubleValue);
                        }
                        break;
                    case "float":
                        if (reader.IsDBNull(i))
                            floatValue = 0;
                        else
                            floatValue = reader.GetFloat(i);
                        o.GetType().GetProperty(name).SetValue(o, floatValue);
                        break;
                    case "string":
                        if (reader.IsDBNull(i))
                            stringValue = "";
                        else
                            stringValue = reader.GetString(i);
                        o.GetType().GetProperty(name).SetValue(o, stringValue);
                        break;
                    case "DateTime":
                        if (reader.IsDBNull(i))
                            longValue = new DateTime(1900,1,1).Ticks;
                        else if( reader.GetFieldType(i) == typeof(long) )
                            longValue = reader.GetInt64(i);
                        else if (reader.GetFieldType(i) == typeof(int))
                            longValue = (long) reader.GetInt32(i);
                        else 
                            longValue = new DateTime(1900, 1, 1).Ticks;
                        dateTime = new DateTime(longValue);
                        if( dateTime < new DateTime(1900,1,1) )
                            dateTime = new DateTime(1900, 1, 1);
                        o.GetType().GetProperty(name).SetValue(o, dateTime);
                        break;
                    case "UnixTime":
                        if (reader.IsDBNull(i))
                            doubleValue = ((new DateTime(1900, 1, 1)) - DateTime.UnixEpoch).TotalSeconds;
                        else if (reader.GetFieldType(i) == typeof(double))
                            doubleValue = reader.GetDouble(i);
                        else if (reader.GetFieldType(i) == typeof(float))
                            doubleValue = reader.GetFloat(i);
                        else if (reader.GetFieldType(i) == typeof(long))
                            doubleValue = reader.GetInt64(i);
                        else if (reader.GetFieldType(i) == typeof(int))
                            doubleValue = reader.GetInt32(i);
                        else
                            doubleValue = ((new DateTime(1900, 1, 1)) - DateTime.UnixEpoch).TotalSeconds;
                        dateTime = DateTime.UnixEpoch.AddSeconds(doubleValue);
                        if (dateTime < new DateTime(1900, 1, 1))
                            dateTime = new DateTime(1900, 1, 1);
                        o.GetType().GetProperty(name).SetValue(o, dateTime);
                        break;
                    case "int":
                        if (reader.IsDBNull(i))
                            intValue = 0;
                        else
                            intValue = reader.GetInt32(i);
                        o.GetType().GetProperty(name).SetValue(o, intValue);
                        break;
                    case "bool":
                        if (reader.IsDBNull(i))
                            boolValue = false;
                        else
                            boolValue = reader.GetInt32(i) != 0;
                        o.GetType().GetProperty(name).SetValue(o, boolValue);
                        break;
                    case "enum":
                        if (reader.IsDBNull(i))
                            intValue = 0;
                        else
                            intValue = reader.GetInt32(i);
                        o.GetType().GetProperty(name).SetValue(o, intValue);
                        break;
                    case "guid":
                        if (reader.IsDBNull(i))
                        {
                            guidValue = Guid.Empty;
                        }
                        else
                        {
                            stringValue = reader.GetString(i);
                            try
                            {
                                guidValue = new Guid(stringValue);
                            }
                            catch
                            {
                                guidValue = Guid.Empty;
                            }
                        }
                        o.GetType().GetProperty(name).SetValue(o, guidValue);
                        break;
                    default:
                        break;
                }
            }

            if (readsId)
            {
                int id = reader.GetInt32(columns.Count);
                o.GetType().GetProperty("Id").SetValue(o, id);
            }
        }

#if __IOS__
        static public void AddParameters(SqliteCommand command, List<Column> columns, object o, bool addId=false )
        {
            SqliteParameter parameter = null;
#endif
#if __MACOS__
        static public void AddParameters(SqliteCommand command, List<Column> columns, object o, bool addId=false )
        {
            SqliteParameter parameter = null;
#endif
#if __WINDOWS__
        static public void AddParameters(SQLiteCommand command, List<Column> columns, object o, bool addId=false )
        {
            SQLiteParameter parameter = null;
#endif
            for (int i = 0; i < columns.Count; i++)
            {
                string name = columns[i].Property;
                var value = o.GetType().GetProperty(name).GetValue(o, null);
                switch (columns[i].Type)
                {
                    case "byte[]":                     
                        if( value != null )
                        {
#if __IOS__
                            parameter = new SqliteParameter("@" + name, System.Data.DbType.Binary);
#endif
#if __MACOS__
                            parameter = new SqliteParameter("@" + name, System.Data.DbType.Binary);
#endif
#if __WINDOWS__
                            parameter = new SQLiteParameter("@" + name, System.Data.DbType.Binary);
#endif
                            parameter.Value = (byte[])value;
                            command.Parameters.Add(parameter);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@" + name, null);
                        }
                        
                        break;
                    case "double":
                        if (columns[i].Modifiers.Contains("null"))
                        {
                            if( ((double?) value).HasValue)
                                command.Parameters.AddWithValue("@" + name, ((double?) value).Value);
                            else
                                command.Parameters.AddWithValue("@" + name, null);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@" + name, (double)value);
                        }
                        break;
                    case "float":
                        command.Parameters.AddWithValue("@" + name, (float)value);
                        break;
                    case "string":
                        command.Parameters.AddWithValue("@" + name, (string)value ?? "");
                        break;
                    case "DateTime":
                        command.Parameters.AddWithValue("@" + name, ((DateTime) value).Ticks);
                        break;
                    case "UnixTime":
                        command.Parameters.AddWithValue("@" + name, ((DateTime)value - DateTime.UnixEpoch).TotalSeconds);
                        break;
                    case "int":
                        command.Parameters.AddWithValue("@" + name, (int)value);
                        break;
                    case "bool":
                        command.Parameters.AddWithValue("@" + name, (bool)value ? 1 : 0);
                        break;
                    case "enum":
                        command.Parameters.AddWithValue("@" + name, (int)value);
                        break;
                    case "guid":
                        command.Parameters.AddWithValue("@" + name, ((Guid)value).ToString());
                        break;
                    default:
                        break;
                }
            }

            if (addId)
            {
                var id = o.GetType().GetProperty("Id").GetValue(o, null);
                command.Parameters.AddWithValue("@Id", (int) id);
            }
        }
#endregion manage values

#region table creation
        static List<TableInfo> tableInfo = new List<TableInfo>();


        static public void CreateTables()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                string typename = type.Name;
                if (type.BaseType != null)
                {

                    // Check for any tables using IRepository, the much more elegant solution
                    var interfaces = type.GetInterfaces();
                    foreach (var i in interfaces)
                    {
                        if( i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<>))
                        {
                            MethodInfo createTable = type.GetMethod("CreateTable");
                            if (createTable != null)
                            {
                                /*
                                PropertyInfo nameProperty = type.GetProperty("TableName",typeof(string));
                                object tableName = nameProperty.GetValue(null);

                                tableInfo.Add(new TableInfo() { Name=(string) tableName, Type = i } );

                                PropertyInfo columnsProperty = type.GetProperty("TableColumns");
                                object tableColumns =  columnsProperty.GetValue(null);

                                createTable.Invoke(null, new object[] { tableName, tableColumns } );
                                */
                                createTable.Invoke(null, new object[] { });
                            }
                        }
                    }


                    // Check for anything that has inherited from Repository, the much less elegant solution
                    // and one that we should eventually do away with
                    if (type.BaseType.Name == "Repository" && typename != "Repository")
                    {
                        var methods = type.GetMethods();
                        MethodInfo createInfo = type.GetMethod("CreateTable",BindingFlags.Static | BindingFlags.Public );
                        if (createInfo != null)
                        {
                            createInfo.Invoke(null, null);
                        }

                        PropertyInfo nameProperty = type.GetProperty("TableName", typeof(string));
                        if (nameProperty != null)
                        {
                            object tableName = nameProperty.GetValue(null);
                            tableInfo.Add(new TableInfo() { Name = (string)tableName, Type = type });
                        }
                    }
                }
            }
        }
#endregion table creation

#region rubbish
        static public string UpdateString(string columns)
        {
            string[] names = columns.Split(',');
            string updateString = "(";
            bool first = false;
            foreach (string name in names)
            {
                if (first)
                {
                    updateString += "name=?";
                    first = false;
                }
                else
                {
                    updateString += ",name=?";
                }
            }
            updateString += ")";
            return updateString;
        }
#endregion rubbish

#region transactions       

        static bool isSyncing = false;

        static public object Deserialise(Type t, string data)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(t);
            var stream = new MemoryStream();

            ASCIIEncoding encoding = new ASCIIEncoding();

            stream.Write(encoding.GetBytes(data), 0, data.Length);
            stream.Position = 0;
            object o = serializer.ReadObject(stream);
            return o;
        }

        #endregion transactions
    }
}

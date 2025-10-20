using System;
using System.IO;
#if __IOS__
using Mono.Data.Sqlite;
#endif
#if __MACOS__
using Mono.Data.Sqlite;
#endif
#if __WINDOWS__
using System.Data.SQLite;
#endif

namespace OpenTrader
{
    public partial class Repository
    {

        private static string connectionString;
        private static string documentsPath;
        private static string appDataPath;
        private static string configFile;

        public static string DocumentsPath
        {
            get
            {
                string path = documentsPath + Path.DirectorySeparatorChar + "OpenTrader";
                System.IO.Directory.CreateDirectory(path);
                return path;
            }
        }

#if __IOS__
        protected static SqliteConnection dbConnection;
#endif
#if __MACOS__
        protected static SqliteConnection dbConnection;
#endif
#if __WINDOWS__
        protected static SQLiteConnection dbConnection;
#endif

#if __IOS__
        static public SqliteConnection Connection
#endif
#if __MACOS__
        static public SqliteConnection Connection
#endif
#if __WINDOWS__
        static public SQLiteConnection Connection
#endif

        {
            get
            {
                if (dbConnection == null)
                {
                    documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                    appDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    appDataPath += System.IO.Path.DirectorySeparatorChar + "Lucis" + System.IO.Path.DirectorySeparatorChar + "OpenTrader";
                    configFile = appDataPath + System.IO.Path.DirectorySeparatorChar + "Config.db";
                    connectionString = "Data Source=" + configFile + "; Version=3";

                    DirectoryInfo ConfigDir = new DirectoryInfo(appDataPath);
                    if (!ConfigDir.Exists)
                        ConfigDir.Create();

#if __IOS__
                    dbConnection = new SqliteConnection(connectionString);
#endif
#if __MACOS__
                    dbConnection = new SqliteConnection(connectionString);
#endif
#if __WINDOWS__
                    dbConnection = new SQLiteConnection(connectionString);
#endif
                    dbConnection.Open();
                }
                return dbConnection;
            }
        }

        public Repository()
        {
            if (dbConnection == null)
            {
                documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                appDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                appDataPath += System.IO.Path.DirectorySeparatorChar + "Lucis" + System.IO.Path.DirectorySeparatorChar + "OpenTrader";
                configFile = appDataPath + System.IO.Path.DirectorySeparatorChar + "Config.db";
                connectionString = "Data Source=" + configFile + "; Version=3";

                DirectoryInfo ConfigDir = new DirectoryInfo(appDataPath);
                if (!ConfigDir.Exists)
                    ConfigDir.Create();

#if __IOS__
                dbConnection = new SqliteConnection(connectionString);
#endif
#if __MACOS__
                dbConnection = new SqliteConnection(connectionString);
#endif
#if __WINDOWS__
                dbConnection = new SQLiteConnection(connectionString);
#endif
                dbConnection.Open();
            }
        }
    }
}

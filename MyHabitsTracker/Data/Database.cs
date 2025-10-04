using System.IO;
using Microsoft.Data.Sqlite;

namespace MyHabitsTracker.Data
{
    /**
     * @brief Database helper for SQLite file location and connection.
     *
     * Places DB in %APPDATA%/MyHabitsTracker/mht.db and ensures PRAGMA foreign_keys is turned on.
     */
    public static class Database
    {
        /**
         * @brief Get path to application folder under %APPDATA%.
         * @return Full path to the application's folder.
         */
        private static string GetAppFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "MyHabitsTracker");
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        /**
         * @brief Path to the SQLite file.
         */
        public static string DbPath
        {
            get
            {
                string folder = GetAppFolder();
                return Path.Combine(folder, "mht.db");
            }
        }

        /**
         * @brief Create and open an SQLite connection. PRAGMA foreign_keys is enabled.
         * @return Open SqliteConnection (caller must dispose it).
         */
        public static SqliteConnection GetConnection()
        {
            SqliteConnectionStringBuilder csb = new SqliteConnectionStringBuilder();
            csb.DataSource = DbPath;
            csb.ForeignKeys = true;
            string connStr = csb.ToString();

            SqliteConnection conn = new SqliteConnection(connStr);
            conn.Open();

            using (SqliteCommand pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }

            return conn;
        }

        /**
         * @brief Ensure database file exists and schema initialized.
         *
         * If DB does not exist, creates tables and index required by the application.
         */
        public static void EnsureDatabase()
        {
            if (File.Exists(DbPath))
            {
                return;
            }

            SqliteConnection conn = GetConnection();
            try
            {
                using (SqliteTransaction tx = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText =
                            "CREATE TABLE IF NOT EXISTS Habits ( " +
                            "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "Title TEXT NOT NULL, " +
                            "Description TEXT, " +
                            "IsPositive INTEGER NOT NULL DEFAULT 1, " +
                            "TrackType INTEGER NOT NULL, " +
                            "TargetValue REAL NULL, " +
                            "CreatedAt TEXT NOT NULL, " +
                            "UpdatedAt TEXT NOT NULL " +
                            "); " +
                            "CREATE TABLE IF NOT EXISTS HabitEntries ( " +
                            "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                            "HabitId INTEGER NOT NULL, " +
                            "Date TEXT NOT NULL, " +
                            "Completed INTEGER NOT NULL DEFAULT 0, " +
                            "NumericValue REAL NULL, " +
                            "DurationSeconds INTEGER NULL, " +
                            "Note TEXT NULL, " +
                            "CreatedAt TEXT NOT NULL, " +
                            "FOREIGN KEY (HabitId) REFERENCES Habits(Id) ON DELETE CASCADE " +
                            "); " +
                            "CREATE UNIQUE INDEX IF NOT EXISTS idx_habitentry_habitid_date ON HabitEntries(HabitId, Date);";
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }
    }
}
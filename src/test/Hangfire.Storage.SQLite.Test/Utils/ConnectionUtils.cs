using System;
using SQLite;

namespace Hangfire.Storage.SQLite.Test.Utils
{
    public static class ConnectionUtils
    {
        public static SQLiteStorage CreateStorage()
        {
            var storageOptions = new SQLiteStorageOptions();

            return CreateStorage(storageOptions);
        }

        public static SQLiteStorage CreateStorage(SQLiteStorageOptions storageOptions)
        {
            // See SQLite Docs: https://www.sqlite.org/c3ref/c_open_autoproxy.html
            const SQLiteOpenFlags SQLITE_OPEN_MEMORY = (SQLiteOpenFlags) 0x00000080;
            const SQLiteOpenFlags SQLITE_OPEN_URI = (SQLiteOpenFlags) 0x00000040;
            const SQLiteOpenFlags flags = // open the database in memory
                // SQLITE_OPEN_MEMORY |
                // for whatever reason, if we don't use URI-mode, shared in-memory databases dont work.
                SQLITE_OPEN_URI |
                // open the database in read/write mode
                SQLiteOpenFlags.ReadWrite |
                // create the database if it doesn't exist
                SQLiteOpenFlags.Create |
                // enable multi-threaded database access
                SQLiteOpenFlags.NoMutex;

            var dbId = $"file:hangfire_{Guid.NewGuid():n}.db?mode=memory&cache=shared";
            return new SQLiteStorage(new SQLiteDbConnectionFactory(() =>
                    new SQLiteConnection(dbId,
                        flags,
                        storeDateTimeAsTicks: true)
                    {
                        BusyTimeout = TimeSpan.FromSeconds(10),
                    }),
                storageOptions);
        }

        /// <summary>
        /// Only use this if you have a single thread.
        /// For multi-threaded tests, use <see cref="CreateStorage()"/> directly and
        /// then call <see cref="SQLiteStorage.CreateAndOpenConnection"/> per Thread.
        /// </summary>
        public static HangfireDbContext CreateConnection()
        {
            return CreateStorage().CreateAndOpenConnection();
        }
    }
}
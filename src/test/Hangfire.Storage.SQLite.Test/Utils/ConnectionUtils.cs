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

            const SQLiteOpenFlags flags = // open the database in memory
                SQLITE_OPEN_MEMORY |
                // open the database in read/write mode
                SQLiteOpenFlags.ReadWrite |
                // create the database if it doesn't exist
                SQLiteOpenFlags.Create |
                // enable multi-threaded database access
                SQLiteOpenFlags.SharedCache;

            var connection = new SQLiteConnection($"hangfire_{Guid.NewGuid():n}.db",
                flags,
                storeDateTimeAsTicks: true);
            return new SQLiteStorage(connection, storageOptions);
        }

        public static HangfireDbContext CreateConnection()
        {
            return CreateStorage().Connection;
        }
    }
}
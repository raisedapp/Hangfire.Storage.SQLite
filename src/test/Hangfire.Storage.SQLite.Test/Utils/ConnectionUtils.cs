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

        /// <summary>
        /// For multi-threaded tests make sure to call <see cref="SQLiteStorage.CreateAndOpenConnection"/> per Thread.
        /// For proper testing always ensure that at least ONE in-memory connection is alive
        /// so that the in-memory database will not be deleted unexpectedly
        /// </summary>
        public static SQLiteStorage CreateStorage(SQLiteStorageOptions storageOptions)
        {
            const SQLiteOpenFlags flags = // open the database in memory
                // URI Mode is required to have multiple separate-inmemory
                // databases, and allow shared access
                (SQLiteOpenFlags) SQLitePCL.raw.SQLITE_OPEN_URI |
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
    }
}
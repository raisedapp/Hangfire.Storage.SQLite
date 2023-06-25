using Hangfire.Server;
using SQLite;
using System;
using System.Collections.Generic;
using Hangfire.Logging;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteStorage : JobStorage
    {
        private readonly string _connectionString;

        private readonly SQLiteStorageOptions _storageOptions;

        /// <summary>
        /// Database context
        /// </summary>
        public HangfireDbContext Connection { get; }

        /// <summary>
        /// Queue providers collection
        /// </summary>
        public PersistentJobQueueProviderCollection QueueProviders { get; }

        /// <summary>
        /// Constructs Job Storage by database connection string
        /// </summary>
        /// <param name="databasePath">SQLite connection string</param>
        public SQLiteStorage(string databasePath)
            : this(databasePath, new SQLiteStorageOptions())
        {
        }

        /// <summary>
        /// Constructs Job Storage by database connection string and options
        /// </summary>
        /// <param name="databasePath">SQLite connection string</param>
        /// <param name="storageOptions">Storage options</param>
        public SQLiteStorage(string databasePath, SQLiteStorageOptions storageOptions)
            : this(new SQLiteConnection(
                    string.IsNullOrWhiteSpace(databasePath) ? throw new ArgumentNullException(nameof(databasePath)) : databasePath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex,
                    storeDateTimeAsTicks: true
                ), storageOptions)
        {
        }

        /// <summary>
        /// Constructs Job Storage by database connection string and options
        /// </summary>
        /// <param name="dbConnection">SQLite connection</param>
        /// <param name="storageOptions">Storage options</param>
        public SQLiteStorage(SQLiteConnection dbConnection, SQLiteStorageOptions storageOptions)
        {
            if (dbConnection == null)
            {
                throw new ArgumentNullException(nameof(dbConnection));
            }

            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));

            Connection = new HangfireDbContext(dbConnection, storageOptions.Prefix);
            Connection.Init(_storageOptions);

            var defaultQueueProvider = new SQLiteJobQueueProvider(_storageOptions);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);
        }

        public override IStorageConnection GetConnection()
        {
            return new HangfireSQLiteConnection(Connection, _storageOptions, QueueProviders);
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            return new SQLiteMonitoringApi(Connection, QueueProviders);
        }

        /// <summary>
        /// Opens connection to database
        /// </summary>
        /// <returns>Database context</returns>
        public HangfireDbContext CreateAndOpenConnection()
        {
            return Connection;
        }

        /// <summary>
        /// Returns text representation of the object
        /// </summary>
        public override string ToString()
        {
            return $"Connection string: {_connectionString},  prefix: {_storageOptions.Prefix}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
#pragma warning disable 618
        public override IEnumerable<IServerComponent> GetComponents()
#pragma warning restore 618
        {
            yield return new ExpirationManager(this, _storageOptions.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _storageOptions.CountersAggregateInterval);
        }
    }
}
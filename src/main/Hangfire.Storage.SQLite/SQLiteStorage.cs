using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// <param name="connectionString">LiteDB connection string</param>
        public SQLiteStorage(string connectionString)
            : this(connectionString, new SQLiteStorageOptions())
        {
        }

        /// <summary>
        /// Constructs Job Storage by database connection string and options
        /// </summary>
        /// <param name="connectionString">LiteDB connection string</param>
        /// <param name="storageOptions">Storage options</param>
        public SQLiteStorage(string connectionString, SQLiteStorageOptions storageOptions)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _connectionString = connectionString;
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));

            Connection = HangfireDbContext.Instance(connectionString, storageOptions.Prefix);
            Connection.Init(_storageOptions);

            var defaultQueueProvider = new SQLiteJobQueueProvider(_storageOptions);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);
        }

        public override IStorageConnection GetConnection()
        {
            return new SQLiteConnection(Connection, _storageOptions, QueueProviders);
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
            return _connectionString != null
                ? HangfireDbContext.Instance(_connectionString, _storageOptions.Prefix)
                : null;
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

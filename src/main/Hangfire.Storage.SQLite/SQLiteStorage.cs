using Hangfire.Server;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Logging;
using Hangfire.Storage.SQLite.Entities;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteDbConnectionFactory
    {
        private readonly Func<SQLiteConnection> _getConnection;

        public SQLiteDbConnectionFactory(Func<SQLiteConnection> getConnection)
        {
            _getConnection = getConnection;
        }

        public SQLiteConnection Create()
        {
            return _getConnection();
        }
    }

    public class SQLiteStorage : JobStorage
    {
        private readonly string _connectionString;

        private readonly SQLiteDbConnectionFactory _dbConnectionFactory;
        private readonly SQLiteStorageOptions _storageOptions;

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
            : this(new SQLiteDbConnectionFactory(() => new SQLiteConnection(
                string.IsNullOrWhiteSpace(databasePath) ? throw new ArgumentNullException(nameof(databasePath)) : databasePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.NoMutex,
                storeDateTimeAsTicks: true
            ) {BusyTimeout = TimeSpan.FromSeconds(10)}), storageOptions)
        {
        }

        /// <summary>
        /// Constructs Job Storage by database connection string and options
        /// </summary>
        /// <param name="dbConnectionFactory">Factory that creates SQLite connections</param>
        /// <param name="storageOptions">Storage options</param>
        public SQLiteStorage(SQLiteDbConnectionFactory dbConnectionFactory, SQLiteStorageOptions storageOptions)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));

            var defaultQueueProvider = new SQLiteJobQueueProvider(_storageOptions);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);

            using (var dbContext = CreateAndOpenConnection())
            {
                // Use this to initialize the database as soon as possible
                // in case of error, the user will immediately get an exception at startup
            }
        }

        public override IStorageConnection GetConnection()
        {
            var dbContext = CreateAndOpenConnection();
            return new HangfireSQLiteConnection(dbContext, _storageOptions, QueueProviders);
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            var dbContext = CreateAndOpenConnection();
            return new SQLiteMonitoringApi(dbContext, QueueProviders);
        }

        private readonly ConcurrentQueue<PooledHangfireDbContext> _dbContextPool = new ConcurrentQueue<PooledHangfireDbContext>();

        /// <summary>
        /// Opens connection to database
        /// </summary>
        /// <returns>Database context</returns>
        public HangfireDbContext CreateAndOpenConnection()
        {
            if (_dbContextPool.TryDequeue(out var dbContext))
            {
                return dbContext;
            }

            dbContext = new PooledHangfireDbContext(_dbConnectionFactory.Create(), ctx => _dbContextPool.Enqueue(ctx), _storageOptions.Prefix);
            dbContext.Init(_storageOptions);
            return dbContext;
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
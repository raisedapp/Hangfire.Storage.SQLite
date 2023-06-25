using Hangfire.Logging;
using Hangfire.Storage.SQLite.Entities;
using Newtonsoft.Json;
using SQLite;
using System;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// Represents SQLite database context for Hangfire
    /// </summary>
    public class HangfireDbContext
    {
        private readonly ILog Logger = LogProvider.For<HangfireDbContext>();

        /// <summary>
        /// 
        /// </summary>
        public SQLiteConnection Database { get; }

        /// <summary>
        /// 
        /// </summary>
        public SQLiteStorageOptions StorageOptions { get; private set; }

        /// <summary>
        /// SQLite database connection identifier
        /// </summary>
        public string ConnectionId { get; }


        /// <summary>
        /// Starts SQLite database using a connection string for file system database
        /// </summary>
        /// <param name="connection">the database path</param>
        /// <param name="logger"></param>
        /// <param name="prefix">Table prefix</param>
        internal HangfireDbContext(SQLiteConnection connection, string prefix = "hangfire")
        {
            //UTC - Internal JSON
            GlobalConfiguration.Configuration
                .UseSerializerSettings(new JsonSerializerSettings()
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateFormatString = "yyyy-MM-dd HH:mm:ss.fff"
                });

            Database = connection;

            ConnectionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes initial tables schema for Hangfire
        /// </summary>
        public void Init(SQLiteStorageOptions storageOptions)
        {
            StorageOptions = storageOptions;

            InitializePragmas(storageOptions);

            Database.CreateTable<AggregatedCounter>();
            Database.CreateTable<Counter>();
            Database.CreateTable<HangfireJob>();
            Database.CreateTable<HangfireList>();
            Database.CreateTable<Hash>();
            Database.CreateTable<JobParameter>();
            Database.CreateTable<JobQueue>();
            Database.CreateTable<HangfireServer>();
            Database.CreateTable<Set>();
            Database.CreateTable<State>();
            Database.CreateTable<DistributedLock>();

            AggregatedCounterRepository = Database.Table<AggregatedCounter>();
            CounterRepository = Database.Table<Counter>();
            HangfireJobRepository = Database.Table<HangfireJob>();
            HangfireListRepository = Database.Table<HangfireList>();
            HashRepository = Database.Table<Hash>();
            JobParameterRepository = Database.Table<JobParameter>();
            JobQueueRepository = Database.Table<JobQueue>();
            HangfireServerRepository = Database.Table<HangfireServer>();
            SetRepository = Database.Table<Set>();
            StateRepository = Database.Table<State>();
            DistributedLockRepository = Database.Table<DistributedLock>();
        }

        private void InitializePragmas(SQLiteStorageOptions storageOptions)
        {
            try
            {
                Database.ExecuteScalar<string>($"PRAGMA journal_mode = {storageOptions.JournalMode}", Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, () => $"Error set journal mode. Details: {ex}");
            }

            try
            {
                Database.ExecuteScalar<string>($"PRAGMA auto_vacuum = '{(int)storageOptions.AutoVacuumSelected}'", Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, () => $"Error set auto vacuum mode. Details: {ex}");
            }
        }

        public TableQuery<AggregatedCounter> AggregatedCounterRepository { get; private set; }

        public TableQuery<Counter> CounterRepository { get; private set; }

        public TableQuery<HangfireJob> HangfireJobRepository { get; private set; }

        public TableQuery<HangfireList> HangfireListRepository { get; private set; }

        public TableQuery<Hash> HashRepository { get; private set; }

        public TableQuery<JobParameter> JobParameterRepository { get; private set; }

        public TableQuery<JobQueue> JobQueueRepository { get; private set; }

        public TableQuery<HangfireServer> HangfireServerRepository { get; private set; }

        public TableQuery<Set> SetRepository { get; private set; }

        public TableQuery<State> StateRepository { get; private set; }

        public TableQuery<DistributedLock> DistributedLockRepository { get; private set; }
    }
}

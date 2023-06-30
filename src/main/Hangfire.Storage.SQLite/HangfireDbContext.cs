using Hangfire.Logging;
using Hangfire.Storage.SQLite.Entities;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    internal class PooledHangfireDbContext : HangfireDbContext
    {
        private readonly Action<PooledHangfireDbContext> _onDispose;
        public bool PhaseOut { get; set; }

        internal PooledHangfireDbContext(SQLiteConnection connection, Action<PooledHangfireDbContext> onDispose, string prefix = "hangfire") 
            : base(connection, prefix)
        {
            _onDispose = onDispose;
        }

        protected override void Dispose(bool disposing)
        {
            if (PhaseOut)
            {
                base.Dispose(disposing);
                return;
            }
            _onDispose(this);
        }
    }
    
    /// <summary>
    /// Represents SQLite database context for Hangfire
    /// </summary>
    public class HangfireDbContext : IDisposable
    {
        private readonly ILog Logger = LogProvider.For<HangfireDbContext>();

        /// <summary>
        /// 
        /// </summary>
        public SQLiteConnection Database { get; private set; }

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

            TryFewTimesDueToConcurrency(() => InitializePragmas(storageOptions));
            TryFewTimesDueToConcurrency(() => Database.CreateTable<AggregatedCounter>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<Counter>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<HangfireJob>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<HangfireList>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<Hash>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<JobParameter>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<JobQueue>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<HangfireServer>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<Set>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<State>());
            TryFewTimesDueToConcurrency(() => Database.CreateTable<DistributedLock>());
            
            void TryFewTimesDueToConcurrency(Action action, int times = 10)
            {
                var current = 0;
                while (current < times)
                {
                    try
                    {
                        action();
                        return;
                    }
                    catch (SQLiteException e) when (e.Result == SQLite3.Result.Locked)
                    {
                        // This can happen if too many connections are opened
                        // at the same time, trying to create tables
                        Thread.Sleep(10);
                    }
                    current++;
                }
            }
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

        public TableQuery<AggregatedCounter> AggregatedCounterRepository => Database.Table<AggregatedCounter>();

        public TableQuery<Counter> CounterRepository => Database.Table<Counter>();

        public TableQuery<HangfireJob> HangfireJobRepository => Database.Table<HangfireJob>();

        public TableQuery<HangfireList> HangfireListRepository => Database.Table<HangfireList>();

        public TableQuery<Hash> HashRepository => Database.Table<Hash>();

        public TableQuery<JobParameter> JobParameterRepository => Database.Table<JobParameter>();

        public TableQuery<JobQueue> JobQueueRepository => Database.Table<JobQueue>();

        public TableQuery<HangfireServer> HangfireServerRepository => Database.Table<HangfireServer>();

        public TableQuery<Set> SetRepository => Database.Table<Set>();

        public TableQuery<State> StateRepository => Database.Table<State>();

        public TableQuery<DistributedLock> DistributedLockRepository => Database.Table<DistributedLock>();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Database?.Dispose();
                Database = null;
            }
            GC.SuppressFinalize(this);
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
    }
}

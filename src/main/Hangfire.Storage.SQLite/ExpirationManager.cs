using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage.SQLite.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// Represents Hangfire expiration manager for LiteDB database
    /// </summary>
#pragma warning disable CS0618
    public class ExpirationManager : IBackgroundProcess, IServerComponent
#pragma warning restore CS0618
    {
        private const string DistributedLockKey = "locks:expirationmanager";
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(5);

        // This value should be high enough to optimize the deletion as much, as possible,
        // reducing the number of queries. But low enough to cause lock escalations (it
        // appears, when ~5000 locks were taken, but this number is a subject of version).
        // Note, that lock escalation may also happen during the cascade deletions for
        // State (3-5 rows/job usually) and JobParameters (2-3 rows/job usually) tables.
        private const int NumberOfRecordsInSinglePass = 1000;

        private static readonly string[] ProcessedTables =
        {
            DefaultValues.AggregatedCounterTblName,
            DefaultValues.CounterTblName,
            DefaultValues.HangfireJobTblName,
            DefaultValues.HangfireListTblName,
            DefaultValues.SetTblName,
            DefaultValues.HashTblName,
        };

        private static readonly ILog Logger = LogProvider.For<ExpirationManager>();

        private readonly SQLiteStorage _storage;
        private readonly TimeSpan _checkInterval;

        /// <summary>
        /// Constructs expiration manager with one hour checking interval
        /// </summary>
        /// <param name="storage">LiteDb storage</param>
        public ExpirationManager(SQLiteStorage storage)
            : this(storage, TimeSpan.FromHours(1))
        {
        }

        /// <summary>
        /// Constructs expiration manager with specified checking interval
        /// </summary>
        /// <param name="storage">LiteDB storage</param>
        /// <param name="checkInterval">Checking interval</param>
        public ExpirationManager(SQLiteStorage storage, TimeSpan checkInterval)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _checkInterval = checkInterval;
        }

        /// <summary>
        /// Run expiration manager to remove outdated records
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public void Execute([NotNull] BackgroundProcessContext context)
        {
            Execute(context.StoppingToken);
        }

        /// <summary>
        /// Run expiration manager to remove outdated records
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public void Execute(CancellationToken cancellationToken)
        {
            HangfireDbContext connection = _storage.CreateAndOpenConnection();

            foreach (var table in ProcessedTables)
            {
                Logger.Debug($"Removing outdated records from the '{table}' table...");

                int affected;
                do
                {
                    affected = RemoveExpireRows(connection, table);
                } while (affected == NumberOfRecordsInSinglePass);

                Logger.Trace($"Outdated records removed from the '{table}' table...");
            }

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
        }

        private int RemoveExpireRows(HangfireDbContext db, string table) 
        {
            var now = DateTime.UtcNow;
            var deleteScript = $"DELETE FROM [{table}] WHERE rowid IN (SELECT rowid FROM [{table}] WHERE ExpireAt > {DateTime.MinValue.Ticks} AND ExpireAt < {now.Ticks} LIMIT {NumberOfRecordsInSinglePass})";
            int rowsAffected = 0;

            try
            {
                var _lock = new SQLiteDistributedLock(DistributedLockKey, DefaultLockTimeout,
                    db, db.StorageOptions);

                using (_lock) 
                {
                    rowsAffected = db.Database.Execute(deleteScript);
                }
            }
            catch (DistributedLockTimeoutException e) when (e.Resource == DistributedLockKey)
            {
                // DistributedLockTimeoutException here doesn't mean that outdated records weren't removed.
                // It just means another Hangfire server did this work.
                Logger.Log(
                    LogLevel.Debug,
                    () => $@"An exception was thrown during acquiring distributed lock on the {DistributedLockKey} resource within {DefaultLockTimeout.TotalSeconds} seconds. Outdated records were not removed. It will be retried in {_checkInterval.TotalSeconds} seconds.",
                    e);
            }
            catch (Exception)
            {

            }

            return rowsAffected;
        }
    }
}

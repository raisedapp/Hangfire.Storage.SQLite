using System;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteStorageOptions
    {
        private TimeSpan _queuePollInterval;

        private TimeSpan _distributedLockLifetime;

        /// <summary>
        /// Constructs storage options with default parameters
        /// </summary>
        public SQLiteStorageOptions()
        {
            Prefix = "hangfire";
            QueuePollInterval = TimeSpan.FromSeconds(15);
            InvisibilityTimeout = TimeSpan.FromMinutes(30);
            DistributedLockLifetime = TimeSpan.FromSeconds(30);
            JobExpirationCheckInterval = TimeSpan.FromHours(1);
            CountersAggregateInterval = TimeSpan.FromMinutes(5);

            ClientId = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        /// <summary>
        /// Collection name prefix for all Hangfire related collections
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Poll interval for queue
        /// </summary>
        public TimeSpan QueuePollInterval
        {
            get => _queuePollInterval;
            set
            {
                var message = $"The QueuePollInterval property value should be positive. Given: {value}.";

                if (value == TimeSpan.Zero)
                {
                    throw new ArgumentException(message, nameof(value));
                }

                if (value != value.Duration())
                {
                    throw new ArgumentException(message, nameof(value));
                }

                _queuePollInterval = value;
            }
        }

        /// <summary>
        /// Lifetime of distributed lock
        /// </summary>
        public TimeSpan DistributedLockLifetime
        {
            get => _distributedLockLifetime;
            set
            {
                var message = $"The DistributedLockLifetime property value should be positive. Given: {value}.";

                if (value == TimeSpan.Zero)
                {
                    throw new ArgumentException(message, nameof(value));
                }

                if (value != value.Duration())
                {
                    throw new ArgumentException(message, nameof(value));
                }

                _distributedLockLifetime = value;
            }
        }

        /// <summary>
        /// ClientId identifier
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Invisibility timeout
        /// </summary>
        public TimeSpan InvisibilityTimeout { get; set; }

        /// <summary>
        /// Expiration check inteval for jobs
        /// </summary>
        public TimeSpan JobExpirationCheckInterval { get; set; }

        /// <summary>
        /// Counters interval
        /// </summary>
        public TimeSpan CountersAggregateInterval { get; set; }

        /// <summary>
        /// Set AutoVacuum Mode In SQLite.
        /// Defaults to <see cref="AutoVacuum.NONE"/>.
        /// </summary>
        public AutoVacuum AutoVacuumSelected { get; set; } = AutoVacuum.NONE;

        public enum AutoVacuum
        {
            NONE = 0,
            FULL = 1,
            INCREMENTAL = 2
        }

        /// <summary>
        /// Set journal_mode in SQLite.
        /// Defaults to <see cref="JournalModes.DELETE"/>.
        /// </summary>
        public JournalModes JournalMode { get; set; } = JournalModes.DELETE;

        public enum JournalModes
        {
            DELETE,
            TRUNCATE,
            PERSIST,
            MEMORY,
            WAL,
            OFF
        }

        /// <summary>
        /// Limits the amount of pooled SQLiteConnections.
        /// </summary>
        public int PoolSize { get; set; } = 10;
    }
}
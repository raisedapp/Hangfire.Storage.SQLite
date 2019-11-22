using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;
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
            DateTime now = DateTime.UtcNow;

            /*
            RemoveExpiredRecord(connection.Job, _ => _.ExpireAt != null && _.ExpireAt.Value.ToUniversalTime() < now);
            RemoveExpiredRecord(connection.StateDataAggregatedCounter, _ => _.ExpireAt != null && _.ExpireAt.Value.ToUniversalTime() < now);
            RemoveExpiredRecord(connection.StateDataCounter, _ => _.ExpireAt != null && _.ExpireAt.Value.ToUniversalTime() < now);
            RemoveExpiredRecord(connection.StateDataHash, _ => _.ExpireAt != null && _.ExpireAt.Value.ToUniversalTime() < now);
            RemoveExpiredRecord(connection.StateDataSet, _ => _.ExpireAt != null && _.ExpireAt.Value.ToUniversalTime() < now);
            RemoveExpiredRecord(connection.StateDataList, _ => _.ExpireAt != null && _.ExpireAt.Value.ToUniversalTime() < now);

            cancellationToken.WaitHandle.WaitOne(_checkInterval);
            */
        }
    }
}

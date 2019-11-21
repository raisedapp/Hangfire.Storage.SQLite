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
    /// Represents Counter collection aggregator for SQLite database
    /// </summary> 
    #pragma warning disable CS0618
    public class CountersAggregator : IBackgroundProcess, IServerComponent
    #pragma warning restore CS0618
    {
        private static readonly ILog Logger = LogProvider.For<CountersAggregator>();
        private const int NumberOfRecordsInSinglePass = 1000;

        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromMilliseconds(500);

        private readonly SQLiteStorage _storage;
        private readonly TimeSpan _interval;

        /// <summary>
        /// Constructs Counter collection aggregator
        /// </summary>
        /// <param name="storage">LiteDB storage</param>
        /// <param name="interval">Checking interval</param>
        public CountersAggregator(SQLiteStorage storage, TimeSpan interval)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _interval = interval;
        }

        /// <summary>
        /// Runs aggregator
        /// </summary>
        /// <param name="context">Background processing context</param>
        public void Execute([NotNull] BackgroundProcessContext context)
        {
            Execute(context.StoppingToken);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns text representation of the object
        /// </summary>
        public override string ToString()
        {
            return "SQLite Counter Collection Aggregator";
        }
    }
}

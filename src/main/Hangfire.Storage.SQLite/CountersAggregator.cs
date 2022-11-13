using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Hangfire.Storage.SQLite.Entities;

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
        /// <param name="storage">SQLite storage</param>
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

        /// <summary>
        /// Runs aggregator
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public void Execute(CancellationToken cancellationToken)
        {
            Logger.DebugFormat("Aggregating records in 'Counter' table...");

            long removedCount = 0;
            
            do
            {
                using (var storageConnection = (HangfireSQLiteConnection)_storage.GetConnection())
                {
                    var storageDb = storageConnection.DbContext;

                    var recordsToAggregate = storageDb
                        .CounterRepository
                        .Take(NumberOfRecordsInSinglePass)
                        .ToList();

                    var recordsToMerge = recordsToAggregate
                        .GroupBy(_ => _.Key).Select(_ => new
                        {
                            _.Key,
                            Value = _.Sum(x => x.Value),
                            ExpireAt = _.Max(x => x.ExpireAt)
                        });

                    foreach (var id in recordsToAggregate.Select(_ => _.Id))
                    {
                        storageDb
                            .CounterRepository
                            .Delete(_ => _.Id == id);
                        removedCount++;
                    }

                    foreach (var item in recordsToMerge)
                    {
                        AggregatedCounter aggregatedItem = storageDb
                            .AggregatedCounterRepository
                            .FirstOrDefault(_ => _.Key == item.Key);

                        if (aggregatedItem != null)
                        {
                            var aggregatedCounters = storageDb.AggregatedCounterRepository.Where(_ => _.Key == item.Key).ToList();

                            foreach (var counter in aggregatedCounters)
                            {
                                counter.Value += item.Value;
                                counter.ExpireAt = item.ExpireAt > aggregatedItem.ExpireAt
                                    ? item.ExpireAt
                                    : aggregatedItem.ExpireAt;
                                storageDb.Database.Update(counter);
                            }
                        }
                        else
                        {
                            storageDb
                                .Database
                                .Insert(new AggregatedCounter
                                {
                                    Key = item.Key,
                                    Value = item.Value,
                                    ExpireAt = item.ExpireAt
                                });
                        }
                    }
                }

                if (removedCount >= NumberOfRecordsInSinglePass)
                {
                    cancellationToken.WaitHandle.WaitOne(DelayBetweenPasses);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            } while (removedCount >= NumberOfRecordsInSinglePass);

            cancellationToken.WaitHandle.WaitOne(_interval);
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

using Hangfire.Storage.SQLite.Entities;
using Hangfire.Storage.SQLite.Test.Utils;
using System;
using System.Threading;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    [Collection("Database")]
    public class CountersAggregatorFacts
    {
        [Fact, CleanDatabase]
        public void CountersAggregatorExecutesProperly()
        {
            var storage = ConnectionUtils.CreateStorage();
            using (var connection = (HangfireSQLiteConnection)storage.GetConnection())
            {
                // Arrange
                connection.DbContext.Database.Insert(new Counter
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = "key",
                    Value = 1L,
                    ExpireAt = DateTime.UtcNow.AddHours(1)
                });

                var aggregator = new CountersAggregator(storage, TimeSpan.Zero);
                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act
                aggregator.Execute(cts.Token);

                // Assert
                Assert.Equal(1, connection.DbContext.AggregatedCounterRepository.Count());
            }
        }
    }
}

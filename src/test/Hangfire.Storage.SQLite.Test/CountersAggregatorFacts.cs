using Hangfire.Storage.SQLite.Entities;
using System;
using System.Threading;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public class CountersAggregatorFacts : SqliteInMemoryTestBase
    {
        [Fact]
        public void CountersAggregatorExecutesProperly()
        {
            using (var connection = (HangfireSQLiteConnection)Storage.GetConnection())
            {
                // Arrange
                connection.DbContext.Database.Insert(new Counter
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = "key",
                    Value = 1L,
                    ExpireAt = DateTime.UtcNow.AddHours(1)
                });

                var aggregator = new CountersAggregator(Storage, TimeSpan.Zero);
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

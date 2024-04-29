using Hangfire.Storage.SQLite.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public class ExpirationManagerFacts : SqliteInMemoryTestBase
    {
        private readonly CancellationToken _token;
        private static PersistentJobQueueProviderCollection _queueProviders;

        public ExpirationManagerFacts()
        {
            _queueProviders = Storage.QueueProviders;

            _token = new CancellationToken(true);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ExpirationManager(null));
        }

        [Fact]
        public void Execute_RemovesOutdatedRecords()
        {
            using var connection = Storage.CreateAndOpenConnection();
            CreateExpirationEntries(connection, DateTime.UtcNow.AddMonths(-1));
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.True(IsEntryExpired(connection));
        }

        [Fact]
        public void Execute_DoesNotRemoveEntries_WithNoExpirationTimeSet()
        {
            using var connection = Storage.CreateAndOpenConnection();
            CreateExpirationEntries(connection, null);
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.False(IsEntryExpired(connection));
        }

        [Fact]
        public void Execute_DoesNotRemoveEntries_WithFreshExpirationTime()
        {
            using var connection = Storage.CreateAndOpenConnection();
            CreateExpirationEntries(connection, DateTime.UtcNow.AddMonths(1));
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.False(IsEntryExpired(connection));
        }

        [Fact]
        public void Execute_Processes_CounterTable()
        {
            using var connection = Storage.CreateAndOpenConnection();
            connection.Database.Insert(new Counter
            {
                Id = Guid.NewGuid().ToString(),
                Key = "key",
                Value = 1L,
                ExpireAt = DateTime.UtcNow.AddMonths(-1)
            });
            var manager = CreateManager();
            manager.Execute(_token);
            var count = connection.CounterRepository.Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void Execute_Processes_JobTable()
        {
            using var connection = Storage.CreateAndOpenConnection();
            connection.Database.Insert(new HangfireJob()
            {
                InvocationData = "",
                Arguments = "",
                CreatedAt = DateTime.UtcNow,
                ExpireAt = DateTime.UtcNow.AddMonths(-1),
            });
            var manager = CreateManager();
            manager.Execute(_token);
            var count = connection.HangfireJobRepository.Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void Execute_Processes_ListTable()
        {
            using var connection = Storage.CreateAndOpenConnection();
            connection.Database.Insert(new HangfireList()
            {
                Key = "key",
                ExpireAt = DateTime.UtcNow.AddMonths(-1)
            });
            var manager = CreateManager();
            manager.Execute(_token);
            var count = connection
                .HangfireListRepository
                .Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void Execute_Processes_SetTable()
        {
            using var connection = Storage.CreateAndOpenConnection();
            connection.Database.Insert(new Set
            {
                Key = "key",
                Score = 0,
                Value = "",
                ExpireAt = DateTime.UtcNow.AddMonths(-1)
            });
            var manager = CreateManager();
            manager.Execute(_token);
            var count = connection
                .SetRepository
                .Count();
            Assert.Equal(0, count);
        }

        [Fact]
        public void Execute_Processes_HashTable()
        {
            using var connection = Storage.CreateAndOpenConnection();
            connection.Database.Insert(new Hash()
            {
                Key = "key",
                Field = "field",
                Value = "",
                ExpireAt = DateTime.UtcNow.AddMonths(-1)
            });
            var manager = CreateManager();
            manager.Execute(_token);
            var count = connection
                .HashRepository
                .Count();
            Assert.Equal(0, count);
        }


        [Fact]
        public void Execute_Processes_AggregatedCounterTable()
        {
            using var connection = Storage.CreateAndOpenConnection();
            connection.Database.Insert(new AggregatedCounter
            {
                Key = "key",
                Value = 1,
                ExpireAt = DateTime.UtcNow.AddMonths(-1)
            });
            var manager = CreateManager();
            manager.Execute(_token);
            Assert.Equal(0, connection
                .CounterRepository
                .Count());
        }

        private static void CreateExpirationEntries(HangfireDbContext connection, DateTime? expireAt)
        {
            Commit(connection, x => x.AddToSet("my-key", "my-value"));
            Commit(connection, x => x.AddToSet("my-key", "my-value1"));
            Commit(connection, x => x.SetRangeInHash("my-hash-key",
                new[]
                {
                    new KeyValuePair<string, string>("key", "value"),
                    new KeyValuePair<string, string>("key1", "value1")
                }));
            Commit(connection, x => x.AddRangeToSet("my-key", new[] { "my-value", "my-value1" }));

            if (expireAt.HasValue)
            {
                var expireIn = expireAt.Value - DateTime.UtcNow;
                Commit(connection, x => x.ExpireHash("my-hash-key", expireIn));
                Commit(connection, x => x.ExpireSet("my-key", expireIn));
            }
        }

        private static bool IsEntryExpired(HangfireDbContext connection)
        {
            var countSet = connection
                .SetRepository
                .Count();
            var countHash = connection
                .HashRepository
                .Count();

            return countHash == 0 && countSet == 0;
        }

        private ExpirationManager CreateManager()
        {
            return new ExpirationManager(Storage);
        }

        private static void Commit(HangfireDbContext connection, Action<SQLiteWriteOnlyTransaction> action)
        {
            using (SQLiteWriteOnlyTransaction transaction = new SQLiteWriteOnlyTransaction(connection, _queueProviders))
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}
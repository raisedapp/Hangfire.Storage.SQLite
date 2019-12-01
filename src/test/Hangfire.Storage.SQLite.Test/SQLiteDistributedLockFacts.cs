using System;
using System.Threading;
using Hangfire.Storage.SQLite.Entities;
using Hangfire.Storage.SQLite.Test.Utils;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    [Collection("Database")]
    public class SQLiteDistributedLockFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenResourceIsNull()
        {
            UseConnection(database =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SQLiteDistributedLock(null, TimeSpan.Zero, database, new SQLiteStorageOptions()));

                Assert.Equal("resource", exception.ParamName);
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SQLiteDistributedLock("resource1", TimeSpan.Zero, null, new SQLiteStorageOptions()));

            Assert.Equal("database", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Ctor_SetLock_WhenResourceIsNotLocked()
        {
            UseConnection(database =>
            {
                using (
                    new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount =
                        database.DistributedLockRepository.Count(_ => _.Resource== "resource1");
                    Assert.Equal(1, locksCount);
                }
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_SetReleaseLock_WhenResourceIsNotLocked()
        {
            UseConnection(database =>
            {
                using (new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount = database.DistributedLockRepository.Count(_ => _.Resource== "resource1");
                    Assert.Equal(1, locksCount);
                }

                var locksCountAfter = database.DistributedLockRepository.Count(_ => _.Resource=="resource1");
                Assert.Equal(0, locksCountAfter);
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_AcquireLockWithinSameThread_WhenResourceIsLocked()
        {
            UseConnection(database =>
            {
                using (new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount = database.DistributedLockRepository.Count(_ => _.Resource=="resource1");
                    Assert.Equal(1, locksCount);

                    using (new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                    {
                        locksCount = database.DistributedLockRepository.Count(_ => _.Resource== "resource1");
                        Assert.Equal(1, locksCount);
                    }
                }
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_WhenResourceIsLocked()
        {
            UseConnection(database =>
            {
                using (new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount = database.DistributedLockRepository.Count(_ => _.Resource== "resource1");
                    Assert.Equal(1, locksCount);

                    var t = new Thread(() =>
                    {
                        Assert.Throws<DistributedLockTimeoutException>(() =>
                                new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()));
                    });
                    t.Start();
                    Assert.True(t.Join(5000), "Thread is hanging unexpected");
                }
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_WaitForLock_SignaledAtLockRelease()
        {
            UseConnection(database =>
            {
                var t = new Thread(() =>
                {
                    using (new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    }
                });
                t.Start();

                // Wait just a bit to make sure the above lock is acuired
                Thread.Sleep(TimeSpan.FromSeconds(1));

                // Record when we try to aquire the lock
                var startTime = DateTime.UtcNow;
                using (new SQLiteDistributedLock("resource1", TimeSpan.FromSeconds(10), database, new SQLiteStorageOptions()))
                {
                    Assert.InRange(DateTime.UtcNow - startTime, TimeSpan.Zero, TimeSpan.FromSeconds(5));
                }
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsIsNull()
        {
            UseConnection(database =>
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, null));

                Assert.Equal("storageOptions", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_SetLockExpireAtWorks_WhenResourceIsNotLocked()
        {
            UseConnection(database =>
            {
                using (new SQLiteDistributedLock("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions() { DistributedLockLifetime = TimeSpan.FromSeconds(3) }))
                {
                    DateTime initialExpireAt = DateTime.UtcNow;
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    DistributedLock lockEntry = database.DistributedLockRepository.FirstOrDefault(_ => _.Resource=="resource1");
                    Assert.NotNull(lockEntry);
                    Assert.True(lockEntry.ExpireAt > initialExpireAt);
                }
            });
        }

        private static void UseConnection(Action<HangfireDbContext> action)
        {
            var connection = ConnectionUtils.CreateConnection();
            action(connection);
        }        
    }
}
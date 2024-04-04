using Hangfire.Storage.SQLite.Entities;
using Hangfire.Storage.SQLite.Test.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public class SQLiteDistributedLockFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenResourceIsNull()
        {
            UseConnection(database =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => SQLiteDistributedLock.Acquire(null, TimeSpan.Zero, database, new SQLiteStorageOptions()));

                Assert.Equal("resource", exception.ParamName);
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, null, new SQLiteStorageOptions()));

            Assert.Equal("database", exception.ParamName);
        }

        [Fact]
        public void Ctor_SetLock_WhenResourceIsNotLocked()
        {
            UseConnection(database =>
            {
                using (
                    SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount =
                        database.DistributedLockRepository.Count(_ => _.Resource == "resource1");
                    Assert.Equal(1, locksCount);
                }
            });
        }

        [Fact]
        public void Ctor_SetReleaseLock_WhenResourceIsNotLocked()
        {
            UseConnection(database =>
            {
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount = database.DistributedLockRepository.Count(_ => _.Resource == "resource1");
                    Assert.Equal(1, locksCount);
                }

                var locksCountAfter = database.DistributedLockRepository.Count(_ => _.Resource == "resource1");
                Assert.Equal(0, locksCountAfter);
            });
        }

        [Fact]
        public void Ctor_AcquireLockWithinSameThread_WhenResourceIsLocked_Should_Fail()
        {
            UseConnection(database =>
            {
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount = database.DistributedLockRepository.Count(_ => _.Resource == "resource1");
                    Assert.Equal(1, locksCount);

                    Assert.Throws<DistributedLockTimeoutException>(() =>
                    {
                        using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                        {
                            locksCount = database.DistributedLockRepository.Count(_ => _.Resource == "resource1");
                            Assert.Equal(1, locksCount);
                        }
                    });
                }
            });
        }
        
        private Thread NewBackgroundThread(ThreadStart start)
        {
            return new Thread(start)
            {
                IsBackground = true
            };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenResourceIsLocked()
        {
            UseConnection(database =>
            {
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                {
                    var locksCount = database.DistributedLockRepository.Count(_ => _.Resource == "resource1");
                    Assert.Equal(1, locksCount);

                    var t = NewBackgroundThread(() =>
                    {
                        Assert.Throws<DistributedLockTimeoutException>(() =>
                                SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()));
                    });
                    t.Start();
                    Assert.True(t.Join(5000), "Thread is hanging unexpected");
                }
            });
        }

        [Fact]
        public void Ctor_WaitForLock_SignaledAtLockRelease()
        {
            var storage = ConnectionUtils.CreateStorage();
            using var mre = new ManualResetEventSlim();
            var t = NewBackgroundThread(() =>
            {
                UseConnection(database =>
                {
                    using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions()))
                    {
                        mre.Set();
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    }
                }, storage);
            });
            UseConnection(database =>
            {
                t.Start();

                // Wait just a bit to make sure the above lock is acuired
                mre.Wait(TimeSpan.FromSeconds(5));

                // Record when we try to aquire the lock
                var startTime = Stopwatch.StartNew();
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.FromMinutes(15), database, new SQLiteStorageOptions()))
                {
                    Assert.InRange(startTime.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
                }

                t.Join();
            }, storage);
        }

        [Fact]
        public void Ctor_WaitForLock_OnlySingleLockCanBeAcquired()
        {
            var numThreads = 10;
            long concurrencyCounter = 0;
            using var manualResetEvent = new ManualResetEventSlim();
            var success = new bool[numThreads];
            var storage = ConnectionUtils.CreateStorage();
            
            // Spawn multiple threads to race each other.
            var threads = Enumerable.Range(0, numThreads).Select(i => NewBackgroundThread(() =>
            {
                using var connection = storage.CreateAndOpenConnection();
                // Wait for the start signal.
                manualResetEvent.Wait();

                // Attempt to acquire the distributed lock.
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.FromSeconds(10), connection, new SQLiteStorageOptions()))
                {
                    // Find out if any other threads managed to acquire the lock.
                    var oldConcurrencyCounter = Interlocked.CompareExchange(ref concurrencyCounter, 1, 0);

                    // The old concurrency counter should be 0 as only one thread should be allowed to acquire the lock.
                    success[i] = oldConcurrencyCounter == 0;

                    Interlocked.MemoryBarrier();

                    // Hold the lock for some time.
                    Thread.Sleep(100);

                    Interlocked.Decrement(ref concurrencyCounter);
                }
            })).ToList();

            threads.ForEach(t => t.Start());

            manualResetEvent.Set();

            threads.ForEach(t => Assert.True(t.Join(TimeSpan.FromSeconds(120)), "Thread is hanging unexpected"));

            // All the threads should report success.
            Assert.DoesNotContain(false, success);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsIsNull()
        {
            UseConnection(database =>
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, null));

                Assert.Equal("storageOptions", exception.ParamName);
            });
        }

        [Fact]
        public void Ctor_SetLockExpireAtWorks_WhenResourceIsNotLocked()
        {
            UseConnection(database =>
            {
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.Zero, database, new SQLiteStorageOptions() { DistributedLockLifetime = TimeSpan.FromSeconds(3) }))
                {
                    DateTime initialExpireAt = DateTime.UtcNow;
                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    DistributedLock lockEntry = database.DistributedLockRepository.FirstOrDefault(_ => _.Resource == "resource1");
                    Assert.NotNull(lockEntry);
                    Assert.True(lockEntry.ExpireAt > initialExpireAt);
                }
            });
        }

        // see https://github.com/raisedapp/Hangfire.Storage.SQLite/issues/38
        [Fact]
        public void Ctor_SetLockExpireAtWorks_WhenResourceIsLockedAndExpiring()
        {
            UseConnection(database =>
            {
                // add a lock (taken by another process who is now killed) which will expire in 3 seconds from now
                database.Database.Insert(new DistributedLock
                {
                    Id = Guid.NewGuid().ToString(),
                    Resource = "resource1",
                    ExpireAt = DateTime.UtcNow.Add(TimeSpan.FromSeconds(3))
                });

                // try to get the lock in the next 10 seconds
                // ideally, after ~3 seconds, the constructor should succeed
                using (SQLiteDistributedLock.Acquire("resource1", TimeSpan.FromSeconds(10), database, new SQLiteStorageOptions() { DistributedLockLifetime = TimeSpan.FromSeconds(3) }))
                {
                    DistributedLock lockEntry = database.DistributedLockRepository.FirstOrDefault(_ => _.Resource == "resource1");
                    Assert.NotNull(lockEntry);
                }
            });
        }
        
        [Fact]
        public async Task Heartbeat_Fires_WithSuccess()
        {
            await UseConnectionAsync(async database =>
            {
                // try to get the lock in the next 10 seconds
                // ideally, after ~3 seconds, the constructor should succeed
                using var slock = SQLiteDistributedLock.Acquire("resource1", TimeSpan.FromSeconds(10), database, new SQLiteStorageOptions
                {
                    DistributedLockLifetime = TimeSpan.FromSeconds(3)
                });
                var result = await WaitForHeartBeat(slock, TimeSpan.FromSeconds(3));
                Assert.True(result);
            });
        }
        
        [Fact]
        public void Heartbeat_Fires_With_Fail_If_Lock_No_Longer_Exists()
        {
            UseConnection(database =>
            {
                // try to get the lock in the next 10 seconds
                // ideally, after ~3 seconds, the constructor should succeed
                using var slock = SQLiteDistributedLock.Acquire("resource1", TimeSpan.FromSeconds(10), database, new SQLiteStorageOptions
                {
                    DistributedLockLifetime = TimeSpan.FromSeconds(14)
                });

                using var mre = new ManualResetEventSlim();
                bool? lastResult = null;
                slock.Heartbeat += success =>
                {
                    lastResult = success;
                    if (!success)
                    {
                        mre.Set();
                    }
                };
                database.DistributedLockRepository.Delete(x => x.Resource == "resource1");

                mre.Wait(TimeSpan.FromSeconds(10));
                Assert.False(lastResult, "Lock should have not been updated");
            });
        }

        private async Task<bool> WaitForHeartBeat(SQLiteDistributedLock slock, TimeSpan timeOut)
        {
            var tcs = new TaskCompletionSource<bool>();
            Action<bool> onHeartbeat = success => tcs.TrySetResult(success);
            slock.Heartbeat += onHeartbeat;

            try
            {
                return await tcs.Task.WaitAsync(timeOut);
            }
            finally
            {
                slock.Heartbeat -= onHeartbeat;
            }
        }
        
        private static void UseConnection(Action<HangfireDbContext> action, SQLiteStorage storage = null)
        {
            using var connection = storage?.CreateAndOpenConnection() ?? ConnectionUtils.CreateConnection();
            action(connection);
        }
        
        private static async Task UseConnectionAsync(Func<HangfireDbContext, Task> func, SQLiteStorage storage = null)
        {
            using var connection = storage?.CreateAndOpenConnection() ?? ConnectionUtils.CreateConnection();
            await func(connection);
        }
    }
}
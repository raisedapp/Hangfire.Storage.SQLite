using Hangfire.Logging;
using Hangfire.Storage.SQLite.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// Represents distibuted lock implementation for SQLite
    /// </summary>
    public class SQLiteDistributedLock : IDisposable
    {
        private static readonly ILog Logger = LogProvider.For<SQLiteDistributedLock>();

        private static readonly ThreadLocal<Dictionary<string, int>> AcquiredLocks
                    = new ThreadLocal<Dictionary<string, int>>(() => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        private readonly string _resource;
        private readonly string _resourceKey;

        private readonly HangfireDbContext _dbContext;

        private readonly SQLiteStorageOptions _storageOptions;

        private Timer _heartbeatTimer;

        private bool _completed;

        private string EventWaitHandleName => string.Intern($@"{GetType().FullName}.{_resource}");

        /// <summary>
        /// Creates SQLite distributed lock
        /// </summary>
        /// <param name="resource">Lock resource</param>
        /// <param name="timeout">Lock timeout</param>
        /// <param name="database">Lock database</param>
        /// <param name="storageOptions">Database options</param>
        /// <exception cref="DistributedLockTimeoutException">Thrown if lock is not acuired within the timeout</exception>
        public SQLiteDistributedLock(string resource, TimeSpan timeout, HangfireDbContext database,
            SQLiteStorageOptions storageOptions)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _dbContext = database ?? throw new ArgumentNullException(nameof(database));
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
            _resourceKey = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentException($@"The {nameof(resource)} cannot be empty", nameof(resource));
            }
            if (timeout.TotalSeconds > int.MaxValue)
            {
                throw new ArgumentException($"The timeout specified is too large. Please supply a timeout equal to or less than {int.MaxValue} seconds", nameof(timeout));
            }

            if (!AcquiredLocks.Value.ContainsKey(_resource) || AcquiredLocks.Value[_resource] == 0)
            {
                Cleanup();
                Acquire(timeout);
                AcquiredLocks.Value[_resource] = 1;
                StartHeartBeat();
            }
            else
            {
                AcquiredLocks.Value[_resource]++;
            }
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        /// <exception cref="DistributedLockTimeoutException"></exception>
        public void Dispose()
        {
            if (_completed)
            {
                return;
            }
            _completed = true;

            if (!AcquiredLocks.Value.ContainsKey(_resource))
            {
                return;
            }

            AcquiredLocks.Value[_resource]--;

            if (AcquiredLocks.Value[_resource] > 0)
            {
                return;
            }

            // Timer callback may be invoked after the Dispose method call,
            // but since we use the resource key, we will not disturb other owners.
            AcquiredLocks.Value.Remove(_resource);

            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
            }

            Release();

            Cleanup();
        }

        private void Acquire(TimeSpan timeout)
        {
            try
            {
                var isLockAcquired = false;
                var now = DateTime.UtcNow;
                var lockTimeoutTime = now.Add(timeout);

                while (lockTimeoutTime >= now)
                {
                    Cleanup();

                    lock (EventWaitHandleName)
                    {
                        var result = _dbContext.DistributedLockRepository.FirstOrDefault(_ => _.Resource == _resource);

                        if (result == null)
                        {
                            try
                            {
                                var distributedLock = new DistributedLock();
                                distributedLock.Id = Guid.NewGuid().ToString();
                                distributedLock.Resource = _resource;
                                distributedLock.ResourceKey = _resourceKey;
                                distributedLock.ExpireAt = DateTime.UtcNow.Add(_storageOptions.DistributedLockLifetime);

                                _dbContext.Database.Insert(distributedLock);

                                // we were able to acquire the lock - break the loop
                                isLockAcquired = true;
                                break;
                            }
                            catch (SQLiteException e) when (e.Result == SQLite3.Result.Constraint)
                            {
                                // The lock already exists preventing us from inserting.
                                continue;
                            }
                        }
                    }

                    // we couldn't acquire the lock - wait a bit and try again
                    var waitTime = (int)timeout.TotalMilliseconds / 10;
                    lock (EventWaitHandleName)
                        Monitor.Wait(EventWaitHandleName, waitTime);

                    now = DateTime.UtcNow;
                }

                if (!isLockAcquired)
                {
                    throw new DistributedLockTimeoutException(_resource);
                }
            }
            catch (DistributedLockTimeoutException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Release the lock
        /// </summary>
        /// <exception cref="DistributedLockTimeoutException"></exception>
        private void Release()
        {
            try
            {
                // Remove resource lock (if it's still ours)
                _dbContext.DistributedLockRepository.Delete(_ => _.Resource == _resource && _.ResourceKey == _resourceKey);
                lock (EventWaitHandleName)
                    Monitor.Pulse(EventWaitHandleName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void Cleanup()
        {
            try
            {
                // Delete expired locks (of any owner)
                _dbContext.DistributedLockRepository.
                    Delete(x => x.Resource == _resource && x.ExpireAt < DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Unable to clean up locks on the resource '{0}'. {1}", _resource, ex);
            }
        }

        /// <summary>
        /// Starts database heartbeat
        /// </summary>
        private void StartHeartBeat()
        {
            TimeSpan timerInterval = TimeSpan.FromMilliseconds(_storageOptions.DistributedLockLifetime.TotalMilliseconds / 5);

            _heartbeatTimer = new Timer(state =>
            {
                // Timer callback may be invoked after the Dispose method call,
                // but since we use the resource key, we will not disturb other owners.
                try
                {
                    var distributedLock = _dbContext.DistributedLockRepository.FirstOrDefault(x => x.Resource == _resource && x.ResourceKey == _resourceKey);
                    if (distributedLock != null)
                    {
                        distributedLock.ExpireAt = DateTime.UtcNow.Add(_storageOptions.DistributedLockLifetime);

                        _dbContext.Database.Update(distributedLock);
                    }
                    else
                    {
                        Logger.ErrorFormat("Unable to update heartbeat on the resource '{0}'. The resource is not locked or is locked by another owner.", _resource);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Unable to update heartbeat on the resource '{0}'. {1}", _resource, ex);
                }
            }, null, timerInterval, timerInterval);
        }
    }
}

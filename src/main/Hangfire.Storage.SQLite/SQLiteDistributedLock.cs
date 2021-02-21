using Hangfire.Logging;
using Hangfire.Storage.SQLite.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// Represents distibuted lock implementation for SQLite
    /// </summary>
    public class SQLiteDistributedLock : IDisposable
    {
        // EventWaitHandle is not supported on UNIX systems
        // https://github.com/dotnet/coreclr/pull/1387
        // Instead of using a compiler directive, we catch the
        // exception and handles it. This way, when EventWaitHandle
        // becomes available on UNIX, we will start working.
        private static bool _isEventWaitHandleSupported = true;

        private static readonly ILog Logger = LogProvider.For<SQLiteDistributedLock>();

        private static readonly ThreadLocal<Dictionary<string, int>> AcquiredLocks
                    = new ThreadLocal<Dictionary<string, int>>(() => new Dictionary<string, int>());

        private readonly string _resource;

        private readonly HangfireDbContext _dbContext;

        private readonly SQLiteStorageOptions _storageOptions;

        private Timer _heartbeatTimer;

        private bool _completed;

        private string EventWaitHandleName => $@"{GetType().FullName}.{_resource}";

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
        /// <exception cref="LiteDbDistributedLockException"></exception>
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
            // so we are using lock to avoid unsynchronized calls.
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
                // If result is null, then it means we acquired the lock
                var isLockAcquired = false;
                var now = DateTime.UtcNow;
                var lockTimeoutTime = now.Add(timeout);

                while (!isLockAcquired && (lockTimeoutTime >= now))
                {
                    var result = _dbContext.DistributedLockRepository.FirstOrDefault(_ => _.Resource == _resource);
                    var distributedLock = result ?? new DistributedLock();

                    if (string.IsNullOrWhiteSpace(distributedLock.Id))
                        distributedLock.Id = Guid.NewGuid().ToString();

                    distributedLock.Resource = _resource;
                    distributedLock.ExpireAt = DateTime.UtcNow.Add(_storageOptions.DistributedLockLifetime);

                    var rowsAffected = _dbContext.Database.Update(distributedLock);
                    if (rowsAffected == 0)
                    {
                        try
                        {
                            _dbContext.Database.Insert(distributedLock);
                        }
                        catch(SQLiteException e) when (e.Result == SQLite3.Result.Constraint)
                        {
                            // The lock already exists preventing us from inserting.
                            continue;
                        }
                    }

                    // If result is null, then it means we acquired the lock
                    if (result == null)
                    {
                        isLockAcquired = true;
                    }
                    else
                    {
                        EventWaitHandle eventWaitHandle = null;
                        var waitTime = (int)timeout.TotalMilliseconds / 10;
                        if (_isEventWaitHandleSupported)
                        {
                            try
                            {
                                // Wait on the event. This allows us to be "woken" up sooner rather than later.
                                // We wait in chunks as we need to "wake-up" from time to time and poll mongo,
                                // in case that the lock was acquired on another machine or instance.
                                eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventWaitHandleName);
                                eventWaitHandle.WaitOne(waitTime);
                            }
                            catch (PlatformNotSupportedException)
                            {
                                // See _isEventWaitHandleSupported definition for more info.
                                _isEventWaitHandleSupported = false;
                                eventWaitHandle = null;
                            }
                        }
                        if (eventWaitHandle == null)
                        {
                            // Sleep for a while and then check if the lock has been released.
                            Thread.Sleep(waitTime);
                        }
                        now = DateTime.UtcNow;
                    }
                }

                if (!isLockAcquired && lockTimeoutTime >= now)
                {
                    throw new DistributedLockTimeoutException($"Could not place a lock on the resource \'{_resource}\': The lock request timed out.");
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
        /// <exception cref="LiteDbDistributedLockException"></exception>
        private void Release()
        {
            try
            {
                // Remove resource lock
                _dbContext.DistributedLockRepository.Delete(_ => _.Resource == _resource);
                if (_isEventWaitHandleSupported)
                {
                    try
                    {
                        if (EventWaitHandle.TryOpenExisting(EventWaitHandleName, out EventWaitHandle eventWaitHandler))
                        {
                            eventWaitHandler.Set();
                        }
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // See _isEventWaitHandleSupported definition for more info.
                        _isEventWaitHandleSupported = false;
                    }
                }
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
                // Delete expired locks
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
                // so we are using lock to avoid unsynchronized calls.
                try
                {
                    var distributedLock = _dbContext.DistributedLockRepository.FirstOrDefault(x => x.Resource == _resource);
                    distributedLock.ExpireAt = DateTime.UtcNow.Add(_storageOptions.DistributedLockLifetime);

                    _dbContext.Database.Update(distributedLock);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("Unable to update heartbeat on the resource '{0}'. {1}", _resource, ex);
                }
            }, null, timerInterval, timerInterval);
        }
    }
}

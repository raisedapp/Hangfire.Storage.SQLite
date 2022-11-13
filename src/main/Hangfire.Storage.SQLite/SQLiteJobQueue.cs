using Hangfire.Storage.SQLite.Entities;
using System;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteJobQueue : IPersistentJobQueue
    {
        private readonly SQLiteStorageOptions _storageOptions;

        private readonly HangfireDbContext _dbContext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="storageOptions"></param>
        public SQLiteJobQueue(HangfireDbContext connection, SQLiteStorageOptions storageOptions)
        {
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
            _dbContext = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queues"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null)
            {
                throw new ArgumentNullException(nameof(queues));
            }

            if (queues.Length == 0)
            {
                throw new ArgumentException("Queue array must be non-empty.", nameof(queues));
            }

            JobQueue fetchedJob = null;
            while (fetchedJob == null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var queue in queues)
                {
                    var lockQueue = string.Intern($"f13333e1-a0c8-48c8-bf8c-788e89030329_{queue}");
                    var dateCondition = DateTime.UtcNow
                        .AddSeconds(_storageOptions.InvisibilityTimeout.Negate().TotalSeconds);

                    lock (lockQueue)
                    {
                        fetchedJob = _dbContext.JobQueueRepository.FirstOrDefault(_ => _.Queue == queue &&
                            (_.FetchedAt == null));

                        if (fetchedJob == null)
                            fetchedJob = _dbContext.JobQueueRepository.FirstOrDefault(_ => _.Queue == queue &&
                               _.FetchedAt < dateCondition);

                        if (fetchedJob != null)
                        {
                            fetchedJob.FetchedAt = DateTime.UtcNow;
                            _dbContext.Database.Update(fetchedJob);

                            break;
                        }
                    }
                }

                if (fetchedJob == null)
                {
                    // Wait for a while before polling again.
                    cancellationToken.WaitHandle.WaitOne(_storageOptions.QueuePollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return new SQLiteFetchedJob(_dbContext, fetchedJob.Id, fetchedJob.JobId, fetchedJob.Queue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="jobId"></param>
        public void Enqueue(string queue, string jobId)
        {
            _dbContext.Database.Insert(new JobQueue
            {
                JobId = int.Parse(jobId),
                Queue = queue
            });
        }
    }
}

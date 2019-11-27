using System;
using System.Collections.Generic;
using System.Text;
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
            /*
            if (queues == null)
            {
                throw new ArgumentNullException(nameof(queues));
            }

            if (queues.Length == 0)
            {
                throw new ArgumentException("Queue array must be non-empty.", nameof(queues));
            }

            var fetchConditions = new[]
            {
                Query.EQ("FetchedAt", null),
                Query.LT("FetchedAt", DateTime.UtcNow.AddSeconds(_storageOptions.InvisibilityTimeout.Negate().TotalSeconds))
            };
            var fetchConditionsIndex = 0;

            JobQueue fetchedJob = null;
            while (fetchedJob == null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fetchCondition = fetchConditions[fetchConditionsIndex];

                foreach (var queue in queues)
                {
                    var lockQueue = string.Intern($"f13333e1-a0c8-48c8-bf8c-788e89030329_{queue}");
                    lock (lockQueue)
                    {
                        fetchedJob = _connection.JobQueue.FindOne(Query.And(fetchCondition, Query.EQ("Queue", queue)));

                        if (fetchedJob != null)
                        {
                            fetchedJob.FetchedAt = DateTime.UtcNow;
                            _connection.JobQueue.Update(fetchedJob);
                            break;
                        }
                    }
                }

                if (fetchedJob == null && fetchConditionsIndex == fetchConditions.Length - 1)
                {
                    // ...and we are out of fetch conditions as well.
                    // Wait for a while before polling again.
                    cancellationToken.WaitHandle.WaitOne(_storageOptions.QueuePollInterval);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Move on to next fetch condition
                fetchConditionsIndex = (fetchConditionsIndex + 1) % fetchConditions.Length;
            }

            return new LiteDbFetchedJob(_connection, fetchedJob.Id, fetchedJob.JobId, fetchedJob.Queue);
            */
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="jobId"></param>
        public void Enqueue(string queue, string jobId)
        {
            /*
            _connection.JobQueue.Insert(new JobQueue
            {
                JobId = int.Parse(jobId),
                Queue = queue
            });
            */
        }
    }
}

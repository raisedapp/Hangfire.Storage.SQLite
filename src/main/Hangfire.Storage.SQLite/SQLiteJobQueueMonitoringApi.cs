using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly HangfireDbContext _connection;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public SQLiteJobQueueMonitoringApi(HangfireDbContext connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            //var enqueuedCount = _connection.JobQueue.Count(_ => _.Queue == queue && _.FetchedAt == null);
            //
            //var fetchedCount = _connection.JobQueue.Count(_ => _.Queue == queue && _.FetchedAt != null);

            return new EnqueuedAndFetchedCountDto
            {
                EnqueuedCount = 0,
                FetchedCount = 0
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="from"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public IEnumerable<int> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            //return _connection.JobQueue
            //    .Find(_ => _.Queue == queue && _.FetchedAt == null)
            //    .Skip(from)
            //    .Take(perPage)
            //    .Select(_ => _.JobId)
            //    .AsEnumerable().Where(jobQueueJobId =>
            //    {
            //        var job = _connection.Job.Find(_ => _.Id == jobQueueJobId).FirstOrDefault();
            //        return job?.StateHistory != null;
            //    }).ToArray();

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="from"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        public IEnumerable<int> GetFetchedJobIds(string queue, int from, int perPage)
        {
            //return _connection.JobQueue
            //    .Find(_ => _.Queue == queue && _.FetchedAt != null)
            //    .Skip(from)
            //    .Take(perPage)
            //    .Select(_ => _.JobId)
            //    .AsEnumerable()
            //    .Where(jobQueueJobId =>
            //    {
            //        var job = _connection.Job.Find(_ => _.Id == jobQueueJobId).FirstOrDefault();
            //        return job != null;
            //    }).ToArray();

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetQueues()
        {
            //return _connection.JobQueue
            //    .FindAll()
            //    .Select(_ => _.Queue)
            //    .AsEnumerable().Distinct().ToList();

            return null;
        }
    }
}

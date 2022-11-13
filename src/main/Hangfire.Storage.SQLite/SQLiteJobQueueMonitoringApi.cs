using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteJobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly HangfireDbContext _dbContext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public SQLiteJobQueueMonitoringApi(HangfireDbContext connection)
        {
            _dbContext = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            var enqueuedCount = _dbContext.JobQueueRepository.Count(_ => _.Queue == queue && _.FetchedAt == null);

            var fetchedCount = _dbContext.JobQueueRepository.Count(_ => _.Queue == queue && _.FetchedAt != null);

            return new EnqueuedAndFetchedCountDto
            {
                EnqueuedCount = enqueuedCount,
                FetchedCount = fetchedCount
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
            return _dbContext.JobQueueRepository
                .Where(_ => _.Queue == queue && _.FetchedAt == null)
                .Skip(from)
                .Take(perPage)
                .Select(_ => _.JobId)
                .AsEnumerable().Where(jobQueueJobId =>
                {
                    var job = _dbContext.StateRepository.FirstOrDefault(x => x.JobId == jobQueueJobId);
                    return job != null;
                }).ToArray();
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
            return _dbContext.JobQueueRepository
                .Where(_ => _.Queue == queue && _.FetchedAt != null)
                .Skip(from)
                .Take(perPage)
                .Select(_ => _.JobId)
                .AsEnumerable()
                .Where(jobQueueJobId =>
                {
                    var job = _dbContext.HangfireJobRepository.FirstOrDefault(_ => _.Id == jobQueueJobId);
                    return job != null;
                }).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetQueues()
        {
            return _dbContext.JobQueueRepository
                .ToList()
                .Select(_ => _.Queue)
                .AsEnumerable().Distinct().ToList();
        }
    }
}

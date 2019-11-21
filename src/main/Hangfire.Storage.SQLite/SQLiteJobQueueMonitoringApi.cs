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

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int from, int perPage)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetQueues()
        {
            throw new NotImplementedException();
        }
    }
}

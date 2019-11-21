using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPersistentJobQueueMonitoringApi
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetQueues();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="from"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        IEnumerable<int> GetEnqueuedJobIds(string queue, int from, int perPage);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="from"></param>
        /// <param name="perPage"></param>
        /// <returns></returns>
        IEnumerable<int> GetFetchedJobIds(string queue, int from, int perPage);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue);
    }
}

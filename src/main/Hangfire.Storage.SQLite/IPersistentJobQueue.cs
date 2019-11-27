using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPersistentJobQueue
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queues"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="jobId"></param>
        void Enqueue(string queue, string jobId);
    }
}

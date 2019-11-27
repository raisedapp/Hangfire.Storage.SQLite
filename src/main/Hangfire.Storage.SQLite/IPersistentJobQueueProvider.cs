using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPersistentJobQueueProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        IPersistentJobQueue GetJobQueue(HangfireDbContext connection);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(HangfireDbContext connection);
    }
}

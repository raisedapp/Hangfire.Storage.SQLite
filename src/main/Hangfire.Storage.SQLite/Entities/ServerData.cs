using System;
using System.Collections.Generic;

namespace Hangfire.Storage.SQLite.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class ServerData
    {
        /// <summary>
        /// 
        /// </summary>
        public int WorkerCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> Queues { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? StartedAt { get; set; }
    }
}
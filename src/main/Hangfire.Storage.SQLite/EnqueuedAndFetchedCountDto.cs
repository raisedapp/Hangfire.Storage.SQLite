using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public class EnqueuedAndFetchedCountDto
    {
        /// <summary>
        /// 
        /// </summary>
        public int? EnqueuedCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? FetchedCount { get; set; }
    }
}

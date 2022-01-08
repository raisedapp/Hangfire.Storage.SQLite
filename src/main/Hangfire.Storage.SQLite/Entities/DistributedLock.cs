using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    /// <summary>
    /// Document used for holding a distributed lock in SQLite.
    /// </summary>
    [Table(DefaultValues.DistributedLockTblName)]
    public class DistributedLock
    {
        /// <summary>
        /// The unique id of the document.
        /// </summary>
        [PrimaryKey]
        public string Id { get; set; }

        /// <summary>
        /// The name of the resource being held.
        /// </summary>
        [Unique]
        public string Resource { get; set; }

        /// <summary>
        /// The owner key for this resource.
        /// Prevents race conditions and changes to locks that are owned by other entities.
        /// </summary>
        public string ResourceKey { get; set; }

        /// <summary>
        /// The timestamp for when the lock expires.
        /// This is used if the lock is not maintained or 
        /// cleaned up by the owner (e.g. process was shut down).
        /// </summary>
        public DateTime ExpireAt { get; set; }
    }
}

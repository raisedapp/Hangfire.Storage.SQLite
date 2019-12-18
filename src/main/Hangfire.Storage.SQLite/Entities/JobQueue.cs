using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table("JobQueue")]
    public class JobQueue
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobId { get; set; }

        [MaxLength(DefaultValues.MaxLengthQueueColumn)]
        [Indexed(Name = "IX_JobQueue_Queue", Order = 1, Unique = false)]
        public string Queue { get; set; }

        [Indexed(Name = "IX_JobQueue_FetchedAt", Order = 2, Unique = false)]
        public DateTime FetchedAt { get; set; }
    }
}

using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    public class JobQueue
    {
        private string _jobQueuePK = string.Empty;
        [PrimaryKey]
        public string JobQueuePK { 
            get => Id + "_" + Queue;
            set => _jobQueuePK = value;
        }

        //[AutoIncrement]
        [Indexed(Name = "IX_JobQueue_Id", Order = 1, Unique = false)]
        public int Id { get; set; }

        public int JobId { get; set; }

        [MaxLength(DefaultValues.MaxLengthQueueColumn)]
        [Indexed(Name = "IX_JobQueue_Queue", Order = 2, Unique = false)]
        public string Queue { get; set; }

        [Indexed(Name = "IX_JobQueue_FetchedAt", Order = 3, Unique = false)]
        public DateTime? FetchedAt { get; set; }
    }
}

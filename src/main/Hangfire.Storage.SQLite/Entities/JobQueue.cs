using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class JobQueue
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobId { get; set; }

        [PrimaryKey]
        public string Queue { get; set; }

        [Indexed(Name = "IX_JobQueue_FetchedAt", Order = 1, Unique = false)]
        public DateTime? FetchedAt { get; set; }
    }
}

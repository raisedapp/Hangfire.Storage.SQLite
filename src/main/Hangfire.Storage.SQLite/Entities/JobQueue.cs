using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class JobQueue
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string Queue { get; set; }

        public DateTime? FetchedAt { get; set; }
    }
}

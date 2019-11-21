using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Job
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int StateId { get; set; }

        [Indexed(Name = "IX_Job_StateName", Order = 1, Unique = false)]
        public string StateName { get; set; }

        public string InvocationDate { get; set; }

        public string Arguments { get; set; }

        public DateTime CreatedAt { get; set; }

        [Indexed(Name = "IX_Job_ExpireAt", Order = 2, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

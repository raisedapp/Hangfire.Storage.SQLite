using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Job
    {
        public int Id { get; set; }

        public int StateId { get; set; }

        //Index
        public string StateName { get; set; }

        public string InvocationDate { get; set; }

        public string Arguments { get; set; }

        public int CreatedAt { get; set; }

        //Index
        public DateTime? ExpireAt { get; set; }
    }
}

using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Counter
    {
        [PrimaryKey]
        public string Key { get; set; }

        public int Value { get; set; }

        [Indexed(Name = "IX_Counter_ExpireAt", Order = 1, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

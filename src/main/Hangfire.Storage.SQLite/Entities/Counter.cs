using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    public class Counter
    {
        [PrimaryKey]
        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        public string Key { get; set; }

        public long Value { get; set; }

        [Indexed(Name = "IX_Counter_ExpireAt", Order = 1, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

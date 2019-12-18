using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table("AggregatedCounter")]
    public class AggregatedCounter
    {
        [PrimaryKey]
        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        public string Key { get; set; }

        public decimal Value { get; set; }

        [Indexed(Name = "IX_AggregatedCounter_ExpireAt", Order = 1, Unique = false)]
        public DateTime ExpireAt { get; set; }
    }
}

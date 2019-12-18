using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table("Counter")]
    public class Counter
    {
        [PrimaryKey]
        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        public string Key { get; set; }

        public decimal Value { get; set; }

        [Indexed(Name = "IX_Counter_ExpireAt", Order = 1, Unique = false)]
        public DateTime ExpireAt { get; set; }
    }
}

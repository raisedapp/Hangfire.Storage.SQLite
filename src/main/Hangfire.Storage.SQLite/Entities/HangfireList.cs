using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table("List")]
    internal class HangfireList
    {
        //PK
        [AutoIncrement]
        public int Id { get; set; }

        [PrimaryKey]
        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        public string Key { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }

        [Indexed(Name = "IX_List_ExpireAt", Order = 1, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

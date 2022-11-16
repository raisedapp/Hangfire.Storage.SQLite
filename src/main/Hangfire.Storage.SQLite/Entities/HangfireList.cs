using SQLite;
using System;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table(DefaultValues.HangfireListTblName)]
    public class HangfireList
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        [Indexed(Name = "IX_List_Key", Order = 1, Unique = false)]
        public string Key { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }

        [Indexed(Name = "IX_List_ExpireAt", Order = 2, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

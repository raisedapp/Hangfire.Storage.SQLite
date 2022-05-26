using SQLite;
using System;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table(DefaultValues.HangfireJobTblName)]
    public class HangfireJob
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int StateId { get; set; }

        [Indexed(Name = "IX_Job_StateName", Order = 1, Unique = false)]
        [MaxLength(DefaultValues.MaxLengthStateNameColumn)]
        public string StateName { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string InvocationData { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Arguments { get; set; }

        public DateTime CreatedAt { get; set; }

        [Indexed(Name = "IX_Job_ExpireAt", Order = 2, Unique = false)]
        public DateTime ExpireAt { get; set; }
    }
}

using SQLite;
using System;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table(DefaultValues.StateTblName)]
    public class State
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "IX_State_JobId", Order = 1, Unique = false)]
        public int JobId { get; set; }

        [MaxLength(DefaultValues.MaxLengthStateNameColumn)]
        public string Name { get; set; }

        [MaxLength(DefaultValues.MaxLengthReasonColumn)]
        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Data { get; set; }

        [Indexed(Name = "IX_State_ExpireAt", Order = 2, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

using SQLite;
using System;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table(DefaultValues.JobParameterTblName)]
    public class JobParameter
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "IX_JobParameter_JobId", Order = 1, Unique = false)]
        public int JobId { get; set; }

        [MaxLength(DefaultValues.MaxLengthNameColumn)]
        [Indexed(Name = "IX_JobParameter_Name", Order = 2, Unique = false)]
        public string Name { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }

        [Indexed(Name = "IX_JobParameter_ExpireAt", Order = 3, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

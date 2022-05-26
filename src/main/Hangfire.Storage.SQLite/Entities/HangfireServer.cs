using SQLite;
using System;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table(DefaultValues.HangfireServerTblName)]
    public class HangfireServer
    {
        [PrimaryKey]
        [MaxLength(DefaultValues.MaxLengthIdColumn)]
        public string Id { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Data { get; set; }

        public DateTime LastHeartbeat { get; set; }
    }
}

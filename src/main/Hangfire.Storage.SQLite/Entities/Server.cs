using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Server
    {
        [PrimaryKey]
        [MaxLength(DefaultValues.MaxLengthIdColumn)]
        public string Id { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Data { get; set; }

        public DateTime LastHeartbeat { get; set; }
    }
}

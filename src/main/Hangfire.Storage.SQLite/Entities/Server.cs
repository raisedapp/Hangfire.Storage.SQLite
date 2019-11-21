using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Server
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string Data { get; set; }

        public DateTime LastHeartbeat { get; set; }
    }
}

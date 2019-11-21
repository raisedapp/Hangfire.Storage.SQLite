using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class State
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [PrimaryKey]
        public int JobId { get; set; }

        public string Name { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Data { get; set; }
    }
}

using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class JobParameter
    {
        [PrimaryKey]
        public int JobId { get; set; }

        [PrimaryKey]
        public string Name { get; set; }

        public string Value { get; set; }
    }
}

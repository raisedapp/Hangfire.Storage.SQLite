using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class AggregatedCounter
    {
        public string Key { get; set; }

        public int Value { get; set; }

        //Index
        public DateTime ExpireAt { get; set; }
    }
}

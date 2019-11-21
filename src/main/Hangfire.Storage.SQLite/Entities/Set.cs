using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Set
    {
        public string Key { get; set; }

        public decimal Score { get; set; }

        public string Value { get; set; }
    }
}

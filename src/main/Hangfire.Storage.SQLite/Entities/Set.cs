using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Set
    {
        [PrimaryKey]
        public string Key { get; set; }

        public decimal Score { get; set; }

        [PrimaryKey]
        public string Value { get; set; }
    }
}

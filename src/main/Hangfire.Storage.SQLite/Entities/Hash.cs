using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Hash
    {
        [PrimaryKey]
        public string Key { get; set; }

        [PrimaryKey]
        public string Field { get; set; }

        public string Value { get; set; }

        [Indexed(Name = "IX_Hash_ExpireAt", Order = 1, Unique = false)]
        public DateTime ExpireAt { get; set; }
    }
}

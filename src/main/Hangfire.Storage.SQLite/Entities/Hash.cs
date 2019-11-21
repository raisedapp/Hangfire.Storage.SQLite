using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Hash
    {
        public string Key { get; set; }

        public string Field { get; set; }

        public string Value { get; set; }

        //Index
        public DateTime ExpireAt { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class JobParameter
    {
        public int JobId { get; set; }

        //PK - Index
        public string Name { get; set; }

        public string Value { get; set; }
    }
}

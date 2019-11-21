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
        [MaxLength(DefaultValues.MaxLengthNameColumn)]
        public string Name { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }
    }
}

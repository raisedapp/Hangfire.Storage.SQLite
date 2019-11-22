using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    public class JobParameter
    {
        [PrimaryKey]
        public string JobParameterPK { get { return JobId + "_" + Name; } }

        [Indexed(Name = "IX_JobParameter_JobId", Order = 1, Unique = false)]
        public int JobId { get; set; }

        [MaxLength(DefaultValues.MaxLengthNameColumn)]
        [Indexed(Name = "IX_JobParameter_Name", Order = 2, Unique = false)]
        public string Name { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }
    }
}

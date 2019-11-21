using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal class Set
    {
        [PrimaryKey]
        public string SetPK { get { return Key + "_" + Value; } }

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        [Indexed(Name = "IX_Set_Key", Order = 1, Unique = false)]
        public string Key { get; set; }

        public decimal Score { get; set; }

        [MaxLength(DefaultValues.MaxLengthSetValueColumn)]
        [Indexed(Name = "IX_Set_Value", Order = 2, Unique = false)]
        public string Value { get; set; }
    }
}

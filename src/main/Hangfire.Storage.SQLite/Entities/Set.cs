using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    public class Set
    {
        private string _setPK = string.Empty;
        
        [PrimaryKey]
        public string SetPK { get { return Key + "_" + Value; } set { _setPK = value; } }

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        [Indexed(Name = "IX_Set_Key", Order = 1, Unique = false)]
        public string Key { get; set; }

        public decimal Score { get; set; }

        [MaxLength(DefaultValues.MaxLengthSetValueColumn)]
        [Indexed(Name = "IX_Set_Value", Order = 2, Unique = false)]
        public string Value { get; set; }
    }
}

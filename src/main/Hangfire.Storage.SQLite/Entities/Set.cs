using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table(DefaultValues.SetTblName)]
    public class Set
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        [Indexed(Name = "IX_Set_Key", Order = 1, Unique = false)]
        public string Key { get; set; }

        public decimal Score { get; set; }

        [MaxLength(DefaultValues.MaxLengthSetValueColumn)]
        [Indexed(Name = "IX_Set_Value", Order = 2, Unique = false)]
        public string Value { get; set; }

        [Indexed(Name = "IX_Set_ExpireAt", Order = 3, Unique = false)]
        public DateTime ExpireAt { get; set; }
    }
}

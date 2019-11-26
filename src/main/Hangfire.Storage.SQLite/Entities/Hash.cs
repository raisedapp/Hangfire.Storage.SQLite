using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    public class Hash
    {
        private string _hashPK = string.Empty;
            
        [PrimaryKey]
        public string HashPK { 
            get => Key + "_" + Field;
            set => _hashPK = value;
        }

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        [Indexed(Name = "IX_Hash_Key", Order = 1, Unique = false)]
        public string Key { get; set; }

        [MaxLength(DefaultValues.MaxLengthValueColumn)]
        [Indexed(Name = "IX_Hash_Field", Order = 2, Unique = false)]
        public string Field { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }

        [Indexed(Name = "IX_Hash_ExpireAt", Order = 3, Unique = false)]
        public DateTime ExpireAt { get; set; }
    }
}

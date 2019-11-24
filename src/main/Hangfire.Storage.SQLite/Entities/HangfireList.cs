using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    [Table("List")]
    public class HangfireList
    {
        private string _listPK = string.Empty;
        
        [PrimaryKey]
        public string ListPK { 
            get => Id + "_" + Key;
            set => _listPK = value;
        }

        //[AutoIncrement]
        [Indexed(Name = "IX_List_Id", Order = 1, Unique = false)]
        public int Id { get; set; }

        [MaxLength(DefaultValues.MaxLengthKeyColumn)]
        [Indexed(Name = "IX_List_Key", Order = 2, Unique = false)]
        public string Key { get; set; }

        [MaxLength(DefaultValues.MaxLengthVarCharColumn)]
        public string Value { get; set; }

        [Indexed(Name = "IX_List_ExpireAt", Order = 3, Unique = false)]
        public DateTime? ExpireAt { get; set; }
    }
}

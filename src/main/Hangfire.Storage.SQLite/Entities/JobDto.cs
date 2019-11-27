using System;
using System.Collections.Generic;

namespace Hangfire.Storage.SQLite.Entities
{
    public class JobDto
    {
        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string IdString => Id.ToString();

        /// <summary>
        /// 
        /// </summary>
        public string StateName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string InvocationData { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<State> StateHistory { get; set; } = new List<State>();

        /// <summary>
        /// 
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? ExpireAt { get; set; }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// Represents SQLite database context for Hangfire
    /// </summary>
    public class HangfireDbContext
    {
        private readonly string _prefix;

        /// <summary>
        /// 
        /// </summary>
        public SQLiteStorageOptions StorageOptions { get; private set; }

        private static readonly object Locker = new object();
        private static volatile HangfireDbContext _instance;

        /// <summary>
        /// SQLite database connection identifier
        /// </summary>
        public string ConnectionId { get; }


        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        /// <param name="connectionString">Connection string for SQLite database</param>
        /// <param name="prefix">Table prefix</param>
        private HangfireDbContext(string connectionString, string prefix = "hangfire")
        {
            //UTC - Internal JSON
            GlobalConfiguration.Configuration
                .UseSerializerSettings(new JsonSerializerSettings()
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateFormatString = "yyyy-MM-dd HH:mm:ss.fff"
                });

            ConnectionId = Guid.NewGuid().ToString();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        internal static HangfireDbContext Instance(string connectionString, string prefix)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes initial tables schema for Hangfire
        /// </summary>
        public void Init(SQLiteStorageOptions storageOptions)
        {
            StorageOptions = storageOptions;
        }
    }
}

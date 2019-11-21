using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly SQLiteStorageOptions _storageOptions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageOptions"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SQLiteJobQueueProvider(SQLiteStorageOptions storageOptions)
        {
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        }

        public IPersistentJobQueue GetJobQueue(HangfireDbContext connection)
        {
            return new SQLiteJobQueue(connection, _storageOptions);
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(HangfireDbContext connection)
        {
            return new SQLiteJobQueueMonitoringApi(connection);
        }
    }
}

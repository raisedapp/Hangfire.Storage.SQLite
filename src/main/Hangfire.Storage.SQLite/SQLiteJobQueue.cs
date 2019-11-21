using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteJobQueue : IPersistentJobQueue
    {
        private readonly SQLiteStorageOptions _storageOptions;

        private readonly HangfireDbContext _connection;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="storageOptions"></param>
        public SQLiteJobQueue(HangfireDbContext connection, SQLiteStorageOptions storageOptions)
        {
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Enqueue(string queue, string jobId)
        {
            throw new NotImplementedException();
        }
    }
}

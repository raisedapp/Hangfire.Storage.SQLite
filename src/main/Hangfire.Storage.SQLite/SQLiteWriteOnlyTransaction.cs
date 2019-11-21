using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.States;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteWriteOnlyTransaction : JobStorageTransaction
    {
        private readonly Queue<Action<HangfireDbContext>> _commandQueue = new Queue<Action<HangfireDbContext>>();

        private readonly HangfireDbContext _connection;

        private readonly PersistentJobQueueProviderCollection _queueProviders;

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="queueProviders"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SQLiteWriteOnlyTransaction(HangfireDbContext connection,
            PersistentJobQueueProviderCollection queueProviders)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _queueProviders = queueProviders ?? throw new ArgumentNullException(nameof(queueProviders));
        }

        public override void AddJobState(string jobId, IState state)
        {
            throw new NotImplementedException();
        }

        public override void AddToQueue(string queue, string jobId)
        {
            throw new NotImplementedException();
        }

        public override void AddToSet(string key, string value)
        {
            throw new NotImplementedException();
        }

        public override void AddToSet(string key, string value, double score)
        {
            throw new NotImplementedException();
        }

        public override void Commit()
        {
            throw new NotImplementedException();
        }

        public override void DecrementCounter(string key)
        {
            throw new NotImplementedException();
        }

        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override void IncrementCounter(string key)
        {
            throw new NotImplementedException();
        }

        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        public override void InsertToList(string key, string value)
        {
            throw new NotImplementedException();
        }

        public override void PersistJob(string jobId)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFromList(string key, string value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveFromSet(string key, string value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveHash(string key)
        {
            throw new NotImplementedException();
        }

        public override void SetJobState(string jobId, IState state)
        {
            throw new NotImplementedException();
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            throw new NotImplementedException();
        }

        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            throw new NotImplementedException();
        }
    }
}

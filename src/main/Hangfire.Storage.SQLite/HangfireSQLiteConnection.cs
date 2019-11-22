using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage.SQLite.Entities;

namespace Hangfire.Storage.SQLite
{
    public class HangfireSQLiteConnection : JobStorageConnection
    {
        private readonly SQLiteStorageOptions _storageOptions;

        private readonly PersistentJobQueueProviderCollection _queueProviders;

        private static readonly object _lock = new object();

        public HangfireDbContext Database { get; }

        /// <summary>
        /// Ctor using default storage options
        /// </summary>
        public HangfireSQLiteConnection(HangfireDbContext database, PersistentJobQueueProviderCollection queueProviders)
            : this(database, new SQLiteStorageOptions(), queueProviders)
        {
        }

#pragma warning disable 1591
        public HangfireSQLiteConnection(
            HangfireDbContext database,
            SQLiteStorageOptions storageOptions,
            PersistentJobQueueProviderCollection queueProviders)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
            _queueProviders = queueProviders ?? throw new ArgumentNullException(nameof(queueProviders));
        }

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            return new SQLiteDistributedLock($"HangFire:{resource}", timeout, Database, _storageOptions);
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            if (serverId == null)
                throw new ArgumentNullException(nameof(serverId));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            //var data = new ServerData
            //{
            //    WorkerCount = context.WorkerCount,
            //    Queues = context.Queues,
            //    StartedAt = DateTime.UtcNow
            //};

            //var server = Database.Server.FindById(serverId);
            //if (server == null)
            //{
            //    server = new Entities.Server
            //    {
            //        Id = serverId,
            //        Data = SerializationHelper.Serialize(data, SerializationOption.User),
            //        LastHeartbeat = DateTime.UtcNow
            //    };
            //    Database.Server.Insert(server);
            //}
            //else
            //{
            //    server.LastHeartbeat = DateTime.UtcNow;
            //    server.Data = SerializationHelper.Serialize(data, SerializationOption.User);
            //    Database.Server.Update(server);
            //}
        }

        public override string CreateExpiredJob(Job job, IDictionary<string, string> parameters,
            DateTime createdAt, TimeSpan expireIn)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            lock (_lock)
            {
                var invocationData = InvocationData.SerializeJob(job);

                var jobDto = new HangfireJob
                {
                    //InvocationData = SerializationHelper.Serialize(invocationData, SerializationOption.User),
                    Arguments = invocationData.Arguments,
                    //Parameters = parameters.ToDictionary(kv => kv.Key, kv => kv.Value),
                    CreatedAt = createdAt,
                    ExpireAt = createdAt.Add(expireIn)
                };

                var jobId = jobDto.Id;

                return Convert.ToString(jobId);
            }
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new SQLiteWriteOnlyTransaction(Database, _queueProviders);
        }

        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null || queues.Length == 0)
                throw new ArgumentNullException(nameof(queues));

            //var providers = queues
            //    .Select(queue => _queueProviders.GetProvider(queue))
            //    .Distinct()
            //    .ToArray();

            //if (providers.Length != 1)
            //{
            //    throw new InvalidOperationException(
            //        $"Multiple provider instances registered for queues: {string.Join(", ", queues)}. You should choose only one type of persistent queues per server instance.");
            //}

            //var persistentQueue = providers[0].GetJobQueue(Database);
            //return persistentQueue.Dequeue(queues, cancellationToken);

            return null;
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            //var result = Database
            //    .StateDataHash
            //    .Find(_ => _.Key == key)
            //    .AsEnumerable()
            //    .Select(_ => new { _.Field, _.Value })
            //    .ToDictionary(x => x.Field, x => Convert.ToString(x.Value));

            //return result.Count != 0 ? result : null;

            return new Dictionary<string, string>();
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            //var result = Database
            //    .StateDataSet
            //    .Find(_ => _.Key == key)
            //    .OrderBy(_ => _.Id)
            //    .Select(_ => _.Value)
            //    .ToList();

            return new HashSet<string>();
        }

        public override string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (toScore < fromScore)
            {
                throw new ArgumentException("The 'toScore' value must be higher or equal to the 'fromScore' value.");
            }

            //return Database
            //    .StateDataSet
            //    .Find(_ => _.Key == key &&
            //          _.Score >= fromScore &&
            //           _.Score <= toScore)
            //    .OrderBy(_ => _.Score)
            //    .Select(_ => _.Value)
            //    .FirstOrDefault() as string;

            return string.Empty;
        }

        public override JobData GetJobData(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            var iJobId = int.Parse(jobId);
            //var jobData = Database
            //    .Job
            //    .Find(_ => _.Id == iJobId)
            //    .FirstOrDefault();

            //if (jobData == null)
            //    return null;

            //// TODO: conversion exception could be thrown.
            //var invocationData = SerializationHelper.Deserialize<InvocationData>(jobData.InvocationData,
            //    SerializationOption.User);
            //invocationData.Arguments = jobData.Arguments;

            //Job job = null;
            //JobLoadException loadException = null;

            //try
            //{
            //    job = invocationData.Deserialize();
            //}
            //catch (JobLoadException ex)
            //{
            //    loadException = ex;
            //}

            return new JobData
            {
                Job = null,
                State = string.Empty,
                CreatedAt = DateTime.UtcNow,
                LoadException = null
            };
        }

        public override string GetJobParameter(string id, string name)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            //var iJobId = int.Parse(id);
            //var parameters = Database
            //    .Job
            //    .Find(j => j.Id == iJobId)
            //    .Select(job => job.Parameters)
            //    .FirstOrDefault();

            //string value = null;
            //parameters?.TryGetValue(name, out value);

            return string.Empty;
        }

        public override StateData GetStateData(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            //var iJobId = int.Parse(jobId);
            //var latest = Database
            //    .Job
            //    .Find(j => j.Id == iJobId)
            //    .Select(x => x.StateHistory)
            //    .FirstOrDefault();

            //var state = latest?.LastOrDefault();

            //if (state == null)
            //    return null;

            return new StateData
            {
                Name = string.Empty,
                Reason = string.Empty,
                Data = new Dictionary<string, string>()
            };
        }

        public override void Heartbeat(string serverId)
        {
            if (serverId == null)
            {
                throw new ArgumentNullException(nameof(serverId));
            }

            //var server = Database.Server.FindById(serverId);
            //if (server == null)
            //    return;

            //server.LastHeartbeat = DateTime.UtcNow;
            //Database.Server.Update(server);
        }

        public override void RemoveServer(string serverId)
        {
            if (serverId == null)
            {
                throw new ArgumentNullException(nameof(serverId));
            }

            //Database.Server.Delete(_ => _.Id == serverId);
        }

        public override int RemoveTimedOutServers(TimeSpan timeout)
        {
            if (timeout.Duration() != timeout)
            {
                throw new ArgumentException("The 'timeout' value must be positive.", nameof(timeout));
            }

            var delCount = 0;
            //var servers = Database.Server.FindAll();

            //foreach (var server in servers)
            //{
            //    if (server.LastHeartbeat < DateTime.UtcNow.Add(timeOut.Negate()))
            //    {
            //        Database.Server.Delete(server.Id);
            //        delCount++;
            //    }
            //}

            return delCount;
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            //var iJobId = int.Parse(id);
            //var liteJob = Database.Job.FindById(iJobId);
            //if (liteJob.Parameters == null)
            //{
            //    liteJob.Parameters = new Dictionary<string, string>();
            //}
            //if (liteJob.Parameters.ContainsKey(name))
            //{
            //    liteJob.Parameters.Remove(name);
            //}
            //liteJob.Parameters.Add(name, value);

            //Database.Job.Update(liteJob);
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            using (var transaction = new SQLiteWriteOnlyTransaction(Database, _queueProviders))
            {
                transaction.SetRangeInHash(key, keyValuePairs);
                transaction.Commit();
            }
        }
    }
}

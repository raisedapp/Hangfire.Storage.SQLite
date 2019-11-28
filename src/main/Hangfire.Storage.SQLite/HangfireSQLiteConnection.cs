using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage.SQLite.Entities;
using Newtonsoft.Json;

namespace Hangfire.Storage.SQLite
{
    public class HangfireSQLiteConnection : JobStorageConnection
    {
        private readonly SQLiteStorageOptions _storageOptions;

        private readonly PersistentJobQueueProviderCollection _queueProviders;

        private static readonly object _lock = new object();

        public HangfireDbContext DbContext { get; }

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
            DbContext = database ?? throw new ArgumentNullException(nameof(database));
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
            _queueProviders = queueProviders ?? throw new ArgumentNullException(nameof(queueProviders));
        }

        public override IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            return new SQLiteDistributedLock($"HangFire:{resource}", timeout, DbContext, _storageOptions);
        }

        public override void AnnounceServer(string serverId, ServerContext context)
        {
            if (serverId == null)
                throw new ArgumentNullException(nameof(serverId));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var data = new ServerData
            {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues,
                StartedAt = DateTime.UtcNow
            };

            var server = DbContext.HangfireServerRepository.SingleOrDefault(_ => _.Id == serverId);
            if (server == null)
            {
                server = new HangfireServer
                {
                    Id = serverId,
                    Data = SerializationHelper.Serialize(data, SerializationOption.User),
                    LastHeartbeat = DateTime.UtcNow
                };

                DbContext.Database.Insert(server);
            }
            else
            {
                server.LastHeartbeat = DateTime.UtcNow;
                server.Data = SerializationHelper.Serialize(data, SerializationOption.User);
                DbContext.Database.Update(server);
            }
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

                var newJob = new HangfireJob()
                {
                    InvocationData = SerializationHelper.Serialize(invocationData, SerializationOption.User),
                    Arguments = invocationData.Arguments,
                    CreatedAt = createdAt,
                    ExpireAt = createdAt.Add(expireIn)
                };

                DbContext.Database.Insert(newJob);

                var parametersArray = parameters.ToArray();

                foreach (var parameter in parametersArray) 
                {
                    DbContext.Database.Insert(new JobParameter()
                    {
                        JobId = newJob.Id,
                        Name = parameter.Key,
                        Value = parameter.Value
                    });
                }

                var jobId = newJob.Id;

                return Convert.ToString(jobId);
            }
        }

        public override IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new SQLiteWriteOnlyTransaction(DbContext, _queueProviders);
        }

        public override IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null || queues.Length == 0)
                throw new ArgumentNullException(nameof(queues));

            var providers = queues
                .Select(queue => _queueProviders.GetProvider(queue))
                .Distinct()
                .ToArray();

            if (providers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Multiple provider instances registered for queues: {string.Join(", ", queues)}. You should choose only one type of persistent queues per server instance.");
            }

            var persistentQueue = providers[0].GetJobQueue(DbContext);

            return persistentQueue.Dequeue(queues, cancellationToken);
        }

        public override Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var result = DbContext
                .HashRepository
                .Where(_ => _.Key == key)
                .Select(_ => new { _.Field, _.Value })
                .ToList()
                .ToDictionary(x => x.Field, x => x.Value);

            return result.Count != 0 ? result : null;
        }

        public override HashSet<string> GetAllItemsFromSet(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var result = DbContext
                .SetRepository
                .Where(_ => _.Key == key)
                .Select(_ => _.Value)
                .ToList();

            return new HashSet<string>(result.Cast<string>());
        }

        public override List<string> GetRangeFromSet(string key, int startingFrom, int endingAt)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return DbContext
                .SetRepository
                .Where(_ => _.Key == key)
                .Skip(startingFrom)
                .Take(endingAt - startingFrom + 1) // inclusive -- ensure the last element is included
                .Select(dto => (string)dto.Value)
                .ToList();
        }

        public override long GetSetCount(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return DbContext
                .SetRepository
                .Count(_ => _.Key == key);
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

            var fromScoreDec = fromScore.ToInt64();
            var toScoreDec = toScore.ToInt64();

            return DbContext
                .SetRepository
                .Where(_ => _.Key == key &&
                      _.Score >= fromScoreDec &&
                       _.Score <= toScoreDec)
                .OrderBy(_ => _.Score)
                .Select(_ => _.Value)
                .FirstOrDefault();
        }

        public override JobData GetJobData(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            var iJobId = int.Parse(jobId);
            var jobData = DbContext
                .HangfireJobRepository
                .FirstOrDefault(_ => _.Id == iJobId);

            if (jobData == null)
                return null;

            // TODO: conversion exception could be thrown.
            var invocationData = SerializationHelper.Deserialize<InvocationData>(jobData.InvocationData,
                SerializationOption.User);

            if (!string.IsNullOrEmpty(jobData.Arguments))
            {
                invocationData.Arguments = jobData.Arguments;
            }

            Job job = null;
            JobLoadException loadException = null;

            try
            {
                job = invocationData.DeserializeJob();
            }
            catch (JobLoadException ex)
            {
                loadException = ex;
            }

            return new JobData
            {
                Job = job,
                State = jobData.StateName,
                CreatedAt = jobData.CreatedAt,
                LoadException = loadException
            };
        }

        public override string GetJobParameter(string id, string name)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var iJobId = int.Parse(id);
            var value = DbContext
                .JobParameterRepository
                .Where(_ => _.JobId == iJobId && _.Name == name)
                .Select(_ => _.Value)
                .FirstOrDefault();

            return value;
        }

        public override StateData GetStateData(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            var iJobId = int.Parse(jobId);
            var latest = DbContext
                .StateRepository
                .Where(_ => _.JobId == iJobId)
                .OrderBy(_ => _.CreatedAt)
                .ToList();

            var state = latest?.LastOrDefault();

            if (state == null)
                return null;

            return new StateData
            {
                Name = state.Name,
                Reason = state.Reason,
                Data = JsonConvert.DeserializeObject<Dictionary<string, string>>(state.Data)
            };
        }

        public override void Heartbeat(string serverId)
        {
            if (serverId == null)
            {
                throw new ArgumentNullException(nameof(serverId));
            }

            var server = DbContext.HangfireServerRepository.FirstOrDefault(_ => _.Id == serverId);
            if (server == null)
                return;

            server.LastHeartbeat = DateTime.UtcNow;
            DbContext.Database.Update(server);
        }

        public override void RemoveServer(string serverId)
        {
            if (serverId == null)
            {
                throw new ArgumentNullException(nameof(serverId));
            }

            DbContext.HangfireServerRepository.Delete(_ => _.Id == serverId);
        }

        public override int RemoveTimedOutServers(TimeSpan timeout)
        {
            if (timeout.Duration() != timeout)
            {
                throw new ArgumentException("The 'timeout' value must be positive.", nameof(timeout));
            }

            var delCount = 0;
            var servers = DbContext.HangfireServerRepository.ToList();

            foreach (var server in servers)
            {
                if (server.LastHeartbeat < DateTime.UtcNow.Add(timeout.Negate()))
                {
                    DbContext.HangfireServerRepository.Delete(_ => _.Id == server.Id);
                    delCount++;
                }
            }

            return delCount;
        }

        public override void SetJobParameter(string id, string name, string value)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var iJobId = int.Parse(id);
            var jobParameter = DbContext.JobParameterRepository
                .SingleOrDefault(_ => _.JobId == iJobId && _.Name == name);

            if (jobParameter != null)
            {
                jobParameter.Value = value;
                DbContext.Database.Update(jobParameter);
            }
            else 
            {
                var newParameter = new JobParameter()
                {
                    JobId = Convert.ToInt32(id),
                    Name = name,
                    Value = value
                };

                DbContext.Database.Insert(newParameter);
            }
        }

        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            using (var transaction = new SQLiteWriteOnlyTransaction(DbContext, _queueProviders))
            {
                transaction.SetRangeInHash(key, keyValuePairs);
                transaction.Commit();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hangfire.States;
using Hangfire.Storage.SQLite.Entities;

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
        
        private void QueueCommand(Action<HangfireDbContext> action)
        {
            _commandQueue.Enqueue(action);
        }
        
        public override void AddJobState(string jobId, IState state)
        {
            QueueCommand(_ =>
            {
                var iJobId = int.Parse(jobId);
                var jobState = new State()
                {
                    JobId = iJobId,
                    Name = state.Name,
                    Reason = state.Reason,
                    CreatedAt = DateTime.UtcNow,
                    Data = state.ToString()
                };

                _.Database.Insert(jobState);
            });            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="jobId"></param>
        public override void AddToQueue(string queue, string jobId)
        {
            var provider = _queueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue(_connection);

            QueueCommand(_ =>
            {
                persistentQueue.Enqueue(queue, jobId);
            });            
        }

        public override void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score"></param>        
        public override void AddToSet(string key, string value, double score)
        {
            var scoreDec = score.ToInt64();

            QueueCommand(_ =>
            {
                var set = new Set
                {
                    Score = scoreDec,
                    Key = key,
                    Value = value,
                    ExpireAt = DateTime.MinValue
                };

                var oldSet = _.SetRepository.FirstOrDefault(x => x.Key == key && x.Value == value);

                if (oldSet == null)
                {
                    _.Database.Insert(set);
                }
                else
                {
                    set.Value = value;
                    set.Score = scoreDec;
                    _.Database.Update(set);
                }
            });
        }

        public override void Commit()
        {
            foreach (var action in _commandQueue)
            {
                action.Invoke(_connection);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireIn"></param>        
        public override void DecrementCounter(string key)
        {
            QueueCommand(_ => 
            {
                _.Database.Insert(new Counter
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = key,
                    Value = -1L
                });
            });

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireIn"></param>
        public override void DecrementCounter(string key, TimeSpan expireIn)
        {
            QueueCommand(_ =>
            {
                _.Database.Insert(new Counter
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = key,
                    Value = -1L,
                    ExpireAt = DateTime.UtcNow.Add(expireIn)
                });
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="expireIn"></param>        
        public override void ExpireJob(string jobId, TimeSpan expireIn)
        {
            QueueCommand(_ => 
            {
                var iJobId = int.Parse(jobId);
                var job = _.HangfireJobRepository.FirstOrDefault(x => x.Id == iJobId);
                
                if (job != null) 
                {
                    job.ExpireAt = DateTime.UtcNow.Add(expireIn);
                    _.Database.Update(job);
                }
            });   
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public override void IncrementCounter(string key)
        {
            QueueCommand(_ => 
            {
                _.Database.Insert(new Counter
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = key,
                    Value = +1L
                });
            });
        }

        public override void IncrementCounter(string key, TimeSpan expireIn)
        {
            QueueCommand(_ =>
            {
                _.Database.Insert(new Counter
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = key,
                    Value = +1L,
                    ExpireAt = DateTime.UtcNow.Add(expireIn)
                });
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void InsertToList(string key, string value)
        {
            QueueCommand(_ => 
            {
                _.Database.Insert(new HangfireList
                {
                    Key = key,
                    Value = value
                });
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        public override void PersistJob(string jobId)
        {
            QueueCommand(_ => 
            {
                var iJobId = int.Parse(jobId);
                var job = _.HangfireJobRepository.FirstOrDefault(x => x.Id == iJobId);

                if (job != null) 
                {
                    job.ExpireAt = DateTime.MinValue;
                    _.Database.Update(job);
                }
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void RemoveFromList(string key, string value)
        {
            QueueCommand(_ => 
            {
                _.HangfireListRepository.Delete(x => x.Key == key && x.Value == value);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>        
        public override void RemoveFromSet(string key, string value)
        {
            QueueCommand(_ => 
            {
                _.HangfireListRepository.Delete(x => x.Key == key && x.Value == value);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>       
        public override void RemoveHash(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            QueueCommand(_ => 
            {
                _.HashRepository.Delete(x => x.Key == key);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="state"></param>        
        public override void SetJobState(string jobId, IState state)
        {
            QueueCommand(_ => 
            {
                var iJobId = int.Parse(jobId);
                var job = _.HangfireJobRepository.FirstOrDefault(x => x.Id == iJobId);

                if (job != null)
                {
                    job.StateName = state.Name;

                    _.Database.Insert(new State
                    {
                        JobId = iJobId,
                        Name = state.Name,
                        Reason = state.Reason,
                        CreatedAt = DateTime.UtcNow,
                        Data = state.ToString()
                    });

                    _.Database.Update(job);
                }
            });        
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyValuePairs"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (keyValuePairs == null)
                throw new ArgumentNullException(nameof(keyValuePairs));
            
            foreach (var keyValuePair in keyValuePairs)
            {
                var field = keyValuePair.Key;
                var value = keyValuePair.Value;
                
                QueueCommand(_ =>
                {
                    var hash = new Hash
                    {
                        Key = key,
                        Field = field,
                        Value = value,
                        ExpireAt = DateTime.MinValue
                    };

                    var oldHash = _.HashRepository.FirstOrDefault(x => x.Key == key && x.Field == field);
                    if (oldHash == null)
                    {
                        _.Database.Insert(hash);
                    }
                    else
                    {
                        _.Database.Update(hash);
                    }
                });
            }        
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keepStartingFrom"></param>
        /// <param name="keepEndingAt"></param>
        public override void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
            QueueCommand(_ =>
            {
                var start = keepStartingFrom + 1;
                var end = keepEndingAt + 1;
                
                var items = _.HangfireListRepository
                    .Where(x => x.Key == key)
                    .Reverse()
                    .Select((data, i) => new {Index = i + 1, Data = data.Id})
                    .Where(x => !((x.Index >= start) && (x.Index <= end)))
                    .Select(x => x.Data)
                    .ToList();

                foreach(var id in items)
                {
                    _.HangfireListRepository.Delete(x => x.Id == id);
                }
            });
        }
    }
}

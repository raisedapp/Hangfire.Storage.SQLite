using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Hangfire.States;
using Hangfire.Storage.SQLite.Entities;
using Newtonsoft.Json;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteWriteOnlyTransaction : JobStorageTransaction
    {
        private readonly Queue<Action<HangfireDbContext>> _commandQueue = new Queue<Action<HangfireDbContext>>();

        private readonly HangfireDbContext _dbContext;

        private readonly PersistentJobQueueProviderCollection _queueProviders;

        private static readonly object _lockObject = new object();

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="queueProviders"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SQLiteWriteOnlyTransaction(HangfireDbContext connection,
            PersistentJobQueueProviderCollection queueProviders)
        {
            _dbContext = connection ?? throw new ArgumentNullException(nameof(connection));
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
                    Data = JsonConvert.SerializeObject(state.SerializeData())
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
            var persistentQueue = provider.GetJobQueue(_dbContext);

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
            var scoreDec = Convert.ToDecimal(score);

            QueueCommand(_ =>
            {
                var set = new Set
                {
                    Score = scoreDec,
                    Key = key,
                    Value = value,
                    ExpireAt = null
                };

                var oldSet = _.SetRepository.FirstOrDefault(x => x.Key == key && x.Value == value);

                if (oldSet == null)
                {
                    _.Database.Insert(set);
                }
                else
                {
                    set.Id = oldSet.Id;
                    set.Score = scoreDec;

                    _.Database.Update(set);
                }
            });
        }

        public override void Commit()
        {
            lock (_lockObject) 
            {
                _commandQueue.ToList().ForEach(_ =>
                {
                    _.Invoke(_dbContext);
                });
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
                    var expireAt = DateTime.UtcNow.Add(expireIn);
                    job.ExpireAt = expireAt;

                    _.Database.Update(job);
                    _.Database.Execute($"UPDATE [{DefaultValues.StateTblName}] SET ExpireAt = {expireAt.Ticks} WHERE JobId = {jobId}");
                    _.Database.Execute($"UPDATE [{DefaultValues.JobParameterTblName}] SET ExpireAt = {expireAt.Ticks} WHERE JobId = {jobId}");
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
                    Value = value,
                    ExpireAt = null
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
                    job.ExpireAt = null;
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
                _.SetRepository.Delete(x => x.Key == key && x.Value == value);
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

                    try
                    {
                        _.Database.BeginTransaction();

                        _.Database.Insert(new State
                        {
                            JobId = iJobId,
                            Name = state.Name,
                            Reason = state.Reason,
                            CreatedAt = DateTime.UtcNow,
                            Data = JsonConvert.SerializeObject(state.SerializeData())
                        });
                        _.Database.Update(job);

                        _.Database.Commit();
                    }
                    catch (Exception ex)
                    {
                        _.Database.Rollback();
                        
                        throw ex;
                    }
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
                        ExpireAt = null
                    };

                    var oldHash = _.HashRepository.FirstOrDefault(x => x.Key == key && x.Field == field);
                    if (oldHash == null)
                    {
                        _.Database.Insert(hash);
                    }
                    else
                    {
                        hash.Id = oldHash.Id;
                        _.Database.Update(hash);
                    }
                });
            }        
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireIn"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void ExpireList(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            QueueCommand(x =>
            {
                var states = x.HangfireListRepository.Where(_ => _.Key == key).ToList();
                foreach (var state in states)
                {
                    state.ExpireAt = DateTime.UtcNow.Add(expireIn);
                    x.Database.Update(state);
                }

            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireIn"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void ExpireHash(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            QueueCommand(_ =>
            {
                var states = _.HashRepository.Where(x => x.Key == key).ToList();

                foreach (var state in states)
                {
                    state.ExpireAt = DateTime.UtcNow.Add(expireIn);
                    _.Database.Update(state);
                }
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireIn"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void ExpireSet(string key, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            
            QueueCommand(x =>
            {
                var states = x.SetRepository.Where(_ => _.Key == key).ToList();
                foreach(var state in states)
                {
                    state.ExpireAt = DateTime.UtcNow.Add(expireIn);
                    x.Database.Update(state);
                }
            });
        }        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void PersistSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            QueueCommand(x =>
            {
                var states = x.SetRepository.Where(_ => _.Key == key).ToList();
                foreach(var state in states)
                {
                    state.ExpireAt = null;
                    x.Database.Update(state);
                }
                
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void PersistList(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            QueueCommand(x =>
            {
                var states = x.HangfireListRepository.Where(_ => _.Key == key).ToList();
                foreach(var state in states)
                {
                    state.ExpireAt = null;
                    x.Database.Update(state);
                }       
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void PersistHash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            QueueCommand(x =>
            {
                var states = x.HashRepository.Where(_ => _.Key == key).ToList();
                foreach(var state in states)
                {
                    state.ExpireAt = null;
                    x.Database.Update(state);
                }    
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void AddRangeToSet(string key, IList<string> items)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (items == null) throw new ArgumentNullException(nameof(items));
            
            foreach (var item in items)
            {
                QueueCommand(x =>
                {
                    var state = new Set
                    {
                        Key = key,
                        Value = item,
                        ExpireAt = null,
                        Score = 0.0m
                    };

                    var oldSet = x.SetRepository.FirstOrDefault(_ => _.Key == key && _.Value == item);

                    if (oldSet == null)
                    {
                        x.Database.Insert(state);
                    }
                    else
                    {
                        state.Id = oldSet.Id;
                        x.Database.Update(state);
                    }
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void RemoveSet(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            QueueCommand(x => x.SetRepository.Delete(_ => _.Key == key));
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

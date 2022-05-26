using Hangfire.States;
using Hangfire.Storage.SQLite.Entities;
using Hangfire.Storage.SQLite.Test.Utils;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    [Collection("Database")]
    public class SQLiteWriteOnlyTransactionFacts
    {
        private readonly PersistentJobQueueProviderCollection _queueProviders;

        public SQLiteWriteOnlyTransactionFacts()
        {
            Mock<IPersistentJobQueueProvider> defaultProvider = new Mock<IPersistentJobQueueProvider>();
            defaultProvider.Setup(x => x.GetJobQueue(It.IsNotNull<HangfireDbContext>()))
                .Returns(new Mock<IPersistentJobQueue>().Object);

            _queueProviders = new PersistentJobQueueProviderCollection(defaultProvider.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_IfConnectionIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SQLiteWriteOnlyTransaction(null, _queueProviders));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_IfProvidersCollectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new SQLiteWriteOnlyTransaction(ConnectionUtils.CreateConnection(), null));

            Assert.Equal("queueProviders", exception.ParamName);
        }


        [Fact, CleanDatabase]
        public void ExpireJob_SetsJobExpirationData()
        {
            UseConnection(database =>
            {
                HangfireJob job = new HangfireJob
                {

                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                database.Database.Insert(job);

                HangfireJob anotherJob = new HangfireJob
                {

                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                database.Database.Insert(anotherJob);

                var jobId = job.Id.ToString();
                var anotherJobId = anotherJob.Id;

                Commit(database, x => x.ExpireJob(jobId, TimeSpan.FromDays(1)));

                var testJob = GetTestJob(database, job.Id);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < testJob.ExpireAt && testJob.ExpireAt <= DateTime.UtcNow.AddDays(1));

                var anotherTestJob = GetTestJob(database, anotherJobId);
                Assert.Equal(DateTime.MinValue, anotherTestJob.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void PersistJob_ClearsTheJobExpirationData()
        {
            UseConnection(database =>
            {
                HangfireJob job = new HangfireJob
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow,
                    ExpireAt = DateTime.UtcNow
                };
                database.Database.Insert(job);

                HangfireJob anotherJob = new HangfireJob
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow,
                    ExpireAt = DateTime.UtcNow
                };
                database.Database.Insert(anotherJob);

                var jobId = job.Id.ToString();
                var anotherJobId = anotherJob.Id;

                Commit(database, x => x.PersistJob(jobId));

                var testjob = GetTestJob(database, job.Id);
                Assert.Equal(DateTime.MinValue, testjob.ExpireAt);

                var anotherTestJob = GetTestJob(database, anotherJobId);
                Assert.NotEqual(DateTime.MinValue, anotherTestJob.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void SetJobState_AppendsAStateAndSetItToTheJob()
        {
            UseConnection(database =>
            {
                HangfireJob job = new HangfireJob
                {

                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                database.Database.Insert(job);

                HangfireJob anotherJob = new HangfireJob
                {

                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                database.Database.Insert(anotherJob);

                var serializedData = new Dictionary<string, string> { { "Name", "Value" } };

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData()).Returns(serializedData);

                Commit(database, x => x.SetJobState(Convert.ToString(job.Id), state.Object));

                var testJob = GetTestJob(database, job.Id);
                Assert.Equal("State", testJob.StateName);
                Assert.Equal(1, database.StateRepository.Count(x => x.JobId == testJob.Id));

                var anotherTestJob = GetTestJob(database, anotherJob.Id);
                Assert.Null(anotherTestJob.StateName);
                Assert.Equal(0, database.StateRepository.Count(x => x.JobId == anotherJob.Id));

                var jobWithStates = database.HangfireJobRepository.ToList().FirstOrDefault();

                var jobState = database.StateRepository.FirstOrDefault(x => x.JobId == jobWithStates.Id);
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.True(jobState.CreatedAt > DateTime.MinValue);
                Assert.Equal(JsonConvert.SerializeObject(serializedData), jobState.Data);
            });
        }

        [Fact, CleanDatabase]
        public void AddJobState_JustAddsANewRecordInATable()
        {
            UseConnection(database =>
            {
                HangfireJob job = new HangfireJob
                {

                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                database.Database.Insert(job);

                var jobId = Convert.ToString(job.Id);
                var serializedData = new Dictionary<string, string> { { "Name", "Value" } };

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData()).Returns(serializedData);

                Commit(database, x => x.AddJobState(jobId, state.Object));

                var testJob = GetTestJob(database, job.Id);
                Assert.Null(testJob.StateName);

                var jobWithStates = database.HangfireJobRepository.ToList().Single();
                var jobState = database.StateRepository.Last(x => x.JobId == jobWithStates.Id);

                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.True(jobState.CreatedAt > DateTime.MinValue);
                Assert.Equal(JsonConvert.SerializeObject(serializedData), jobState.Data);
            });
        }

        [Fact, CleanDatabase]
        public void AddToQueue_CallsEnqueue_OnTargetPersistentQueue()
        {
            UseConnection(database =>
            {
                var correctJobQueue = new Mock<IPersistentJobQueue>();
                var correctProvider = new Mock<IPersistentJobQueueProvider>();
                correctProvider.Setup(x => x.GetJobQueue(It.IsNotNull<HangfireDbContext>()))
                    .Returns(correctJobQueue.Object);

                _queueProviders.Add(correctProvider.Object, new[] { "default" });

                Commit(database, x => x.AddToQueue("default", "1"));

                correctJobQueue.Verify(x => x.Enqueue("default", "1"));
            });
        }

        [Fact, CleanDatabase]
        public void IncrementCounter_AddsRecordToCounterTable_WithPositiveValue()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.IncrementCounter("my-key"));

                Counter record = database.CounterRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(1L, record.Value);
                Assert.Equal(DateTime.MinValue, record.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void IncrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.IncrementCounter("my-key", TimeSpan.FromDays(1)));

                Counter record = database.CounterRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(1L, record.Value);
                Assert.NotEqual(DateTime.MinValue, record.ExpireAt);

                var expireAt = record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            });
        }

        [Fact, CleanDatabase]
        public void IncrementCounter_WithExistingKey_AddsAnotherRecord()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.IncrementCounter("my-key");
                    x.IncrementCounter("my-key");
                });

                var recordCount = database.CounterRepository.Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void DecrementCounter_AddsRecordToCounterTable_WithNegativeValue()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.DecrementCounter("my-key"));

                Counter record = database.CounterRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1L, record.Value);
                Assert.Equal(DateTime.MinValue, record.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void DecrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.DecrementCounter("my-key", TimeSpan.FromDays(1)));

                Counter record = database.CounterRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1L, record.Value);
                Assert.NotEqual(DateTime.MinValue, record.ExpireAt);

                var expireAt = (DateTime)record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            });
        }

        [Fact, CleanDatabase]
        public void DecrementCounter_WithExistingKey_AddsAnotherRecord()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.DecrementCounter("my-key");
                    x.DecrementCounter("my-key");
                });

                var recordCount = database.CounterRepository.Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void AddToSet_AddsARecord_IfThereIsNo_SuchKeyAndValue()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.AddToSet("my-key", "my-value"));

                Set record = database.SetRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(0.0m, record.Score, 2);
            });
        }

        [Fact, CleanDatabase]
        public void AddToSet_AddsARecord_WhenKeyIsExists_ButValuesAreDifferent()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "another-value");
                });

                var recordCount = database.SetRepository.Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void AddToSet_DoesNotAddARecord_WhenBothKeyAndValueAreExist()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value");
                });

                var recordCount = database.SetRepository.Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void AddToSet_WithScore_AddsARecordWithScore_WhenBothKeyAndValueAreNotExist()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.AddToSet("my-key", "my-value", 3.2));

                Set record = database.SetRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(3.2m, record.Score, 3);
            });
        }

        [Fact, CleanDatabase]
        public void AddToSet_WithScore_UpdatesAScore_WhenBothKeyAndValueAreExist()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value", 3.2);
                });

                Set record = database.SetRepository.ToList().First();

                Assert.Equal(3.2m, record.Score, 3);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromSet_RemovesARecord_WithGivenKeyAndValue()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    var key = "my-key";
                    var value = "my-value";

                    x.AddToSet(key, value);
                    x.RemoveFromSet(key, value);
                });

                var recordCount = database.SetRepository.Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameKey_AndDifferentValue()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "different-value");
                });

                var recordCount = database.SetRepository.Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameValue_AndDifferentKey()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("different-key", "my-value");
                });

                var recordCount = database.SetRepository.Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void InsertToList_AddsARecord_WithGivenValues()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.InsertToList("my-key", "my-value"));

                HangfireList record = database.HangfireListRepository.ToList().Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
            });
        }

        [Fact, CleanDatabase]
        public void InsertToList_AddsAnotherRecord_WhenBothKeyAndValueAreExist()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromList_RemovesAllRecords_WithGivenKeyAndValue()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "my-value");
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameKey_ButDifferentValue()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "different-value");
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameValue_ButDifferentKey()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("different-key", "my-value");
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void TrimList_TrimsAList_ToASpecifiedRange()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.InsertToList("my-key", "3");
                    x.TrimList("my-key", 1, 2);
                });

                var records = database.HangfireListRepository.ToList().ToArray();

                Assert.Equal(2, records.Length);
                Assert.Equal("1", records[0].Value);
                Assert.Equal("2", records[1].Value);
            });
        }

        [Fact, CleanDatabase]
        public void TrimList_RemovesRecordsToEnd_IfKeepAndingAt_GreaterThanMaxElementIndex()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.TrimList("my-key", 1, 100);
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(2, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void TrimList_RemovesAllRecords_WhenStartingFromValue_GreaterThanMaxElementIndex()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 100);
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void TrimList_RemovesAllRecords_IfStartFromGreaterThanEndingAt()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 0);
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(0, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void TrimList_RemovesRecords_OnlyOfAGivenKey()
        {
            UseConnection(database =>
            {
                Commit(database, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("another-key", 1, 0);
                });

                var recordCount = database.HangfireListRepository.Count();

                Assert.Equal(1, recordCount);
            });
        }

        [Fact, CleanDatabase]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection(database =>
            {
                ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(database, x => x.SetRangeInHash(null, new Dictionary<string, string>())));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull()
        {
            UseConnection(database =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(database, x => x.SetRangeInHash("some-hash", null)));

                Assert.Equal("keyValuePairs", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void SetRangeInHash_MergesAllRecords()
        {
            UseConnection(database =>
            {
                Commit(database, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                        {
                            { "Key1", "Value1" },
                            { "Key2", "Value2" }
                        }));

                var result = database.HashRepository.Where(_ => _.Key == "some-hash").ToList()
                    .ToDictionary(x => x.Field, x => x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection(database =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(database, x => x.RemoveHash(null)));
            });
        }

        [Fact, CleanDatabase]
        public void RemoveHash_RemovesAllHashRecords()
        {
            UseConnection(database =>
            {
                // Arrange
                Commit(database, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                        {
                            { "Key1", "Value1" },
                            { "Key2", "Value2" }
                        }));

                // Act
                Commit(database, x => x.RemoveHash("some-hash"));

                // Assert
                var count = database.HashRepository.Count();
                Assert.Equal(0, count);
            });
        }

        [Fact, CleanDatabase]
        public void ExpireSet_SetsSetExpirationData()
        {
            UseConnection(database =>
            {
                var set1 = new Set { Key = "Set1", Value = "value1" };
                database.Database.Insert(set1);

                var set2 = new Set { Key = "Set2", Value = "value2" };
                database.Database.Insert(set2);

                Commit(database, x => x.ExpireSet(set1.Key, TimeSpan.FromDays(1)));

                var testSet1 = GetTestSet(database, set1.Key).FirstOrDefault();
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < testSet1.ExpireAt && testSet1.ExpireAt <= DateTime.UtcNow.AddDays(1));

                var testSet2 = GetTestSet(database, set2.Key).FirstOrDefault();
                Assert.NotNull(testSet2);
                Assert.Equal(DateTime.MinValue, testSet2.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void ExpireList_SetsListExpirationData()
        {
            UseConnection(database =>
            {
                var list1 = new HangfireList { Key = "List1", Value = "value1" };
                database.Database.Insert(list1);

                var list2 = new HangfireList { Key = "List2", Value = "value2" };
                database.Database.Insert(list2);

                Commit(database, x => x.ExpireList(list1.Key, TimeSpan.FromDays(1)));

                var testList1 = GetTestList(database, list1.Key);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < testList1.ExpireAt && testList1.ExpireAt <= DateTime.UtcNow.AddDays(1));

                var testList2 = GetTestList(database, list2.Key);
                Assert.Equal(DateTime.MinValue, testList2.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void ExpireHash_SetsHashExpirationData()
        {
            UseConnection(database =>
            {
                var hash1 = new Hash { Key = "Hash1", Value = "value1" };
                database.Database.Insert(hash1);

                var hash2 = new Hash { Key = "Hash2", Value = "value2" };
                database.Database.Insert(hash2);

                Commit(database, x => x.ExpireHash(hash1.Key, TimeSpan.FromDays(1)));

                var testHash1 = GetTestHash(database, hash1.Key);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < testHash1.ExpireAt && testHash1.ExpireAt <= DateTime.UtcNow.AddDays(1));

                var testHash2 = GetTestHash(database, hash2.Key);
                Assert.Equal(DateTime.MinValue, testHash2.ExpireAt);
            });
        }


        [Fact, CleanDatabase]
        public void PersistSet_ClearsTheSetExpirationData()
        {
            UseConnection(database =>
            {
                var set1 = new Set { Key = "Set1", Value = "value1", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set1);

                var set2 = new Set { Key = "Set2", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set2);

                Commit(database, x => x.PersistSet(set1.Key));

                var testSet1 = GetTestSet(database, set1.Key).First();
                Assert.Equal(DateTime.MinValue, testSet1.ExpireAt);

                var testSet2 = GetTestSet(database, set2.Key).First();
                Assert.NotEqual(DateTime.MinValue, testSet2.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void PersistList_ClearsTheListExpirationData()
        {
            UseConnection(database =>
            {
                var list1 = new HangfireList { Key = "List1", Value = "value1", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(list1);

                var list2 = new HangfireList { Key = "List2", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(list2);

                Commit(database, x => x.PersistList(list1.Key));

                var testList1 = GetTestList(database, list1.Key);
                Assert.Equal(DateTime.MinValue, testList1.ExpireAt);

                var testList2 = GetTestList(database, list2.Key);
                Assert.NotEqual(DateTime.MinValue, testList2.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void PersistHash_ClearsTheHashExpirationData()
        {
            UseConnection(database =>
            {
                var hash1 = new Hash { Key = "Hash1", Value = "value1", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(hash1);

                var hash2 = new Hash { Key = "Hash2", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(hash2);

                Commit(database, x => x.PersistHash(hash1.Key));

                var testHash1 = GetTestHash(database, hash1.Key);
                Assert.Equal(DateTime.MinValue, testHash1.ExpireAt);

                var testHash2 = GetTestHash(database, hash2.Key);
                Assert.NotEqual(DateTime.MinValue, testHash2.ExpireAt);
            });
        }

        [Fact, CleanDatabase]
        public void AddRangeToSet_AddToExistingSetData()
        {
            UseConnection(database =>
            {
                var set1Val1 = new Set { Key = "Set1", Value = "value1", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set1Val1);

                var set1Val2 = new Set { Key = "Set1", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set1Val2);

                var set2 = new Set { Key = "Set2", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set2);

                var values = new[] { "test1", "test2", "test3" };
                Commit(database, x => x.AddRangeToSet(set1Val1.Key, values));

                var testSet1 = GetTestSet(database, set1Val1.Key);
                var valuesToTest = new List<string>(values) { "value1", "value2" };

                Assert.NotNull(testSet1);
                // verify all values are present in testSet1
                Assert.True(testSet1.Select(s => s.Value.ToString()).All(value => valuesToTest.Contains(value)));
                Assert.Equal(5, testSet1.Count);

                var testSet2 = GetTestSet(database, set2.Key);
                Assert.NotNull(testSet2);
                Assert.Equal(1, testSet2.Count);
            });
        }


        [Fact, CleanDatabase]
        public void RemoveSet_ClearsTheSetData()
        {
            UseConnection(database =>
            {
                var set1Val1 = new Set { Key = "Set1", Value = "value1", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set1Val1);

                var set1Val2 = new Set { Key = "Set1", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set1Val2);

                var set2 = new Set { Key = "Set2", Value = "value2", ExpireAt = DateTime.UtcNow };
                database.Database.Insert(set2);

                Commit(database, x => x.RemoveSet(set1Val1.Key));

                var testSet1 = GetTestSet(database, set1Val1.Key);
                Assert.Equal(0, testSet1.Count);

                var testSet2 = GetTestSet(database, set2.Key);
                Assert.Equal(1, testSet2.Count);
            });
        }

        private static HangfireJob GetTestJob(HangfireDbContext database, int jobId)
        {
            return database.HangfireJobRepository.FirstOrDefault(x => x.Id == jobId);
        }

        private static IList<Set> GetTestSet(HangfireDbContext database, string key)
        {
            return database.SetRepository.Where(_ => _.Key == key).ToList();
        }

        private static dynamic GetTestList(HangfireDbContext database, string key)
        {
            return database.HangfireListRepository.FirstOrDefault(_ => _.Key == key);
        }

        private static dynamic GetTestHash(HangfireDbContext database, string key)
        {
            return database.HashRepository.FirstOrDefault(_ => _.Key == key);
        }

        private void UseConnection(Action<HangfireDbContext> action)
        {
            HangfireDbContext connection = ConnectionUtils.CreateConnection();
            action(connection);
        }

        private void Commit(HangfireDbContext connection, Action<SQLiteWriteOnlyTransaction> action)
        {
            using (SQLiteWriteOnlyTransaction transaction = new SQLiteWriteOnlyTransaction(connection, _queueProviders))
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}

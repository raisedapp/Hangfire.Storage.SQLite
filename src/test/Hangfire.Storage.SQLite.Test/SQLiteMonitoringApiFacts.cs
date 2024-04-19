using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage.SQLite.Entities;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public class SQLiteMonitoringApiFacts : SqliteInMemoryTestBase
    {
        private const string DefaultQueue = "default";
        private const string FetchedStateName = "Fetched";
        private const int From = 0;
        private const int PerPage = 5;
        private readonly Mock<IPersistentJobQueueMonitoringApi> _persistentJobQueueMonitoringApi;
        private readonly PersistentJobQueueProviderCollection _providers;

        public SQLiteMonitoringApiFacts()
        {
            var queue = new Mock<IPersistentJobQueue>();
            _persistentJobQueueMonitoringApi = new Mock<IPersistentJobQueueMonitoringApi>();

            var provider = new Mock<IPersistentJobQueueProvider>();
            provider.Setup(x => x.GetJobQueue(It.IsNotNull<HangfireDbContext>())).Returns(queue.Object);
            provider.Setup(x => x.GetJobQueueMonitoringApi(It.IsNotNull<HangfireDbContext>()))
                .Returns(_persistentJobQueueMonitoringApi.Object);

            _providers = new PersistentJobQueueProviderCollection(provider.Object);
        }

        [Fact]
        public void GetStatistics_ReturnsZero_WhenNoJobsExist()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var result = monitoringApi.GetStatistics();
                Assert.Equal(0, result.Enqueued);
                Assert.Equal(0, result.Failed);
                Assert.Equal(0, result.Processing);
                Assert.Equal(0, result.Scheduled);
            });
        }

        [Fact]
        public void GetStatistics_ReturnsExpectedCounts_WhenJobsExist()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                CreateJobInState(database, EnqueuedState.StateName);
                CreateJobInState(database, EnqueuedState.StateName);
                CreateJobInState(database, FailedState.StateName);
                CreateJobInState(database, ProcessingState.StateName);
                CreateJobInState(database, ScheduledState.StateName);
                CreateJobInState(database, ScheduledState.StateName);

                var result = monitoringApi.GetStatistics();
                Assert.Equal(2, result.Enqueued);
                Assert.Equal(1, result.Failed);
                Assert.Equal(1, result.Processing);
                Assert.Equal(2, result.Scheduled);
            });
        }

        [Fact]
        public void JobDetails_ReturnsNull_WhenThereIsNoSuchJob()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var result = monitoringApi.JobDetails("547527");
                Assert.Null(result);
            });
        }

        [Fact]
        public void JobDetails_ReturnsResult_WhenJobExists()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var job1 = CreateJobInState(database, EnqueuedState.StateName);

                var result = monitoringApi.JobDetails(Convert.ToString(job1.Id));

                Assert.NotNull(result);
                Assert.NotNull(result.Job);
                Assert.Equal("Arguments", result.Job.Args[0]);
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < result.CreatedAt);
                Assert.True(result.CreatedAt < DateTime.UtcNow.AddMinutes(1));
            });
        }

        [Fact]
        public void EnqueuedJobs_ReturnsEmpty_WhenThereIsNoJobs()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var jobIds = new List<int>();

                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetEnqueuedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.EnqueuedJobs(DefaultQueue, From, PerPage);

                Assert.Empty(resultList);
            });
        }

        [Fact]
        public void EnqueuedJobs_ReturnsSingleJob_WhenOneJobExistsThatIsNotFetched()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var unfetchedJob = CreateJobInState(database, EnqueuedState.StateName);

                var jobIds = new List<int> { unfetchedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetEnqueuedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.EnqueuedJobs(DefaultQueue, From, PerPage);

                Assert.Single(resultList);
            });
        }

        [Fact]
        public void EnqueuedJobs_ReturnsEmpty_WhenOneJobExistsThatIsFetched()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var fetchedJob = CreateJobInState(database, FetchedStateName);

                var jobIds = new List<int> { fetchedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetEnqueuedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.EnqueuedJobs(DefaultQueue, From, PerPage);

                Assert.Empty(resultList);
            });
        }

        [Fact]
        public void EnqueuedJobs_ReturnsUnfetchedJobsOnly_WhenMultipleJobsExistsInFetchedAndUnfetchedStates()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var unfetchedJob = CreateJobInState(database, EnqueuedState.StateName);
                var unfetchedJob2 = CreateJobInState(database, EnqueuedState.StateName);
                var fetchedJob = CreateJobInState(database, FetchedStateName);

                var jobIds = new List<int> { unfetchedJob.Id, unfetchedJob2.Id, fetchedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetEnqueuedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.EnqueuedJobs(DefaultQueue, From, PerPage);

                Assert.Equal(2, resultList.Count);
            });
        }

        [Fact]
        public void FetchedJobs_ReturnsEmpty_WhenThereIsNoJobs()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var jobIds = new List<int>();

                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetFetchedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.FetchedJobs(DefaultQueue, From, PerPage);

                Assert.Empty(resultList);
            });
        }

        [Fact]
        public void FetchedJobs_ReturnsSingleJob_WhenOneJobExistsThatIsFetched()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var fetchedJob = CreateJobInState(database, FetchedStateName);

                var jobIds = new List<int> { fetchedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetFetchedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.FetchedJobs(DefaultQueue, From, PerPage);

                Assert.Single(resultList);
            });
        }

        [Fact]
        public void FetchedJobs_ReturnsEmpty_WhenOneJobExistsThatIsNotFetched()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var unfetchedJob = CreateJobInState(database, EnqueuedState.StateName);

                var jobIds = new List<int> { unfetchedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetFetchedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.FetchedJobs(DefaultQueue, From, PerPage);

                Assert.Empty(resultList);
            });
        }

        [Fact]
        public void FetchedJobs_ReturnsFetchedJobsOnly_WhenMultipleJobsExistsInFetchedAndUnfetchedStates()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var fetchedJob = CreateJobInState(database, FetchedStateName);
                var fetchedJob2 = CreateJobInState(database, FetchedStateName);
                var unfetchedJob = CreateJobInState(database, EnqueuedState.StateName);

                var jobIds = new List<int> { fetchedJob.Id, fetchedJob2.Id, unfetchedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                    .GetFetchedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.FetchedJobs(DefaultQueue, From, PerPage);

                Assert.Equal(2, resultList.Count);
            });
        }

        [Fact]
        public void ProcessingJobs_ReturnsProcessingJobsOnly_WhenMultipleJobsExistsInProcessingSucceededAndEnqueuedState()
        {
            UseMonitoringApi((database, monitoringApi) =>
            {
                var processingJob = CreateJobInState(database, ProcessingState.StateName);

                var succeededJob = CreateJobInState(database, SucceededState.StateName, liteJob =>
                {
                    var processingState = new State()
                    {
                        Name = ProcessingState.StateName,
                        Reason = null,
                        CreatedAt = DateTime.UtcNow,
                        Data = JsonConvert.SerializeObject(new Dictionary<string, string>
                        {
                            ["ServerId"] = Guid.NewGuid().ToString(),
                            ["StartedAt"] =
                            JobHelper.SerializeDateTime(DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(500)))
                        })
                    };

                    //var succeededState = liteJob.StateHistory[0];
                    //liteJob.StateHistory = new List<State> { processingState, succeededState };

                    return liteJob;
                });

                var enqueuedJob = CreateJobInState(database, EnqueuedState.StateName);

                var jobIds = new List<int> { processingJob.Id, succeededJob.Id, enqueuedJob.Id };
                _persistentJobQueueMonitoringApi.Setup(x => x
                        .GetFetchedJobIds(DefaultQueue, From, PerPage))
                    .Returns(jobIds);

                var resultList = monitoringApi.ProcessingJobs(From, PerPage);

                Assert.Single(resultList);

            });
        }

        private void UseMonitoringApi(Action<HangfireDbContext, SQLiteMonitoringApi> action)
        {
            var connection = new SQLiteMonitoringApi(Storage, _providers);
            using var dbContext = Storage.CreateAndOpenConnection();
            action(dbContext, connection);
        }

        private HangfireJob CreateJobInState(HangfireDbContext database, string stateName, Func<HangfireJob, HangfireJob> visitor = null)
        {
            var job = Job.FromExpression(() => Console.WriteLine("TEST"));

            Dictionary<string, string> stateData;

            if (stateName == EnqueuedState.StateName)
            {
                stateData = new Dictionary<string, string> { ["EnqueuedAt"] = $"{DateTime.UtcNow:o}" };
            }
            else if (stateName == ProcessingState.StateName)
            {
                stateData = new Dictionary<string, string>
                {
                    ["ServerId"] = Guid.NewGuid().ToString(),
                    ["StartedAt"] = JobHelper.SerializeDateTime(DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(500)))
                };
            }
            else
            {
                stateData = new Dictionary<string, string>();
            }

            var hangfireJob = new HangfireJob
            {
                InvocationData = SerializationHelper.Serialize(InvocationData.SerializeJob(job)),
                Arguments = "[\"\\\"Arguments\\\"\"]",
                StateName = stateName,
                CreatedAt = DateTime.UtcNow
            };

            database.Database.Insert(hangfireJob);

            var jobState = new State()
            {
                JobId = hangfireJob.Id,
                Name = stateName,
                Reason = null,
                CreatedAt = DateTime.UtcNow,
                Data = JsonConvert.SerializeObject(stateData)
            };

            if (visitor != null)
            {
                hangfireJob = visitor(hangfireJob);
            }


            database.Database.Insert(jobState);

            var jobQueue = new JobQueue
            {
                FetchedAt = DateTime.MinValue,
                JobId = hangfireJob.Id,
                Queue = DefaultQueue
            };

            if (stateName == FetchedStateName)
            {
                jobQueue.FetchedAt = DateTime.UtcNow;
            }

            database.Database.Insert(jobQueue);

            return hangfireJob;
        }
    }
}

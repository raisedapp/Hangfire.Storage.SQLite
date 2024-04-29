using Hangfire.Storage.SQLite.Entities;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public class SQLiteJobQueueFacts : SqliteInMemoryTestBase
    {
        private static readonly string[] DefaultQueues = { "default" };

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new SQLiteJobQueue(null, new SQLiteStorageOptions()));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsValueIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    new SQLiteJobQueue(connection, null));

                Assert.Equal("storageOptions", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsNull()
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                var exception = Assert.Throws<ArgumentNullException>(() =>
                    queue.Dequeue(null, CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ShouldThrowAnException_WhenQueuesCollectionIsEmpty()
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                var exception = Assert.Throws<ArgumentException>(() =>
                    queue.Dequeue(new string[0], CreateTimingOutCancellationToken()));

                Assert.Equal("queues", exception.ParamName);
            });
        }

        [Fact]
        public void Dequeue_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                var queue = CreateJobQueue(connection);

                Assert.Throws<OperationCanceledException>(() =>
                    queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact]
        public void Dequeue_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            UseConnection(connection =>
            {
                var cts = new CancellationTokenSource(200);
                var queue = CreateJobQueue(connection);

                Assert.Throws<OperationCanceledException>(() =>
                    queue.Dequeue(DefaultQueues, cts.Token));
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            // Arrange
            UseConnection(connection =>
            {
                var jobQueue = new JobQueue
                {
                    JobId = 1,
                    Queue = "default"
                };

                connection.Database.Insert(jobQueue);

                var queue = CreateJobQueue(connection);

                // Act
                var payload = (SQLiteFetchedJob)queue.Dequeue(DefaultQueues, CreateTimingOutCancellationToken());

                // Assert
                Assert.Equal("1", payload.JobId);
                Assert.Equal("default", payload.Queue);
            });
        }

        [Fact]
        public void Dequeue_ShouldLeaveJobInTheQueue_ButSetItsFetchedAtValue()
        {
            // Arrange
            UseConnection(connection =>
            {
                var job = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(job);

                var jobQueue = new JobQueue
                {
                    JobId = job.Id,
                    Queue = "default"
                };
                connection.Database.Insert(jobQueue);

                var queue = CreateJobQueue(connection);

                // Act
                var payload = queue.Dequeue(DefaultQueues, CreateTimingOutCancellationToken());
                var payloadJobId = int.Parse(payload.JobId);
                // Assert
                Assert.NotNull(payload);

                var fetchedAt = connection.JobQueueRepository.FirstOrDefault(_ => _.JobId == payloadJobId).FetchedAt;

                Assert.NotEqual(fetchedAt, DateTime.MinValue);
                Assert.True(fetchedAt > DateTime.UtcNow.AddMinutes(-1));
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchATimedOutJobs_FromTheSpecifiedQueue()
        {
            // Arrange
            UseConnection(connection =>
            {
                var job = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(job);

                var jobQueue = new JobQueue
                {
                    JobId = job.Id,
                    Queue = "default",
                    FetchedAt = DateTime.UtcNow.AddDays(-1)
                };
                connection.Database.Insert(jobQueue);

                var queue = CreateJobQueue(connection);

                // Act
                var payload = queue.Dequeue(DefaultQueues, CreateTimingOutCancellationToken());

                // Assert
                Assert.NotEmpty(payload.JobId);
            });
        }

        [Fact]
        public void Dequeue_ShouldSetFetchedAt_OnlyForTheFetchedJob()
        {
            // Arrange
            UseConnection(connection =>
            {
                var job1 = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(job1);

                var job2 = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(job2);

                connection.Database.Insert(new JobQueue
                {
                    JobId = job1.Id,
                    Queue = "default"
                });

                connection.Database.Insert(new JobQueue
                {
                    JobId = job2.Id,
                    Queue = "default"
                });

                var queue = CreateJobQueue(connection);

                // Act
                var payload = queue.Dequeue(DefaultQueues, CreateTimingOutCancellationToken());
                var payloadJobId = int.Parse(payload.JobId);

                // Assert
                var otherJobFetchedAt = connection.JobQueueRepository.FirstOrDefault(_ => _.JobId != payloadJobId).FetchedAt;

                Assert.Equal(otherJobFetchedAt, DateTime.MinValue);
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            UseConnection(connection =>
            {
                var job1 = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(job1);

                connection.Database.Insert(new JobQueue
                {
                    JobId = job1.Id,
                    Queue = "critical"
                });


                var queue = CreateJobQueue(connection);

                Assert.Throws<OperationCanceledException>(() => queue.Dequeue(DefaultQueues, CreateTimingOutCancellationToken()));
            });
        }

        [Fact]
        public void Dequeue_ShouldFetchJobs_FromMultipleQueuesBasedOnQueuePriority()
        {
            UseConnection(connection =>
            {
                var criticalJob = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(criticalJob);

                var defaultJob = new HangfireJob()
                {
                    InvocationData = "",
                    Arguments = "",
                    CreatedAt = DateTime.UtcNow
                };
                connection.Database.Insert(defaultJob);

                connection.Database.Insert(new JobQueue
                {
                    JobId = defaultJob.Id,
                    Queue = "default"
                });

                connection.Database.Insert(new JobQueue
                {
                    JobId = criticalJob.Id,
                    Queue = "critical"
                });

                var queue = CreateJobQueue(connection);

                var critical = (SQLiteFetchedJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(critical.JobId);
                Assert.Equal("critical", critical.Queue);

                var @default = (SQLiteFetchedJob)queue.Dequeue(
                    new[] { "critical", "default" },
                    CreateTimingOutCancellationToken());

                Assert.NotNull(@default.JobId);
                Assert.Equal("default", @default.Queue);
            });
        }

        [Fact]
        public void Enqueue_AddsAJobToTheQueue()
        {
            UseConnection(connection =>
            {
                var queue = CreateJobQueue(connection);

                queue.Enqueue("default", "1");

                var record = connection.JobQueueRepository.ToList().Single();
                Assert.Equal("1", record.JobId.ToString());
                Assert.Equal("default", record.Queue);
                Assert.Equal(record.FetchedAt, DateTime.MinValue);
            });
        }

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return source.Token;
        }

        private static SQLiteJobQueue CreateJobQueue(HangfireDbContext connection)
        {
            return new SQLiteJobQueue(connection, new SQLiteStorageOptions());
        }

        private void UseConnection(Action<HangfireDbContext> action)
        {
            using var connection = Storage.CreateAndOpenConnection();
            action(connection);
        }
    }
}
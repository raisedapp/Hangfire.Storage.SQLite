using System;
using System.Linq;
using Hangfire.Storage.SQLite.Entities;
using Hangfire.Storage.SQLite.Test.Utils;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    [Collection("Database")]
    public class SQLiteFetchedJobFacts
    {
        private const int JobId = 0;
        private const string Queue = "queue";

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SQLiteFetchedJob(null, 0, JobId, Queue));

                Assert.Equal("connection", exception.ParamName);
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(() => new SQLiteFetchedJob(connection, 0, null, Queue));

                Assert.Equal("jobId", exception.ParamName);
            });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            UseConnection(connection =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SQLiteFetchedJob(connection, 0, JobId, null));

                Assert.Equal("queue", exception.ParamName);
            });
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            UseConnection(connection =>
            {
                var fetchedJob = new SQLiteFetchedJob(connection, 0, JobId, Queue);

                Assert.Equal(JobId.ToString(), fetchedJob.JobId);
                Assert.Equal(Queue, fetchedJob.Queue);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_ReallyDeletesTheJobFromTheQueue()
        {
            UseConnection(connection =>
            {
                // Arrange
                var queue = "default";
                var jobId = 1;
                var id = CreateJobQueueRecord(connection, jobId, queue);
                var processingJob = new SQLiteFetchedJob(connection, id, jobId, queue);

                // Act
                processingJob.RemoveFromQueue();

                // Assert
                var count = connection.JobQueueRepository.Count();
                Assert.Equal(0, count);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_DoesNotDelete_UnrelatedJobs()
        {
            UseConnection(connection =>
            {
                // Arrange
                CreateJobQueueRecord(connection, 1, "default");
                CreateJobQueueRecord(connection, 2, "critical");
                CreateJobQueueRecord(connection, 3, "default");

                var fetchedJob = new SQLiteFetchedJob(connection, 0, 999, "default");

                // Act
                fetchedJob.RemoveFromQueue();

                // Assert
                var count = connection.JobQueueRepository.Count();
                Assert.Equal(3, count);
            });
        }

        [Fact, CleanDatabase]
        public void Requeue_SetsFetchedAtValueToNull()
        {
            UseConnection(connection =>
            {
                // Arrange
                var queue = "default";
                var jobId = 1;
                var id = CreateJobQueueRecord(connection, jobId, queue);
                var processingJob = new SQLiteFetchedJob(connection, id, jobId, queue);

                // Act
                processingJob.Requeue();

                // Assert
                var record = connection.JobQueueRepository.ToList().Single();
                Assert.Equal(record.FetchedAt, DateTime.MinValue);
            });
        }

        [Fact, CleanDatabase]
        public void Dispose_SetsFetchedAtValueToNull_IfThereWereNoCallsToComplete()
        {
            UseConnection(connection =>
            {
                // Arrange
                var queue = "default";
                var jobId = 1;
                var id = CreateJobQueueRecord(connection, jobId, queue);
                var processingJob = new SQLiteFetchedJob(connection, id, jobId, queue);

                // Act
                processingJob.Dispose();

                // Assert
                var record = connection.JobQueueRepository.ToList().Single();
                Assert.Equal(record.FetchedAt, DateTime.MinValue);
            });
        }

        private static int CreateJobQueueRecord(HangfireDbContext connection, int jobId, string queue)
        {
            var jobQueue = new JobQueue
            {
                JobId = jobId,
                Queue = queue,
                FetchedAt = DateTime.UtcNow
            };

            connection.Database.Insert(jobQueue);

            return jobQueue.Id;
        }

        private static void UseConnection(Action<HangfireDbContext> action)
        {
            var connection = ConnectionUtils.CreateConnection();
            action(connection);
        }       
    }
}
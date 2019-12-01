using System;
using System.Reflection;
using System.Threading;
using Hangfire.Storage.SQLite.Entities;
using Xunit.Sdk;

namespace Hangfire.Storage.SQLite.Test.Utils
{
    public class CleanDatabaseAttribute : BeforeAfterTestAttribute
    {
        private static readonly object GlobalLock = new object();
        
        public override void Before(MethodInfo methodUnderTest)
        {
            Monitor.Enter(GlobalLock);

            RecreateDatabaseAndInstallObjects();
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Monitor.Exit(GlobalLock);
        }

        private static void RecreateDatabaseAndInstallObjects()
        {
            var context = ConnectionUtils.CreateConnection();
            try
            {
                context.Init(new SQLiteStorageOptions());

                context.Database.DeleteAll<DistributedLock>();
                context.Database.DeleteAll<State>();
                context.Database.DeleteAll<Hash>();
                context.Database.DeleteAll<Set>();
                context.Database.DeleteAll<HangfireList>();
                context.Database.DeleteAll<Counter>();
                context.Database.DeleteAll<AggregatedCounter>();
                context.Database.DeleteAll<HangfireJob>();
                context.Database.DeleteAll<JobQueue>();
                context.Database.DeleteAll<JobParameter>();
                context.Database.DeleteAll<HangfireServer>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to cleanup database.", ex);
            }
        }
    }
}
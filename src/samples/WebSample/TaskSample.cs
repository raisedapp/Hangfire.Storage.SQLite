using Hangfire.JobsLogger;
using Hangfire.Server;

namespace WebSample
{
    public class TaskSample
    {
        public void TaskMethod()
        {
            Console.WriteLine("Testing Web Sample!!!");
        }

        public void TaskMethod2(PerformContext? pfContext)
        {
            pfContext.LogInformation("Testing Web Sample!!!");
            Console.WriteLine("Testing Web Sample!!!");
        }
    }
}
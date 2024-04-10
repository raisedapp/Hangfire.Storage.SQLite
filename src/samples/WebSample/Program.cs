using Hangfire;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.JobsLogger;
using Hangfire.Server;
using Hangfire.Storage.SQLite;
using WebSample;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddTransient<TaskSample>();
services.AddTransient<IBackgroundProcess, ProcessMonitor>(x => new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(10)));
services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSQLiteStorage("Hangfire.db")
    .UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(10))
    .UseJobsLogger());
services.AddHangfireServer(options =>
{
    options.Queues = new[] { "test_queue_1", "default" };
});

var app = builder.Build();

app.UseHangfireDashboard(string.Empty);

RecurringJob.AddOrUpdate("TaskMethod()", (TaskSample t) => t.TaskMethod(), Cron.Minutely);
RecurringJob.AddOrUpdate("TaskMethod2()", (TaskSample t) => t.TaskMethod2(null), Cron.Minutely);

var t = app.Services.GetService<IBackgroundJobClient>();
t.Enqueue(queue: "test_queue_1", methodCall: () => Console.WriteLine("Testing......"));
t.Enqueue(queue: "test_queue_1", methodCall: () => Console.WriteLine("Testing......"));
t.Enqueue(queue: "test_queue_1", methodCall: () => Console.WriteLine("Testing......"));
t.Enqueue(queue: "test_queue_1", methodCall: () => Console.WriteLine("Testing......"));
t.Enqueue(queue: "test_queue_1", methodCall: () => Console.WriteLine("Testing......"));
t.Enqueue(queue: "test_queue_1", methodCall: () => Console.WriteLine("Testing......"));

app.Run();
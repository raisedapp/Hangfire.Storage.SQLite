using Hangfire;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.JobsLogger;
using Hangfire.Server;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace WebSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage("Hangfire.db", new SQLiteStorageOptions() { AutoVacuumSelected = SQLiteStorageOptions.AutoVacuum.FULL, RecurringAutoCleanIsEnabled = true })
                .UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(30))
                .UseJobsLogger());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer(additionalProcesses: new[] { new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(30)) });
            app.UseHangfireDashboard(string.Empty);

            RecurringJob.AddOrUpdate(() => TaskMethod(), Cron.Minutely);
            RecurringJob.AddOrUpdate(() => TaskMethod2(null), Cron.Minutely);
        }

        public void TaskMethod()
        {
            Console.WriteLine("Testing Web Sample!!!");
        }

        public void TaskMethod2(PerformContext pfContext)
        {
            pfContext.LogInformation("Testing Web Sample!!!");
            Console.WriteLine("Testing Web Sample!!!");
        }
    }
}

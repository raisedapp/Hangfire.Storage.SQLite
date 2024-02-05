using Hangfire;
using Hangfire.Storage.SQLite;

try
{
    GlobalConfiguration.Configuration.
    UseColouredConsoleLogProvider().
    UseSQLiteStorage("Hangfire.db", new SQLiteStorageOptions() { AutoVacuumSelected = SQLiteStorageOptions.AutoVacuum.FULL });

    using (new BackgroundJobServer())
    {
        BackgroundJob.Enqueue(() => Console.WriteLine("ConsoleSample Start :)"));
        RecurringJob.AddOrUpdate("Console.WriteLine()", () => Console.WriteLine("Background Job: Hello, world!"), Cron.Minutely);

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error in execution. Detail: {ex}");
    Console.ReadLine();
}
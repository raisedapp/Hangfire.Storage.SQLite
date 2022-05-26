namespace Hangfire.Storage.SQLite.Test.Utils
{
    public static class ConnectionUtils
    {
        private const string Ext = "db";

        private static string GetConnectionString()
        {
            return $"Hangfire-Tests.{Ext}";
        }

        public static SQLiteStorage CreateStorage()
        {
            var storageOptions = new SQLiteStorageOptions();

            return CreateStorage(storageOptions);
        }

        public static SQLiteStorage CreateStorage(SQLiteStorageOptions storageOptions)
        {
            var connectionString = GetConnectionString();
            return new SQLiteStorage(connectionString, storageOptions);
        }

        public static HangfireDbContext CreateConnection()
        {
            return CreateStorage().Connection;
        }
    }
}
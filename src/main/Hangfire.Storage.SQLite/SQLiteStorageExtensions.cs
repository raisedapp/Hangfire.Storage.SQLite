using Hangfire.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public static class SQLiteStorageExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IGlobalConfiguration<SQLiteStorage> UseSQLiteStorage(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            
            var storage = new SQLiteStorage("Hangfire.db", new SQLiteStorageOptions());
            
            return configuration.UseStorage(storage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="databasePath"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IGlobalConfiguration<SQLiteStorage> UseSQLiteStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] string databasePath,
            SQLiteStorageOptions options = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (databasePath == null) throw new ArgumentNullException(nameof(databasePath));
            if (options == null) options = new SQLiteStorageOptions();
            
            var storage = new SQLiteStorage(databasePath, options);
            
            return configuration.UseStorage(storage);
        }
    }
}

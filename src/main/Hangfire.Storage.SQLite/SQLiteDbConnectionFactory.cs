using System;
using SQLite;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteDbConnectionFactory
    {
        private readonly Func<SQLiteConnection> _getConnection;

        public SQLiteDbConnectionFactory(Func<SQLiteConnection> getConnection)
        {
            _getConnection = getConnection;
        }

        public SQLiteConnection Create()
        {
            return _getConnection();
        }
    }
}
using System;
using Hangfire.Storage.SQLite.Test.Utils;

namespace Hangfire.Storage.SQLite.Test;

public class SqliteInMemoryTestBase : IDisposable
{
    protected SQLiteStorage Storage { get; } = ConnectionUtils.CreateStorage();
    /// <summary>
    /// This connection ensures that an in-memory database
    /// will stay alive until the test finishes
    /// </summary>
    private readonly IDisposable _rootingInMemoryConnection;

    protected SqliteInMemoryTestBase()
    {
        _rootingInMemoryConnection = Storage.CreateAndOpenConnection();
    }

    public virtual void Dispose()
    {
        _rootingInMemoryConnection?.Dispose();
        Storage?.Dispose();
    }
}
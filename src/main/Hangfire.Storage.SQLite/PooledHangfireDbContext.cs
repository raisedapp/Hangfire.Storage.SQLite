using System;
using SQLite;

namespace Hangfire.Storage.SQLite
{
    internal class PooledHangfireDbContext : HangfireDbContext
    {
        private readonly Action<PooledHangfireDbContext> _onDispose;
        public bool PhaseOut { get; set; }

        internal PooledHangfireDbContext(SQLiteConnection connection, Action<PooledHangfireDbContext> onDispose, string prefix = "hangfire") 
            : base(connection, prefix)
        {
            _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        }

        protected override void Dispose(bool disposing)
        {
            _onDispose(this);
            if (PhaseOut)
            {
                base.Dispose(disposing);
            }
        }
    }
}
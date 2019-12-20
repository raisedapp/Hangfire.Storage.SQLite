using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite.Entities
{
    internal static class DefaultValues
    {
        public const int MaxLengthKeyColumn = 100;

        public const int MaxLengthValueColumn = 100;

        public const int MaxLengthSetValueColumn = 256;

        public const int MaxLengthStateNameColumn = 20;

        public const int MaxLengthNameColumn = 40;

        public const int MaxLengthQueueColumn = 50;

        public const int MaxLengthIdColumn = 100;

        public const int MaxLengthReasonColumn = 100;

        public const int MaxLengthVarCharColumn = 16000;

        public const string AggregatedCounterTblName = "AggregatedCounter";

        public const string CounterTblName = "Counter";

        public const string DistributedLockTblName = "DistributedLock";

        public const string HangfireJobTblName = "Job";

        public const string HangfireListTblName = "List";

        public const string HangfireServerTblName = "Server";

        public const string HashTblName = "Hash";

        public const string JobParameterTblName = "JobParameter";

        public const string SetTblName = "Set";

        public const string StateTblName = "State";
    }
}

using System;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// <see cref="DateTime"/> helper
    /// </summary>
    public static class DateTimes
    {
        public static DateTime? NullIfDefault(this DateTime time) => time == default ?
            (DateTime?)null :
            time;
    }
}

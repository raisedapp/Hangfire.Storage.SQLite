using System;

namespace Hangfire.Storage.SQLite
{
    internal static class ObjectExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public static long ToInt64(this object value)
        {
            long longValue = 0L;

            try
            {
                longValue = Convert.ToInt64(value);
            }
            catch (Exception)
            {
                //Nothing..
            }

            return longValue;
        }
    }
}
namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public class EnqueuedAndFetchedCountDto
    {
        /// <summary>
        /// 
        /// </summary>
        public int? EnqueuedCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int? FetchedCount { get; set; }
    }
}

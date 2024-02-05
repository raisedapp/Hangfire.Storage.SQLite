using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public class SQLiteStorageOptionsFacts
    {
        [Fact]
        public void Ctor_SetsTheDefaultOptions()
        {
            SQLiteStorageOptions storageOptions = new SQLiteStorageOptions();

            Assert.Equal("hangfire", storageOptions.Prefix);
            Assert.True(storageOptions.InvisibilityTimeout > TimeSpan.Zero);
        }

        [Fact]
        public void Ctor_SetsTheDefaultOptions_ShouldGenerateClientId()
        {
            var storageOptions = new SQLiteStorageOptions();
            Assert.False(String.IsNullOrWhiteSpace(storageOptions.ClientId));
        }

        [Fact]
        public void Ctor_SetsTheDefaultOptions_ShouldGenerateUniqueClientId()
        {
            var storageOptions1 = new SQLiteStorageOptions();
            var storageOptions2 = new SQLiteStorageOptions();
            var storageOptions3 = new SQLiteStorageOptions();

            IEnumerable<string> result = new[] { storageOptions1.ClientId, storageOptions2.ClientId, storageOptions3.ClientId }.Distinct();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsEqualToZero()
        {
            var storageOptions = new SQLiteStorageOptions();
            Assert.Throws<ArgumentException>(
                () => storageOptions.QueuePollInterval = TimeSpan.Zero);
        }

        [Fact]
        public void Set_QueuePollInterval_ShouldThrowAnException_WhenGivenIntervalIsNegative()
        {
            var storageOptions = new SQLiteStorageOptions();
            Assert.Throws<ArgumentException>(
                () => storageOptions.QueuePollInterval = TimeSpan.FromSeconds(-1));
        }

        [Fact]
        public void Set_QueuePollInterval_SetsTheValue()
        {
            var storageOptions = new SQLiteStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(1)
            };
            Assert.Equal(TimeSpan.FromSeconds(1), storageOptions.QueuePollInterval);
        }
    }
}

using Hangfire.Storage.SQLite.Entities;
using System;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public partial class HangfireSQLiteConnectionFacts
    {
        [Fact]
        [Trait("Feature", "ExtendedApi")]
        public void GetSetCount_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection((database, connection) =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetSetCount(null));
            });
        }

        [Fact]
        [Trait("Feature", "ExtendedApi")]
        public void GetSetCount_ReturnsZero_WhenSetDoesNotExist()
        {
            UseConnection((database, connection) =>
            {
                var result = connection.GetSetCount("my-set");
                Assert.Equal(0, result);
            });
        }

        [Fact]
        [Trait("Feature", "ExtendedApi")]
        public void GetSetCount_ReturnsNumberOfElements_InASet()
        {
            UseConnection((database, connection) =>
            {
                database.Database.Insert(new Set
                {
                    Key = "set-1",
                    Value = "value-1"
                });
                database.Database.Insert(new Set
                {
                    Key = "set-2",
                    Value = "value-1"
                });
                database.Database.Insert(new Set
                {
                    Key = "set-1",
                    Value = "value-2"
                });

                var result = connection.GetSetCount("set-1");

                Assert.Equal(2, result);
            });
        }
        
        [Fact]
        [Trait("Feature", "Connection.GetSetCount.Limited")]
        public void Limited_GetSetCount_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection((database, connection) =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => connection.GetSetCount(null));
            });
        }

        [Fact]
        [Trait("Feature", "Connection.GetSetCount.Limited")]
        public void Limited_GetSetCount_ReturnsZero_WhenSetDoesNotExist()
        {
            UseConnection((database, connection) =>
            {
                var result = connection.GetSetCount(new []{"my-set"}, 1);
                Assert.Equal(0, result);
            });
        }

        [Fact]
        [Trait("Feature", "Connection.GetSetCount.Limited")]
        public void Limited_GetSetCount_Returns_UpToLimit_NumberOfElements_InASet()
        {
            UseConnection((database, connection) =>
            {
                database.Database.Insert(new Set
                {
                    Key = "set-1",
                    Value = "value-1"
                });
                database.Database.Insert(new Set
                {
                    Key = "set-2",
                    Value = "value-1"
                });
                database.Database.Insert(new Set
                {
                    Key = "set-1",
                    Value = "value-2"
                });

                var result = connection.GetSetCount(new []{"set-1", "set-2"}, 2);

                Assert.Equal(2, result);
            });
        }
        
        [Fact]
        [Trait("Feature", "Connection.GetSetCount.Limited")]
        public void Limited_GetSetCount_Returns_AllCounts_IfLimitHighEnough()
        {
            UseConnection((database, connection) =>
            {
                database.Database.Insert(new Set
                {
                    Key = "set-1",
                    Value = "value-1"
                });
                database.Database.Insert(new Set
                {
                    Key = "set-2",
                    Value = "value-1"
                });
                database.Database.Insert(new Set
                {
                    Key = "set-1",
                    Value = "value-2"
                });

                var result = connection.GetSetCount(new []{"set-1", "set-2"}, 99999);

                Assert.Equal(3, result);
            });
        }
    }
}

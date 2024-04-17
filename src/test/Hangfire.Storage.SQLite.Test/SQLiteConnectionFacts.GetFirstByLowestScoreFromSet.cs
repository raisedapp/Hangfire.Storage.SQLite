using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage.SQLite.Entities;
using Hangfire.Storage.SQLite.Test.Utils;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace Hangfire.Storage.SQLite.Test
{
    public partial class HangfireSQLiteConnectionFacts
    {
        [Fact]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection((database, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetFirstByLowestScoreFromSet(null, 0, 1));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ThrowsAnException_ToScoreIsLowerThanFromScore()
        {
            UseConnection((database, connection) => Assert.Throws<ArgumentException>(
                () => connection.GetFirstByLowestScoreFromSet("key", 0, -1)));
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ReturnsNull_WhenTheKeyDoesNotExist()
        {
            UseConnection((database, connection) =>
            {
                var result = connection.GetFirstByLowestScoreFromSet(
                    "key", 0, 1);

                Assert.Null(result);
            });
        }

        [Fact]
        public void GetFirstByLowestScoreFromSet_ReturnsTheValueWithTheLowestScore()
        {
            UseConnection((database, connection) =>
            {
                database.Database.Insert(new Set
                {
                    Key = "key",
                    Score = 1.0m,
                    Value = "1.0"
                });
                database.Database.Insert(new Set
                {
                    Key = "key",
                    Score = -1.0m,
                    Value = "-1.0"
                });
                database.Database.Insert(new Set
                {
                    Key = "key",
                    Score = -5.0m,
                    Value = "-5.0"
                });
                database.Database.Insert(new Set
                {
                    Key = "another-key",
                    Score = -2.0m,
                    Value = "-2.0"
                });

                var result = connection.GetFirstByLowestScoreFromSet("key", -1.0, 3.0);

                Assert.Equal("-1.0", result);
            });
        }
        
        [Fact]
        [Trait("Feature", "BatchedGetFirstByLowestScoreFromSet")]
        public void Batch_GetFirstByLowestScoreFromSet_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection((database, connection) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => connection.GetFirstByLowestScoreFromSet(null, 0, 1, 1));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact]
        [Trait("Feature", "BatchedGetFirstByLowestScoreFromSet")]
        public void Batch_GetFirstByLowestScoreFromSet_ThrowsAnException_ToScoreIsLowerThanFromScore()
        {
            UseConnection((database, connection) => Assert.Throws<ArgumentException>(
                () => connection.GetFirstByLowestScoreFromSet("key", 0, -1, 1)));
        }

        [Fact]
        [Trait("Feature", "BatchedGetFirstByLowestScoreFromSet")]
        public void Batch_GetFirstByLowestScoreFromSet_ReturnsNull_WhenTheKeyDoesNotExist()
        {
            UseConnection((database, connection) =>
            {
                var result = connection.GetFirstByLowestScoreFromSet(
                    "key", 0, 1, 1);

                Assert.Empty(result);
            });
        }
        
        [Fact]
        [Trait("Feature", "BatchedGetFirstByLowestScoreFromSet")]
        public void Batch_GetFirstByLowestScoreFromSet_ReturnsTheValuesWithTheLowestScore()
        {
            UseConnection((database, connection) =>
            {
                database.Database.InsertAll(new []
                {
                    new Set
                    {
                        Key = "key",
                        Score = 1.0m,
                        Value = "1.0"
                    },
                    new Set
                    {
                        Key = "key",
                        Score = -1.0m,
                        Value = "-1.0"
                    },
                    new Set
                    {
                        Key = "key",
                        Score = -4.0m,
                        Value = "-4.0"
                    },
                    new Set
                    {
                        Key = "key",
                        Score = -5.0m,
                        Value = "-5.0"
                    },
                    new Set
                    {
                        Key = "another-key",
                        Score = -2.0m,
                        Value = "-2.0"
                    }
                }, typeof(Set));

                var result = connection.GetFirstByLowestScoreFromSet("key", -5.0, 3.0, count: 3);

                Assert.Equal(new []{ "-5.0", "-4.0", "-1.0" }, result);
            });
        }
    }
}

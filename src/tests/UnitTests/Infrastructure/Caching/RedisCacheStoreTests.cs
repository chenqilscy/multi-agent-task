using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Exceptions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CKY.MultiAgentFramework.UnitTests.Infrastructure.Caching
{
    /// <summary>
    /// RedisCacheStore 单元测试
    /// </summary>
    public class RedisCacheStoreTests : IDisposable
    {
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly RedisCacheStore _cacheStore;
        private readonly RedisCacheStoreOptions _options;

        public RedisCacheStoreTests()
        {
            _mockRedis = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _mockDatabase = new Mock<IDatabase>(MockBehavior.Strict);
            _options = new RedisCacheStoreOptions
            {
                EnableVerboseLogging = true
            };

            _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_mockDatabase.Object);

            _cacheStore = new RedisCacheStore(
                _mockRedis.Object,
                Mock.Of<ILogger<RedisCacheStore>>(),
                Options.Create(_options));
        }

        public void Dispose()
        {
            _mockRedis.VerifyAll();
            _mockDatabase.VerifyAll();
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDeserializedValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var expected = new TestModel { Name = "Test", Value = 123 };
            var json = System.Text.Json.JsonSerializer.Serialize(expected, _options.JsonSerializerOptions);

            _mockDatabase.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(json);

            // Act
            var result = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(123, result.Value);
            _mockDatabase.Verify(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            _mockDatabase.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            // Act
            var result = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.Null(result);
            _mockDatabase.Verify(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task SetAsync_ShouldSerializeAndStore()
        {
            // Arrange
            var key = "test-key";
            var value = new TestModel { Name = "Test", Value = 123 };

            _mockDatabase.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            await _cacheStore.SetAsync(key, value);

            // Assert
            _mockDatabase.Verify(x => x.StringSetAsync(
                key,
                It.Is<RedisValue>(v => v.ToString().Contains("Test")),
                null,
                When.Always,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task SetAsync_WithExpiry_ShouldStoreWithTTL()
        {
            // Arrange
            var key = "test-key";
            var value = new TestModel { Name = "Test", Value = 123 };
            var expiry = TimeSpan.FromMinutes(5);

            _mockDatabase.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            await _cacheStore.SetAsync(key, value, expiry);

            // Assert
            _mockDatabase.Verify(x => x.StringSetAsync(
                key,
                It.IsAny<RedisValue>(),
                expiry,
                When.Always,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveKey()
        {
            // Arrange
            var key = "test-key";

            _mockDatabase.Setup(x => x.KeyDeleteAsync(
                key,
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            await _cacheStore.DeleteAsync(key);

            // Assert
            _mockDatabase.Verify(x => x.KeyDeleteAsync(
                key,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnMultipleValues()
        {
            // Arrange
            var keys = new[] { "key1", "key2", "key3" };
            var values = new[]
            {
                new TestModel { Name = "Test1", Value = 1 },
                new TestModel { Name = "Test2", Value = 2 },
                new TestModel { Name = "Test3", Value = 3 }
            };

            var redisValues = values.Select(v =>
                (RedisValue)System.Text.Json.JsonSerializer.Serialize(v, _options.JsonSerializerOptions))
                .ToArray();

            _mockDatabase.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey[]>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(redisValues);

            // Act
            var result = await _cacheStore.GetBatchAsync<TestModel>(keys);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Test1", result["key1"]?.Name);
            Assert.Equal("Test2", result["key2"]?.Name);
            Assert.Equal("Test3", result["key3"]?.Name);
            _mockDatabase.Verify(x => x.StringGetAsync(
                It.IsAny<RedisKey[]>(),
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";

            _mockDatabase.Setup(x => x.KeyExistsAsync(
                key,
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            var exists = await _cacheStore.ExistsAsync(key);

            // Assert
            Assert.True(exists);
            _mockDatabase.Verify(x => x.KeyExistsAsync(
                key,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            _mockDatabase.Setup(x => x.KeyExistsAsync(
                key,
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            // Act
            var exists = await _cacheStore.ExistsAsync(key);

            // Assert
            Assert.False(exists);
            _mockDatabase.Verify(x => x.KeyExistsAsync(
                key,
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnNull_WhenExceptionOccurs()
        {
            // Arrange
            var key = "test-key";

            _mockDatabase.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisException("Connection failed"));

            // Act
            var result = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.Null(result); // 降级：返回 null 而非抛出异常
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRedisIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RedisCacheStore(
                    null!,
                    Mock.Of<ILogger<RedisCacheStore>>(),
                    Options.Create(_options)));
        }

        private class TestModel
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}

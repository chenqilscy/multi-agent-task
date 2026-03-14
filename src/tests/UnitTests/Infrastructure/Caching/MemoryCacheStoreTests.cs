using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.UnitTests.Infrastructure.Caching
{
    /// <summary>
    /// MemoryCacheStore 单元测试
    /// </summary>
    public class MemoryCacheStoreTests : IDisposable
    {
        private readonly MemoryCache _cache;
        private readonly MemoryCacheStore _cacheStore;

        public MemoryCacheStoreTests()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _cacheStore = new MemoryCacheStore(_cache, Mock.Of<ILogger<MemoryCacheStore>>());
        }

        public void Dispose()
        {
            _cache.Dispose();
        }

        [Fact]
        public async Task GetAsync_ShouldReturnValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var expected = new TestModel { Name = "Test", Value = 123 };

            await _cacheStore.SetAsync(key, expected);

            // Act
            var result = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(123, result.Value);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var result = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAsync_ShouldStoreValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestModel { Name = "Test", Value = 123 };

            // Act
            await _cacheStore.SetAsync(key, value);

            // Assert
            var result = await _cacheStore.GetAsync<TestModel>(key);
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(123, result.Value);
        }

        [Fact]
        public async Task SetAsync_WithExpiry_ShouldExpireAfterTime()
        {
            // Arrange
            var key = "test-key";
            var value = new TestModel { Name = "Test", Value = 123 };
            var expiry = TimeSpan.FromMilliseconds(100);

            // Act
            await _cacheStore.SetAsync(key, value, expiry);
            var result1 = await _cacheStore.GetAsync<TestModel>(key);
            await Task.Delay(150); // 等待过期
            var result2 = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.NotNull(result1); // 过期前存在
            Assert.Null(result2);    // 过期后不存在
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveKey()
        {
            // Arrange
            var key = "test-key";
            var value = new TestModel { Name = "Test", Value = 123 };
            await _cacheStore.SetAsync(key, value);

            // Act
            await _cacheStore.DeleteAsync(key);
            var result = await _cacheStore.GetAsync<TestModel>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnMultipleValues()
        {
            // Arrange
            var data = new Dictionary<string, TestModel>
            {
                ["key1"] = new() { Name = "Test1", Value = 1 },
                ["key2"] = new() { Name = "Test2", Value = 2 },
                ["key3"] = new() { Name = "Test3", Value = 3 }
            };

            foreach (var item in data)
            {
                await _cacheStore.SetAsync(item.Key, item.Value);
            }

            // Act
            var result = await _cacheStore.GetBatchAsync<TestModel>(data.Keys);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Test1", result["key1"]?.Name);
            Assert.Equal("Test2", result["key2"]?.Name);
            Assert.Equal("Test3", result["key3"]?.Name);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            await _cacheStore.SetAsync(key, new TestModel());

            // Act
            var exists = await _cacheStore.ExistsAsync(key);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var exists = await _cacheStore.ExistsAsync(key);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenCacheIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MemoryCacheStore(null!, Mock.Of<ILogger<MemoryCacheStore>>()));
        }

        private class TestModel
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}

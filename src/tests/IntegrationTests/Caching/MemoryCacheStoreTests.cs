using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Caching
{
    /// <summary>
    /// MemoryCacheStore 集成测试
    /// 验证内存缓存的完整 CRUD 和过期功能
    /// </summary>
    public class MemoryCacheStoreTests : IDisposable
    {
        private readonly MemoryCache _memoryCache;
        private readonly ICacheStore _cacheStore;

        public MemoryCacheStoreTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cacheStore = new MemoryCacheStore(_memoryCache, NullLogger<MemoryCacheStore>.Instance);
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        [Fact]
        public async Task SetAsync_ThenGetAsync_ShouldReturnValue()
        {
            var value = new TestData { Name = "Test", Value = 42 };
            await _cacheStore.SetAsync("key1", value);

            var result = await _cacheStore.GetAsync<TestData>("key1");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Test");
            result.Value.Should().Be(42);
        }

        [Fact]
        public async Task GetAsync_NonExistentKey_ShouldReturnNull()
        {
            var result = await _cacheStore.GetAsync<TestData>("nonexistent");
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveKey()
        {
            await _cacheStore.SetAsync("delete-me", new TestData { Name = "Gone" });
            await _cacheStore.DeleteAsync("delete-me");

            var result = await _cacheStore.GetAsync<TestData>("delete-me");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnCorrectStatus()
        {
            await _cacheStore.SetAsync("exists-key", new TestData { Name = "Here" });

            var exists = await _cacheStore.ExistsAsync("exists-key");
            var notExists = await _cacheStore.ExistsAsync("missing-key");

            exists.Should().BeTrue();
            notExists.Should().BeFalse();
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnMultipleValues()
        {
            await _cacheStore.SetAsync("batch:1", new TestData { Name = "A" });
            await _cacheStore.SetAsync("batch:2", new TestData { Name = "B" });

            var results = await _cacheStore.GetBatchAsync<TestData>(["batch:1", "batch:2", "batch:missing"]);

            results.Should().HaveCount(3);
            results["batch:1"]!.Name.Should().Be("A");
            results["batch:2"]!.Name.Should().Be("B");
            results["batch:missing"].Should().BeNull();
        }

        [Fact]
        public async Task SetAsync_WithExpiry_ShouldExpire()
        {
            await _cacheStore.SetAsync("expiry-key", new TestData { Name = "Temp" }, TimeSpan.FromMilliseconds(200));

            var immediate = await _cacheStore.GetAsync<TestData>("expiry-key");
            immediate.Should().NotBeNull();

            await Task.Delay(500);

            var expired = await _cacheStore.GetAsync<TestData>("expiry-key");
            expired.Should().BeNull("cache entry should have expired");
        }

        [Fact]
        public async Task SetAsync_OverwriteExistingKey_ShouldUpdateValue()
        {
            await _cacheStore.SetAsync("overwrite", new TestData { Name = "Old" });
            await _cacheStore.SetAsync("overwrite", new TestData { Name = "New" });

            var result = await _cacheStore.GetAsync<TestData>("overwrite");
            result!.Name.Should().Be("New");
        }

        private class TestData
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}

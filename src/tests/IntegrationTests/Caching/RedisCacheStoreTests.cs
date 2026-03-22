using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;
using FluentAssertions;

namespace CKY.MultiAgentFramework.IntegrationTests.Caching
{
    /// <summary>
    /// Redis 缓存存储集成测试
    /// 使用 Testcontainers 启动真实的 Redis 实例
    /// </summary>
    /// <remarks>
    /// 前置条件：
    /// 1. 需要安装 Docker
    /// 2. Docker 服务正在运行
    /// </remarks>
    public class RedisCacheStoreTests : IAsyncLifetime
    {
        private readonly RedisContainer _redisContainer;
        private IConnectionMultiplexer? _connection;
        private ICacheStore? _cacheStore;

        public RedisCacheStoreTests()
        {
            // 配置 Redis Testcontainer
            _redisContainer = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .WithPortBinding(6379, true)
                .Build();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // 启动 Redis 容器
                await _redisContainer.StartAsync();

                // 创建 Redis 连接
                var connectionString = _redisContainer.GetConnectionString();
                _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);

                // 创建 RedisCacheStore 实例
                var options = Options.Create(new RedisCacheStoreOptions
                {
                    DatabaseId = 0
                });
                _cacheStore = new RedisCacheStore(
                    _connection,
                    NullLogger<RedisCacheStore>.Instance,
                    options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "无法启动 Redis Testcontainer。请确保 Docker 正在运行。", ex);
            }
        }

        public async Task DisposeAsync()
        {
            // 清理连接和容器
            await _connection?.CloseAsync()!;
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }

        [Fact]
        public async Task SetAsync_ThenGetAsync_ShouldReturnCachedValue()
        {
            // Arrange
            var key = "test:user:123";
            var value = new TestUser
            {
                Id = "123",
                Name = "Test User",
                Email = "test@example.com"
            };

            // Act
            await _cacheStore!.SetAsync(key, value, TimeSpan.FromMinutes(5));
            var result = await _cacheStore.GetAsync<TestUser>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(value.Id);
            result.Name.Should().Be(value.Name);
            result.Email.Should().Be(value.Email);
        }

        [Fact]
        public async Task SetAsync_WithExpiry_ShouldExpireAfterTime()
        {
            // Arrange
            var key = "test:expiry";
            var value = new TestUser { Id = "1", Name = "Expiry Test" };

            // Act
            await _cacheStore!.SetAsync(key, value, TimeSpan.FromSeconds(2));
            var immediateResult = await _cacheStore.GetAsync<TestUser>(key);

            // Wait for expiry
            await Task.Delay(3000);
            var expiredResult = await _cacheStore.GetAsync<TestUser>(key);

            // Assert
            immediateResult.Should().NotBeNull();
            expiredResult.Should().BeNull("缓存应该已过期");
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnMultipleValues()
        {
            // Arrange
            var data = new Dictionary<string, TestUser>
            {
                ["test:batch:1"] = new TestUser { Id = "1", Name = "User 1" },
                ["test:batch:2"] = new TestUser { Id = "2", Name = "User 2" },
                ["test:batch:3"] = new TestUser { Id = "3", Name = "User 3" }
            };

            // Act
            foreach (var kvp in data)
            {
                await _cacheStore!.SetAsync(kvp.Key, kvp.Value);
            }

            var results = await _cacheStore!.GetBatchAsync<TestUser>(data.Keys);

            // Assert
            results.Should().HaveCount(3);
            results["test:batch:1"]!.Name.Should().Be("User 1");
            results["test:batch:2"]!.Name.Should().Be("User 2");
            results["test:batch:3"]!.Name.Should().Be("User 3");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveKey()
        {
            // Arrange
            var key = "test:delete";
            var value = new TestUser { Id = "1", Name = "Delete Me" };
            await _cacheStore!.SetAsync(key, value);

            // Act
            await _cacheStore.DeleteAsync(key);
            var result = await _cacheStore.GetAsync<TestUser>(key);

            // Assert
            result.Should().BeNull("键应该已被删除");
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnCorrectStatus()
        {
            // Arrange
            var key = "test:exists";
            var value = new TestUser { Id = "1", Name = "Exists Test" };

            // Act
            var existsBefore = await _cacheStore!.ExistsAsync(key);
            await _cacheStore.SetAsync(key, value);
            var existsAfter = await _cacheStore.ExistsAsync(key);

            // Assert
            existsBefore.Should().BeFalse();
            existsAfter.Should().BeTrue();
        }

        [Fact]
        public async Task SetAsync_ThenGetAsync_WithComplexObject_ShouldWork()
        {
            // Arrange
            var key = "test:complex";
            var value = new ComplexObject
            {
                Id = "123",
                Name = "Complex Object",
                Tags = new[] { "tag1", "tag2", "tag3" },
                Metadata = new Dictionary<string, object>
                {
                    ["key1"] = "value1",
                    ["key2"] = 42,
                    ["key3"] = true
                },
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await _cacheStore!.SetAsync(key, value, TimeSpan.FromMinutes(10));
            var result = await _cacheStore.GetAsync<ComplexObject>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Tags.Should().BeEquivalentTo(value.Tags);
            // JSON 反序列化 Dictionary<string, object> 后值类型变为 JsonElement
            result.Metadata.Should().ContainKey("key1");
            result.Metadata["key1"].ToString().Should().Be("value1");
            result.Metadata["key2"].ToString().Should().Be("42");
            result.Metadata["key3"].ToString().Should().Be("True");
            result.CreatedAt.Should().BeCloseTo(value.CreatedAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetAsync_WithNonExistentKey_ShouldReturnNull()
        {
            // Act
            var result = await _cacheStore!.GetAsync<TestUser>("non:existent:key");

            // Assert
            result.Should().BeNull();
        }

        // 测试辅助类
        private class TestUser
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        private class ComplexObject
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string[] Tags { get; set; } = Array.Empty<string>();
            public Dictionary<string, object> Metadata { get; set; } = new();
            public DateTime CreatedAt { get; set; }
        }
    }
}

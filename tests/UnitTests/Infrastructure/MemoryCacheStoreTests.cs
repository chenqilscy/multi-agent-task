using CKY.MultiAgentFramework.Infrastructure.Caching;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Infrastructure
{
    public class MemoryCacheStoreTests
    {
        private readonly MemoryCacheStore _sut;

        public MemoryCacheStoreTests()
        {
            _sut = new MemoryCacheStore();
        }

        [Fact]
        public async Task SetAsync_ThenGetAsync_ShouldReturnValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestEntity { Id = "1", Name = "测试" };

            // Act
            await _sut.SetAsync(key, value);
            var result = await _sut.GetAsync<TestEntity>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be("1");
            result.Name.Should().Be("测试");
        }

        [Fact]
        public async Task GetAsync_WhenKeyNotExists_ShouldReturnNull()
        {
            // Act
            var result = await _sut.GetAsync<TestEntity>("non-existent-key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveValue()
        {
            // Arrange
            var key = "delete-test";
            await _sut.SetAsync(key, new TestEntity { Id = "1" });

            // Act
            await _sut.DeleteAsync(key);
            var result = await _sut.GetAsync<TestEntity>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
        {
            // Arrange
            var key = "exists-test";
            await _sut.SetAsync(key, new TestEntity { Id = "1" });

            // Act
            var exists = await _sut.ExistsAsync(key);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyExpired_ShouldReturnFalse()
        {
            // Arrange
            var key = "expiry-test";
            await _sut.SetAsync(key, new TestEntity { Id = "1" }, TimeSpan.FromMilliseconds(1));

            // Wait for expiry
            await Task.Delay(50);

            // Act
            var exists = await _sut.ExistsAsync(key);

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task GetBatchAsync_ShouldReturnAllKeys()
        {
            // Arrange
            await _sut.SetAsync("k1", new TestEntity { Id = "1" });
            await _sut.SetAsync("k2", new TestEntity { Id = "2" });

            // Act
            var results = await _sut.GetBatchAsync<TestEntity>(["k1", "k2", "k3"]);

            // Assert
            results.Should().HaveCount(3);
            results["k1"].Should().NotBeNull();
            results["k2"].Should().NotBeNull();
            results["k3"].Should().BeNull();
        }

        private class TestEntity
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}

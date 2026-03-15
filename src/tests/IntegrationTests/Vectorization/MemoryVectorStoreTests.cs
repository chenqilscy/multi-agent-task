using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Vectorization
{
    /// <summary>
    /// MemoryVectorStore 集成测试
    /// 验证向量存储的 CRUD 和语义搜索功能
    /// </summary>
    public class MemoryVectorStoreTests
    {
        private readonly IVectorStore _vectorStore;

        public MemoryVectorStoreTests()
        {
            _vectorStore = new MemoryVectorStore(NullLogger<MemoryVectorStore>.Instance);
        }

        [Fact]
        public async Task CreateCollection_ThenInsert_ThenSearch_ShouldReturnResults()
        {
            // Arrange
            var collection = "test-collection-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 3);

            var points = new List<VectorPoint>
            {
                new() { Id = "1", Vector = [1.0f, 0.0f, 0.0f], Metadata = new() { ["label"] = "x-axis" } },
                new() { Id = "2", Vector = [0.0f, 1.0f, 0.0f], Metadata = new() { ["label"] = "y-axis" } },
                new() { Id = "3", Vector = [0.0f, 0.0f, 1.0f], Metadata = new() { ["label"] = "z-axis" } },
            };
            await _vectorStore.InsertAsync(collection, points);

            // Act - search for vector close to x-axis
            var results = await _vectorStore.SearchAsync(collection, [0.9f, 0.1f, 0.0f], topK: 2);

            // Assert
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("1", "x-axis vector should be most similar");
            results[0].Score.Should().BeGreaterThan(0.9f);
        }

        [Fact]
        public async Task SearchAsync_WithTopK_ShouldLimitResults()
        {
            // Arrange
            var collection = "topk-test-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 2);

            var points = Enumerable.Range(1, 10).Select(i => new VectorPoint
            {
                Id = i.ToString(),
                Vector = [i * 0.1f, 1.0f - i * 0.1f],
            }).ToList();
            await _vectorStore.InsertAsync(collection, points);

            // Act
            var results = await _vectorStore.SearchAsync(collection, [0.5f, 0.5f], topK: 3);

            // Assert
            results.Should().HaveCount(3);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemovePoints()
        {
            // Arrange
            var collection = "delete-test-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 2);

            var points = new List<VectorPoint>
            {
                new() { Id = "a", Vector = [1.0f, 0.0f] },
                new() { Id = "b", Vector = [0.0f, 1.0f] },
                new() { Id = "c", Vector = [0.5f, 0.5f] },
            };
            await _vectorStore.InsertAsync(collection, points);

            // Act
            await _vectorStore.DeleteAsync(collection, ["a", "b"]);
            var results = await _vectorStore.SearchAsync(collection, [1.0f, 0.0f], topK: 10);

            // Assert
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("c");
        }

        [Fact]
        public async Task DeleteCollectionAsync_ShouldRemoveEntireCollection()
        {
            // Arrange
            var collection = "delete-col-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 2);
            await _vectorStore.InsertAsync(collection, [new VectorPoint { Id = "1", Vector = [1f, 0f] }]);

            // Act
            await _vectorStore.DeleteCollectionAsync(collection);

            // Assert - inserting into deleted collection should throw
            var act = () => _vectorStore.InsertAsync(collection, [new VectorPoint { Id = "2", Vector = [0f, 1f] }]);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task InsertAsync_IntoNonExistentCollection_ShouldThrow()
        {
            var act = () => _vectorStore.InsertAsync("nonexistent", [new VectorPoint { Id = "1", Vector = [1f] }]);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task SearchAsync_InNonExistentCollection_ShouldThrow()
        {
            var act = () => _vectorStore.SearchAsync("nonexistent", [1f], topK: 5);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task CreateCollectionAsync_DuplicateName_ShouldNotThrow()
        {
            // Arrange
            var collection = "dup-test-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 3);

            // Act - creating again should be idempotent
            var act = () => _vectorStore.CreateCollectionAsync(collection, 3);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnMetadata()
        {
            // Arrange
            var collection = "meta-test-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 2);

            var points = new List<VectorPoint>
            {
                new()
                {
                    Id = "doc1",
                    Vector = [1.0f, 0.0f],
                    Metadata = new() { ["title"] = "Document 1", ["category"] = "tech" }
                }
            };
            await _vectorStore.InsertAsync(collection, points);

            // Act
            var results = await _vectorStore.SearchAsync(collection, [1.0f, 0.0f], topK: 1);

            // Assert
            results.Should().HaveCount(1);
            results[0].Metadata["title"].Should().Be("Document 1");
            results[0].Metadata["category"].Should().Be("tech");
        }

        [Fact]
        public async Task SearchAsync_CosineSimilarity_ShouldRankCorrectly()
        {
            // Arrange - vectors at different angles
            var collection = "cosine-test-" + Guid.NewGuid().ToString("N")[..8];
            await _vectorStore.CreateCollectionAsync(collection, 2);

            await _vectorStore.InsertAsync(collection, new List<VectorPoint>
            {
                new() { Id = "same", Vector = [1.0f, 0.0f] },       // 0° from query
                new() { Id = "close", Vector = [0.9f, 0.1f] },      // small angle
                new() { Id = "orthogonal", Vector = [0.0f, 1.0f] }, // 90° from query
            });

            // Act
            var results = await _vectorStore.SearchAsync(collection, [1.0f, 0.0f], topK: 3);

            // Assert - should be ordered by decreasing similarity
            results[0].Id.Should().Be("same");
            results[1].Id.Should().Be("close");
            results[2].Id.Should().Be("orthogonal");
            results[0].Score.Should().BeGreaterThan(results[1].Score);
            results[1].Score.Should().BeGreaterThan(results[2].Score);
        }
    }
}

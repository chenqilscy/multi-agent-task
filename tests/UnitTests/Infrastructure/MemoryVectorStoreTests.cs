using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Vectorization;
using FluentAssertions;

namespace CKY.MultiAgentFramework.Tests.Infrastructure
{
    public class MemoryVectorStoreTests
    {
        private readonly MemoryVectorStore _sut;
        private const string TestCollection = "test_collection";

        public MemoryVectorStoreTests()
        {
            _sut = new MemoryVectorStore();
        }

        [Fact]
        public async Task CreateCollectionAsync_ShouldSucceed()
        {
            // Act & Assert
            await _sut.Invoking(s => s.CreateCollectionAsync(TestCollection, 128))
                .Should().NotThrowAsync();
        }

        [Fact]
        public async Task InsertAsync_ThenSearchAsync_ShouldFindSimilarVectors()
        {
            // Arrange
            await _sut.CreateCollectionAsync(TestCollection, 4);
            var vector = new float[] { 1, 0, 0, 0 };

            var point = new VectorPoint
            {
                Id = "point1",
                Vector = vector,
                Metadata = new Dictionary<string, object> { ["label"] = "test" }
            };

            // Act
            await _sut.InsertAsync(TestCollection, [point]);
            var results = await _sut.SearchAsync(TestCollection, vector, topK: 5);

            // Assert
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("point1");
            results[0].Score.Should().BeApproximately(1.0f, 0.001f);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveVector()
        {
            // Arrange
            await _sut.CreateCollectionAsync(TestCollection, 4);
            var point = new VectorPoint
            {
                Id = "delete-point",
                Vector = new float[] { 1, 0, 0, 0 }
            };
            await _sut.InsertAsync(TestCollection, [point]);

            // Act
            await _sut.DeleteAsync(TestCollection, ["delete-point"]);
            var results = await _sut.SearchAsync(TestCollection, new float[] { 1, 0, 0, 0 });

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteCollectionAsync_ShouldRemoveAllVectors()
        {
            // Arrange
            await _sut.CreateCollectionAsync("temp_collection", 4);
            await _sut.InsertAsync("temp_collection", [new VectorPoint { Id = "p1", Vector = new float[] { 1, 0, 0, 0 } }]);

            // Act
            await _sut.DeleteCollectionAsync("temp_collection");
            var results = await _sut.SearchAsync("temp_collection", new float[] { 1, 0, 0, 0 });

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_WithFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            await _sut.CreateCollectionAsync(TestCollection, 4);
            var vector = new float[] { 1, 0, 0, 0 };

            await _sut.InsertAsync(TestCollection,
            [
                new VectorPoint { Id = "p1", Vector = vector, Metadata = new Dictionary<string, object> { ["userId"] = "user1" } },
                new VectorPoint { Id = "p2", Vector = vector, Metadata = new Dictionary<string, object> { ["userId"] = "user2" } }
            ]);

            // Act
            var results = await _sut.SearchAsync(
                TestCollection,
                vector,
                filter: new Dictionary<string, object> { ["userId"] = "user1" });

            // Assert
            results.Should().HaveCount(1);
            results[0].Id.Should().Be("p1");
        }
    }
}

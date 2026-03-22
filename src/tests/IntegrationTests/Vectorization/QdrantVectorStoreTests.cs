using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Qdrant;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Xunit;
using Xunit.Abstractions;

namespace CKY.MultiAgentFramework.IntegrationTests.Vectorization;

/// <summary>
/// QdrantVectorStore 集成测试
/// 注意：需要运行 Qdrant 实例（docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant:v1.9.0）
/// 如果 Qdrant 未运行，测试会自动跳过
/// </summary>
public class QdrantVectorStoreTests : IAsyncLifetime
{
    private QdrantVectorStore? _vectorStore;
    private QdrantClient? _client;
    private bool _qdrantAvailable;
    private readonly string _testPrefix = $"test-{Guid.NewGuid():N}"[..16];
    private readonly ITestOutputHelper _output;

    public QdrantVectorStoreTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _client = new QdrantClient("localhost", 6334);
            await _client.ListCollectionsAsync();
            _qdrantAvailable = true;

            var options = Options.Create(new QdrantVectorStoreOptions
            {
                Host = "localhost",
                Port = 6334,
                DistanceMetric = "Cosine"
            });

            _vectorStore = new QdrantVectorStore(
                _client,
                NullLogger<QdrantVectorStore>.Instance,
                options);
        }
        catch
        {
            _qdrantAvailable = false;
            _output.WriteLine("Qdrant 未运行，跳过集成测试");
        }
    }

    public async Task DisposeAsync()
    {
        if (_qdrantAvailable && _client != null)
        {
            try
            {
                var collections = await _client.ListCollectionsAsync();
                foreach (var col in collections)
                {
                    if (col.StartsWith(_testPrefix))
                        await _client.DeleteCollectionAsync(col);
                }
            }
            catch { /* 忽略清理错误 */ }
        }

        _client?.Dispose();
    }

    private bool SkipIfQdrantUnavailable()
    {
        if (!_qdrantAvailable)
        {
            _output.WriteLine("⚠️ Qdrant 未运行，跳过此测试。启动方式：docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant:v1.9.0");
            return true;
        }
        return false;
    }

    [Fact]
    public async Task CreateCollection_InsertAndSearch_ShouldReturnResults()
    {
        if (SkipIfQdrantUnavailable()) return;

        var collection = $"{_testPrefix}-crud";
        await _vectorStore!.CreateCollectionAsync(collection, 3);

        var points = new List<VectorPoint>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Vector = [1.0f, 0.0f, 0.0f],
                Metadata = new() { ["label"] = "x-axis" }
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Vector = [0.0f, 1.0f, 0.0f],
                Metadata = new() { ["label"] = "y-axis" }
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Vector = [0.0f, 0.0f, 1.0f],
                Metadata = new() { ["label"] = "z-axis" }
            },
        };
        await _vectorStore.InsertAsync(collection, points);

        var results = await _vectorStore.SearchAsync(collection, [0.9f, 0.1f, 0.0f], topK: 2);

        results.Should().HaveCount(2);
        results[0].Score.Should().BeGreaterThan(0.8f);
        results[0].Metadata.Should().ContainKey("label");
    }

    [Fact]
    public async Task SearchAsync_WithTopK_ShouldLimitResults()
    {
        if (SkipIfQdrantUnavailable()) return;

        var collection = $"{_testPrefix}-topk";
        await _vectorStore!.CreateCollectionAsync(collection, 4);

        var points = Enumerable.Range(1, 10).Select(i =>
        {
            var v = new float[4];
            v[i % 4] = 1.0f;
            return new VectorPoint
            {
                Id = Guid.NewGuid().ToString(),
                Vector = v,
            };
        }).ToList();

        await _vectorStore.InsertAsync(collection, points);

        var results = await _vectorStore.SearchAsync(collection, [1f, 0f, 0f, 0f], topK: 3);

        results.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePoints()
    {
        if (SkipIfQdrantUnavailable()) return;

        var collection = $"{_testPrefix}-del";
        await _vectorStore!.CreateCollectionAsync(collection, 2);

        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        var id3 = Guid.NewGuid().ToString();

        await _vectorStore.InsertAsync(collection, new List<VectorPoint>
        {
            new() { Id = id1, Vector = [1.0f, 0.0f] },
            new() { Id = id2, Vector = [0.0f, 1.0f] },
            new() { Id = id3, Vector = [0.5f, 0.5f] },
        });

        await _vectorStore.DeleteAsync(collection, [id1, id2]);

        var results = await _vectorStore.SearchAsync(collection, [1.0f, 0.0f], topK: 10);
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(id3);
    }

    [Fact]
    public async Task DeleteCollectionAsync_ShouldRemoveCollection()
    {
        if (SkipIfQdrantUnavailable()) return;

        var collection = $"{_testPrefix}-delcol";
        await _vectorStore!.CreateCollectionAsync(collection, 2);
        await _vectorStore.InsertAsync(collection, [
            new VectorPoint { Id = Guid.NewGuid().ToString(), Vector = [1f, 0f] }
        ]);

        await _vectorStore.DeleteCollectionAsync(collection);

        var act = async () => await _vectorStore.SearchAsync(collection, [1f, 0f]);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task InsertAsync_WithMetadata_ShouldPreservePayload()
    {
        if (SkipIfQdrantUnavailable()) return;

        var collection = $"{_testPrefix}-meta";
        await _vectorStore!.CreateCollectionAsync(collection, 3);

        var pointId = Guid.NewGuid().ToString();
        await _vectorStore.InsertAsync(collection, [
            new VectorPoint
            {
                Id = pointId,
                Vector = [1.0f, 0.0f, 0.0f],
                Metadata = new()
                {
                    ["content"] = "智能家居灯光控制教程",
                    ["document_id"] = "doc-123",
                    ["chunk_index"] = 0
                }
            }
        ]);

        var results = await _vectorStore.SearchAsync(collection, [1.0f, 0.0f, 0.0f], topK: 1);

        results.Should().HaveCount(1);
        results[0].Id.Should().Be(pointId);
        results[0].Metadata["content"].Should().Be("智能家居灯光控制教程");
        results[0].Metadata["document_id"].Should().Be("doc-123");
    }
}

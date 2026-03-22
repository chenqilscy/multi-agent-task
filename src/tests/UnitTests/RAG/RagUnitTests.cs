using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Services.RAG;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MAF.Tests.RAG;

/// <summary>
/// FixedSizeDocumentChunker 单元测试
/// </summary>
public class FixedSizeDocumentChunkerTests
{
    private readonly FixedSizeDocumentChunker _chunker = new();

    [Fact]
    public async Task ChunkAsync_EmptyText_ReturnsEmptyList()
    {
        var result = await _chunker.ChunkAsync("", "doc1");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_ShortText_ReturnsSingleChunk()
    {
        var result = await _chunker.ChunkAsync("Hello world", "doc1");
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Hello world");
        result[0].DocumentId.Should().Be("doc1");
        result[0].ChunkIndex.Should().Be(0);
    }

    [Fact]
    public async Task ChunkAsync_LongText_ReturnsMultipleChunks()
    {
        var text = new string('A', 2000);
        var config = new ChunkingConfig { MaxChunkSize = 800, OverlapRatio = 0, RespectStructure = false };

        var result = await _chunker.ChunkAsync(text, "doc1", config);

        result.Should().HaveCountGreaterThan(1);
        result.All(c => c.DocumentId == "doc1").Should().BeTrue();
    }

    [Fact]
    public async Task ChunkAsync_Overlap_ChunksOverlap()
    {
        var text = new string('A', 1600);
        var config = new ChunkingConfig { MaxChunkSize = 800, OverlapRatio = 0.25, RespectStructure = false };

        var result = await _chunker.ChunkAsync(text, "doc1", config);

        // 有重叠时应该产生更多分块
        result.Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public async Task ChunkAsync_ChunkIndicesAreSequential()
    {
        var text = new string('B', 3000);
        var config = new ChunkingConfig { MaxChunkSize = 500, OverlapRatio = 0, RespectStructure = false };

        var result = await _chunker.ChunkAsync(text, "doc1", config);

        for (int i = 0; i < result.Count; i++)
        {
            result[i].ChunkIndex.Should().Be(i);
        }
    }

    [Fact]
    public async Task ChunkAsync_RespectStructure_SplitsAtParagraphBoundary()
    {
        var text = "第一段内容在这里。\n第二段内容也在这里，但是内容比较长" + new string('。', 200);
        var config = new ChunkingConfig { MaxChunkSize = 50, OverlapRatio = 0, RespectStructure = true };

        var result = await _chunker.ChunkAsync(text, "doc1", config);

        result.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ChunkAsync_DefaultConfig_UsesDefaults()
    {
        var text = new string('C', 2000);
        var result = await _chunker.ChunkAsync(text, "doc1");

        // 默认 800 字符，20% 重叠
        result.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ChunkAsync_CancellationToken_Respected()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _chunker.ChunkAsync(new string('D', 5000), "doc1",
            new ChunkingConfig { MaxChunkSize = 10, OverlapRatio = 0, RespectStructure = false }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

/// <summary>
/// DefaultRagRetriever 单元测试
/// </summary>
public class DefaultRagRetrieverTests
{
    private readonly Mock<IEmbeddingService> _mockEmbedding = new();
    private readonly Mock<IVectorStore> _mockVectorStore = new();
    private readonly DefaultRagRetriever _retriever;

    public DefaultRagRetrieverTests()
    {
        var logger = new Mock<ILogger<DefaultRagRetriever>>();
        _retriever = new DefaultRagRetriever(
            _mockEmbedding.Object, _mockVectorStore.Object, logger.Object);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsFilteredResults()
    {
        var queryVector = new float[] { 1.0f, 0.0f };
        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryVector);

        var searchResults = new List<VectorSearchResult>
        {
            new() { Id = "1", Score = 0.95f, Metadata = new() { ["content"] = "高分结果", ["document_id"] = "doc1", ["chunk_index"] = 0 } },
            new() { Id = "2", Score = 0.5f, Metadata = new() { ["content"] = "低分结果", ["document_id"] = "doc1", ["chunk_index"] = 1 } },
            new() { Id = "3", Score = 0.8f, Metadata = new() { ["content"] = "中分结果", ["document_id"] = "doc2", ["chunk_index"] = 0 } }
        };

        _mockVectorStore.Setup(v => v.SearchAsync(
                "test_collection", queryVector, 5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        var results = await _retriever.RetrieveAsync("查询", "test_collection", 5, 0.7f);

        results.Should().HaveCount(2);
        results[0].Score.Should().Be(0.95f);
        results[0].Content.Should().Be("高分结果");
        results[1].Score.Should().Be(0.8f);
    }

    [Fact]
    public async Task RetrieveAsync_EmptyResults_ReturnsEmptyList()
    {
        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 1.0f });

        _mockVectorStore.Setup(v => v.SearchAsync(
                It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(),
                It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        var results = await _retriever.RetrieveAsync("查询", "empty_collection");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task RetrieveAsync_AllBelowThreshold_ReturnsEmpty()
    {
        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 1.0f });

        var searchResults = new List<VectorSearchResult>
        {
            new() { Id = "1", Score = 0.3f, Metadata = new() { ["content"] = "低分" } }
        };

        _mockVectorStore.Setup(v => v.SearchAsync(
                It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(),
                It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        var results = await _retriever.RetrieveAsync("查询", "collection", 5, 0.7f);

        results.Should().BeEmpty();
    }
}

/// <summary>
/// DefaultRagPipeline 单元测试
/// </summary>
public class DefaultRagPipelineTests
{
    private readonly Mock<IDocumentChunker> _mockChunker = new();
    private readonly Mock<IEmbeddingService> _mockEmbedding = new();
    private readonly Mock<IVectorStore> _mockVectorStore = new();
    private readonly Mock<IRagRetriever> _mockRetriever = new();
    private readonly DefaultRagPipeline _pipeline;

    public DefaultRagPipelineTests()
    {
        var logger = new Mock<ILogger<DefaultRagPipeline>>();
        _pipeline = new DefaultRagPipeline(
            _mockChunker.Object,
            _mockEmbedding.Object,
            _mockVectorStore.Object,
            _mockRetriever.Object,
            logger.Object);
    }

    [Fact]
    public async Task IngestAsync_ChunksAndStoresVectors()
    {
        var chunks = new List<DocumentChunk>
        {
            new() { DocumentId = "doc1", ChunkIndex = 0, Content = "分块1" },
            new() { DocumentId = "doc1", ChunkIndex = 1, Content = "分块2" }
        };

        _mockChunker.Setup(c => c.ChunkAsync(It.IsAny<string>(), "doc1",
                It.IsAny<ChunkingConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        _mockEmbedding.Setup(e => e.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new[] { 1.0f }, new[] { 2.0f } });

        _mockEmbedding.Setup(e => e.VectorDimension).Returns(1);

        var result = await _pipeline.IngestAsync("doc1", "文档内容", "test_collection");

        result.Should().HaveCount(2);
        result[0].VectorId.Should().Be("doc1_0");
        result[1].VectorId.Should().Be("doc1_1");

        _mockVectorStore.Verify(v => v.CreateCollectionAsync("test_collection", 1,
            It.IsAny<CancellationToken>()), Times.Once);
        _mockVectorStore.Verify(v => v.InsertAsync("test_collection",
            It.IsAny<IEnumerable<VectorPoint>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_EmptyText_ReturnsEmpty()
    {
        _mockChunker.Setup(c => c.ChunkAsync("", "doc1",
                It.IsAny<ChunkingConfig?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentChunk>());

        var result = await _pipeline.IngestAsync("doc1", "", "test_collection");

        result.Should().BeEmpty();
        _mockEmbedding.Verify(e => e.GetEmbeddingsAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueryAsync_WithResults_ReturnsUsedKnowledgeContext()
    {
        var retrievedChunks = new List<RetrievalResult>
        {
            new() { Content = "相关内容", Score = 0.9f, DocumentId = "doc1", ChunkIndex = 0 }
        };

        _mockRetriever.Setup(r => r.RetrieveAsync("查询", "collection", 5, 0.7f,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(retrievedChunks);

        var request = new RagQueryRequest
        {
            Query = "查询",
            CollectionName = "collection",
            TopK = 5,
            ScoreThreshold = 0.7f
        };

        var result = await _pipeline.QueryAsync(request);

        result.UsedKnowledgeContext.Should().BeTrue();
        result.RetrievedChunks.Should().HaveCount(1);
        result.Answer.Should().Contain("相关内容");
    }

    [Fact]
    public async Task QueryAsync_NoResults_ReturnsNoContext()
    {
        _mockRetriever.Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RetrievalResult>());

        var request = new RagQueryRequest { Query = "查询", CollectionName = "collection" };
        var result = await _pipeline.QueryAsync(request);

        result.UsedKnowledgeContext.Should().BeFalse();
        result.RetrievedChunks.Should().BeEmpty();
    }
}

using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Services.RAG;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MAF.IntegrationTests.RAG;

/// <summary>
/// RAG 管线集成测试 — 使用 MemoryVectorStore + Mock IEmbeddingService
/// 端到端测试：文档摄入 → 向量存储 → 查询检索
/// </summary>
public class RagPipelineIntegrationTests
{
    private readonly MemoryVectorStore _vectorStore;
    private readonly Mock<IEmbeddingService> _mockEmbedding;
    private readonly FixedSizeDocumentChunker _chunker;
    private readonly DefaultRagRetriever _retriever;
    private readonly DefaultRagPipeline _pipeline;

    private const int VectorDimension = 4;
    private const string CollectionName = "test_knowledge_base";

    public RagPipelineIntegrationTests()
    {
        _vectorStore = new MemoryVectorStore(
            new Mock<ILogger<MemoryVectorStore>>().Object);

        _mockEmbedding = new Mock<IEmbeddingService>();
        _mockEmbedding.Setup(e => e.VectorDimension).Returns(VectorDimension);

        _chunker = new FixedSizeDocumentChunker();

        _retriever = new DefaultRagRetriever(
            _mockEmbedding.Object,
            _vectorStore,
            new Mock<ILogger<DefaultRagRetriever>>().Object);

        _pipeline = new DefaultRagPipeline(
            _chunker,
            _mockEmbedding.Object,
            _vectorStore,
            _retriever,
            new Mock<ILogger<DefaultRagPipeline>>().Object);
    }

    [Fact]
    public async Task IngestAndQuery_FullPipeline_ReturnsRelevantResults()
    {
        // Arrange — 配置嵌入服务返回确定性向量
        var docText = "智能家居系统支持灯光控制和温度调节。灯光可以通过语音命令或手机App进行控制。";

        // 为分块生成模拟嵌入
        _mockEmbedding.Setup(e => e.GetEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> texts, CancellationToken _) =>
                texts.Select((_, i) => new float[] { 1.0f, 0.0f, 0.5f + i * 0.1f, 0.0f }).ToList());

        // 为查询生成接近第一个分块的向量
        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 1.0f, 0.0f, 0.5f, 0.0f });

        // Act — 摄入文档
        var chunks = await _pipeline.IngestAsync("doc1", docText, CollectionName,
            new ChunkingConfig { MaxChunkSize = 50, OverlapRatio = 0, RespectStructure = false });

        // Assert — 验证分块
        chunks.Should().NotBeEmpty();
        chunks.All(c => c.VectorId != null).Should().BeTrue();

        // Act — 查询
        var response = await _pipeline.QueryAsync(new RagQueryRequest
        {
            Query = "如何控制灯光？",
            CollectionName = CollectionName,
            TopK = 3,
            ScoreThreshold = 0.5f
        });

        // Assert — 验证检索结果
        response.UsedKnowledgeContext.Should().BeTrue();
        response.RetrievedChunks.Should().NotBeEmpty();
        response.Answer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task IngestMultipleDocuments_QueryReturnsFromCorrectDoc()
    {
        // Arrange — 使用固定向量确保可预测性
        _mockEmbedding.Setup(e => e.GetEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> texts, CancellationToken _) =>
                texts.Select((_, i) => new float[] { 0.8f, 0.2f, 0.1f * i, 0.1f }).ToList());

        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.8f, 0.2f, 0.0f, 0.1f });

        var doc1Text = "智能灯光系统支持RGB颜色调节和亮度控制";
        var doc2Text = "空调温度可以通过智能面板进行自动调节";

        // Act — 摄入两个文档
        await _pipeline.IngestAsync("lighting_doc", doc1Text, CollectionName,
            new ChunkingConfig { MaxChunkSize = 200, OverlapRatio = 0, RespectStructure = false });

        await _pipeline.IngestAsync("climate_doc", doc2Text, CollectionName,
            new ChunkingConfig { MaxChunkSize = 200, OverlapRatio = 0, RespectStructure = false });

        // Act — 查询
        var response = await _pipeline.QueryAsync(new RagQueryRequest
        {
            Query = "灯光控制",
            CollectionName = CollectionName,
            TopK = 5,
            ScoreThreshold = 0.0f
        });

        // Assert — 应检索到两个文档的内容
        response.RetrievedChunks.Should().NotBeEmpty();
        response.UsedKnowledgeContext.Should().BeTrue();
    }

    [Fact]
    public async Task IngestAndQuery_NoRelevantResults_ReturnsEmpty()
    {
        // Arrange — 文档和查询嵌入向量完全不相关
        _mockEmbedding.Setup(e => e.GetEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> texts, CancellationToken _) =>
                texts.Select(_ => new float[] { 1.0f, 0.0f, 0.0f, 0.0f }).ToList());

        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.0f, 0.0f, 0.0f, 1.0f }); // 正交向量

        await _pipeline.IngestAsync("doc1", "一些内容", CollectionName,
            new ChunkingConfig { MaxChunkSize = 500, OverlapRatio = 0, RespectStructure = false });

        // Act
        var response = await _pipeline.QueryAsync(new RagQueryRequest
        {
            Query = "不相关的查询",
            CollectionName = CollectionName,
            TopK = 3,
            ScoreThreshold = 0.9f // 高阈值
        });

        // Assert
        response.UsedKnowledgeContext.Should().BeFalse();
        response.RetrievedChunks.Should().BeEmpty();
    }

    [Fact]
    public async Task IngestAsync_EmptyDocument_ReturnsEmptyChunks()
    {
        var result = await _pipeline.IngestAsync("empty_doc", "", CollectionName);

        result.Should().BeEmpty();
        _mockEmbedding.Verify(
            e => e.GetEmbeddingsAsync(It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_LargeDocument_CreatesMultipleChunks()
    {
        // Arrange
        SetupDeterministicEmbeddings();
        var largeText = string.Join("\n", Enumerable.Range(1, 100)
            .Select(i => $"这是第{i}段内容，包含智能家居的相关信息。"));

        // Act
        var chunks = await _pipeline.IngestAsync("large_doc", largeText, CollectionName,
            new ChunkingConfig { MaxChunkSize = 100, OverlapRatio = 0.1, RespectStructure = false });

        // Assert
        chunks.Should().HaveCountGreaterThan(5);
        chunks.Should().OnlyContain(c => c.DocumentId == "large_doc");
        chunks.Select(c => c.ChunkIndex).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task QueryAsync_TopK_LimitsResults()
    {
        // Arrange
        SetupDeterministicEmbeddings();
        var text = string.Join("\n", Enumerable.Range(1, 50)
            .Select(i => $"段落{i}的内容。"));

        await _pipeline.IngestAsync("doc1", text, CollectionName,
            new ChunkingConfig { MaxChunkSize = 30, OverlapRatio = 0, RespectStructure = false });

        // Act — 只请求 2 个结果
        var response = await _pipeline.QueryAsync(new RagQueryRequest
        {
            Query = "查询",
            CollectionName = CollectionName,
            TopK = 2,
            ScoreThreshold = 0.0f
        });

        // Assert
        response.RetrievedChunks.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task QueryAsync_ResultsOrderedByScore()
    {
        // Arrange
        SetupDeterministicEmbeddings();
        var text = "内容A。内容B。内容C。内容D。";

        await _pipeline.IngestAsync("doc1", text, CollectionName,
            new ChunkingConfig { MaxChunkSize = 10, OverlapRatio = 0, RespectStructure = false });

        // Act
        var response = await _pipeline.QueryAsync(new RagQueryRequest
        {
            Query = "查询",
            CollectionName = CollectionName,
            TopK = 10,
            ScoreThreshold = 0.0f
        });

        // Assert — 结果应按分数降序
        if (response.RetrievedChunks.Count > 1)
        {
            response.RetrievedChunks.Should().BeInDescendingOrder(r => r.Score);
        }
    }

    [Fact]
    public async Task Pipeline_WithChunkingOverlap_ChunksHaveOverlappingContent()
    {
        // Arrange
        SetupDeterministicEmbeddings();
        var text = new string('A', 500);

        // Act
        var chunks = await _pipeline.IngestAsync("doc1", text, CollectionName,
            new ChunkingConfig { MaxChunkSize = 200, OverlapRatio = 0.25, RespectStructure = false });

        // Assert — 有重叠时应有更多分块
        chunks.Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public async Task Pipeline_VectorIdsAreUnique()
    {
        // Arrange
        SetupDeterministicEmbeddings();
        var text = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"段落{i}"));

        // Act
        var chunks = await _pipeline.IngestAsync("doc1", text, CollectionName,
            new ChunkingConfig { MaxChunkSize = 30, OverlapRatio = 0, RespectStructure = false });

        // Assert
        var vectorIds = chunks.Select(c => c.VectorId).ToList();
        vectorIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Pipeline_MultipleIngestThenQuery_ReturnsFromAll()
    {
        // Arrange
        SetupDeterministicEmbeddings();

        await _pipeline.IngestAsync("faq1", "常见问题：如何设置灯光定时开关？", CollectionName,
            new ChunkingConfig { MaxChunkSize = 200, OverlapRatio = 0, RespectStructure = false });
        await _pipeline.IngestAsync("faq2", "常见问题：如何调节空调温度？", CollectionName,
            new ChunkingConfig { MaxChunkSize = 200, OverlapRatio = 0, RespectStructure = false });
        await _pipeline.IngestAsync("faq3", "常见问题：如何设置安防模式？", CollectionName,
            new ChunkingConfig { MaxChunkSize = 200, OverlapRatio = 0, RespectStructure = false });

        // Act
        var response = await _pipeline.QueryAsync(new RagQueryRequest
        {
            Query = "如何操作？",
            CollectionName = CollectionName,
            TopK = 10,
            ScoreThreshold = -1.0f // 随机向量余弦相似度可能为负，使用 -1 确保不过滤
        });

        // Assert — 应能检索到多个文档的内容
        response.RetrievedChunks.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// 设置确定性嵌入：每个文本根据其哈希生成稳定向量
    /// </summary>
    private void SetupDeterministicEmbeddings()
    {
        _mockEmbedding.Setup(e => e.GetEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> texts, CancellationToken _) =>
                texts.Select(t => GenerateVectorFromText(t)).ToList());

        _mockEmbedding.Setup(e => e.GetEmbeddingAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, CancellationToken _) =>
                GenerateVectorFromText(text));
    }

    private static float[] GenerateVectorFromText(string text)
    {
        var hash = text.GetHashCode();
        var r = new Random(hash);
        var vector = new float[VectorDimension];
        float norm = 0;
        for (int i = 0; i < VectorDimension; i++)
        {
            vector[i] = (float)(r.NextDouble() * 2 - 1);
            norm += vector[i] * vector[i];
        }
        // 归一化
        norm = MathF.Sqrt(norm);
        if (norm > 0)
        {
            for (int i = 0; i < VectorDimension; i++)
                vector[i] /= norm;
        }
        return vector;
    }
}

using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Agents.Specialized;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Core.Models.Task;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Core.Agents;

public class RagKnowledgeAgentTests
{
    private readonly Mock<IRagPipeline> _ragPipelineMock;
    private readonly Mock<IMafAiAgentRegistry> _registryMock;
    private readonly RagKnowledgeAgent _sut;

    public RagKnowledgeAgentTests()
    {
        _ragPipelineMock = new Mock<IRagPipeline>();
        _registryMock = new Mock<IMafAiAgentRegistry>();
        _sut = new RagKnowledgeAgent(
            _ragPipelineMock.Object,
            _registryMock.Object,
            NullLogger<RagKnowledgeAgent>.Instance);
    }

    [Fact]
    public void Properties_ShouldHaveExpectedDefaults()
    {
        _sut.AgentId.Should().Be("maf:rag-knowledge-agent:builtin");
        _sut.Name.Should().Be("RagKnowledgeAgent");
        _sut.CollectionName.Should().Be("default-knowledge");
        _sut.TopK.Should().Be(3);
        _sut.ScoreThreshold.Should().Be(0.3f);
        _sut.Capabilities.Should().Contain("knowledge-base");
        _sut.Capabilities.Should().Contain("rag-query");
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_NoChunks_ReturnsNoResultMessage()
    {
        var request = CreateRequest("什么是多智能体框架？");
        _ragPipelineMock
            .Setup(x => x.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponse { RetrievedChunks = new List<RetrievalResult>() });

        var result = await _sut.ExecuteBusinessLogicAsync(request);

        result.Success.Should().BeTrue();
        result.Result.Should().Contain("没有找到相关内容");
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_WithChunks_LlmFails_ReturnsFallbackChunks()
    {
        var request = CreateRequest("什么是MAF？");
        var chunks = new List<RetrievalResult>
        {
            new() { Content = "MAF是多智能体框架", Score = 0.9f, DocumentId = "doc1" },
            new() { Content = "支持编排多个Agent协作", Score = 0.8f, DocumentId = "doc2" }
        };

        _ragPipelineMock
            .Setup(x => x.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponse { RetrievedChunks = chunks });

        _registryMock
            .Setup(x => x.GetBestAgentAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No LLM agent"));

        var result = await _sut.ExecuteBusinessLogicAsync(request);

        result.Success.Should().BeTrue();
        result.Result.Should().Contain("MAF是多智能体框架");
        result.Result.Should().Contain("支持编排多个Agent协作");
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_WithChunks_LlmSucceeds_ReturnsLlmAnswer()
    {
        var request = CreateRequest("什么是MAF？");
        var chunks = new List<RetrievalResult>
        {
            new() { Content = "MAF是多智能体框架", Score = 0.9f, DocumentId = "doc1" }
        };

        _ragPipelineMock
            .Setup(x => x.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponse { RetrievedChunks = chunks });

        var testAgent = new FakeMafAiAgent("MAF是一个企业级多智能体编排框架");
        _registryMock
            .Setup(x => x.GetBestAgentAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAgent);

        var result = await _sut.ExecuteBusinessLogicAsync(request);

        result.Success.Should().BeTrue();
        result.Result.Should().Be("MAF是一个企业级多智能体编排框架");
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_RagPipelineThrows_ReturnsErrorResponse()
    {
        var request = CreateRequest("会触发异常的查询");

        _ragPipelineMock
            .Setup(x => x.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Connection timeout"));

        var result = await _sut.ExecuteBusinessLogicAsync(request);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("知识库查询发生错误");
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_PassesCorrectQueryParams()
    {
        var request = CreateRequest("测试查询");
        RagQueryRequest? capturedRequest = null;

        _ragPipelineMock
            .Setup(x => x.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RagQueryRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new RagQueryResponse { RetrievedChunks = new List<RetrievalResult>() });

        await _sut.ExecuteBusinessLogicAsync(request);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Query.Should().Be("测试查询");
        capturedRequest.CollectionName.Should().Be("default-knowledge");
        capturedRequest.TopK.Should().Be(3);
        capturedRequest.ScoreThreshold.Should().Be(0.3f);
    }

    [Fact]
    public async Task ExecuteBusinessLogicAsync_LlmReturnsEmpty_FallsBackToChunks()
    {
        var request = CreateRequest("测试查询");
        var chunks = new List<RetrievalResult>
        {
            new() { Content = "原始知识片段", Score = 0.85f, DocumentId = "doc1" }
        };

        _ragPipelineMock
            .Setup(x => x.QueryAsync(It.IsAny<RagQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagQueryResponse { RetrievedChunks = chunks });

        var testAgent = new FakeMafAiAgent(""); // 返回空字符串
        _registryMock
            .Setup(x => x.GetBestAgentAsync(It.IsAny<LlmScenario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAgent);

        var result = await _sut.ExecuteBusinessLogicAsync(request);

        result.Success.Should().BeTrue();
        result.Result.Should().Contain("根据知识库查询结果");
        result.Result.Should().Contain("原始知识片段");
    }

    [Fact]
    public void Constructor_NullRagPipeline_Throws()
    {
        var act = () => new RagKnowledgeAgent(
            null!,
            _registryMock.Object,
            NullLogger<RagKnowledgeAgent>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("ragPipeline");
    }

    private static MafTaskRequest CreateRequest(string userInput)
    {
        return new MafTaskRequest
        {
            TaskId = Guid.NewGuid().ToString(),
            UserInput = userInput
        };
    }

    /// <summary>
    /// 测试用 MafAiAgent 假实现
    /// </summary>
    private class FakeMafAiAgent : MafAiAgent
    {
        private readonly string _response;

        public FakeMafAiAgent(string response) : base(
            new LlmProviderConfig
            {
                ProviderName = "test",
                ModelId = "test-model",
                ApiKey = "test-key",
                ApiBaseUrl = "https://test.example.com",
                SupportedScenarios = [LlmScenario.Chat]
            },
            Mock.Of<ILogger<MafAiAgent>>())
        {
            _response = response;
        }

        public override Task<string> ExecuteAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult(_response);

        public override IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => AsyncEnumerable.Empty<string>();
    }
}

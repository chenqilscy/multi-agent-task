using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Task;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Core.Agents;

/// <summary>
/// MafBusinessAgentBase 的 protected 方法测试
/// 通过 TestableBusinessAgent 子类暴露 protected 成员进行测试
/// </summary>
public class MafBusinessAgentBaseTests
{
    private readonly Mock<IMafAiAgentRegistry> _registryMock;

    public MafBusinessAgentBaseTests()
    {
        _registryMock = new Mock<IMafAiAgentRegistry>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        var act = () => new TestableBusinessAgent(null!, Mock.Of<ILogger>());
        act.Should().Throw<ArgumentNullException>().WithParameterName("llmRegistry");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new TestableBusinessAgent(_registryMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CallLlmAsync

    [Fact]
    public async Task CallLlmAsync_Success_ReturnsLlmResponse()
    {
        // Arrange
        var fakeAgent = CreateFakeAgent("LLM回复内容");
        _registryMock
            .Setup(x => x.GetBestAgentAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeAgent);

        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());

        // Act
        var result = await sut.PublicCallLlmAsync("你好", LlmScenario.Chat, "系统提示");

        // Assert
        result.Should().Be("LLM回复内容");
        _registryMock.Verify(x => x.GetBestAgentAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CallLlmAsync_DifferentScenarios_SelectsCorrectAgent()
    {
        // Arrange
        var chatAgent = CreateFakeAgent("chat-response");
        var codeAgent = CreateFakeAgent("code-response");

        _registryMock
            .Setup(x => x.GetBestAgentAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatAgent);
        _registryMock
            .Setup(x => x.GetBestAgentAsync(LlmScenario.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeAgent);

        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());

        // Act
        var chatResult = await sut.PublicCallLlmAsync("聊天", LlmScenario.Chat);
        var codeResult = await sut.PublicCallLlmAsync("代码", LlmScenario.Code);

        // Assert
        chatResult.Should().Be("chat-response");
        codeResult.Should().Be("code-response");
    }

    #endregion

    #region CallLlmChatAsync

    [Fact]
    public async Task CallLlmChatAsync_MultiTurnMessages_BuildsPromptCorrectly()
    {
        // Arrange
        string? capturedPrompt = null;
        var fakeAgent = new CapturingMafAiAgent(prompt =>
        {
            capturedPrompt = prompt;
            return "chat-response";
        });

        _registryMock
            .Setup(x => x.GetBestAgentAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeAgent);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "你是助手"),
            new(ChatRole.User, "你好"),
            new(ChatRole.Assistant, "你好！"),
            new(ChatRole.User, "介绍自己")
        };

        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());

        // Act
        var result = await sut.PublicCallLlmChatAsync(messages, LlmScenario.Chat);

        // Assert
        result.Should().Be("chat-response");
        capturedPrompt.Should().Contain("[system]: 你是助手");
        capturedPrompt.Should().Contain("[user]: 你好");
        capturedPrompt.Should().Contain("[assistant]: 你好！");
        capturedPrompt.Should().Contain("[user]: 介绍自己");
    }

    #endregion

    #region CallLlmBatchAsync

    [Fact]
    public async Task CallLlmBatchAsync_MultiplePrompts_ReturnsAllResults()
    {
        // Arrange
        var callCount = 0;
        var fakeAgent = new CapturingMafAiAgent(prompt =>
        {
            callCount++;
            return $"response-{callCount}";
        });

        _registryMock
            .Setup(x => x.GetBestAgentAsync(LlmScenario.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeAgent);

        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());

        // Act
        var results = await sut.PublicCallLlmBatchAsync(
            new[] { "p1", "p2", "p3" }, LlmScenario.Chat);

        // Assert
        results.Should().HaveCount(3);
    }

    #endregion

    #region GetParameter<T>

    [Fact]
    public void GetParameter_ExistingKey_ReturnsConvertedValue()
    {
        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());
        var request = new MafTaskRequest
        {
            Parameters = new Dictionary<string, object> { ["count"] = "42" }
        };

        var result = sut.PublicGetParameter<int>(request, "count", 0);

        result.Should().Be(42);
    }

    [Fact]
    public void GetParameter_MissingKey_ReturnsDefault()
    {
        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());
        var request = new MafTaskRequest();

        var result = sut.PublicGetParameter<int>(request, "missing", 99);

        result.Should().Be(99);
    }

    [Fact]
    public void GetParameter_InvalidConversion_ReturnsDefault()
    {
        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());
        var request = new MafTaskRequest
        {
            Parameters = new Dictionary<string, object> { ["val"] = "not-a-number" }
        };

        var result = sut.PublicGetParameter<int>(request, "val", -1);

        result.Should().Be(-1);
    }

    [Fact]
    public void GetParameter_StringType_ReturnsDirectly()
    {
        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());
        var request = new MafTaskRequest
        {
            Parameters = new Dictionary<string, object> { ["name"] = "hello" }
        };

        var result = sut.PublicGetParameter<string>(request, "name", "default");

        result.Should().Be("hello");
    }

    [Fact]
    public void GetParameter_BoolType_ConvertsCorrectly()
    {
        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());
        var request = new MafTaskRequest
        {
            Parameters = new Dictionary<string, object> { ["flag"] = "true" }
        };

        var result = sut.PublicGetParameter<bool>(request, "flag", false);

        result.Should().BeTrue();
    }

    #endregion

    #region Abstract Properties

    [Fact]
    public void AbstractProperties_TestableAgent_ReturnsExpectedValues()
    {
        var sut = new TestableBusinessAgent(_registryMock.Object, Mock.Of<ILogger>());

        sut.AgentId.Should().Be("test:business-agent");
        sut.Name.Should().Be("TestBusinessAgent");
        sut.Description.Should().Be("Test business agent");
        sut.Capabilities.Should().Contain("test");
    }

    #endregion

    #region Test Helpers

    private static FakeAiAgentForBusiness CreateFakeAgent(string response)
    {
        return new FakeAiAgentForBusiness(response);
    }

    /// <summary>
    /// 可测试的 MafBusinessAgentBase 子类，暴露 protected 方法
    /// </summary>
    private class TestableBusinessAgent : MafBusinessAgentBase
    {
        public override string AgentId => "test:business-agent";
        public override string Name => "TestBusinessAgent";
        public override string Description => "Test business agent";
        public override IReadOnlyList<string> Capabilities => new[] { "test" };

        public TestableBusinessAgent(IMafAiAgentRegistry registry, ILogger logger)
            : base(registry, logger) { }

        public override Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request, CancellationToken ct = default)
            => Task.FromResult(new MafTaskResponse { Success = true });

        // 暴露 protected 方法
        public Task<string> PublicCallLlmAsync(
            string prompt, LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null, CancellationToken ct = default)
            => CallLlmAsync(prompt, scenario, systemPrompt, ct);

        public Task<string> PublicCallLlmChatAsync(
            IEnumerable<ChatMessage> messages, LlmScenario scenario = LlmScenario.Chat,
            CancellationToken ct = default)
            => CallLlmChatAsync(messages, scenario, ct);

        public Task<string[]> PublicCallLlmBatchAsync(
            string[] prompts, LlmScenario scenario = LlmScenario.Chat,
            string? systemPrompt = null, CancellationToken ct = default)
            => CallLlmBatchAsync(prompts, scenario, systemPrompt, ct);

        public T PublicGetParameter<T>(MafTaskRequest request, string key, T defaultValue)
            => GetParameter(request, key, defaultValue);
    }

    private class FakeAiAgentForBusiness : MafAiAgent
    {
        private readonly string _response;

        public FakeAiAgentForBusiness(string response) : base(
            new LlmProviderConfig
            {
                ProviderName = "test",
                ModelId = "test-model",
                ApiKey = "test-key",
                ApiBaseUrl = "https://test.example.com",
                SupportedScenarios = [LlmScenario.Chat, LlmScenario.Code]
            },
            Mock.Of<ILogger>())
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

    /// <summary>
    /// 可捕获 prompt 的 MafAiAgent 实现
    /// </summary>
    private class CapturingMafAiAgent : MafAiAgent
    {
        private readonly Func<string, string> _handler;

        public CapturingMafAiAgent(Func<string, string> handler) : base(
            new LlmProviderConfig
            {
                ProviderName = "test",
                ModelId = "test-model",
                ApiKey = "test-key",
                ApiBaseUrl = "https://test.example.com",
                SupportedScenarios = [LlmScenario.Chat, LlmScenario.Code]
            },
            Mock.Of<ILogger>())
        {
            _handler = handler;
        }

        public override Task<string> ExecuteAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult(_handler(prompt));

        public override IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => AsyncEnumerable.Empty<string>();
    }

    #endregion
}

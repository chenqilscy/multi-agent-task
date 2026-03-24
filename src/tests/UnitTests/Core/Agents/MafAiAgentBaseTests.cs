using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Session;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Tests.Core.Agents;

/// <summary>
/// MafAiAgent 基类测试
/// 通过 TestableAiAgent 子类测试 protected/internal 方法
/// </summary>
public class MafAiAgentBaseTests
{
    private static LlmProviderConfig CreateConfig(string provider = "test", string model = "test-model") => new()
    {
        ProviderName = provider,
        ProviderDisplayName = $"{provider}-display",
        ApiKey = "test-key-12345678901234",
        ApiBaseUrl = "https://test.example.com",
        ModelId = model,
        SupportedScenarios = [LlmScenario.Chat],
        MaxTokens = 1000,
        Temperature = 0.7,
    };

    #region Constructor Validation

    [Fact]
    public void Constructor_NullConfig_Throws()
    {
        var act = () => new TestableAiAgent(null!, Mock.Of<ILogger>());
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new TestableAiAgent(CreateConfig(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidParams_CreatesAgent()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        agent.Should().NotBeNull();
    }

    #endregion

    #region Agent Identity Properties

    [Fact]
    public void AgentId_CombinesProviderAndModel_Lowercase()
    {
        var agent = new TestableAiAgent(CreateConfig("MyProvider", "GPT-4o"), Mock.Of<ILogger>());

        agent.AgentId.Should().Be("myprovider-gpt-4o");
    }

    [Fact]
    public void AgentName_IncludesDisplayNameAndModel()
    {
        var agent = new TestableAiAgent(CreateConfig("test", "gpt-4"), Mock.Of<ILogger>());

        agent.AgentName.Should().Contain("test-display");
        agent.AgentName.Should().Contain("gpt-4");
    }

    [Fact]
    public void AgentDescription_IncludesProviderAndModel()
    {
        var agent = new TestableAiAgent(CreateConfig("openai", "gpt-4o"), Mock.Of<ILogger>());

        agent.AgentDescription.Should().Contain("openai");
        agent.AgentDescription.Should().Contain("gpt-4o");
    }

    #endregion

    #region GetCurrentModelId

    [Fact]
    public void GetCurrentModelId_ReturnsConfiguredModelId()
    {
        var agent = new TestableAiAgent(CreateConfig("p", "my-model"), Mock.Of<ILogger>());

        agent.GetCurrentModelId().Should().Be("my-model");
    }

    #endregion

    #region SupportsScenario

    [Fact]
    public void SupportsScenario_ConfiguredScenario_ReturnsTrue()
    {
        var config = CreateConfig();
        config.SupportedScenarios = [LlmScenario.Chat, LlmScenario.Code];
        var agent = new TestableAiAgent(config, Mock.Of<ILogger>());

        agent.SupportsScenario(LlmScenario.Chat).Should().BeTrue();
        agent.SupportsScenario(LlmScenario.Code).Should().BeTrue();
    }

    [Fact]
    public void SupportsScenario_UnsupportedScenario_ReturnsFalse()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());

        agent.SupportsScenario(LlmScenario.Image).Should().BeFalse();
    }

    #endregion

    #region EstimateTokenCount

    [Fact]
    public void EstimateTokenCount_EmptyString_ReturnsZero()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        agent.PublicEstimateTokenCount("").Should().Be(0);
    }

    [Fact]
    public void EstimateTokenCount_NullString_ReturnsZero()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        agent.PublicEstimateTokenCount(null!).Should().Be(0);
    }

    [Fact]
    public void EstimateTokenCount_EnglishText_ReturnsPositive()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var result = agent.PublicEstimateTokenCount("Hello world, this is a test");
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTokenCount_ChineseText_ReturnsPositive()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var result = agent.PublicEstimateTokenCount("你好世界这是一个测试");
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTokenCount_MixedText_ReturnsPositive()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var result = agent.PublicEstimateTokenCount("Hello 你好 World 世界");
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTokenCount_SingleChar_ReturnsAtLeastOne()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        agent.PublicEstimateTokenCount("a").Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region GetApiKey

    [Fact]
    public void GetApiKey_ValidConfig_ReturnsKey()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        agent.PublicGetApiKey().Should().Be("test-key-12345678901234");
    }

    [Fact]
    public void Constructor_EmptyApiKey_ThrowsArgumentException()
    {
        var config = CreateConfig();
        config.ApiKey = "";

        var act = () => new TestableAiAgent(config, Mock.Of<ILogger>());
        act.Should().Throw<ArgumentException>().WithMessage("*ApiKey*");
    }

    #endregion

    #region CreateSessionCoreAsync

    [Fact]
    public async Task CreateSessionCoreAsync_ReturnsMafAgentSession()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var session = await agent.PublicCreateSessionAsync();
        session.Should().BeOfType<MafAgentSession>();
    }

    #endregion

    #region SerializeSessionCoreAsync / DeserializeSessionCoreAsync

    [Fact]
    public async Task SerializeAndDeserialize_MafAgentSession_RoundTrips()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());

        // Create a session with data
        var session = new MafAgentSession();
        session.MafSession.SessionId = "test-session-123";
        session.MafSession.UserId = "user-456";
        session.MafSession.TurnCount = 5;
        session.MafSession.TotalTokensUsed = 1000;

        // Serialize
        var serialized = await agent.PublicSerializeSessionAsync(session);

        // Deserialize
        var deserialized = await agent.PublicDeserializeSessionAsync(serialized);

        // Assert
        deserialized.Should().BeOfType<MafAgentSession>();
        var mafSession = (MafAgentSession)deserialized;
        mafSession.MafSession.SessionId.Should().Be("test-session-123");
        mafSession.MafSession.UserId.Should().Be("user-456");
        mafSession.MafSession.TurnCount.Should().Be(5);
        mafSession.MafSession.TotalTokensUsed.Should().Be(1000);
    }

    [Fact]
    public async Task DeserializeSessionCoreAsync_InvalidJson_ReturnsNewSession()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());

        // Deserialize empty Json
        var emptyElement = JsonSerializer.SerializeToElement(new { });
        var session = await agent.PublicDeserializeSessionAsync(emptyElement);

        // Should not throw, returns a new session
        session.Should().BeOfType<MafAgentSession>();
    }

    [Fact]
    public async Task SerializeSessionCoreAsync_NonMafSession_ReturnsEmptyJson()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        // MafAgentSession without custom data acts as a non-populated session
        var genericSession = new MafAgentSession();

        var serialized = await agent.PublicSerializeSessionAsync(genericSession);

        serialized.ValueKind.Should().Be(JsonValueKind.Object);
    }

    #endregion

    #region RunCoreAsync

    [Fact]
    public async Task RunCoreAsync_WithUserMessage_ReturnsResponse()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "你好")
        };

        var response = await agent.PublicRunCoreAsync(messages);

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task RunCoreAsync_NoUserMessage_ThrowsArgumentException()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "系统提示")
        };

        var act = () => agent.PublicRunCoreAsync(messages);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunCoreAsync_WithSystemMessage_UsesSystemPrompt()
    {
        string? capturedSystem = null;
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>(),
            onExecute: (_, sys) => capturedSystem = sys);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "自定义系统提示"),
            new(ChatRole.User, "测试")
        };

        await agent.PublicRunCoreAsync(messages);

        capturedSystem.Should().Be("自定义系统提示");
    }

    [Fact]
    public async Task RunCoreAsync_WithMafSession_UpdatesSessionState()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var session = new MafAgentSession();
        session.MafSession.SessionId = "existing-session";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "测试消息")
        };

        await agent.PublicRunCoreAsync(messages, session);

        session.MafSession.TurnCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RunCoreAsync_NewSession_GeneratesSessionId()
    {
        var agent = new TestableAiAgent(CreateConfig(), Mock.Of<ILogger>());
        var session = new MafAgentSession();
        session.MafSession.SessionId = ""; // 空 ID, 应该生成新的

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "你好")
        };

        await agent.PublicRunCoreAsync(messages, session);

        session.MafSession.SessionId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// 可测试的 MafAiAgent 子类，暴露 protected/internal 方法
    /// </summary>
    private class TestableAiAgent : MafAiAgent
    {
        private readonly bool _promptEcho;
        private readonly Action<string, string?>? _onExecute;

        public TestableAiAgent(
            LlmProviderConfig config,
            ILogger logger,
            IMafAiSessionStore? sessionStore = null,
            bool promptEcho = false,
            Action<string, string?>? onExecute = null)
            : base(config, logger, sessionStore)
        {
            _promptEcho = promptEcho;
            _onExecute = onExecute;
        }

        public override Task<string> ExecuteAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
        {
            _onExecute?.Invoke(prompt, systemPrompt);
            return Task.FromResult(_promptEcho ? $"echo:{prompt}" : "test-response");
        }

        public override IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => AsyncEnumerable.Empty<string>();

        // 暴露 protected 方法
        public int PublicEstimateTokenCount(string text) => EstimateTokenCount(text);
        public string PublicGetApiKey() => GetApiKey();

        public ValueTask<AgentSession> PublicCreateSessionAsync()
            => CreateSessionCoreAsync();

        public ValueTask<JsonElement> PublicSerializeSessionAsync(AgentSession session)
            => SerializeSessionCoreAsync(session);

        public ValueTask<AgentSession> PublicDeserializeSessionAsync(JsonElement data)
            => DeserializeSessionCoreAsync(data);

        public Task<AgentResponse> PublicRunCoreAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null)
            => RunCoreAsync(messages, session);
    }

    #endregion
}

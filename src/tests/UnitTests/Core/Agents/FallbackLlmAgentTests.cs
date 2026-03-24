using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Core.Agents;

/// <summary>
/// FallbackLlmAgent 单元测试
/// 覆盖 Fallback 链逻辑、统计信息、历史管理
/// </summary>
public class FallbackLlmAgentTests
{
    private static LlmProviderConfig CreateConfig(string provider, string model = "test-model") => new()
    {
        ProviderName = provider,
        ProviderDisplayName = provider,
        ApiKey = "test-key-12345678901234",
        ApiBaseUrl = "https://test.example.com",
        ModelId = model,
        SupportedScenarios = [LlmScenario.Chat],
        MaxTokens = 1000,
        Temperature = 0.7,
    };

    #region ExecuteAsync — Primary Succeeds

    [Fact]
    public async Task ExecuteAsync_PrimarySucceeds_ReturnsPrimaryResult()
    {
        // Arrange
        var primary = new FakeMafAiAgent("primary-response", CreateConfig("primary"));
        var fallback = new FakeMafAiAgent("fallback-response", CreateConfig("fallback"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent> { fallback }, Mock.Of<ILogger>());

        // Act
        var result = await sut.ExecuteAsync("test-model", "hello");

        // Assert
        result.Should().Be("primary-response");
        sut.FallbackHistory.Should().HaveCount(1);
        sut.FallbackHistory[0].SuccessAgentId.Should().Be(primary.AgentId);
    }

    #endregion

    #region ExecuteAsync — Primary Fails, Fallback Succeeds

    [Fact]
    public async Task ExecuteAsync_PrimaryFails_FallbackSucceeds_ReturnsFallbackResult()
    {
        // Arrange
        var primary = new FailingMafAiAgent(CreateConfig("primary"), new HttpRequestException("主 Agent 失败"));
        var fallback = new FakeMafAiAgent("fallback-response", CreateConfig("fallback"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent> { fallback }, Mock.Of<ILogger>());

        // Act
        var result = await sut.ExecuteAsync("test-model", "hello");

        // Assert
        result.Should().Be("fallback-response");
        sut.FallbackHistory.Should().HaveCount(1);
        sut.FallbackHistory[0].SuccessAgentId.Should().Be(fallback.AgentId);
        sut.FallbackHistory[0].Attempts.Should().HaveCount(2);
    }

    #endregion

    #region ExecuteAsync — All Fail

    [Fact]
    public async Task ExecuteAsync_AllAgentsFail_ThrowsInvalidOperationException()
    {
        // Arrange
        var primary = new FailingMafAiAgent(CreateConfig("primary"), new HttpRequestException("主错误"));
        var fallback1 = new FailingMafAiAgent(CreateConfig("fallback1"), new TimeoutException("超时"));
        var fallback2 = new FailingMafAiAgent(CreateConfig("fallback2"), new InvalidOperationException("无效"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent> { fallback1, fallback2 }, Mock.Of<ILogger>());

        // Act
        var act = () => sut.ExecuteAsync("test-model", "hello");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("All agents failed");
        sut.FallbackHistory.Should().HaveCount(1);
        sut.FallbackHistory[0].SuccessAgentId.Should().BeNull();
        sut.FallbackHistory[0].ErrorMessage.Should().Contain("All 3 agents failed");
        sut.FallbackHistory[0].Attempts.Should().HaveCount(3);
    }

    #endregion

    #region ExecuteAsync — Prompt Truncation

    [Fact]
    public async Task ExecuteAsync_LongPrompt_TruncatesTo100Chars()
    {
        // Arrange
        var longPrompt = new string('A', 200);
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent>(), Mock.Of<ILogger>());

        // Act
        await sut.ExecuteAsync("test-model", longPrompt);

        // Assert
        sut.FallbackHistory[0].Prompt.Should().HaveLength(100);
    }

    [Fact]
    public async Task ExecuteAsync_ShortPrompt_NotTruncated()
    {
        // Arrange
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent>(), Mock.Of<ILogger>());

        // Act
        await sut.ExecuteAsync("test-model", "short");

        // Assert
        sut.FallbackHistory[0].Prompt.Should().Be("short");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyPrompt_RecordsEmpty()
    {
        // Arrange
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent>(), Mock.Of<ILogger>());

        // Act
        await sut.ExecuteAsync("test-model", "");

        // Assert
        sut.FallbackHistory[0].Prompt.Should().BeEmpty();
    }

    #endregion

    #region ExecuteStreamingAsync

    [Fact]
    public async Task ExecuteStreamingAsync_ReturnsNonStreamingResultAsSingleChunk()
    {
        // Arrange
        var primary = new FakeMafAiAgent("streamed-response", CreateConfig("primary"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent>(), Mock.Of<ILogger>());

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in sut.ExecuteStreamingAsync("test-model", "hello"))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Should().Be("streamed-response");
    }

    #endregion

    #region GetStatistics

    [Fact]
    public void GetStatistics_NoHistory_ReturnsZeros()
    {
        // Arrange
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent>(), Mock.Of<ILogger>());

        // Act
        var stats = sut.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(0);
        stats.SuccessfulRequests.Should().Be(0);
        stats.FallbackRate.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
    }

    [Fact]
    public async Task GetStatistics_MixedResults_CalculatesCorrectly()
    {
        // Arrange
        var primary = new FailingMafAiAgent(CreateConfig("primary"), new Exception("error"));
        var fallback = new FakeMafAiAgent("ok", CreateConfig("fallback"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent> { fallback }, Mock.Of<ILogger>());

        // 3 成功（全部由 fallback 完成）
        await sut.ExecuteAsync("m", "p1");
        await sut.ExecuteAsync("m", "p2");
        await sut.ExecuteAsync("m", "p3");

        // Act
        var stats = sut.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(3);
        stats.SuccessfulRequests.Should().Be(3);
        stats.FallbackRate.Should().Be(1.0); // 所有成功都是 fallback
        stats.AgentUsageCounts.Should().ContainKey(fallback.AgentId);
        stats.AgentUsageCounts[fallback.AgentId].Should().Be(3);
    }

    #endregion

    #region ClearHistory

    [Fact]
    public async Task ClearHistory_RemovesAllHistory()
    {
        // Arrange
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent>(), Mock.Of<ILogger>());
        await sut.ExecuteAsync("m", "p1");
        await sut.ExecuteAsync("m", "p2");
        sut.FallbackHistory.Should().HaveCount(2);

        // Act
        sut.ClearHistory();

        // Assert
        sut.FallbackHistory.Should().BeEmpty();
        sut.GetStatistics().TotalRequests.Should().Be(0);
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_NullPrimaryAgent_Throws()
    {
        var act = () => new FallbackLlmAgent(null!, new List<MafAiAgent>(), Mock.Of<ILogger>());
        act.Should().Throw<Exception>(); // NRE from CreateFallbackConfig before null guard
    }

    [Fact]
    public void Constructor_NullFallbackAgents_Throws()
    {
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var act = () => new FallbackLlmAgent(primary, null!, Mock.Of<ILogger>());
        act.Should().Throw<Exception>(); // NRE from CreateFallbackConfig before null guard
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var primary = new FakeMafAiAgent("ok", CreateConfig("primary"));
        var act = () => new FallbackLlmAgent(primary, new List<MafAiAgent>(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region SupportsScenario

    [Fact]
    public void SupportsScenario_CombinesAllAgentScenarios()
    {
        // Arrange
        var config1 = CreateConfig("primary");
        config1.SupportedScenarios = [LlmScenario.Chat];
        var config2 = CreateConfig("fallback");
        config2.SupportedScenarios = [LlmScenario.Code, LlmScenario.Image];

        var primary = new FakeMafAiAgent("ok", config1);
        var fallback = new FakeMafAiAgent("ok", config2);
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent> { fallback }, Mock.Of<ILogger>());

        // Assert — all scenarios from all agents should be supported
        sut.SupportsScenario(LlmScenario.Chat).Should().BeTrue();
        sut.SupportsScenario(LlmScenario.Code).Should().BeTrue();
        sut.SupportsScenario(LlmScenario.Image).Should().BeTrue();
    }

    #endregion

    #region Multiple Fallbacks — Mid-chain Success

    [Fact]
    public async Task ExecuteAsync_SecondFallbackSucceeds_SkipsThird()
    {
        // Arrange
        var primary = new FailingMafAiAgent(CreateConfig("p"), new Exception("e1"));
        var fb1 = new FailingMafAiAgent(CreateConfig("fb1"), new Exception("e2"));
        var fb2 = new FakeMafAiAgent("mid-chain-ok", CreateConfig("fb2"));
        var fb3 = new FakeMafAiAgent("should-not-reach", CreateConfig("fb3"));
        var sut = new FallbackLlmAgent(primary, new List<MafAiAgent> { fb1, fb2, fb3 }, Mock.Of<ILogger>());

        // Act
        var result = await sut.ExecuteAsync("m", "hello");

        // Assert
        result.Should().Be("mid-chain-ok");
        sut.FallbackHistory[0].Attempts.Should().HaveCount(3); // p, fb1, fb2
        sut.FallbackHistory[0].SuccessAgentId.Should().Be(fb2.AgentId);
    }

    #endregion

    #region Test Helpers

    private class FakeMafAiAgent : MafAiAgent
    {
        private readonly string _response;

        public FakeMafAiAgent(string response, LlmProviderConfig config) : base(config, Mock.Of<ILogger>())
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

    private class FailingMafAiAgent : MafAiAgent
    {
        private readonly Exception _exception;

        public FailingMafAiAgent(LlmProviderConfig config, Exception exception) : base(config, Mock.Of<ILogger>())
        {
            _exception = exception;
        }

        public override Task<string> ExecuteAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromException<string>(_exception);

        public override IAsyncEnumerable<string> ExecuteStreamingAsync(
            string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default)
            => throw _exception;
    }

    #endregion
}

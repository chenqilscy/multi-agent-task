using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Agents.Providers;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Services.Factory;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Services.Factory;

/// <summary>
/// LlmAgentFactory 单元测试
/// 验证工厂的创建逻辑、验证逻辑和提供商路由
/// </summary>
public class LlmAgentFactoryTests
{
    private readonly Mock<ILlmProviderConfigRepository> _configRepoMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly LlmAgentFactory _factory;

    public LlmAgentFactoryTests()
    {
        _loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        _factory = new LlmAgentFactory(
            Mock.Of<ILogger<LlmAgentFactory>>(),
            _configRepoMock.Object,
            _loggerFactoryMock.Object,
            _httpClientFactoryMock.Object);
    }

    private static LlmProviderConfig CreateConfig(
        string provider = "openai",
        string model = "gpt-4o",
        bool enabled = true,
        int priority = 0,
        params LlmScenario[] scenarios) => new()
    {
        ProviderName = provider,
        ProviderDisplayName = "Test Provider",
        ApiKey = "test-key-12345678901234567890",
        ApiBaseUrl = provider == "azure-openai"
            ? "https://my-resource.openai.azure.com"
            : "https://api.openai.com/v1/",
        ModelId = model,
        SupportedScenarios = scenarios.Length > 0
            ? scenarios.ToList()
            : [LlmScenario.Chat],
        IsEnabled = enabled,
        Priority = priority,
    };

    #region CreateAgentAsync Tests

    [Theory]
    [InlineData("openai", typeof(OpenAiLlmAgent))]
    [InlineData("azure-openai", typeof(AzureOpenAiLlmAgent))]
    [InlineData("azureopenai", typeof(AzureOpenAiLlmAgent))]
    public async Task CreateAgentAsync_ReturnsCorrectType(string provider, Type expectedType)
    {
        // Arrange
        var config = CreateConfig(provider);

        // Act
        var agent = await _factory.CreateAgentAsync(config, LlmScenario.Chat);

        // Assert
        agent.Should().BeOfType(expectedType);
    }

    [Fact]
    public async Task CreateAgentAsync_NullConfig_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _factory.CreateAgentAsync(null!, LlmScenario.Chat));
    }

    [Fact]
    public async Task CreateAgentAsync_UnsupportedScenario_ThrowsInvalidOperation()
    {
        var config = CreateConfig(scenarios: LlmScenario.Chat);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _factory.CreateAgentAsync(config, LlmScenario.Image));
    }

    [Fact]
    public async Task CreateAgentAsync_DisabledProvider_ThrowsInvalidOperation()
    {
        var config = CreateConfig(enabled: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _factory.CreateAgentAsync(config, LlmScenario.Chat));
    }

    [Fact]
    public async Task CreateAgentAsync_UnsupportedProvider_ThrowsNotSupported()
    {
        var config = CreateConfig("unknown-provider");

        await Assert.ThrowsAsync<NotSupportedException>(
            () => _factory.CreateAgentAsync(config, LlmScenario.Chat));
    }

    [Fact]
    public async Task CreateAgentAsync_InvalidConfig_ThrowsArgumentException()
    {
        var config = new LlmProviderConfig
        {
            ProviderName = "openai",
            ApiKey = "", // Invalid: empty API key
            ModelId = "gpt-4o",
            ApiBaseUrl = "https://api.openai.com/v1",
            SupportedScenarios = [LlmScenario.Chat],
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _factory.CreateAgentAsync(config, LlmScenario.Chat));
    }

    #endregion

    #region CreateAgentByProviderAsync Tests

    [Fact]
    public async Task CreateAgentByProviderAsync_LoadsFromRepository()
    {
        // Arrange
        var config = CreateConfig("openai");
        _configRepoMock
            .Setup(r => r.GetByNameAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var agent = await _factory.CreateAgentByProviderAsync("openai", LlmScenario.Chat);

        // Assert
        agent.Should().BeOfType<OpenAiLlmAgent>();
        _configRepoMock.Verify(r => r.GetByNameAsync("openai", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAgentByProviderAsync_NotFound_ThrowsInvalidOperation()
    {
        _configRepoMock
            .Setup(r => r.GetByNameAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmProviderConfig?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _factory.CreateAgentByProviderAsync("nonexistent", LlmScenario.Chat));
    }

    [Fact]
    public async Task CreateAgentByProviderAsync_EmptyName_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _factory.CreateAgentByProviderAsync("", LlmScenario.Chat));
    }

    #endregion

    #region CreateBestAgentForScenarioAsync Tests

    [Fact]
    public async Task CreateBestAgentForScenarioAsync_SelectsHighestPriority()
    {
        // Arrange
        var configs = new List<LlmProviderConfig>
        {
            CreateConfig("openai", "gpt-3.5", priority: 10, scenarios: LlmScenario.Chat),
            CreateConfig("openai", "gpt-4o", priority: 1, scenarios: LlmScenario.Chat),
        };
        _configRepoMock
            .Setup(r => r.GetAllEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var agent = await _factory.CreateBestAgentForScenarioAsync(LlmScenario.Chat);

        // Assert
        agent.Config.Priority.Should().Be(1);
        agent.Config.ModelId.Should().Be("gpt-4o");
    }

    [Fact]
    public async Task CreateBestAgentForScenarioAsync_NoProviders_ThrowsInvalidOperation()
    {
        _configRepoMock
            .Setup(r => r.GetAllEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LlmProviderConfig>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _factory.CreateBestAgentForScenarioAsync(LlmScenario.Chat));
    }

    #endregion

    #region CreateAllAgentsForScenarioAsync Tests

    [Fact]
    public async Task CreateAllAgentsForScenarioAsync_ReturnsAllMatchingProviders()
    {
        // Arrange — use different provider names to avoid deduplication
        var configs = new List<LlmProviderConfig>
        {
            CreateConfig("openai", "gpt-4o", priority: 1, scenarios: LlmScenario.Chat),
            CreateConfig("azure-openai", "my-deployment", priority: 2, scenarios: LlmScenario.Chat),
        };
        _configRepoMock
            .Setup(r => r.GetAllEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var agents = await _factory.CreateAllAgentsForScenarioAsync(LlmScenario.Chat);

        // Assert
        agents.Should().HaveCount(2);
        agents[0].Should().BeOfType<OpenAiLlmAgent>();
        agents[1].Should().BeOfType<AzureOpenAiLlmAgent>();
    }

    [Fact]
    public async Task CreateAllAgentsForScenarioAsync_NoMatch_ReturnsEmptyList()
    {
        _configRepoMock
            .Setup(r => r.GetAllEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LlmProviderConfig>());

        var agents = await _factory.CreateAllAgentsForScenarioAsync(LlmScenario.Video);

        agents.Should().BeEmpty();
    }

    #endregion

    #region IsScenarioSupportedAsync Tests

    [Fact]
    public async Task IsScenarioSupportedAsync_Supported_ReturnsTrue()
    {
        var config = CreateConfig(scenarios: [LlmScenario.Chat, LlmScenario.Code]);
        _configRepoMock
            .Setup(r => r.GetByNameAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await _factory.IsScenarioSupportedAsync("openai", LlmScenario.Chat);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsScenarioSupportedAsync_NotSupported_ReturnsFalse()
    {
        var config = CreateConfig(scenarios: LlmScenario.Chat);
        _configRepoMock
            .Setup(r => r.GetByNameAsync("openai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await _factory.IsScenarioSupportedAsync("openai", LlmScenario.Image);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsScenarioSupportedAsync_ProviderNotFound_ReturnsFalse()
    {
        _configRepoMock
            .Setup(r => r.GetByNameAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LlmProviderConfig?)null);

        var result = await _factory.IsScenarioSupportedAsync("unknown", LlmScenario.Chat);

        result.Should().BeFalse();
    }

    #endregion
}

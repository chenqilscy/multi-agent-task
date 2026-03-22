using CKY.MultiAgentFramework.Core.Models.LLM;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models;

public class LlmProviderConfigTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new LlmProviderConfig();

        config.Id.Should().BeNull();
        config.ProviderName.Should().BeEmpty();
        config.ProviderDisplayName.Should().BeEmpty();
        config.ApiBaseUrl.Should().BeEmpty();
        config.ApiKey.Should().BeEmpty();
        config.ModelId.Should().BeEmpty();
        config.ModelDisplayName.Should().BeEmpty();
        config.SupportedScenarios.Should().BeEmpty();
        config.MaxTokens.Should().Be(2000);
        config.Temperature.Should().Be(0.7);
        config.IsEnabled.Should().BeTrue();
        config.Priority.Should().Be(0);
        config.CostPer1kTokens.Should().Be(0);
        config.AdditionalParameters.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidConfig_ShouldNotThrow()
    {
        var config = CreateValidConfig();
        config.Invoking(c => c.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_EmptyProviderName_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.ProviderName = "";
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*ProviderName*");
    }

    [Fact]
    public void Validate_EmptyApiKey_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.ApiKey = "";
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*ApiKey*");
    }

    [Fact]
    public void Validate_EmptyModelId_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.ModelId = "";
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*ModelId*");
    }

    [Fact]
    public void Validate_InvalidApiBaseUrl_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.ApiBaseUrl = "not-a-uri";
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*ApiBaseUrl*");
    }

    [Fact]
    public void Validate_EmptyScenarios_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.SupportedScenarios = new List<LlmScenario>();
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*scenario*");
    }

    [Fact]
    public void Validate_TemperatureOutOfRange_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.Temperature = 2.5;
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*Temperature*");
    }

    [Fact]
    public void Validate_NegativeTemperature_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.Temperature = -0.1;
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*Temperature*");
    }

    [Fact]
    public void Validate_ZeroMaxTokens_ShouldThrow()
    {
        var config = CreateValidConfig();
        config.MaxTokens = 0;
        config.Invoking(c => c.Validate()).Should().Throw<ArgumentException>()
            .WithMessage("*MaxTokens*");
    }

    [Fact]
    public void Validate_MultipleErrors_ShouldIncludeAll()
    {
        var config = new LlmProviderConfig();
        var action = () => config.Validate();
        action.Should().Throw<ArgumentException>()
            .WithMessage("*ProviderName*")
            .WithMessage("*ApiKey*")
            .WithMessage("*ModelId*");
    }

    [Fact]
    public void GetApiKeyForLogging_EmptyKey_ShouldReturnNotSet()
    {
        var config = new LlmProviderConfig { ApiKey = "" };
        config.GetApiKeyForLogging().Should().Be("[NOT SET]");
    }

    [Fact]
    public void GetApiKeyForLogging_WhitespaceKey_ShouldReturnNotSet()
    {
        var config = new LlmProviderConfig { ApiKey = "   " };
        config.GetApiKeyForLogging().Should().Be("[NOT SET]");
    }

    [Fact]
    public void GetApiKeyForLogging_ShortKey_ShouldReturnRedacted()
    {
        var config = new LlmProviderConfig { ApiKey = "12345678" };
        config.GetApiKeyForLogging().Should().Be("[REDACTED]");
    }

    [Fact]
    public void GetApiKeyForLogging_LongKey_ShouldMaskMiddle()
    {
        var config = new LlmProviderConfig { ApiKey = "abcdefghijklmnop" };
        var result = config.GetApiKeyForLogging();
        result.Should().Be("abcd...mnop");
    }

    private static LlmProviderConfig CreateValidConfig() => new()
    {
        ProviderName = "test-provider",
        ApiKey = "test-api-key-12345",
        ModelId = "test-model",
        ApiBaseUrl = "https://api.test.com/v1/",
        SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat },
        Temperature = 0.7,
        MaxTokens = 2000
    };
}

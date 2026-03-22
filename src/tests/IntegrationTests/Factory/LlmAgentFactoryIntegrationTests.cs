using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Services.DependencyInjection;
using CKY.MultiAgentFramework.Services.Factory;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.Factory;

/// <summary>
/// LlmAgentFactory 集成测试
/// 验证 DI 注册和 HttpClient 工厂集成
/// </summary>
public class LlmAgentFactoryIntegrationTests
{
    [Fact]
    public void AddLlmAgentFactory_ShouldRegisterILlmAgentFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddLlmAgentFactory();

        // Assert
        var descriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ILlmAgentFactory));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(LlmAgentFactory));
    }

    [Fact]
    public void AddLlmAgentFactory_ShouldRegisterNamedHttpClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddLlmAgentFactory();
        var provider = services.BuildServiceProvider();

        // Assert - 验证 IHttpClientFactory 已注册
        var factory = provider.GetService<IHttpClientFactory>();
        factory.Should().NotBeNull("IHttpClientFactory should be registered by AddLlmAgentFactory");
    }

    [Fact]
    public void AddLlmAgentFactory_WithCustomTimeout_ShouldConfigure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddLlmAgentFactory(options =>
        {
            options.DefaultTimeout = TimeSpan.FromSeconds(120);
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<IHttpClientFactory>();
        factory.Should().NotBeNull();

        var client = factory!.CreateClient("ZhipuAIAgent");
        client.Should().NotBeNull();
        client.Timeout.Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void AddLlmAgentFactory_ShouldCreateNamedClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLlmAgentFactory();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act & Assert - 验证各提供商的 Named HttpClient 能正常创建
        var providerNames = new[]
        {
            "ZhipuAIAgent", "TongyiLlmAgent", "WenxinLlmAgent",
            "XunfeiLlmAgent", "BaichuanLlmAgent", "MiniMaxLlmAgent"
        };

        foreach (var name in providerNames)
        {
            var client = factory.CreateClient(name);
            client.Should().NotBeNull($"Named HttpClient '{name}' should be created");
        }
    }
}

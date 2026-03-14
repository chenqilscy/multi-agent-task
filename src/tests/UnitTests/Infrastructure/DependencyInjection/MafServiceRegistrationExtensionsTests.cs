using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Repository.Relational;

namespace CKY.MultiAgentFramework.Infrastructure.DependencyInjection;

/// <summary>
/// Unit tests for MafServiceRegistrationExtensions
/// </summary>
public class MafServiceRegistrationExtensionsTests
{
    [Fact]
    public void AddMafInfrastructureServices_WithNoConfig_ShouldRegisterMemoryImplementations()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddMafInfrastructureServices(configuration);

        // Assert - 验证所有服务都注册了内存/默认实现
        var cacheStoreDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ICacheStore));
        cacheStoreDescriptor.Should().NotBeNull();
        cacheStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryCacheStore));
        cacheStoreDescriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var vectorStoreDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IVectorStore));
        vectorStoreDescriptor.Should().NotBeNull();
        vectorStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryVectorStore));
        vectorStoreDescriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var databaseDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IRelationalDatabase));
        databaseDescriptor.Should().NotBeNull();
        databaseDescriptor?.ImplementationType.Should().Be(typeof(EfCoreRelationalDatabase));
        databaseDescriptor?.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    // TODO: Add configuration-based service registration tests in Task 9
    // TODO: Add multiple implementation scenario tests in Task 10
}

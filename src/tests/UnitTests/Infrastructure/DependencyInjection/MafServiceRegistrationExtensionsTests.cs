using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
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

    [Fact]
    public void AddMafInfrastructureServices_WithRedisConfig_ShouldRegisterRedisCacheStore()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafServices:Implementations:ICacheStore"] = "RedisCacheStore"
            })
            .Build();

        // Act
        services.AddMafInfrastructureServices(configuration);

        // Assert
        var cacheDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ICacheStore));
        cacheDescriptor.Should().NotBeNull();
        cacheDescriptor?.ImplementationType.Should().Be(typeof(RedisCacheStore));
        cacheDescriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMafInfrastructureServices_WithInvalidConfig_ShouldUseDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafServices:Implementations:ICacheStore"] = "InvalidCacheStore"
            })
            .Build();

        // Act & Assert
        var action = () => services.AddMafInfrastructureServices(configuration);

        // 应该不抛出异常，而是使用默认实现
        action.Should().NotThrow();

        // 验证默认实现被注册
        var descriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ICacheStore));
        descriptor.Should().NotBeNull();
        descriptor?.ImplementationType.Should().Be(typeof(MemoryCacheStore));
        descriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}

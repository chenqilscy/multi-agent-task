using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.Caching.Redis;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using CKY.MultiAgentFramework.Infrastructure.Repository.Relational;

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

    [Fact]
    public void AddMafBuiltinServices_ShouldRegisterAllBuiltinServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafStorage:UseBuiltinImplementations"] = "true",
                ["MafStorage:RelationalDatabase:Provider"] = "SQLite"
            })
            .Build();

        // Act
        services.AddMafBuiltinServices(configuration);

        // Assert - 验证所有核心服务都已注册
        // 注意：无 Redis 连接字符串时，降级到 MemoryCacheStore
        var cacheStoreDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ICacheStore));
        cacheStoreDescriptor.Should().NotBeNull("ICacheStore should be registered");
        cacheStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryCacheStore),
            "no Redis connection string configured, should fallback to MemoryCacheStore");
        cacheStoreDescriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var vectorStoreDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IVectorStore));
        vectorStoreDescriptor.Should().NotBeNull("IVectorStore should be registered");
        vectorStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryVectorStore));
        vectorStoreDescriptor?.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var databaseDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IRelationalDatabase));
        databaseDescriptor.Should().NotBeNull("IRelationalDatabase should be registered");
        // AddDbContext 使用工厂模式注册，ImplementationType 可能为 null
        (databaseDescriptor?.ImplementationType != null || databaseDescriptor?.ImplementationFactory != null)
            .Should().BeTrue("should have a concrete implementation or factory");
        databaseDescriptor?.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMafBuiltinServices_WithUseBuiltinFalse_ShouldUseConfigDrivenRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafStorage:UseBuiltinImplementations"] = "false"
            })
            .Build();

        // Act
        services.AddMafBuiltinServices(configuration);

        // Assert - 应该使用默认配置驱动注册（内存实现）
        var cacheStoreDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ICacheStore));
        cacheStoreDescriptor.Should().NotBeNull();
        cacheStoreDescriptor?.ImplementationType.Should().Be(typeof(MemoryCacheStore));
    }

    [Fact]
    public void AddMafBuiltinServices_WithPostgreSQLConfig_ShouldRegisterPostgreSQL()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafStorage:UseBuiltinImplementations"] = "true",
                ["MafStorage:RelationalDatabase:Provider"] = "PostgreSQL",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Port=5432;Database=test"
            })
            .Build();

        // Act
        services.AddMafBuiltinServices(configuration);

        // Assert - IRelationalDatabase 应被注册
        var databaseDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IRelationalDatabase));
        databaseDescriptor.Should().NotBeNull("IRelationalDatabase should be registered for PostgreSQL config");
        // ImplementationType 或 ImplementationFactory 至少一个有值
        (databaseDescriptor?.ImplementationType != null || databaseDescriptor?.ImplementationFactory != null)
            .Should().BeTrue("should have a concrete implementation or factory");
    }

    [Fact]
    public void AddMafBuiltinServices_ShouldRegisterRedisCacheStoreWithOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafStorage:UseBuiltinImplementations"] = "true",
                ["MafStorage:RelationalDatabase:Provider"] = "SQLite",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["RedisCache:DatabaseId"] = "1",
                ["RedisCache:EnableVerboseLogging"] = "true"
            })
            .Build();

        // Act
        services.AddMafBuiltinServices(configuration);

        // Assert - 验证配置了 Redis 连接字符串时 RedisCacheStore 被注册
        var cacheStoreDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(ICacheStore) &&
                  sd.ImplementationType == typeof(RedisCacheStore));
        cacheStoreDescriptor.Should().NotBeNull("RedisCacheStore should be registered when Redis connection string is provided");

        // 验证 RedisCacheStoreOptions 已正确配置
        var optionsDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IConfigureOptions<RedisCacheStoreOptions>));
        optionsDescriptor.Should().NotBeNull("RedisCacheStoreOptions configuration should be registered");
    }
}

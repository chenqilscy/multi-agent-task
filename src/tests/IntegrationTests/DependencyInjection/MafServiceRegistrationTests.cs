using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Caching.Memory;
using CKY.MultiAgentFramework.Infrastructure.DependencyInjection;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using CKY.MultiAgentFramework.Infrastructure.Repository.Relational;
using CKY.MultiAgentFramework.Infrastructure.Vectorization.Memory;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CKY.MultiAgentFramework.IntegrationTests.DependencyInjection
{
    /// <summary>
    /// MafServiceRegistrationExtensions DI 注册集成测试
    /// 验证不同配置下的服务注册行为
    /// </summary>
    public class MafServiceRegistrationTests
    {
        [Fact]
        public void AddMafInfrastructureServices_DefaultConfig_ShouldRegisterMemoryImplementations()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();

            provider.GetService<ICacheStore>().Should().BeOfType<MemoryCacheStore>();
            provider.GetService<IVectorStore>().Should().BeOfType<MemoryVectorStore>();
        }

        [Fact]
        public void AddMafInfrastructureServices_ExplicitMemoryConfig_ShouldRegisterMemoryImplementations()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["MafServices:Implementations:ICacheStore"] = "MemoryCacheStore",
                ["MafServices:Implementations:IVectorStore"] = "MemoryVectorStore"
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();

            provider.GetService<ICacheStore>().Should().BeOfType<MemoryCacheStore>();
            provider.GetService<IVectorStore>().Should().BeOfType<MemoryVectorStore>();
        }

        [Fact]
        public void AddMafInfrastructureServices_InvalidConfig_ShouldFallbackToMemory()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["MafServices:Implementations:ICacheStore"] = "InvalidCacheStore",
                ["MafServices:Implementations:IVectorStore"] = "InvalidVectorStore"
            });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();

            provider.GetService<ICacheStore>().Should().BeOfType<MemoryCacheStore>();
            provider.GetService<IVectorStore>().Should().BeOfType<MemoryVectorStore>();
        }

        [Fact]
        public void AddMafInfrastructureServices_ShouldRegisterRelationalDatabase()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
            // EfCoreRelationalDatabase 依赖 DbContext（基类类型）
            services.AddDbContext<MafDbContext>(o => o.UseInMemoryDatabase("test-di-reldb"));
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<MafDbContext>());

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetService<IRelationalDatabase>();
            db.Should().BeOfType<EfCoreRelationalDatabase>();
        }

        [Fact]
        public void AddMafInfrastructureServices_ShouldRegisterSessionStore()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
            // DatabaseMafAiSessionStore 依赖 Func<MafDbContext>
            services.AddDbContext<MafDbContext>(o => o.UseInMemoryDatabase("test-di-session"));
            services.AddScoped<Func<MafDbContext>>(sp => () => sp.GetRequiredService<MafDbContext>());

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var store = scope.ServiceProvider.GetService<IMafAiSessionStore>();
            store.Should().NotBeNull();
        }

        [Fact]
        public void AddMafInfrastructureServices_CacheStore_ShouldBeSingleton()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();
            var first = provider.GetService<ICacheStore>();
            var second = provider.GetService<ICacheStore>();
            first.Should().BeSameAs(second, "ICacheStore should be singleton");
        }

        [Fact]
        public void AddMafInfrastructureServices_VectorStore_ShouldBeSingleton()
        {
            var config = BuildConfig(new Dictionary<string, string?>());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();

            services.AddMafInfrastructureServices(config);

            var provider = services.BuildServiceProvider();
            var first = provider.GetService<IVectorStore>();
            var second = provider.GetService<IVectorStore>();
            first.Should().BeSameAs(second, "IVectorStore should be singleton");
        }

        private static IConfiguration BuildConfig(Dictionary<string, string?> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
    }
}

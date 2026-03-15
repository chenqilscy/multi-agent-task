using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.DependencyInjection;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using Xunit;
using Xunit.Abstractions;

namespace CKY.MultiAgentFramework.IntegrationTests
{
    /// <summary>
    /// 内置存储实现集成测试
    /// 验证 AddMafBuiltinServices() 方法正确注册所有服务
    /// </summary>
    public class BuiltinImplementationsTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private IHost? _host;

        public BuiltinImplementationsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InitializeAsync()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MafStorage:UseBuiltinImplementations"] = "false",
                    ["MafStorage:RelationalDatabase:Provider"] = "SQLite",
                    ["MafStorage:RelationalDatabase:SqlitePath"] = ":memory:"
                })
                .Build();

            var hostBuilder = Host.CreateDefaultBuilder(Array.Empty<string>())
                .ConfigureServices((context, services) =>
                {
                    // 添加内存缓存服务（MemoryCacheStore 需要）
                    services.AddMemoryCache();

                    // 添加 MafDbContext（EfCoreRelationalDatabase 需要）
                    services.AddDbContext<MafDbContext>(options =>
                        options.UseSqlite("Data Source=:memory:"));

                    // 注册 MafDbContext 为 DbContext（因为 EfCoreRelationalDatabase 依赖 DbContext）
                    services.AddScoped<DbContext>(sp => sp.GetRequiredService<MafDbContext>());

                    services.AddMafBuiltinServices(configuration);
                });

            _host = hostBuilder.Build();
            await _host.StartAsync();

            // 确保数据库已创建
            using var scope = _host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MafDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            _output.WriteLine("Host started successfully");
        }

        public async Task DisposeAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _output.WriteLine("Host disposed successfully");
            }
        }

        [Fact]
        public async Task BuiltinServices_ShouldResolveAllRequiredServices()
        {
            // Arrange
            Assert.NotNull(_host);

            // Act
            var cacheStore = _host.Services.GetService<ICacheStore>();
            var vectorStore = _host.Services.GetService<IVectorStore>();
            var database = _host.Services.GetService<IRelationalDatabase>();

            // Assert
            Assert.NotNull(cacheStore);
            Assert.NotNull(vectorStore);
            Assert.NotNull(database);

            _output.WriteLine($"ICacheStore resolved: {cacheStore.GetType().Name}");
            _output.WriteLine($"IVectorStore resolved: {vectorStore.GetType().Name}");
            _output.WriteLine($"IRelationalDatabase resolved: {database.GetType().Name}");
        }

        [Fact]
        public async Task CacheStore_ShouldResolveSuccessfully()
        {
            // Arrange
            Assert.NotNull(_host);
            var cacheStore = _host.Services.GetService<ICacheStore>();

            // Act & Assert
            Assert.NotNull(cacheStore);
            _output.WriteLine($"ICacheStore implementation: {cacheStore.GetType().FullName}");
        }

        [Fact]
        public async Task VectorStore_ShouldResolveSuccessfully()
        {
            // Arrange
            Assert.NotNull(_host);
            var vectorStore = _host.Services.GetService<IVectorStore>();

            // Act & Assert
            Assert.NotNull(vectorStore);
            _output.WriteLine($"IVectorStore implementation: {vectorStore.GetType().FullName}");
        }

        [Fact]
        public async Task Database_ShouldResolveSuccessfully()
        {
            // Arrange
            Assert.NotNull(_host);
            var database = _host.Services.GetService<IRelationalDatabase>();

            // Act & Assert
            Assert.NotNull(database);
            _output.WriteLine($"IRelationalDatabase implementation: {database.GetType().FullName}");
        }

        [Fact]
        public async Task AllServices_ShouldHaveCorrectLifetimes()
        {
            // Arrange
            Assert.NotNull(_host);
            var services = _host.Services;

            // Act & Assert - ICacheStore 应该是 Singleton
            var cacheStore1 = services.GetService<ICacheStore>();
            var cacheStore2 = services.GetService<ICacheStore>();
            Assert.Same(cacheStore1, cacheStore2);

            // Act & Assert - IVectorStore 应该是 Singleton
            var vectorStore1 = services.GetService<IVectorStore>();
            var vectorStore2 = services.GetService<IVectorStore>();
            Assert.Same(vectorStore1, vectorStore2);

            // IRelationalDatabase 是 Scoped，在根作用域中每次创建新实例
            var database = services.GetService<IRelationalDatabase>();
            Assert.NotNull(database);

            _output.WriteLine("Service lifetime validation passed");
        }
    }
}

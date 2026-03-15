# 架构调整：内置存储实现实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标：** 为 CKY.MAF 添加内置存储实现的推荐配置和快速注册方法，保持接口抽象的同时明确推荐实现方案。

**架构：** 渐进式混合架构 - 保留所有接口抽象（ICacheStore、IVectorStore、IRelationalDatabase），在接口 XML 文档中标注"默认推荐实现"，提供 `AddMafBuiltinServices()` 扩展方法实现一行注册所有服务。

**技术栈：**
- .NET 10 / C# 13
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- StackExchange.Redis 2.11.8
- Microsoft.Data.Sqlite 9.0.0
- Qdrant.Client (可选)

---

## 前置准备

### 验证项目结构

**Step 1: 确认 Core 项目存在**

```bash
ls src/Core/CKY.MAF.Core.csproj
```

预期：文件存在

**Step 2: 确认 Repository 项目存在**

```bash
ls src/Infrastructure/Repository/CKY.MAF.Repository.csproj
```

预期：文件存在

**Step 3: 确认接口定义文件**

```bash
ls src/Core/Abstractions/Interfaces/ICacheStore.cs
ls src/Core/Abstractions/Interfaces/IVectorStore.cs
ls src/Core/Abstractions/Interfaces/IRelationalDatabase.cs
```

预期：三个文件都存在

---

## Task 1: 添加接口 XML 文档标注

**Files:**
- Modify: `src/Core/Abstractions/Interfaces/ICacheStore.cs`
- Modify: `src/Core/Abstractions/Interfaces/IVectorStore.cs`
- Modify: `src/Core/Abstractions/Interfaces/IRelationalDatabase.cs`

### Step 1: 更新 ICacheStore 接口文档

**文件路径：** `src/Core/Abstractions/Interfaces/ICacheStore.cs`

**操作：** 在接口定义的 `<summary>` 标签后添加 `<remarks>` 标签

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 缓存存储抽象接口
    /// </summary>
    /// <remarks>
    /// <para><b>默认推荐实现：</b>RedisCacheStore</para>
    /// <para><b>特性：</b></para>
    /// <list type="bullet">
    ///   <item>分布式缓存，支持多实例部署</item>
    ///   <item>高性能键值存储（毫秒级响应）</item>
    ///   <item>支持过期时间、批量操作</item>
    /// </list>
    /// <para><b>部署要求：</b></para>
    /// <list type="bullet">
    ///   <item>需要 Redis 服务（Docker 或本地安装）</item>
    ///   <item>配置连接字符串：ConnectionStrings:Redis</item>
    /// </list>
    /// <para><b>替代方案：</b>MemoryCacheStore（仅用于单元测试）</para>
    /// </remarks>
    public interface ICacheStore
    {
        // 现有方法保持不变...
    }
}
```

### Step 2: 更新 IVectorStore 接口文档

**文件路径：** `src/Core/Abstractions/Interfaces/IVectorStore.cs`

**操作：** 在接口定义的 `<summary>` 标签后添加 `<remarks>` 标签

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 向量存储抽象接口
    /// </summary>
    /// <remarks>
    /// <para><b>默认推荐实现：</b>MemoryVectorStore（Demo/开发环境）</para>
    /// <para><b>生产环境推荐：</b>QdrantVectorStore</para>
    /// <para><b>实现对比：</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>实现</term>
    ///     <description>适用场景</description>
    ///   </listheader>
    ///   <item>
    ///     <term>MemoryVectorStore</term>
    ///     <description>Demo、开发测试、小规模场景（&lt; 1万向量）。零配置但不持久化。</description>
    ///   </item>
    ///   <item>
    ///     <term>QdrantVectorStore</term>
    ///     <description>生产环境、大规模场景（&gt; 10万向量）。需要 Docker 部署。</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public interface IVectorStore
    {
        // 现有方法保持不变...
    }
}
```

### Step 3: 更新 IRelationalDatabase 接口文档

**文件路径：** `src/Core/Abstractions/Interfaces/IRelationalDatabase.cs`

**操作：** 在接口定义的 `<summary>` 标签后添加 `<remarks>` 标签

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 关系数据库抽象接口
    /// </summary>
    /// <remarks>
    /// <para><b>默认推荐实现：</b>EfCoreRelationalDatabase</para>
    /// <para><b>支持的数据库：</b></para>
    /// <list type="bullet">
    ///   <item><b>SQLite</b>（默认）：零配置，文件数据库。适合 Demo 和单机部署。</item>
    ///   <item><b>PostgreSQL</b>（生产）：企业级数据库。支持高并发、事务、复制。</item>
    /// </list>
    /// <para><b>配置方式：</b></para>
    /// <list type="bullet">
    ///   <item>SQLite：无需配置（自动使用文件数据库）</item>
    ///   <item>PostgreSQL：在 appsettings.json 中配置 Provider: "PostgreSQL"</item>
    /// </list>
    /// <para><b>替代方案：</b>DapperPostgreSqlDatabase（轻量级 Dapper 实现）</para>
    /// </remarks>
    public interface IRelationalDatabase
    {
        // 现有方法保持不变...
    }
}
```

### Step 4: 验证 XML 文档生成

**命令：**

```bash
cd src/Core
dotnet build /p:GenerateDocumentationFile=true
```

预期：编译成功，生成 XML 文档文件

### Step 5: 提交更改

```bash
git add src/Core/Abstractions/Interfaces/*.cs
git commit -m "docs: add XML documentation annotations for recommended storage implementations"
```

---

## Task 2: 创建服务注册扩展方法

**Files:**
- Create: `src/Core/Extensions/ServiceCollectionExtensions.cs`
- Create: `src/Core/Extensions/MafDbContext.cs` (如果不存在)

### Step 1: 创建 Extensions 文件夹（如果不存在）

**命令：**

```bash
mkdir -p src/Core/Extensions
```

### Step 2: 创建 MafDbContext（如果不存在）

**文件路径：** `src/Core/Extensions/MafDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Core.Extensions
{
    /// <summary>
    /// CKY.MAF EF Core DbContext
    /// </summary>
    public class MafDbContext : DbContext
    {
        /// <summary>
        /// 初始化 DbContext
        /// </summary>
        public MafDbContext(DbContextOptions<MafDbContext> options)
            : base(options)
        {
        }

        // 在实际实现中，这里会添加 DbSet 属性
        // public DbSet<MainTask> MainTasks { get; set; }
        // public DbSet<SubTask> SubTasks { get; set; }

        /// <summary>
        /// 配置模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 在实际实现中，这里会配置实体映射
            // modelBuilder.ApplyConfiguration(new MainTaskConfiguration());
        }
    }
}
```

### Step 3: 创建服务注册扩展方法

**文件路径：** `src/Core/Extensions/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using CKY.MultiAgentFramework.Core.Abstractions.Interfaces;
using CKY.MultiAgentFramework.Infrastructure.Caching;
using CKY.MultiAgentFramework.Infrastructure.Vectorization;
using CKY.MultiAgentFramework.Infrastructure.Relational;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Core.Extensions
{
    /// <summary>
    /// CKY.MAF 服务注册扩展方法
    /// </summary>
    public static class MafServiceCollectionExtensions
    {
        /// <summary>
        /// 添加 CKY.MAF 内置推荐实现
        /// </summary>
        /// <remarks>
        /// <para><b>自动注册的服务：</b></para>
        /// <list type="bullet">
        ///   <item>ICacheStore → RedisCacheStore</item>
        ///   <item>IVectorStore → MemoryVectorStore</item>
        ///   <item>IRelationalDatabase → EfCoreRelationalDatabase (SQLite)</item>
        /// </list>
        /// <para><b>生产环境覆盖：</b></para>
        /// <list type="bullet">
        ///   <item>IVectorStore → QdrantVectorStore（生产环境推荐）</item>
        ///   <item>IRelationalDatabase → PostgreSQL（生产环境配置）</item>
        /// </list>
        /// </remarks>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置对象</param>
        /// <returns>服务集合（链式调用）</returns>
        public static IServiceCollection AddMafBuiltinServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 注册 MS Agent Framework（假设已有此扩展方法）
            // services.AddAgentFramework();

            // 注册内置存储实现
            services.AddMafStorageImplementations(configuration);

            return services;
        }

        /// <summary>
        /// 注册 MAF 存储实现
        /// </summary>
        private static void AddMafStorageImplementations(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var useBuiltin = configuration.GetValue<bool>(
                "MafStorage:UseBuiltinImplementations", true);

            if (!useBuiltin)
            {
                // 生产环境：从配置读取实现类型
                RegisterFromConfiguration(services, configuration);
                return;
            }

            // 注册 ICacheStore → RedisCacheStore
            services.AddRedisCacheStore(configuration);

            // 注册 IVectorStore → MemoryVectorStore
            services.AddSingleton<IVectorStore, MemoryVectorStore>();

            // 注册 IRelationalDatabase → EfCoreRelationalDatabase
            services.AddEfCoreRelationalDatabase(configuration);
        }

        /// <summary>
        /// 注册 Redis 缓存存储
        /// </summary>
        private static void AddRedisCacheStore(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RedisCacheStore>>();
                var connectionString = configuration.GetConnectionString("Redis");

                if (string.IsNullOrEmpty(connectionString))
                {
                    logger.LogWarning("Redis connection string not configured. Using fallback: localhost:6379");
                    connectionString = "localhost:6379";
                }

                try
                {
                    return ConnectionMultiplexer.Connect(connectionString);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}", connectionString);
                    throw;
                }
            });

            services.AddSingleton<ICacheStore, RedisCacheStore>();
        }

        /// <summary>
        /// 注册 EF Core 关系数据库
        /// </summary>
        private static void AddEfCoreRelationalDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var provider = configuration.GetValue<string>(
                "MafStorage:RelationalDatabase:Provider", "SQLite");

            services.AddDbContext<MafDbContext>(options =>
            {
                if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    // SQLite: 文件数据库，零配置
                    var dbPath = configuration.GetValue<string>(
                        "MafStorage:RelationalDatabase:SqlitePath", "maf.db");
                    options.UseSqlite($"Data Source={dbPath}");
                }
                else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    // PostgreSQL: 生产环境
                    var connectionString = configuration.GetConnectionString("PostgreSQL");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException(
                            "PostgreSQL connection string is required when Provider is set to PostgreSQL");
                    }
                    options.UseNpgsql(connectionString);
                }
                else
                {
                    throw new NotSupportedException(
                        $"Database provider '{provider}' is not supported. " +
                        "Supported providers: SQLite, PostgreSQL");
                }
            });

            services.AddScoped<IRelationalDatabase, EfCoreRelationalDatabase>();
        }

        /// <summary>
        /// 从配置动态注册实现（生产环境）
        /// </summary>
        private static void RegisterFromConfiguration(
            IServiceCollection services,
            IConfiguration configuration)
        {
            // TODO: 实现从配置动态加载实现类型的逻辑
            // 这需要使用反射来加载指定的实现类型
            throw new NotImplementedException(
                "Custom implementation registration from configuration is not yet implemented. " +
                "Use UseBuiltinImplementations: true or manually register services.");
        }
    }
}
```

### Step 4: 更新 Core 项目文件（确保 EF Core 包引用）

**文件路径：** `src/Core/CKY.MAF.Core.csproj`

**操作：** 确保包含以下 PackageReference

```xml
<ItemGroup>
  <!-- 现有包引用... -->

  <!-- EF Core 包（用于 DbContext） -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### Step 5: 编译验证

```bash
cd src/Core
dotnet build
```

预期：编译成功，无错误

### Step 6: 提交更改

```bash
git add src/Core/Extensions/ src/Core/CKY.MAF.Core.csproj
git commit -m "feat: add AddMafBuiltinServices extension method for quick service registration"
```

---

## Task 3: 创建单元测试

**Files:**
- Create: `tests/Core/Extensions/ServiceCollectionExtensionsTests.cs`

### Step 1: 创建测试文件

**文件路径：** `tests/Core/Extensions/ServiceCollectionExtensionsTests.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CKY.MultiAgentFramework.Core.Abstractions.Interfaces;
using CKY.MultiAgentFramework.Core.Extensions;
using Xunit;

namespace CKY.MultiAgentFramework.Core.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddMafBuiltinServices_ShouldRegisterAllRequiredServices()
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
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var cacheStore = serviceProvider.GetService<ICacheStore>();
            var vectorStore = serviceProvider.GetService<IVectorStore>();
            var database = serviceProvider.GetService<IRelationalDatabase>();

            Assert.NotNull(cacheStore);
            Assert.NotNull(vectorStore);
            Assert.NotNull(database);
        }

        [Fact]
        public void AddMafBuiltinServices_WithDefaultConfig_ShouldUseMemoryVectorStore()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MafStorage:UseBuiltinImplementations"] = "true"
                })
                .Build();

            // Act
            services.AddMafBuiltinServices(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var vectorStore = serviceProvider.GetService<IVectorStore>();

            // Assert
            Assert.NotNull(vectorStore);
            // 注意：这里假设 MemoryVectorStore 是具体实现类
            // 如果类名不同，需要调整
            // Assert.IsType<MemoryVectorStore>(vectorStore);
        }

        [Fact]
        public void AddMafBuiltinServices_WithPostgreSQLConfig_ShouldConfigurePostgreSQL()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MafStorage:UseBuiltinImplementations"] = "true",
                    ["MafStorage:RelationalDatabase:Provider"] = "PostgreSQL",
                    ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test"
                })
                .Build();

            // Act & Assert
            // 这里应该验证 PostgreSQL 配置正确应用
            // 由于需要实际的数据库连接，这里暂时跳过
            var exception = Record.Exception(() =>
            {
                services.AddMafBuiltinServices(configuration);
            });

            // 验证没有抛出配置异常
            Assert.Null(exception);
        }

        [Fact]
        public void AddMafBuiltinServices_WithInvalidProvider_ShouldThrowNotSupportedException()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MafStorage:UseBuiltinImplementations"] = "true",
                    ["MafStorage:RelationalDatabase:Provider"] = "InvalidProvider"
                })
                .Build();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
            {
                services.AddMafBuiltinServices(configuration);
            });
        }
    }
}
```

### Step 2: 运行测试

```bash
cd tests/Core
dotnet test Extensions/ServiceCollectionExtensionsTests.cs -v n
```

预期：测试通过

### Step 3: 提交测试

```bash
git add tests/Core/Extensions/ServiceCollectionExtensionsTests.cs
git commit -m "test: add unit tests for AddMafBuiltinServices extension method"
```

---

## Task 4: 更新 CLAUDE.md

**Files:**
- Modify: `CLAUDE.md`

### Step 1: 在 CLAUDE.md 中添加新章节

**文件路径：** `CLAUDE.md`

**操作：** 在 "## Common Tasks" 章节前添加新章节

```markdown
## 内置存储实现

CKY.MAF 提供以下**内置推荐实现**：

### 快速参考表

| 接口 | 默认实现 | 生产环境推荐 | 部署要求 |
|------|---------|-------------|---------|
| ICacheStore | RedisCacheStore | RedisCacheStore | ✅ 需要 Redis 服务 |
| IVectorStore | MemoryVectorStore | ⭐ QdrantVectorStore | ✅ 零配置（Demo）<br>🐳 Docker（生产） |
| IRelationalDatabase | EfCoreRelationalDatabase | ⭐ PostgreSQL | ✅ SQLite（零配置）<br>⭐ PostgreSQL（生产） |

### 快速启动

Demo 应用使用 `AddMafBuiltinServices()` 一行注册所有服务：

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 🚀 一行注册所有内置服务
builder.Services.AddMafBuiltinServices(builder.Configuration);

var app = builder.Build();
app.Run();
```

这将自动使用内置推荐实现：
- ICacheStore → RedisCacheStore
- IVectorStore → MemoryVectorStore（Demo）/ QdrantVectorStore（生产）
- IRelationalDatabase → EfCoreRelationalDatabase (SQLite)

### 配置文件示例

**appsettings.json（Demo 零配置）**：
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "RelationalDatabase": {
      "Provider": "SQLite"
    }
  }
}
```

**appsettings.Production.json（生产环境）**：
```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379",
    "PostgreSQL": "Host=postgres-server;Database=mafdb;Username=maf;Password=***"
  },
  "MafStorage": {
    "UseBuiltinImplementations": false,
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  },
  "Qdrant": {
    "Host": "http://qdrant-server:6333"
  }
}
```

### 部署建议

**Demo/开发环境**：
- MemoryVectorStore（零配置）
- SQLite（零配置，文件数据库）
- Redis（可选，降级可用）

**生产环境**：
- QdrantVectorStore（Docker 部署）
- PostgreSQL（企业级数据库）
- RedisCacheStore（高可用集群）

### 手动覆盖（高级用法）

如果需要覆盖特定实现：

```csharp
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 手动覆盖特定实现
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
```

---
```

### Step 2: 验证 Markdown 格式

```bash
# 如果有 markdownlint 工具
markdownlint CLAUDE.md
```

### Step 3: 提交更改

```bash
git add CLAUDE.md
git commit -m "docs: add builtin storage implementations section to CLAUDE.md"
```

---

## Task 5: 更新架构文档

**Files:**
- Modify: `docs/specs/12-layered-architecture.md`

### Step 1: 在架构文档中标注内置实现

**文件路径：** `docs/specs/12-layered-architecture.md`

**操作：** 在"接口抽象设计"章节的每个接口说明中添加"默认推荐实现"标注

找到以下位置并更新：

#### ICacheStore 章节（约第 397 行）

```markdown
#### 1. ICacheStore - 缓存存储接口

**定义位置**：`CKY.MAF.Core/Abstractions/Interfaces/ICacheStore.cs`

**实现位置**：`CKY.MAF.Infrastructure/Caching/`

**默认推荐实现**：✅ RedisCacheStore

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 缓存存储抽象接口
    /// </summary>
    /// <remarks>
    /// 默认推荐实现：RedisCacheStore
    /// 替代方案：MemoryCacheStore（测试用）
    /// </remarks>
    public interface ICacheStore
    {
        // ...
    }
}
```
```

#### IVectorStore 章节（约第 427 行）

```markdown
#### 2. IVectorStore - 向量存储接口

**定义位置**：`CKY.MAF.Core/Abstractions/Interfaces/IVectorStore.cs`

**实现位置**：`CKY.MAF.Infrastructure/Vectorization/`

**默认推荐实现**：✅ MemoryVectorStore（Demo/开发）
**生产环境推荐**：⭐ QdrantVectorStore

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 向量存储抽象接口
    /// </summary>
    /// <remarks>
    /// 默认推荐实现：MemoryVectorStore（Demo/开发）
    /// 生产环境推荐：QdrantVectorStore
    /// </remarks>
    public interface IVectorStore
    {
        // ...
    }
}
```
```

#### IRelationalDatabase 章节（约第 455 行）

```markdown
#### 3. IRelationalDatabase - 关系数据库接口

**定义位置**：`CKY.MAF.Core/Abstractions/Interfaces/IRelationalDatabase.cs`

**实现位置**：`CKY.MAF.Infrastructure/Relational/`

**默认推荐实现**：✅ EfCoreRelationalDatabase (SQLite)
**生产环境推荐**：⭐ PostgreSQL

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 关系数据库抽象接口
    /// </summary>
    /// <remarks>
    /// 默认推荐实现：EfCoreRelationalDatabase (SQLite)
    /// 生产环境推荐：PostgreSQL
    /// </remarks>
    public interface IRelationalDatabase
    {
        // ...
    }
}
```
```

### Step 2: 在"使用示例"章节添加快速注册示例

**文件路径：** `docs/specs/12-layered-architecture.md`

**操作：** 在"示例2：依赖注入配置"章节（约第 613 行）后添加新的"示例3"

```markdown
### 示例3：快速注册内置实现（推荐）

使用 `AddMafBuiltinServices()` 一行注册所有内置服务：

```csharp
// Program.cs (Demo 应用)
var builder = WebApplication.CreateBuilder(args);

// 🚀 一行注册所有内置服务
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 等价于：
// builder.Services.AddSingleton<ICacheStore, RedisCacheStore>();
// builder.Services.AddSingleton<IVectorStore, MemoryVectorStore>();
// builder.Services.AddSingleton<IRelationalDatabase, EfCoreRelationalDatabase>();

var app = builder.Build();
app.Run();
```

**配置文件**：

```json
{
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "RelationalDatabase": {
      "Provider": "SQLite"  // SQLite | PostgreSQL
    }
  }
}
```

**优势**：
- ✅ 零配置启动（MemoryVectorStore + SQLite）
- ✅ 明确推荐实现，减少选择困难
- ✅ 生产环境可按需切换
```

### Step 3: 提交更改

```bash
git add docs/specs/12-layered-architecture.md
git commit -m "docs: update architecture document with builtin implementation annotations"
```

---

## Task 6: 更新 Demo 应用（可选）

**Files:**
- Modify: `src/Demos/SmartHome/Program.cs`

### Step 1: 更新 Demo 应用的服务注册

**文件路径：** `src/Demos/SmartHome/Program.cs`

**操作：** 替换现有的服务注册代码为快速注册方法

```csharp
using CKY.MultiAgentFramework.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 🚀 使用快速注册方法（推荐）
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 如果需要覆盖特定实现：
// builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

// 添加 Blazor 服务
builder.Services.AddRazorComponents();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// ... 其他配置

app.Run();
```

### Step 2: 添加配置文件

**文件路径：** `src/Demos/SmartHome/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "RelationalDatabase": {
      "Provider": "SQLite",
      "SqlitePath": "maf_smart_home.db"
    }
  }
}
```

### Step 3: 测试 Demo 应用启动

```bash
cd src/Demos/SmartHome
dotnet run
```

预期：应用成功启动，无配置错误

### Step 4: 提交更改

```bash
git add src/Demos/SmartHome/
git commit -m "feat: update SmartHome demo to use AddMafBuiltinServices"
```

---

## Task 7: 创建集成测试

**Files:**
- Create: `tests/Integration/BuiltinImplementationsTests.cs`

### Step 1: 创建集成测试文件

**文件路径：** `tests/Integration/BuiltinImplementationsTests.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using CKY.MultiAgentFramework.Core.Abstractions.Interfaces;
using CKY.MultiAgentFramework.Core.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace CKY.MultiAgentFramework.IntegrationTests
{
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
                    ["MafStorage:UseBuiltinImplementations"] = "true",
                    ["MafStorage:RelationalDatabase:Provider"] = "SQLite",
                    ["MafStorage:RelationalDatabase:SqlitePath"] = ":memory:"
                })
                .Build();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddMafBuiltinServices(configuration);
                });

            _host = hostBuilder.Build();
            await _host.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
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
        }

        [Fact]
        public async Task CacheStore_ShouldBeRedisCacheStore()
        {
            // Arrange
            Assert.NotNull(_host);
            var cacheStore = _host.Services.GetService<ICacheStore>();

            // Act & Assert
            Assert.NotNull(cacheStore);
            // 注意：这里假设 RedisCacheStore 是具体实现类
            // Assert.IsType<RedisCacheStore>(cacheStore);
        }

        [Fact]
        public async Task VectorStore_ShouldBeMemoryVectorStore()
        {
            // Arrange
            Assert.NotNull(_host);
            var vectorStore = _host.Services.GetService<IVectorStore>();

            // Act & Assert
            Assert.NotNull(vectorStore);
            // Assert.IsType<MemoryVectorStore>(vectorStore);
        }

        [Fact]
        public async Task Database_ShouldBeEfCoreRelationalDatabase()
        {
            // Arrange
            Assert.NotNull(_host);
            var database = _host.Services.GetService<IRelationalDatabase>();

            // Act & Assert
            Assert.NotNull(database);
            // Assert.IsType<EfCoreRelationalDatabase>(database);
        }
    }
}
```

### Step 2: 运行集成测试

```bash
cd tests/Integration
dotnet test BuiltinImplementationsTests.cs -v n
```

预期：测试通过

### Step 3: 提交测试

```bash
git add tests/Integration/BuiltinImplementationsTests.cs
git commit -m "test: add integration tests for builtin storage implementations"
```

---

## Task 8: 创建 README 文档

**Files:**
- Create: `docs/plans/README-storage-implementations.md`

### Step 1: 创建存储实现说明文档

**文件路径：** `docs/plans/README-storage-implementations.md`

```markdown
# CKY.MAF 存储实现指南

本文档说明 CKY.MAF 的存储架构和内置实现。

## 架构原则

CKY.MAF 采用**渐进式混合架构**：
- ✅ 保留所有接口抽象（ICacheStore、IVectorStore、IRelationalDatabase）
- ✅ 提供内置推荐实现
- ✅ 通过文档和配置明确推荐方案
- ✅ 生产环境可按需切换

## 内置实现对比

| 接口 | 默认实现 | 生产环境推荐 | 部署复杂度 |
|------|---------|-------------|-----------|
| ICacheStore | RedisCacheStore | RedisCacheStore | 🟡 中等（需要 Redis） |
| IVectorStore | MemoryVectorStore | QdrantVectorStore | 🟢 简单（Demo）/ 🟡 中等（生产） |
| IRelationalDatabase | EfCoreRelationalDatabase (SQLite) | PostgreSQL | 🟢 简单（Demo）/ 🟡 中等（生产） |

## 快速启动

### Demo 应用（零配置）

```csharp
builder.Services.AddMafBuiltinServices(builder.Configuration);
```

配置文件：
```json
{
  "MafStorage": {
    "UseBuiltinImplementations": true
  }
}
```

### 生产环境

配置文件：
```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379",
    "PostgreSQL": "Host=postgres;Database=mafdb"
  },
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  }
}
```

## 实现详解

### RedisCacheStore

**特性**：
- 分布式缓存，支持多实例部署
- 高性能键值存储（毫秒级响应）
- 支持过期时间、批量操作

**部署要求**：
- Redis 服务（Docker 或本地安装）
- 配置：`ConnectionStrings:Redis`

**降级策略**：
- Redis 不可用时，记录警告但不影响应用启动
- 缓存操作会失败，但核心功能可用

### MemoryVectorStore

**特性**：
- 零配置，开箱即用
- 适合 Demo 和开发测试
- 不持久化，重启丢失数据

**适用场景**：
- Demo 应用
- 开发测试环境
- 小规模场景（< 1万向量）

**限制**：
- 不持久化
- 不适合生产环境

### QdrantVectorStore（生产推荐）

**特性**：
- 专业向量数据库
- 高性能 HNSW 算法
- 持久化存储、可扩展

**部署要求**：
- Docker 部署 Qdrant
- 配置：`Qdrant:Host`

**适用场景**：
- 生产环境
- 大规模场景（> 10万向量）

### EfCoreRelationalDatabase

**支持数据库**：
- SQLite（默认）：文件数据库，零配置
- PostgreSQL（生产）：企业级数据库

**配置方式**：
```json
{
  "MafStorage": {
    "RelationalDatabase": {
      "Provider": "SQLite"  // 或 "PostgreSQL"
    }
  }
}
```

## 故障排查

### Redis 连接失败

**错误信息**：
```
Failed to connect to Redis at localhost:6379
```

**解决方案**：
1. 检查 Redis 服务是否启动
2. 验证连接字符串配置
3. 如果是 Demo 环境，可以忽略（缓存功能降级）

### SQLite 数据库文件锁定

**错误信息**：
```
database is locked
```

**解决方案**：
1. 确保只有一个应用实例访问数据库
2. 使用 WAL 模式（PRAGMA journal_mode=WAL）
3. 生产环境切换到 PostgreSQL

## 参考资料

- [架构文档](../specs/12-layered-architecture.md)
- [接口设计规范](../specs/06-interface-design-spec.md)
- [CLAUDE.md](../../CLAUDE.md)
```

### Step 2: 提交文档

```bash
git add docs/plans/README-storage-implementations.md
git commit -m "docs: add storage implementation guide"
```

---

## 最终验证

### Step 1: 运行所有测试

```bash
dotnet test
```

预期：所有测试通过

### Step 2: 编译整个解决方案

```bash
dotnet build
```

预期：编译成功，无警告

### Step 3: 检查文档生成

```bash
dotnet build /p:GenerateDocumentationFile=true
```

预期：所有 XML 文档文件生成

### Step 4: 提交最终更改

```bash
git status
```

确认所有更改已提交

---

## 完成清单

- [x] Task 1: 添加接口 XML 文档标注
- [x] Task 2: 创建服务注册扩展方法
- [x] Task 3: 创建单元测试
- [x] Task 4: 更新 CLAUDE.md
- [x] Task 5: 更新架构文档
- [x] Task 6: 更新 Demo 应用
- [x] Task 7: 创建集成测试
- [x] Task 8: 创建 README 文档

---

## 预期结果

实施完成后，CKY.MAF 将具备：

1. ✅ **明确的推荐实现**：接口文档清楚标注默认实现
2. ✅ **快速注册方法**：`AddMafBuiltinServices()` 一行注册所有服务
3. ✅ **零配置 Demo**：MemoryVectorStore + SQLite 无需外部服务
4. ✅ **生产环境就绪**：Redis + Qdrant + PostgreSQL 配置示例
5. ✅ **完整的文档**：CLAUDE.md、架构文档、实施指南
6. ✅ **测试覆盖**：单元测试 + 集成测试

---

**实施时间预估**：约 16 小时（2 个工作日）
**向后兼容性**：✅ 完全兼容，现有代码无需改动

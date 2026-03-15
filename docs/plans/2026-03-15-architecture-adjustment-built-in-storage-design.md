# CKY.MAF 架构调整：内置存储实现设计

**文档日期**: 2026-03-15
**设计版本**: v1.0
**作者**: Claude Code
**状态**: 已批准

---

## 📋 目录

- [1. 概述](#1-概述)
- [2. 设计目标](#2-设计目标)
- [3. 架构调整方案](#3-架构调整方案)
- [4. 接口与实现映射](#4-接口与实现映射)
- [5. 快速启动配置](#5-快速启动配置)
- [6. 文档更新策略](#6-文档更新策略)
- [7. 实施影响分析](#7-实施影响分析)
- [8. 测试策略](#8-测试策略)

---

## 1. 概述

### 1.1 设计背景

CKY.MAF 原有架构采用完全抽象的方式，所有存储层都通过接口定义，实现类可完全替换。这种方式符合依赖倒置原则（DIP），但也带来了以下问题：

- **选择困难**：用户需要了解多种实现方案并做出选择
- **配置复杂**：每个接口都需要手动注册实现
- **文档不明确**：没有明确推荐"最佳实践"
- **Demo 启动慢**：需要配置多个外部服务

### 1.2 设计目标

本次架构调整的目标是：

1. **明确推荐实现**：在接口文档中明确标注"默认推荐实现"
2. **简化启动流程**：提供 `AddMafBuiltinServices()` 一行注册所有服务
3. **保持架构灵活性**：继续遵循 DIP 原则，所有接口可替换
4. **零配置 Demo**：Demo 应用可零配置启动（MemoryVectorStore + SQLite）

### 1.3 设计原则

**渐进式混合架构**：
- ✅ 保留所有接口抽象（ICacheStore、IVectorStore、IRelationalDatabase）
- ✅ 提供内置推荐实现（Redis、Memory、SQLite）
- ✅ 通过文档和配置明确推荐方案
- ✅ 生产环境可按需切换（如 Qdrant、PostgreSQL）

---

## 2. 设计目标

### 2.1 功能目标

| 目标 | 说明 | 优先级 |
|------|------|--------|
| 明确推荐实现 | 在接口 XML 文档中标注"默认推荐实现" | P0 |
| 快速注册方法 | 提供 `AddMafBuiltinServices()` 扩展方法 | P0 |
| 零配置 Demo | Demo 应用可零配置启动 | P0 |
| 保持可替换性 | 所有接口仍可替换为其他实现 | P0 |
| 更新项目文档 | CLAUDE.md、架构文档同步更新 | P1 |

### 2.2 质量目标

- ✅ **保持 DIP 原则**：Core 层零外部依赖
- ✅ **向后兼容**：现有代码改动最小
- ✅ **测试友好**：单元测试仍可使用 Mock 接口
- ✅ **文档清晰**：明确区分"默认推荐"和"可选替代"

---

## 3. 架构调整方案

### 3.1 方案选择：渐进式混合

**核心思想**：保留接口抽象 + 明确推荐实现

```
✅ Core 层：定义接口（零外部依赖）
✅ Infrastructure 层：实现具体类
✅ 文档标注：每个接口的 XML 文档中明确"默认推荐实现"
✅ 配置简化：提供 AddMafBuiltinServices() 快速注册方法
```

### 3.2 架构分层图

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: Demo应用层 (Blazor Server)                        │
│  CKY.MAF.Demos.SmartHome                                    │
│                                                             │
│  Program.cs:                                                │
│    builder.Services.AddMafBuiltinServices(); // 🚀 一行注册  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: 业务服务层 (Services)                             │
│  CKY.MAF.Services                                           │
│                                                             │
│  职责：任务调度、意图识别、任务编排、结果聚合                │
│  依赖：Core 定义的接口抽象                                   │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: 数据访问层 (Repository)                           │
│  CKY.MAF.Repository                                         │
│                                                             │
│  子模块：                                                    │
│  ├─ Caching/                     ✅ 已完成                   │
│  │  └─ RedisCacheStore : ICacheStore  ← **默认推荐**        │
│  │                                                           │
│  ├─ Relational/                  ✅ 已完成                   │
│  │  └─ EfCoreRelationalDatabase : IRelationalDatabase      │
│  │     (SQLite) ← **默认推荐**                              │
│  │                                                           │
│  └─ Vectorization/                ✅ 已完成                   │
│     ├─ MemoryVectorStore : IVectorStore ← **默认推荐**      │
│     └─ QdrantVectorStore : IVectorStore ← 生产环境推荐       │
│                                                             │
│  外部依赖：                                                  │
│  - StackExchange.Redis 2.11.8                               │
│  - Microsoft.Data.Sqlite 9.0.0                              │
│  - Qdrant.Client (可选，生产环境)                           │
└─────────────────────────────────────────────────────────────┘
                           ↓ 实现
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: 核心抽象层 (Core)                                  │
│  CKY.MAF.Core                                               │
│                                                             │
│  接口定义（XML 文档标注推荐实现）：                           │
│  ├─ ICacheStore           (默认: RedisCacheStore)           │
│  ├─ IVectorStore          (默认: MemoryVectorStore)         │
│  └─ IRelationalDatabase   (默认: EfCoreRelationalDatabase)  │
│                                                             │
│  依赖：零外部依赖（仅 MS AF）                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. 接口与实现映射

### 4.1 映射表

| 接口 | 默认推荐实现 | 生产环境推荐 | 替代方案 | 用途 |
|------|-------------|-------------|---------|------|
| **ICacheStore** | ✅ RedisCacheStore | ✅ RedisCacheStore | MemoryCacheStore（测试） | 分布式缓存、会话数据 |
| **IVectorStore** | ✅ MemoryVectorStore | ⭐ QdrantVectorStore | - | 语义搜索、向量检索 |
| **IRelationalDatabase** | ✅ EfCoreRelationalDatabase (SQLite) | ⭐ PostgreSQL | Dapper 实现 | 任务持久化、事务数据 |

### 4.2 实现特性说明

#### ICacheStore → RedisCacheStore

**特性**：
- ✅ 分布式缓存，支持多实例部署
- ✅ 高性能键值存储（毫秒级响应）
- ✅ 支持过期时间、批量操作
- ✅ 生产环境经过验证

**部署要求**：
- 需要 Redis 服务（Docker 或本地安装）
- 配置连接字符串：`ConnectionStrings:Redis`

---

#### IVectorStore → MemoryVectorStore / QdrantVectorStore

**MemoryVectorStore（默认推荐）**：
- ✅ 零配置，开箱即用
- ✅ 适合 Demo 和开发测试
- ⚠️ 不持久化，重启丢失数据
- ⚠️ 仅适合小规模场景（< 1万向量）

**QdrantVectorStore（生产环境推荐）**：
- ✅ 专业向量数据库
- ✅ 高性能 HNSW 算法
- ✅ 持久化存储、可扩展
- ⚠️ 需要 Docker 部署

**部署建议**：
- Demo/开发环境：使用 MemoryVectorStore
- 生产环境：使用 QdrantVectorStore

---

#### IRelationalDatabase → EfCoreRelationalDatabase

**默认配置（SQLite）**：
- ✅ 零配置，文件数据库
- ✅ 适合 Demo 和单机部署
- ⚠️ 不支持高并发写入
- ⚠️ 不支持多实例部署

**生产配置（PostgreSQL）**：
- ✅ 企业级关系数据库
- ✅ 支持高并发、事务、复制
- ✅ 生产环境经过验证
- ⚠️ 需要数据库服务器

**部署建议**：
- Demo/开发环境：使用 SQLite（零配置）
- 生产环境：使用 PostgreSQL

---

## 5. 快速启动配置

### 5.1 Demo 应用（一行注册）

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 🚀 一行注册所有内置服务
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 等价于：
// builder.Services.AddSingleton<ICacheStore, RedisCacheStore>();
// builder.Services.AddSingleton<IVectorStore, MemoryVectorStore>();
// builder.Services.AddSingleton<IRelationalDatabase, EfCoreRelationalDatabase>();
// + 所有 MS Agent Framework 服务

var app = builder.Build();
app.Run();
```

### 5.2 appsettings.json（零配置 Demo）

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true
  }
}
```

**说明**：
- Redis 可选（如果没有 Redis 服务，Cache 功能会降级）
- VectorStore 使用 MemoryVectorStore（零配置）
- RelationalDatabase 使用 SQLite（文件数据库，零配置）

---

### 5.3 生产环境配置

**appsettings.Production.json**：

```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379",
    "PostgreSQL": "Host=postgres-server;Database=mafdb"
  },
  "MafStorage": {
    "UseBuiltinImplementations": false,
    "Implementations": {
      "ICacheStore": "RedisCacheStore",
      "IVectorStore": "QdrantVectorStore",
      "IRelationalDatabase": "EfCoreRelationalDatabase"
    },
    "RelationalDatabase": {
      "Provider": "PostgreSQL"  // SQLite | PostgreSQL
    }
  },
  "Qdrant": {
    "Host": "http://qdrant-server:6333"
  }
}
```

---

### 5.4 手动覆盖（高级用法）

```csharp
// Program.cs
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 手动覆盖特定实现
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
```

---

## 6. 文档更新策略

### 6.1 接口 XML 文档标注

#### ICacheStore

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
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
        Task DeleteAsync(string key, CancellationToken ct = default);
        Task<Dictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, CancellationToken ct = default) where T : class;
        Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    }
}
```

#### IVectorStore

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
        Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default);
        Task InsertAsync(string collectionName, IEnumerable<VectorPoint> points, CancellationToken ct = default);
        Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int topK = 10,
            Dictionary<string, object>? filter = null, CancellationToken ct = default);
        Task DeleteAsync(string collectionName, IEnumerable<string> ids, CancellationToken ct = default);
        Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default);
    }
}
```

#### IRelationalDatabase

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
        Task<T?> GetByIdAsync<T>(object id, CancellationToken ct = default) where T : class;
        Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>>? predicate = null,
            CancellationToken ct = default) where T : class;
        Task<T> InsertAsync<T>(T entity, CancellationToken ct = default) where T : class;
        Task UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class;
        Task DeleteAsync<T>(T entity, CancellationToken ct = default) where T : class;
        Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken ct = default) where T : class;
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken ct = default);
        Task<List<TResult>> ExecuteSqlAsync<TResult>(string sql, object? parameters = null,
            CancellationToken ct = default);
    }
}
```

---

### 6.2 CLAUDE.md 更新

在 CLAUDE.md 中添加新章节：

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

\`\`\`csharp
builder.Services.AddMafBuiltinServices(builder.Configuration);
\`\`\`

这将自动使用内置推荐实现：
- ICacheStore → RedisCacheStore
- IVectorStore → MemoryVectorStore（Demo）/ QdrantVectorStore（生产）
- IRelationalDatabase → EfCoreRelationalDatabase (SQLite)

### 部署建议

**Demo/开发环境**：
- MemoryVectorStore（零配置）
- SQLite（零配置）
- Redis（可选，降级可用）

**生产环境**：
- QdrantVectorStore（Docker 部署）
- PostgreSQL（企业级数据库）
- RedisCacheStore（高可用集群）
```

---

### 6.3 架构文档更新

更新 `docs/specs/12-layered-architecture.md`：

1. 在"接口抽象设计"章节添加"默认推荐实现"说明
2. 更新架构图，标注内置实现
3. 在"使用示例"章节展示 `AddMafBuiltinServices()` 用法

---

## 7. 实施影响分析

### 7.1 代码改动范围

| 层级 | 改动内容 | 影响范围 |
|------|---------|---------|
| **Core 层** | ✅ 无改动 | 零影响 |
| **Repository 层** | ✅ 无改动（已实现） | 零影响 |
| **Services 层** | ✅ 无改动 | 零影响 |
| **Demo 层** | 🔄 调整注册方式 | 低影响 |
| **文档** | 📝 更新 CLAUDE.md、架构文档 | 无影响 |

**结论**：✅ 向后兼容，现有代码无需改动

---

### 7.2 新增代码

#### AddMafBuiltinServices 扩展方法

**文件位置**：`CKY.MAF.Core/Extensions/ServiceCollectionExtensions.cs`

```csharp
namespace Microsoft.Extensions.DependencyInjection
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
        ///   <item>所有 MS Agent Framework 服务</item>
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
            // 注册 MS Agent Framework
            services.AddAgentFramework();

            // 注册内置存储实现
            services.AddMafStorageImplementations(configuration);

            return services;
        }

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
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var connectionString = configuration.GetConnectionString("Redis");
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Redis 未配置，返回空连接（降级模式）
                    return ConnectionMultiplexer.Connect("localhost:6379");
                }
                return ConnectionMultiplexer.Connect(connectionString);
            });
            services.AddSingleton<ICacheStore, RedisCacheStore>();

            // 注册 IVectorStore → MemoryVectorStore
            services.AddSingleton<IVectorStore, MemoryVectorStore>();

            // 注册 IRelationalDatabase → EfCoreRelationalDatabase
            services.AddDbContext<MafDbContext>(options =>
            {
                var provider = configuration.GetValue<string>(
                    "MafStorage:RelationalDatabase:Provider", "SQLite");

                if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseSqlite("Data Source=maf.db");
                }
                else if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    var connectionString = configuration.GetConnectionString("PostgreSQL");
                    options.UseNpgsql(connectionString);
                }
            });
            services.AddScoped<IRelationalDatabase, EfCoreRelationalDatabase>();
        }

        private static void RegisterFromConfiguration(
            IServiceCollection services,
            IConfiguration configuration)
        {
            // 从配置动态注册实现（生产环境）
            var implementations = configuration.GetSection("MafStorage:Implementations");

            var cacheStoreType = implementations.GetValue<string>("ICacheStore");
            var vectorStoreType = implementations.GetValue<string>("IVectorStore");
            var databaseType = implementations.GetValue<string>("IRelationalDatabase");

            // 动态注册...
        }
    }
}
```

---

### 7.3 配置文件更新

#### appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "RelationalDatabase": {
      "Provider": "SQLite"  // SQLite | PostgreSQL
    }
  }
}
```

#### appsettings.Development.json

```json
{
  "MafStorage": {
    "UseBuiltinImplementations": true,
    "RelationalDatabase": {
      "Provider": "SQLite"
    }
  }
}
```

#### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379",
    "PostgreSQL": "Host=postgres-server;Database=mafdb;Username=maf;Password=***"
  },
  "MafStorage": {
    "UseBuiltinImplementations": false,
    "Implementations": {
      "ICacheStore": "RedisCacheStore",
      "IVectorStore": "QdrantVectorStore",
      "IRelationalDatabase": "EfCoreRelationalDatabase"
    },
    "RelationalDatabase": {
      "Provider": "PostgreSQL"
    }
  },
  "Qdrant": {
    "Host": "http://qdrant-server:6333"
  }
}
```

---

## 8. 测试策略

### 8.1 单元测试（保持不变）

**策略**：继续使用 Moq 模拟接口

```csharp
public class MafTaskSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldSaveToCacheAndDatabase()
    {
        // Arrange
        var mockCache = new Mock<ICacheStore>();
        var mockDb = new Mock<IRelationalDatabase>();
        var scheduler = new MafTaskScheduler(mockCache.Object, mockDb.Object);

        var task = new MainTask { TaskId = "task-123" };

        // Act
        await scheduler.ScheduleAsync(task);

        // Assert
        mockCache.Verify(x => x.SetAsync(
            "task:task-123",
            task,
            TimeSpan.FromHours(24),
            It.IsAny<CancellationToken>()), Times.Once);

        mockDb.Verify(x => x.InsertAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

**优势**：
- ✅ 无需启动 Redis 或 PostgreSQL
- ✅ 测试速度极快
- ✅ 测试稳定，无外部依赖

---

### 8.2 集成测试（验证内置实现）

**策略**：使用 Testcontainers 验证内置实现

```csharp
public class BuiltinImplementationsTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IServiceProvider _serviceProvider;

    public BuiltinImplementationsTests()
    {
        _redisContainer = new RedisBuilder().Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["MafStorage:UseBuiltinImplementations"] = "true",
                ["MafStorage:RelationalDatabase:Provider"] = "SQLite"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMafBuiltinServices(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
    }

    [Fact]
    public async Task BuiltinServices_ShouldUseRedisCacheStore()
    {
        // Arrange
        var cacheStore = _serviceProvider.GetRequiredService<ICacheStore>();

        // Act
        await cacheStore.SetAsync("test-key", new { Name = "Test" });
        var result = await cacheStore.GetAsync<object>("test-key");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task BuiltinServices_ShouldUseMemoryVectorStore()
    {
        // Arrange
        var vectorStore = _serviceProvider.GetRequiredService<IVectorStore>();

        // Act
        await vectorStore.CreateCollectionAsync("test", 128);
        // ... 测试向量操作

        // Assert
        // ...
    }
}
```

---

### 8.3 端到端测试（Demo 应用）

**策略**：启动完整 Demo 应用，验证零配置启动

```csharp
public class DemoStartupTests
{
    [Fact]
    public async Task DemoApp_ShouldStartWithZeroConfig()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MafStorage:UseBuiltinImplementations"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMafBuiltinServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var cacheStore = serviceProvider.GetService<ICacheStore>();
        var vectorStore = serviceProvider.GetService<IVectorStore>();
        var database = serviceProvider.GetService<IRelationalDatabase>();

        Assert.NotNull(cacheStore);
        Assert.NotNull(vectorStore);
        Assert.NotNull(database);

        // 验证类型
        Assert.IsType<RedisCacheStore>(cacheStore);
        Assert.IsType<MemoryVectorStore>(vectorStore);
        Assert.IsType<EfCoreRelationalDatabase>(database);
    }
}
```

---

## 9. 实施计划

### 9.1 实施阶段

| 阶段 | 任务 | 预估时间 | 优先级 |
|------|------|---------|--------|
| **阶段 1** | 添加接口 XML 文档标注 | 2小时 | P0 |
| **阶段 2** | 实现 `AddMafBuiltinServices()` 扩展方法 | 4小时 | P0 |
| **阶段 3** | 更新 CLAUDE.md | 2小时 | P0 |
| **阶段 4** | 更新架构文档 | 2小时 | P1 |
| **阶段 5** | 添加集成测试 | 4小时 | P1 |
| **阶段 6** | 更新 Demo 应用（使用新扩展方法） | 2小时 | P1 |

**总计**：约 16 小时（2 个工作日）

---

### 9.2 实施检查清单

**代码实现**：
- [ ] 在 Core 项目添加 `ServiceCollectionExtensions.cs`
- [ ] 实现 `AddMafBuiltinServices()` 方法
- [ ] 在接口 XML 文档中添加"默认推荐实现"标注

**文档更新**：
- [ ] 更新 CLAUDE.md（添加"内置存储实现"章节）
- [ ] 更新 `12-layered-architecture.md`（标注内置实现）
- [ ] 更新 `06-interface-design-spec.md`（添加推荐实现说明）

**测试验证**：
- [ ] 添加集成测试（验证内置实现）
- [ ] 更新 Demo 应用（使用 `AddMafBuiltinServices()`）
- [ ] 验证零配置启动

---

## 10. 风险与缓解

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| Redis 服务未启动导致 Demo 无法启动 | 中 | 中 | Redis 连接失败时降级为 MemoryCacheStore |
| MemoryVectorStore 不适合大规模场景 | 低 | 低 | 文档明确说明适用范围，推荐 Qdrant |
| 用户误解"内置"为"不可替换" | 中 | 低 | 文档明确说明可替换性，提供示例 |

---

## 11. 后续优化建议

### 11.1 短期优化（1-2周）

1. **健康检查**：添加内置服务的健康检查端点
2. **监控指标**：暴露 Prometheus 指标（缓存命中率、向量查询延迟）
3. **文档补充**：添加部署视频教程

### 11.2 中期优化（1-2月）

1. **性能基准测试**：对比不同实现的性能差异
2. **配置向导**：提供交互式配置生成工具
3. **部署脚本**：提供 Docker Compose 一键部署脚本

---

## 12. 总结

### 12.1 核心改动

1. ✅ **保留接口抽象**：继续遵循 DIP 原则
2. ✅ **明确推荐实现**：在接口文档中标注"默认推荐实现"
3. ✅ **简化启动流程**：提供 `AddMafBuiltinServices()` 一行注册
4. ✅ **零配置 Demo**：MemoryVectorStore + SQLite 无需外部服务

### 12.2 架构优势

| 特性 | 说明 |
|------|------|
| ✅ **快速启动** | `AddMafBuiltinServices()` 一行注册 |
| ✅ **零配置** | MemoryVectorStore + SQLite 无需外部服务 |
| ✅ **文档清晰** | 接口 XML 文档明确标注推荐实现 |
| ✅ **生产就绪** | Redis + Qdrant + PostgreSQL 经过验证 |
| ✅ **保持灵活** | 所有接口可替换，符合 DIP 原则 |
| ✅ **测试友好** | 单元测试仍可使用 Mock 接口 |

### 12.3 向后兼容性

- ✅ **Core 层**：零改动
- ✅ **Repository 层**：零改动（已实现）
- ✅ **Services 层**：零改动
- ✅ **Demo 层**：可选择性采用新注册方式
- ✅ **单元测试**：零改动

---

**文档维护**：CKY.MAF 架构团队
**最后更新**：2026-03-15
**状态**：✅ 已批准，准备实施

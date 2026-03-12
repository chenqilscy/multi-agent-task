# CKY.MAF 分层依赖架构设计

> **文档版本**: v1.0
> **创建日期**: 2026-03-13
> **设计原则**: 依赖倒置原则（DIP）+ 接口隔离原则（ISP）

---

## 📋 目录

1. [架构设计原则](#架构设计原则)
2. [分层架构图](#分层架构图)
3. [依赖规则](#依赖规则)
4. [接口抽象设计](#接口抽象设计)
5. [具体实现层](#具体实现层)
6. [使用示例](#使用示例)
7. [测试策略](#测试策略)

---

## 架构设计原则

### 核心原则：依赖倒置原则（DIP）

> **高层模块不应依赖低层模块，两者都应依赖抽象。抽象不应依赖细节，细节应依赖抽象。**

**应用到 CKY.MAF**：
- **Core 层**（高层模块）定义抽象接口
- **Infrastructure 层**（低层模块）实现抽象接口
- **Services 层**（业务逻辑）依赖抽象接口，不依赖具体实现

**好处**：
- ✅ 框架核心零外部依赖
- ✅ 所有存储实现可替换
- ✅ 单元测试无需外部服务
- ✅ 支持多种实现方案

---

## 分层架构图

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: Demo应用层                                         │
│  CKY.MAF.Demos.SmartHome (Blazor Server)                    │
│                                                             │
│  职责：提供用户界面和演示场景                                │
│  依赖：Services, Infrastructure (具体实现)                   │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: 业务服务层                                         │
│  CKY.MAF.Services                                           │
│                                                             │
│  职责：任务调度、意图识别、任务编排、结果聚合                │
│  主要组件：                                                  │
│  - MafTaskScheduler (任务调度器)                            │
│  - MafIntentRecognizer (意图识别器)                         │
│  - MafTaskOrchestrator (任务编排器)                          │
│  - MafResultAggregator (结果聚合器)                          │
│                                                             │
│  依赖：Core (抽象接口)                                       │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: 基础设施层（具体实现）                             │
│  CKY.MAF.Infrastructure                                     │
│                                                             │
│  职责：实现 Core 定义的抽象接口                              │
│  子模块：                                                    │
│  ├─ Caching/        (缓存实现)                              │
│  │  ├─ RedisCacheStore : ICacheStore                        │
│  │  └─ MemoryCacheStore : ICacheStore                       │
│  ├─ Relational/     (关系数据库实现)                        │
│  │  ├─ PostgreSqlDatabase : IRelationalDatabase            │
│  │  └─ MySqlDatabase : IRelationalDatabase                  │
│  └─ Vectorization/  (向量存储实现)                          │
│     ├─ QdrantVectorStore : IVectorStore                     │
│     └─ MemoryVectorStore : IVectorStore                     │
│                                                             │
│  外部依赖：                                                  │
│  - StackExchange.Redis 2.8.16                               │
│  - Npgsql 8.0.3                                             │
│  - Qdrant.Client 1.9.0                                      │
│                                                             │
│  依赖：Core (实现抽象接口)                                   │
└─────────────────────────────────────────────────────────────┘
                           ↓ 实现
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: 存储抽象层（领域服务接口）                         │
│  CKY.MAF.Core.Abstractions                                  │
│                                                             │
│  职责：定义领域服务接口，依赖存储抽象接口                    │
│  主要接口：                                                  │
│  - IMafSessionStorage (会话存储)                            │
│  - IMafMemoryManager (记忆管理)                             │
│  - ITaskRepository (任务仓储)                               │
│                                                             │
│  依赖：Core.Models, Core.Abstractions.Interfaces            │
└─────────────────────────────────────────────────────────────┘
                           ↓ 组合
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: 核心抽象层                                         │
│  CKY.MAF.Core                                               │
│                                                             │
│  ┌─ Abstractions/Interfaces/ (存储抽象接口)                 │
│  │  - ICacheStore (缓存接口)                                │
│  │  - IVectorStore (向量存储接口)                           │
│  │  - IRelationalDatabase (关系数据库接口)                  │
│  │                                                          │
│  ├─ Models/ (领域模型)                                      │
│  │  - MainTask, SubTask, MessageContext                     │
│  │  - MafAgentBase, MafTaskBase                             │
│  │                                                          │
│  └─ Abstractions/ (Agent 基类)                              │
│     - MafAgentBase : AIAgent                                │
│     - MafMainAgent : MafAgentBase                           │
│     - MafSubAgent : MafAgentBase                            │
│                                                             │
│  外部依赖：                                                  │
│  - Microsoft.AgentFramework (唯一硬性依赖)                  │
│  - Microsoft.Extensions.* (仅抽象包)                        │
│                                                             │
│  依赖：无（核心抽象层）                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 依赖规则

### 规则1：单向依赖

```
Demo → Services → Infrastructure → Core
```

**禁止**：
- ❌ Core 不能依赖任何其他层
- ❌ Infrastructure 不能依赖 Services
- ❌ Services 不能依赖 Demo

### 规则2：Core 层零外部依赖

**Core 项目只能引用**：
- ✅ .NET 基础库（System.*）
- ✅ Microsoft.Extensions.*（仅抽象包，Abstractions）
- ✅ Microsoft.AgentFramework（MS AF 基类）

**Core 项目禁止引用**：
- ❌ 任何具体实现包（Redis、PostgreSQL、Qdrant）
- ❌ 第三方框架（除 MS AF 外）
- ❌ 其他自定义项目

### 规则3：依赖抽象接口

**Services 层**：
```csharp
// ✅ 正确：依赖抽象接口
public class MafTaskScheduler
{
    private readonly ICacheStore _cacheStore;
    private readonly IRelationalDatabase _database;
    private readonly ITaskRepository _taskRepository;

    public MafTaskScheduler(
        ICacheStore cacheStore,
        IRelationalDatabase database,
        ITaskRepository taskRepository)
    {
        _cacheStore = cacheStore;
        _database = database;
        _taskRepository = taskRepository;
    }
}

// ❌ 错误：依赖具体实现
public class MafTaskScheduler
{
    private readonly RedisCacheStore _redis;  // 错误！
    private readonly PostgreSqlDatabase _db;  // 错误！
}
```

### 规则4：实现可替换

**Infrastructure 层**通过依赖注入注册实现：

```csharp
// Startup.cs 或 Program.cs
services.AddSingleton<ICacheStore, RedisCacheStore>();
services.AddSingleton<IRelationalDatabase, PostgreSqlDatabase>();
services.AddSingleton<IVectorStore, QdrantVectorStore>();

// 可替换为其他实现
// services.AddSingleton<ICacheStore, MemoryCacheStore>();
// services.AddSingleton<IRelationalDatabase, MySqlDatabase>();
// services.AddSingleton<IVectorStore, MemoryVectorStore>();
```

---

## 接口抽象设计

### 存储抽象接口（Core.Abstractions.Interfaces）

#### 1. ICacheStore - 缓存存储接口

**定义位置**：`CKY.MAF.Core/Abstractions/Interfaces/ICacheStore.cs`

**实现位置**：`CKY.MAF.Infrastructure/Caching/`

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
    /// <summary>
    /// 缓存存储抽象接口
    /// </summary>
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

**可选实现**：
- `RedisCacheStore` - 使用 StackExchange.Redis
- `MemoryCacheStore` - 使用 IMemoryCache（测试用）
- `NCacheStore` - 使用 NCache（企业级缓存）

---

#### 2. IVectorStore - 向量存储接口

**定义位置**：`CKY.MAF.Core/Abstractions/Interfaces/IVectorStore.cs`

**实现位置**：`CKY.MAF.Infrastructure/Vectorization/`

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
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

**可选实现**：
- `QdrantVectorStore` - 使用 Qdrant.Client
- `MemoryVectorStore` - 内存向量存储（测试用）
- `PineconeVectorStore` - 使用 Pinecone SDK（未来）

---

#### 3. IRelationalDatabase - 关系数据库接口

**定义位置**：`CKY.MAF.Core/Abstractions/Interfaces/IRelationalDatabase.cs`

**实现位置**：`CKY.MAF.Infrastructure/Relational/`

```csharp
namespace CKY.MultiAgentFramework.Core.Abstractions.Interfaces
{
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

**可选实现**：
- `PostgreSqlDatabase` - 使用 Npgsql + Dapper
- `MySqlDatabase` - 使用 MySqlConnector + Dapper
- `InMemoryDatabase` - 内存数据库（测试用）

---

## 具体实现层

### Infrastructure.Caching - Redis 缓存实现

**项目文件**：`CKY.MAF.Infrastructure.Caching.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Core\CKY.MAF.Core.csproj" />
    <PackageReference Include="StackExchange.Redis" Version="2.8.16" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

**实现示例**：

```csharp
namespace CKY.MultiAgentFramework.Infrastructure.Caching
{
    public class RedisCacheStore : ICacheStore
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheStore> _logger;

        public RedisCacheStore(IConnectionMultiplexer redis, ILogger<RedisCacheStore> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value) : null;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null,
            CancellationToken ct = default) where T : class
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry);
        }

        // ... 其他方法实现
    }
}
```

---

### Infrastructure.Relational - PostgreSQL 实现

**项目文件**：`CKY.MAF.Infrastructure.Relational.PostgreSql.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Core\CKY.MAF.Core.csproj" />
    <PackageReference Include="Npgsql" Version="8.0.3" />
    <PackageReference Include="Dapper" Version="2.1.35" />
  </ItemGroup>
</Project>
```

---

### Infrastructure.Vectorization - Qdrant 向量存储

**项目文件**：`CKY.MAF.Infrastructure.Vectorization.Qdrant.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Core\CKY.MAF.Core.csproj" />
    <PackageReference Include="Qdrant.Client" Version="1.9.0" />
  </ItemGroup>
</Project>
```

---

## 使用示例

### 示例1：在 Services 层使用抽象接口

```csharp
namespace CKY.MultiAgentFramework.Services.Scheduling
{
    public class MafTaskScheduler : ITaskScheduler
    {
        private readonly ICacheStore _cacheStore;
        private readonly IRelationalDatabase _database;
        private readonly ILogger<MafTaskScheduler> _logger;

        public MafTaskScheduler(
            ICacheStore cacheStore,
            IRelationalDatabase database,
            ILogger<MafTaskScheduler> logger)
        {
            _cacheStore = cacheStore;
            _database = database;
            _logger = logger;
        }

        public async Task ScheduleAsync(MainTask task, CancellationToken ct = default)
        {
            // 保存到缓存（L2）
            await _cacheStore.SetAsync($"task:{task.TaskId}", task, TimeSpan.FromHours(24), ct);

            // 持久化到数据库（L3）
            await _database.InsertAsync(task, ct);
        }
    }
}
```

**关键点**：
- ✅ `MafTaskScheduler` 不知道底层使用的是 Redis 还是其他缓存
- ✅ 可以轻松切换到 `MemoryCacheStore` 进行单元测试

---

### 示例2：依赖注入配置

```csharp
// Program.cs (Demo 应用)
var builder = WebApplication.CreateBuilder(args);

// 注册 MS Agent Framework
builder.Services.AddAgentFramework();

// 注册存储实现（可替换）
if (builder.Environment.IsDevelopment())
{
    // 开发环境：使用内存实现
    builder.Services.AddSingleton<ICacheStore, MemoryCacheStore>();
    builder.Services.AddSingleton<IRelationalDatabase, InMemoryDatabase>();
    builder.Services.AddSingleton<IVectorStore, MemoryVectorStore>();
}
else
{
    // 生产环境：使用真实实现
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

    builder.Services.AddSingleton<ICacheStore, RedisCacheStore>();
    builder.Services.AddSingleton<IRelationalDatabase, PostgreSqlDatabase>();
    builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
}

// 注册业务服务（自动注入抽象接口）
builder.Services.AddSingleton<ITaskScheduler, MafTaskScheduler>();
builder.Services.AddSingleton<IIntentRecognizer, MafIntentRecognizer>();

var app = builder.Build();
app.Run();
```

---

### 示例3：单元测试（使用 Moq）

```csharp
public class MafTaskSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldSaveToCacheAndDatabase()
    {
        // Arrange
        var mockCache = new Mock<ICacheStore>();
        var mockDb = new Mock<IRelationalDatabase>();
        var scheduler = new MafTaskScheduler(mockCache.Object, mockDb.Object, Mock.Of<ILogger>());

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

## 测试策略

### 单元测试（70%）

**目标**：测试业务逻辑，不依赖外部服务

**策略**：
- 使用 Moq 模拟所有抽象接口
- 测试覆盖所有 Services 层代码
- 目标覆盖率：Services 90%，Core 95%

**示例**：见上节"示例3"

---

### 集成测试（25%）

**目标**：测试 Infrastructure 层实现

**策略**：
- 使用 Testcontainers 启动真实的 Redis/PostgreSQL 容器
- 测试 `RedisCacheStore`、`PostgreSqlDatabase` 等实现
- 确保实现正确对接外部服务

**示例**：

```csharp
public class RedisCacheStoreTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private ICacheStore _cacheStore;

    public RedisCacheStoreTests()
    {
        _redisContainer = new RedisBuilder().Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        var redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        _cacheStore = new RedisCacheStore(redis, Mock.Of<ILogger>());
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test-key";
        var value = new { Name = "Test", Value = 123 };

        // Act
        await _cacheStore.SetAsync(key, value);
        var result = await _cacheStore.GetAsync<object>(key);

        // Assert
        Assert.NotNull(result);
    }
}
```

---

### 端到端测试（5%）

**目标**：测试完整流程

**策略**：
- 启动完整的 Demo 应用
- 使用真实的 LLM API（智谱AI）
- 测试完整的任务调度流程

---

## 扩展性

### 如何添加新的缓存实现

**步骤**：

1. 创建新项目 `CKY.MAF.Infrastructure.Caching.NCache`
2. 实现 `ICacheStore` 接口

```csharp
public class NCacheStore : ICacheStore
{
    // 实现 ICacheStore 的所有方法
}
```

3. 在 Demo 应用中注册

```csharp
builder.Services.AddSingleton<ICacheStore, NCacheStore>();
```

**无需修改**：
- ✅ Core 层代码
- ✅ Services 层代码
- ✅ 单元测试代码

---

## 总结

### 关键设计决策

| 决策 | 原因 | 好处 |
|------|------|------|
| Core 层零外部依赖 | 遵循 DIP 原则 | 框架核心稳定，可替换所有实现 |
| 定义三个存储抽象接口 | 覆盖所有存储需求 | 缓存、向量、关系数据库完全解耦 |
| Infrastructure 层包含所有具体实现 | 集中管理外部依赖 | 易于维护和替换 |
| 使用 Moq 进行单元测试 | 依赖抽象接口 | 测试快速、稳定、无外部依赖 |

### 架构优势

✅ **高度解耦**：所有层通过接口通信
✅ **易于测试**：单元测试无需外部服务
✅ **灵活替换**：可随时切换存储实现
✅ **清晰分层**：依赖方向单一向下
✅ **扩展性强**：添加新实现无需修改核心代码

---

**文档维护**：CKY.MAF 架构团队
**最后更新**：2026-03-13

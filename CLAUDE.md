# CLAUDE.md

语言：使用简体中文

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CKY.MAF** is an enterprise-grade multi-agent framework built as an enhancement layer on top of Microsoft Agent Framework (Preview). It implements intelligent task scheduling, multi-agent collaboration, and provides enterprise-level features including storage abstraction, resilience patterns, and comprehensive observability.

**Architecture Philosophy**: Dependency Inversion Principle (DIP) with 5-layer architecture
- **Layer 5**: Demo应用层 (Blazor Server applications)
- **Layer 4**: 业务服务层 (Task scheduling, orchestration)
- **Layer 3**: 基础设施层 (EF Core, Redis, Qdrant)
- **Layer 2**: 存储抽象层 (Domain services: ISessionStorage, IMemoryManager, IRepository)
- **Layer 1**: 核心抽象层 (Core abstractions: ICacheStore, IVectorStore, IRepository)

**Critical Design Rule**: Core layer has ZERO external dependencies (except Microsoft Agent Framework). All storage implementations are in Infrastructure layer and fully replaceable. Uses EF Core for relational database access.

## Technology Stack

- **.NET 10** (target framework)
- **Microsoft Agent Framework (Preview)** - Required base framework for all AI/LLM operations
- **ASP.NET Core** - Native (no ABP framework)
- **Blazor Server** - Demo application UI
- **Storage**:
  - L1: IMemoryCache (in-memory)
  - L2: Redis (distributed cache)
  - L3: PostgreSQL/SQLite (relational database via EF Core)
  - Vector: Qdrant (semantic search)
  - ORM: Entity Framework Core 9.0.0
- **Testing**: xUnit, FluentAssertions, Moq, Testcontainers
- **LLM Integration**: MS AF native interfaces only - NO SemanticKernel, NO direct provider SDKs
  - Primary: 智谱AI (GLM-4/GLM-4-Plus)
  - Fallback: 通义千问/文心一言/讯飞星火
  - Implement custom `ILlmService` in Infrastructure layer for each provider
- **Monitoring**: Prometheus, Grafana, distributed tracing

## Quick Start Guide

### 快速开始（5 分钟上手）

**1. 构建项目**
```bash
cd src
dotnet build CKY.MAF.slnx
```

**2. 运行单元测试**
```bash
dotnet test tests/UnitTests/CKY.MAF.Tests.csproj
```

**3. 应用数据库迁移**
```bash
# Linux/Mac
bash scripts/migrate-apply.sh

# Windows PowerShell
powershell scripts/migrate-apply.ps1
```

**4. 使用 Repository 模式**
```csharp
// 在服务中注入 Repository
public class MyService
{
    private readonly IMainTaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MyService(IMainTaskRepository taskRepository, IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MainTask> CreateTaskAsync(string title)
    {
        var task = new MainTask { Title = title };
        await _taskRepository.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }
}
```

**5. 配置 LLM Agent**
```csharp
// 使用内置的 AddMafBuiltinServices 扩展方法
builder.Services.AddMafBuiltinServices(builder.Configuration);

// 或手动配置特定 LLM 提供商
var config = new LlmProviderConfig
{
    ProviderName = "ZhipuAI",
    ApiKey = "your-api-key",
    ModelId = "glm-4"
};
builder.Services.AddSingleton<MafAiAgent, ZhipuAILlmAgent>(sp =>
    new ZhipuAILlmAgent(config, sp.GetRequiredService<ILogger<ZhipuAILlmAgent>>()));
```

### 常见命令速查

| 操作 | 命令 |
|------|------|
| 添加新迁移 | `dotnet ef migrations add <Name> --project src/Infrastructure/Repository/CKY.MAF.Repository.csproj --startup-project src/Services/CKY.MAF.Services.csproj --output-dir Data/Migrations` |
| 回滚迁移 | `dotnet ef database update <TargetMigration> --project src/Infrastructure/Repository/CKY.MAF.Repository.csproj --startup-project src/Services/CKY.MAF.Services.csproj` |
| 查看待定迁移 | `dotnet ef migrations list --project src/Infrastructure/Repository/CKY.MAF.Repository.csproj --startup-project src/Services/CKY.MAF.Services.csproj` |

## Documentation Structure

All design documentation is in `docs/specs/` directory (15 documents, ~384KB total):

**Essential Reading** (start here):
1. `01-architecture-overview.md` - Core concepts and design principles
2. `12-layered-architecture.md` - 5-layer DIP architecture (CRITICAL)
3. `09-implementation-guide.md` - Implementation patterns and directory structure

**For Development**:
4. `06-interface-design-spec.md` - All interface definitions and data models
5. `03-task-scheduling-design.md` - Task priority and dependency management
6. `10-testing-guide.md` - Testing strategy (70% unit, 25% integration, 5% E2E)

**For Operations**:
7. `13-performance-benchmarks.md` - Performance metrics and optimization strategies
8. `14-error-handling-guide.md` - Error handling, retry policies, circuit breakers, degradation
9. `08-deployment-guide.md` - Docker/Kubernetes deployment

**Navigation**: See `docs/specs/README.md` for complete document index and role-based reading recommendations.

## Current Project Structure

(Updated for EF Core and Repository pattern)

```
CKY.MAF/
├── Core/
│   ├── CKY.MAF.Core.csproj                    # Core abstractions (Layer 1)
│   └── Abstractions/
│       ├── Interfaces/
│       │   ├── ICacheStore.cs                 # Cache storage abstraction
│       │   ├── IVectorStore.cs                # Vector storage abstraction
│       │   ├── IMainTaskRepository.cs          # Repository interfaces
│       │   ├── ISubTaskRepository.cs
│       │   └── IUnitOfWork.cs                 # Unit of Work pattern
│       └── Models/
│           ├── MainTask.cs                    # EF Core entities
│           ├── SubTask.cs
│           └── MafAgentBase.cs : AIAgent
│
├── Services/
│   ├── CKY.MAF.Services.csproj                # Business logic (Layer 4)
│   ├── Scheduling/
│   │   ├── MafTaskScheduler.cs
│   │   └── PriorityCalculator.cs
│   ├── IntentRecognition/
│   │   └── MafIntentRecognizer.cs
│   └── Orchestration/
│       └── MafTaskOrchestrator.cs
│
├── Infrastructure/Repository/
│   ├── CKY.MAF.Repository.csproj              # EF Core data access (Layer 3)
│   ├── Data/
│   │   ├── MafDbContext.cs                    # EF Core DbContext
│   │   ├── MafDbContextFactory.cs             # Design-time factory
│   │   ├── EntityTypeConfigurations/         # EF Core configurations
│   │   └── Migrations/                        # EF Core migrations
│   ├── Repositories/
│   │   ├── MainTaskRepository.cs              # Repository implementations
│   │   ├── SubTaskRepository.cs
│   │   └── UnitOfWork.cs                      # Unit of Work implementation
│   └── Relational/
│       └── EfCoreRelationalDatabase.cs        # EF Core implementation
│
├── Infrastructure/Caching/
│   ├── RedisCacheStore.cs : ICacheStore       # Layer 3
│   └── MemoryCacheStore.cs : ICacheStore      # Testing
│
├── Infrastructure/Vectorization/
│   ├── QdrantVectorStore.cs : IVectorStore
│   └── MemoryVectorStore.cs : IVectorStore
│
└── Demos/
    └── SmartHome/
        └── CKY.MAF.Demos.SmartHome.csproj     # Blazor Server demo (Layer 5)
```

## Build and Test Commands

```bash
# Build entire solution
dotnet build CKY.MAF.sln

# Run all tests
dotnet test

# Run specific test project
dotnet test CKY.MAF.Tests.Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# EF Core Migrations
dotnet ef migrations add <MigrationName> --project src/Infrastructure/Repository/CKY.MAF.Repository.csproj --startup-project src/Services/CKY.MAF.Services.csproj --output-dir Data/Migrations

# Apply EF Core migrations
dotnet ef database update --project src/Infrastructure/Repository/CKY.MAF.Repository.csproj --startup-project src/Services/CKY.MAF.Services.csproj

# Run specific test
dotnet test --filter "FullyQualifiedName~MafTaskSchedulerTests"

# Run performance benchmarks
dotnet run -c Release -p CKY.MAF.Benchmarks

# Run Blazor demo
dotnet run --project CKY.MAF.Demos.SmartHome
```

## Key Design Patterns

### 1. Dependency Injection Pattern
All Services layer components depend on abstractions (ICacheStore, IVectorStore, IMainTaskRepository, IUnitOfWork), NOT concrete implementations. Concrete implementations are registered at startup:

```csharp
// Program.cs (Demo application)
services.AddSingleton<ICacheStore, RedisCacheStore>();
services.AddSingleton<IVectorStore, QdrantVectorStore>();

// EF Core DbContext and Repository pattern
services.AddDbContext<MafDbContext>(options =>
    options.UseSqlite("Data Source=maf.db"));
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IMainTaskRepository, MainTaskRepository>();
services.AddScoped<ISubTaskRepository, SubTaskRepository>();
```

### 2. Main-Agent/Sub-Agent Pattern
All agents inherit from Microsoft Agent Framework's `AIAgent`:
- `MafMainAgent` : `MafAgentBase` - Coordinates task execution
- `MafSubAgent` : `MafAgentBase` - Executes specific tasks
- Agents communicate via MS AF's A2A (Agent-to-Agent) mechanism

### 3. Resilience Patterns
(From `14-error-handling-guide.md`)

**Retry with Exponential Backoff**:
```csharp
var policy = new RetryPolicy
{
    MaxRetries = 3,
    BackoffStrategy = BackoffStrategy.ExponentialWithJitter,
    InitialBackoffMs = 1000
};
```

**Circuit Breaker**:
- LLM API: 10 failures/60s → break for 120s
- Redis: 20 failures/30s → break for 60s
- PostgreSQL: 5 failures/60s → break for 180s

**Degradation Levels** (5 levels):
- Level 1: Disable non-core features (recommendations)
- Level 2: Disable vector search (use keyword search)
- Level 3: Disable L2 cache (use L1 only)
- Level 4: Use simplified LLM model (GLM-4-Air)
- Level 5: Disable LLM entirely (use rule engine)

### 4. Three-Tier Storage Strategy
- **L1 (Memory)**: Fastest, session data, < 200MB
- **L2 (Redis)**: Distributed cache, 24h TTL, < 85% hit rate target
- **L3 (PostgreSQL)**: Persistent storage, transactional data

## Testing Strategy

**Test Pyramid** (from `10-testing-guide.md`):
- **70% Unit Tests**: Use Moq for all abstractions, zero external dependencies
- **25% Integration Tests**: Use Testcontainers for Redis/PostgreSQL/Qdrant
- **5% E2E Tests**: Full application with real LLM APIs

**Unit Test Example**:
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

        // Act
        await scheduler.ScheduleAsync(task);

        // Assert
        mockCache.Verify(x => x.SetAsync(...), Times.Once);
        mockDb.Verify(x => x.InsertAsync(...), Times.Once);
    }
}
```

**Coverage Targets**:
- Services layer: 90%
- Core layer: 95%
- Infrastructure layer: 80%

## Performance Benchmarks

(From `13-performance-benchmarks.md`)

**Response Time Targets**:
- Simple tasks (intent + single agent): P95 < 1s
- Complex tasks (decomposition + multi-agent): P95 < 5s
- Long conversations (multi-turn): P95 < 3s
- LLM API calls: P95 < 3s

**Throughput Targets**:
- Simple tasks: > 100 req/s
- Complex tasks: > 50 req/s
- Concurrent users: > 100

**Resource Limits**:
- CPU: < 50% normal, < 80% high load, alert at > 80%
- Memory: < 500MB normal, < 1GB high load, alert at > 1GB
- GC pause: < 50ms normal, < 100ms high load

## Important Development Guidelines

### DO:
- ✅ Always depend on abstractions (ICacheStore, IVectorStore, IRelationalDatabase) in Services layer
- ✅ Implement interfaces in Infrastructure layer
- ✅ Use async/await for all I/O operations
- ✅ Write unit tests with Moq for all Services layer code
- ✅ Use Testcontainers for Infrastructure integration tests
- ✅ Follow the 5-layer architecture strictly
- ✅ Keep Core layer with ZERO external dependencies (except MS AF and Microsoft.Extensions.* abstractions)
- ✅ Use Microsoft Agent Framework (MS AF) for all LLM integrations and AI agent functionality
- ✅ Define custom LLM service abstractions (like ILlmService) for provider-specific implementations in Infrastructure layer

### DON'T:
- ❌ Never reference concrete implementations (RedisCacheStore, PostgreSqlDatabase) in Services layer
- ❌ Never add external NuGet packages to Core layer (except MS AF and Microsoft.Extensions.* abstractions)
- ❌ Never use synchronous I/O (no .Result, .Wait(), or blocking calls)
- ❌ Never hardcode storage implementation choices
- ❌ Never skip unit tests for Services layer
- ❌ Never use SemanticKernel API or other LLM orchestration frameworks - use MS AF only
- ❌ Never directly depend on LLM provider SDKs (OpenAI, Azure OpenAI, etc.) in Core or Services layers - use abstractions

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

## Common Tasks

### 注册 Infrastructure 服务

使用自动注册扩展方法：

```csharp
// Program.cs
services.AddMafInfrastructureServices(builder.Configuration);
```

**默认实现**（开发环境）：
- ICacheStore → MemoryCacheStore
- IVectorStore → MemoryVectorStore
- IRelationalDatabase → EfCoreRelationalDatabase (SQLite via EF Core)
- IMafAiSessionStore → DatabaseMafAiSessionStore

**配置覆盖**（生产环境）：

在 `appsettings.Production.json` 中配置：
```json
{
  "MafServices": {
    "Implementations": {
      "ICacheStore": "RedisCacheStore",
      "IVectorStore": "QdrantVectorStore"
    }
  }
}
```

**支持的生命周期**：
- Singleton: ICacheStore, IVectorStore, RedisMafAiSessionStore
- Scoped: IRelationalDatabase, DatabaseMafAiSessionStore (依赖 DbContext)

### Adding a New Storage Implementation

1. Create new project in `Infrastructure/` layer (e.g., `Infrastructure.Caching.NCache`)
2. Implement the relevant interface (e.g., `ICacheStore`)
3. Register in DI container at startup:
   ```csharp
   services.AddSingleton<ICacheStore, NCacheStore>();
   ```
4. Add integration tests using Testcontainers (if external service)
5. NO changes needed in Core or Services layers

### Adding a New Agent

1. Create agent class inheriting from `MafAgentBase` (which inherits from MS AF's `AIAgent`)
2. Implement required methods from base class
3. Register in DI container and MS Agent Framework
4. Use A2A (Agent-to-Agent) mechanism for inter-agent communication
5. Add unit tests with mocked dependencies

### Implementing Task Scheduling

1. Define task with `MainTask` and `SubTask` models
2. Set priority scores (0-100) and dependencies
3. Use `MafTaskScheduler` to schedule tasks
4. Use `MafTaskOrchestrator` to decompose complex tasks
5. Implement retry and circuit breaker for LLM calls

## Documentation Language

All documentation is in **Chinese** (Simplified). This includes:
- Code comments in documentation files
- User-facing messages
- Architecture descriptions

However, **code identifiers** use English naming conventions (PascalCase for types, camelCase for parameters/variables).

## Key Interfaces to Understand

**From `06-interface-design-spec.md`**:

### Storage Abstractions (Layer 1 - Core):
- `ICacheStore` - Get/Set/Delete cached data
- `IVectorStore` - Semantic search with embeddings
- `IRelationalDatabase` - CRUD operations with transaction support

### Domain Services (Layer 2 - Abstractions):
- `ITaskScheduler` - Schedule and execute tasks
- `IIntentRecognizer` - Recognize user intent from natural language
- `ITaskOrchestrator` - Decompose complex tasks into subtasks
- `ISessionStorage` - Store conversation sessions
- `IMemoryManager` - Manage long-term and short-term memory
- `ITaskRepository` - Persist and retrieve tasks

### Agent Base Classes:
- `MafAgentBase : AIAgent` - Base for all agents
- `MafMainAgent : MafAgentBase` - Coordinates execution
- `MafSubAgent : MafAgentBase` - Executes specific tasks

## Troubleshooting

### "Module not found" errors
- Ensure all project references use relative paths
- Verify .csproj files reference correct dependencies
- Check that Infrastructure projects reference Core project

### Unit test failures
- Verify all dependencies are mocked (Moq)
- Ensure no actual Redis/PostgreSQL connections in unit tests
- Check that Services layer uses interface abstractions, not concrete implementations

### LLM API rate limiting
- Check `14-error-handling-guide.md` for retry strategies
- Implement exponential backoff with jitter
- Use circuit breaker to prevent cascading failures
- Consider degradation to simpler models or rule engine

### Performance issues
- Check cache hit rates (target: L1 > 80%, L2 > 85%)
- Verify parallel task execution for independent tasks
- Review database query performance (add indexes if needed)
- Use BenchmarkDotNet to profile bottlenecks
- Reference `13-performance-benchmarks.md` for optimization strategies

## Current Project Status

**Last Updated**: 2026-03-15

**Completed Features**:
- ✅ Core 抽象层（领域模型、接口定义）
- ✅ Repository 模式实现（EF Core + SQLite/PostgreSQL）
- ✅ 基础设施层（Redis 缓存、向量存储）
- ✅ 业务服务层（任务调度、意图识别）
- ✅ EF Core 数据库迁移支持
- ✅ 5 大 LLM 提供商 Agent 实现（智谱AI、通义千问、文心一言、讯飞星火、MiniMax）
- ✅ 压缩上下文统计信息收集

**In Progress**:
- 🔄 Demo 项目更新（使用新的 Repository 模式）
- 🔄 集成测试完善

**TODO**:
- ⏳ PostgreSQL 生产环境配置优化
- ⏳ Qdrant 向量存储 API 调整
- ⏳ LLM Agent 的 HttpClient 支持（LlmAgentFactory）

**Key Components**:
- `Core/CKY.MAF.Core.csproj` - 核心抽象层
- `Infrastructure/Repository/CKY.MAF.Repository.csproj` - EF Core 数据访问
- `Infrastructure/Caching/CKY.MAF.Infrastructure.Caching.csproj` - Redis 缓存
- `Infrastructure/Vectorization/CKY.MAF.Infrastructure.Vectorization.csproj` - 向量存储
- `Services/CKY.MAF.Services.csproj` - 业务服务层

## Resources

- **Design Docs**: `docs/specs/README.md` (start here for navigation)
- **Architecture**: `docs/specs/01-architecture-overview.md` and `12-layered-architecture.md`
- **Implementation**: `docs/specs/09-implementation-guide.md`
- **Testing**: `docs/specs/10-testing-guide.md`
- **Performance**: `docs/specs/13-performance-benchmarks.md`
- **Error Handling**: `docs/specs/14-error-handling-guide.md`

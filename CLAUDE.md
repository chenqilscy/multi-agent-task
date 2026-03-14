# CLAUDE.md

语言：使用简体中文

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CKY.MAF** is an enterprise-grade multi-agent framework built as an enhancement layer on top of Microsoft Agent Framework (Preview). It implements intelligent task scheduling, multi-agent collaboration, and provides enterprise-level features including storage abstraction, resilience patterns, and comprehensive observability.

**Architecture Philosophy**: Dependency Inversion Principle (DIP) with 5-layer architecture
- **Layer 5**: Demo应用层 (Blazor Server applications)
- **Layer 4**: 业务服务层 (Task scheduling, orchestration)
- **Layer 3**: 基础设施层 (Concrete implementations: Redis, PostgreSQL, Qdrant)
- **Layer 2**: 存储抽象层 (Domain services: ISessionStorage, IMemoryManager, ITaskRepository)
- **Layer 1**: 核心抽象层 (Core abstractions: ICacheStore, IVectorStore, IRelationalDatabase)

**Critical Design Rule**: Core layer has ZERO external dependencies (except Microsoft Agent Framework). All storage implementations are in Infrastructure layer and fully replaceable.

## Technology Stack

- **.NET 10** (target framework)
- **Microsoft Agent Framework (Preview)** - Required base framework for all AI/LLM operations
- **ASP.NET Core** - Native (no ABP framework)
- **Blazor Server** - Demo application UI
- **Storage**:
  - L1: IMemoryCache (in-memory)
  - L2: Redis (distributed cache)
  - L3: PostgreSQL (relational database)
  - Vector: Qdrant (semantic search)
- **Testing**: xUnit, FluentAssertions, Moq, Testcontainers
- **LLM Integration**: MS AF native interfaces only - NO SemanticKernel, NO direct provider SDKs
  - Primary: 智谱AI (GLM-4/GLM-4-Plus)
  - Fallback: 通义千问/文心一言/讯飞星火
  - Implement custom `ILlmService` in Infrastructure layer for each provider
- **Monitoring**: Prometheus, Grafana, distributed tracing

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

## Planned Project Structure

(From implementation roadmap - not yet created)

```
CKY.MAF/
├── Core/
│   ├── CKY.MAF.Core.csproj                    # Core abstractions (Layer 1)
│   └── Abstractions/
│       ├── Interfaces/
│       │   ├── ICacheStore.cs                 # Cache storage abstraction
│       │   ├── IVectorStore.cs                # Vector storage abstraction
│       │   └── IRelationalDatabase.cs         # Database abstraction
│       └── Models/
│           ├── MainTask.cs
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
├── Infrastructure/
│   ├── Caching/
│   │   ├── RedisCacheStore.cs : ICacheStore   # Layer 3
│   │   └── MemoryCacheStore.cs : ICacheStore  # Testing
│   ├── Relational/
│   │   ├── PostgreSqlDatabase.cs : IRelationalDatabase
│   │   └── InMemoryDatabase.cs : IRelationalDatabase
│   └── Vectorization/
│       ├── QdrantVectorStore.cs : IVectorStore
│       └── MemoryVectorStore.cs : IVectorStore
│
└── Demos/
    └── SmartHome/
        └── CKY.MAF.Demos.SmartHome.csproj     # Blazor Server demo (Layer 5)
```

## Build and Test Commands

(To be implemented - from roadmap)

```bash
# Build entire solution
dotnet build CKY.MAF.sln

# Run all tests
dotnet test

# Run specific test project
dotnet test CKY.MAF.Tests.Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~MafTaskSchedulerTests"

# Run performance benchmarks
dotnet run -c Release -p CKY.MAF.Benchmarks

# Run Blazor demo
dotnet run --project CKY.MAF.Demos.SmartHome
```

## Key Design Patterns

### 1. Dependency Injection Pattern
All Services layer components depend on abstractions (ICacheStore, IVectorStore, IRelationalDatabase), NOT concrete implementations. Concrete implementations are registered at startup:

```csharp
// Program.cs (Demo application)
services.AddSingleton<ICacheStore, RedisCacheStore>();
services.AddSingleton<IRelationalDatabase, PostgreSqlDatabase>();
services.AddSingleton<IVectorStore, QdrantVectorStore>();
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

**Phase**: Design/Architecture Complete (15 documents, ~384KB)
**Implementation**: Not started yet
**Next Steps**: See `11-implementation-roadmap.md` for 6-phase implementation plan (36 days estimated)

## Resources

- **Design Docs**: `docs/specs/README.md` (start here for navigation)
- **Architecture**: `docs/specs/01-architecture-overview.md` and `12-layered-architecture.md`
- **Implementation**: `docs/specs/09-implementation-guide.md`
- **Testing**: `docs/specs/10-testing-guide.md`
- **Performance**: `docs/specs/13-performance-benchmarks.md`
- **Error Handling**: `docs/specs/14-error-handling-guide.md`

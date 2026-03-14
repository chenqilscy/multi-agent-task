# CKY.MAF 详细实施补齐计划

> **生成日期**: 2026-03-13
> **当前完成度**: 65%
> **目标完成度**: 95%
> **预计工期**: 14个工作日

---

## 📊 执行摘要

基于差距分析，项目当前处于**阶段3-阶段4之间**，核心架构已建立，但以下关键组件缺失：

### 关键差距（按优先级）
1. **P0 - 阻塞性**（必须完成）: Redis缓存、完整任务调度器、Agent继承统一
2. **P1 - 重要**（影响演示）: Qdrant向量存储、A2A通信、Blazor UI、集成测试
3. **P2 - 优化**（长期）: SignalR、Prometheus监控、文档同步

### 实施策略
- **Week 1 (Days 1-5)**: 完成P0阻塞性任务
- **Week 2 (Days 6-10)**: 完成P1重要任务
- **Week 3 (Days 11-14)**: 完成P2优化任务和文档

---

## 🎯 第一周：P0 阻塞性任务（Days 1-5）

### Task 1: 实现 Redis 缓存层 ⭐⭐⭐⭐⭐
**优先级**: P0 - 阻塞性
**工作量**: 1.5天
**依赖**: 无

#### 目标
实现 `RedisCacheStore : ICacheStore`，恢复三层存储架构的L2层。

#### 接口定义
```csharp
// src/Core/Abstractions/ICacheStore.cs:7
public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<Dictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, CancellationToken ct = default) where T : class;
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
```

#### 实现方案

**方案选择**: StackExchange.Redis 2.11.8（稳定版本）

**项目结构**:
```
src/Infrastructure/Caching/
├── CKY.MAF.Infrastructure.Caching.csproj
├── RedisCacheStore.cs          # 主实现
├── MemoryCacheStore.cs         # 测试用实现
└── RedisCacheStoreExtensions.cs # 扩展方法
```

**核心实现代码结构**:

```csharp
// RedisCacheStore.cs
public class RedisCacheStore : ICacheStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheStore> _logger;
    private readonly IDatabase _db;

    public RedisCacheStore(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheStore> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = _redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache key: {Key}", key);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cache key: {Key}", key);
        }
    }

    public async Task<Dictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, CancellationToken ct = default) where T : class
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _db.StringGetAsync(redisKeys);

            var result = new Dictionary<string, T?>();
            for (int i = 0; i < keys.Count(); i++)
            {
                var key = keys.ElementAt(i);
                var value = values[i];
                result[key] = value.HasValue ? JsonSerializer.Deserialize<T>(value) : null;
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch cache keys");
            return new Dictionary<string, T?>();
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cache key existence: {Key}", key);
            return false;
        }
    }
}
```

**依赖注入配置**:
```csharp
// Program.cs 或 Startup.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddSingleton<ICacheStore, RedisCacheStore>();
```

**配置文件**:
```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=your_password,defaultDatabase=0"
  }
}
```

**集成测试**（使用 Testcontainers）:
```csharp
// tests/IntegrationTests/Caching/RedisCacheStoreTests.cs
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
        var logger = new NullLogger<RedisCacheStore>();
        _cacheStore = new RedisCacheStore(redis, logger);
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
        await _cacheStore.SetAsync(key, value, TimeSpan.FromHours(1));
        var result = await _cacheStore.GetAsync<object>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetBatchAsync_ShouldReturnMultipleValues()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["key1"] = new { Id = 1 },
            ["key2"] = new { Id = 2 }
        };

        // Act
        foreach (var item in data)
        {
            await _cacheStore.SetAsync(item.Key, item.Value);
        }

        var result = await _cacheStore.GetBatchAsync<object>(data.Keys);

        // Assert
        Assert.Equal(2, result.Count);
    }
}
```

#### 验收标准
- ✅ 所有接口方法实现完成
- ✅ 序列化/反序列化正常工作
- ✅ 异常处理和日志记录完整
- ✅ 集成测试通过（使用 Testcontainers）
- ✅ 性能测试：Get操作 < 10ms（本地网络）

#### 文件清单
1. `src/Infrastructure/Caching/CKY.MAF.Infrastructure.Caching.csproj`
2. `src/Infrastructure/Caching/RedisCacheStore.cs`
3. `src/Infrastructure/Caching/MemoryCacheStore.cs`
4. `tests/IntegrationTests/Caching/RedisCacheStoreTests.cs`

---

### Task 2: 实现完整任务调度器 ⭐⭐⭐⭐⭐
**优先级**: P0 - 核心
**工作量**: 2天
**依赖**: Redis缓存（Task 1）

#### 目标
实现 `MafTaskScheduler` 类，支持多任务编排和依赖调度。

#### 接口定义
```csharp
// src/Services/Scheduling/ITaskScheduler.cs:10
public interface ITaskScheduler
{
    Task<ScheduleResult> ScheduleAsync(List<DecomposedTask> tasks, CancellationToken ct = default);
    Task<TaskExecutionResult> ExecuteTaskAsync(
        DecomposedTask task,
        Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> executor,
        CancellationToken ct = default);
}
```

#### 实现方案

**核心职责**:
1. **依赖图构建**: 分析任务依赖关系，构建DAG（有向无环图）
2. **优先级计算**: 结合时间因子、资源使用、用户交互计算优先级
3. **并行/串行调度**: 根据依赖关系决定执行顺序
4. **状态管理**: 跟踪任务状态（待执行、执行中、已完成、失败）

**项目结构**:
```
src/Services/Scheduling/
├── ITaskScheduler.cs              # 接口（已存在）
├── MafTaskScheduler.cs            # 主实现（新增）
├── PriorityCalculator.cs          # 优先级计算器（新增）
├── TaskDependencyGraph.cs         # 依赖图（新增）
└── ScheduleResult.cs              # 调度结果（新增）
```

**核心实现代码结构**:

```csharp
// MafTaskScheduler.cs
public class MafTaskScheduler : ITaskScheduler
{
    private readonly ICacheStore _cacheStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriorityCalculator _priorityCalculator;
    private readonly ILogger<MafTaskScheduler> _logger;

    public MafTaskScheduler(
        ICacheStore cacheStore,
        IUnitOfWork unitOfWork,
        IPriorityCalculator priorityCalculator,
        ILogger<MafTaskScheduler> logger)
    {
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _priorityCalculator = priorityCalculator ?? throw new ArgumentNullException(nameof(priorityCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScheduleResult> ScheduleAsync(List<DecomposedTask> tasks, CancellationToken ct = default)
    {
        try
        {
            // 1. 计算优先级
            foreach (var task in tasks)
            {
                task.PriorityScore = await _priorityCalculator.CalculateAsync(task, ct);
            }

            // 2. 构建依赖图
            var graph = new TaskDependencyGraph(tasks);
            if (!graph.Validate())
            {
                throw new InvalidOperationException("Task dependencies contain cycles");
            }

            // 3. 生成执行计划（分层调度）
            var executionGroups = graph.GenerateExecutionGroups();
            var executionPlan = new ExecutionPlan(executionGroups);

            // 4. 缓存执行计划
            await _cacheStore.SetAsync(
                $"schedule:{executionPlan.PlanId}",
                executionPlan,
                TimeSpan.FromHours(24),
                ct);

            // 5. 持久化到数据库
            await _unitOfWork.MainTaskRepository.AddAsync(executionPlan.ToMainTask(), ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ScheduleResult
            {
                PlanId = executionPlan.PlanId,
                TotalTasks = tasks.Count,
                EstimatedDuration = executionPlan.EstimatedDuration,
                ExecutionGroups = executionGroups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule tasks");
            throw;
        }
    }

    public async Task<TaskExecutionResult> ExecuteTaskAsync(
        DecomposedTask task,
        Func<DecomposedTask, CancellationToken, Task<TaskExecutionResult>> executor,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Executing task: {TaskId}", task.TaskId);

            // 更新任务状态为执行中
            task.Status = MafTaskStatus.InProgress;
            await _cacheStore.SetAsync($"task:{task.TaskId}", task, TimeSpan.FromHours(1), ct);

            // 执行任务
            var result = await executor(task, ct);

            // 更新任务状态为完成
            task.Status = MafTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            await _cacheStore.SetAsync($"task:{task.TaskId}", task, TimeSpan.FromHours(24), ct);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task execution failed: {TaskId}", task.TaskId);
            task.Status = MafTaskStatus.Failed;
            task.Error = ex.Message;
            await _cacheStore.SetAsync($"task:{task.TaskId}", task, TimeSpan.FromHours(24), ct);
            throw;
        }
    }
}

// PriorityCalculator.cs
public class PriorityCalculator : IPriorityCalculator
{
    private readonly ILogger<PriorityCalculator> _logger;

    public PriorityCalculator(ILogger<PriorityCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> CalculateAsync(DecomposedTask task, CancellationToken ct = default)
    {
        int score = 0;

        // 时间因子（40%）
        var timeScore = CalculateTimeScore(task);
        score += (int)(timeScore * 0.4);

        // 资源使用（30%）
        var resourceScore = CalculateResourceScore(task);
        score += (int)(resourceScore * 0.3);

        // 用户交互类型（20%）
        var interactionScore = CalculateInteractionScore(task);
        score += (int)(interactionScore * 0.2);

        // 任务复杂度（10%）
        var complexityScore = CalculateComplexityScore(task);
        score += (int)(complexityScore * 0.1);

        return Math.Clamp(score, 0, 100);
    }

    private int CalculateTimeScore(DecomposedTask task)
    {
        if (task.TimeFactor == TimeFactor.Immediate) return 100;
        if (task.TimeFactor == TimeFactor.Urgent) return 80;
        if (task.TimeFactor == TimeFactor.Normal) return 50;
        return 20; // Low priority
    }

    private int CalculateResourceScore(DecomposedTask task)
    {
        if (task.ResourceUsage == ResourceUsage.Critical) return 100;
        if (task.ResourceUsage == ResourceUsage.High) return 80;
        if (task.ResourceUsage == ResourceUsage.Medium) return 50;
        return 30;
    }

    private int CalculateInteractionScore(DecomposedTask task)
    {
        if (task.UserInteractionType == UserInteractionType.Direct) return 100;
        if (task.UserInteractionType == UserInteractionType.Indirect) return 60;
        return 30; // Automated
    }

    private int CalculateComplexityScore(DecomposedTask task)
    {
        // 简单任务优先
        if (task.SubTasks.Count == 0) return 100;
        if (task.SubTasks.Count <= 3) return 70;
        return 40;
    }
}

// TaskDependencyGraph.cs
public class TaskDependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _adjacencyList;
    private readonly List<DecomposedTask> _tasks;

    public TaskDependencyGraph(List<DecomposedTask> tasks)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _adjacencyList = new Dictionary<string, HashSet<string>>();

        BuildGraph();
    }

    private void BuildGraph()
    {
        foreach (var task in _tasks)
        {
            _adjacencyList.TryAdd(task.TaskId, new HashSet<string>());

            foreach (var dependency in task.DependsOnTaskIds)
            {
                if (_adjacencyList.TryGetValue(dependency, out var dependencies))
                {
                    dependencies.Add(task.TaskId);
                }
                else
                {
                    _adjacencyList[dependency] = new HashSet<string> { task.TaskId };
                }
            }
        }
    }

    public bool Validate()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var task in _tasks)
        {
            if (HasCycle(task.TaskId, visited, recursionStack))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasCycle(string taskId, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(taskId))
            return true;

        if (visited.Contains(taskId))
            return false;

        visited.Add(taskId);
        recursionStack.Add(taskId);

        if (_adjacencyList.TryGetValue(taskId, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                if (HasCycle(dependency, visited, recursionStack))
                    return true;
            }
        }

        recursionStack.Remove(taskId);
        return false;
    }

    public List<List<DecomposedTask>> GenerateExecutionGroups()
    {
        var groups = new List<List<DecomposedTask>>();
        var inDegree = new Dictionary<string, int>();
        var taskMap = _tasks.ToDictionary(t => t.TaskId);

        // 计算入度
        foreach (var task in _tasks)
        {
            inDegree[task.TaskId] = task.DependsOnTaskIds.Count;
        }

        var queue = new Queue<string>();

        // 找出所有入度为0的任务
        foreach (var task in _tasks)
        {
            if (inDegree[task.TaskId] == 0)
            {
                queue.Enqueue(task.TaskId);
            }
        }

        while (queue.Count > 0)
        {
            var currentGroup = new List<DecomposedTask>();
            var groupSize = queue.Count;

            for (int i = 0; i < groupSize; i++)
            {
                var taskId = queue.Dequeue();
                currentGroup.Add(taskMap[taskId]);

                // 减少依赖此任务的其他任务的入度
                if (_adjacencyList.TryGetValue(taskId, out var dependencies))
                {
                    foreach (var dependency in dependencies)
                    {
                        inDegree[dependency]--;
                        if (inDegree[dependency] == 0)
                        {
                            queue.Enqueue(dependency);
                        }
                    }
                }
            }

            groups.Add(currentGroup);
        }

        return groups;
    }
}
```

**单元测试**:
```csharp
// tests/UnitTests/Scheduling/MafTaskSchedulerTests.cs
public class MafTaskSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldCalculatePriority()
    {
        // Arrange
        var mockCache = new Mock<ICacheStore>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockPriorityCalc = new Mock<IPriorityCalculator>();
        mockPriorityCalc.Setup(x => x.CalculateAsync(It.IsAny<DecomposedTask>(), default))
            .ReturnsAsync(80);

        var scheduler = new MafTaskScheduler(
            mockCache.Object,
            mockUow.Object,
            mockPriorityCalc.Object,
            Mock.Of<ILogger>());

        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "task1", TimeFactor = TimeFactor.Immediate }
        };

        // Act
        var result = await scheduler.ScheduleAsync(tasks);

        // Assert
        Assert.NotNull(result);
        mockPriorityCalc.Verify(x => x.CalculateAsync(It.IsAny<DecomposedTask>(), default), Times.Once);
    }

    [Fact]
    public void TaskDependencyGraph_ShouldDetectCycles()
    {
        // Arrange
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "A", DependsOnTaskIds = new[] { "B" } },
            new() { TaskId = "B", DependsOnTaskIds = new[] { "C" } },
            new() { TaskId = "C", DependsOnTaskIds = new[] { "A" } } // 循环依赖
        };

        // Act
        var graph = new TaskDependencyGraph(tasks);

        // Assert
        Assert.False(graph.Validate());
    }

    [Fact]
    public void TaskDependencyGraph_ShouldGenerateExecutionGroups()
    {
        // Arrange
        var tasks = new List<DecomposedTask>
        {
            new() { TaskId = "A", DependsOnTaskIds = Array.Empty<string>() },
            new() { TaskId = "B", DependsOnTaskIds = new[] { "A" } },
            new() { TaskId = "C", DependsOnTaskIds = new[] { "A" } },
            new() { TaskId = "D", DependsOnTaskIds = new[] { "B", "C" } }
        };

        // Act
        var graph = new TaskDependencyGraph(tasks);
        var groups = graph.GenerateExecutionGroups();

        // Assert
        Assert.Equal(3, groups.Count);
        Assert.Single(groups[0]); // A
        Assert.Equal(2, groups[1].Count); // B, C
        Assert.Single(groups[2]); // D
    }
}
```

#### 验收标准
- ✅ 依赖图构建和验证正确
- ✅ 优先级计算逻辑完整
- ✅ 支持并行/串行任务调度
- ✅ 单元测试覆盖率 > 90%
- ✅ 集成测试验证完整调度流程

#### 文件清单
1. `src/Services/Scheduling/MafTaskScheduler.cs`
2. `src/Services/Scheduling/PriorityCalculator.cs`
3. `src/Services/Scheduling/TaskDependencyGraph.cs`
4. `src/Services/Scheduling/ScheduleResult.cs`
5. `tests/UnitTests/Scheduling/MafTaskSchedulerTests.cs`

---

### Task 3: 统一 Agent 继承层次 ⭐⭐⭐⭐⭐
**优先级**: P0 - 架构一致性
**工作量**: 1.5天
**依赖**: 无

#### 目标
合并 `MafAgentBase` 和 `LlmAgent` 两条继承路线，建立统一的 Agent 继承层次。

#### 当前问题
```csharp
// 路线1：MafAgentBase（组合模式，不继承AIAgent）
// src/Core/Agents/MafAgentBase.cs:21
public abstract class MafAgentBase
{
    public abstract Task<string> ExecuteAsync(...);
}

// 路线2：LlmAgent（继承AIAgent）
// src/Core/Agents/LlmAgent.cs:21
public abstract class LlmAgent : AIAgent
{
    public abstract Task<string> ExecuteAsync(...);
}
```

#### 解决方案

**统一架构决策**:
- **保留 `LlmAgent : AIAgent`** 路线（符合设计文档）
- **废弃 `MafAgentBase`** 组合模式路线
- **迁移现有实现**到新的继承层次

**新的继承层次**:
```
AIAgent (MS Agent Framework)
  ↓
LlmAgent (CKY.MAF 抽象基类)
  ↓
ZhipuAIAgent, QwenAIAgent (具体厂商实现)
  ↓
LightingAgent, ClimateAgent, MusicAgent (Demo域Agent)
```

**迁移步骤**:

**步骤1**: 更新 `LlmAgent` 基类
```csharp
// src/Core/Agents/LlmAgent.cs (已存在，无需修改)
// 当前实现已经正确继承 AIAgent，保持不变
```

**步骤2**: 标记 `MafAgentBase` 为废弃
```csharp
// src/Core/Agents/MafAgentBase.cs
namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// [已废弃] 请使用 LlmAgent : AIAgent 代替
    /// 此类保留仅用于向后兼容，将在 v2.0 移除
    /// </summary>
    [Obsolete("Use LlmAgent : AIAgent instead. This class will be removed in v2.0.", true)]
    public abstract class MafAgentBase
    {
        // 原有实现保持不变，但编译器会报错
    }
}
```

**步骤3**: 迁移 Demo Agent
```csharp
// src/Demos/SmartHome/Agents/LightingAgent.cs
// 修改前：
public class LightingAgent : MafAgentBase
{
    public override Task<string> ExecuteAsync(...)
}

// 修改后：
public class LightingAgent : LlmAgent
{
    public LightingAgent(
        LlmProviderConfig config,
        ILogger<LightingAgent> logger)
        : base(config, logger)
    {
    }

    public override Task<string> ExecuteAsync(...)
    {
        // 具体实现
    }
}
```

**步骤4**: 更新域 Agent 工厂
```csharp
// src/Demos/SmartHome/SmartHomeAgentFactory.cs
public class SmartHomeAgentFactory
{
    public static T CreateAgent<T>(LlmProviderConfig config, ILogger logger) where T : LlmAgent
    {
        if (typeof(T) == typeof(LightingAgent))
        {
            return (T)(object)new LightingAgent(config, logger);
        }
        // ... 其他Agent
        throw new NotSupportedException($"Agent type {typeof(T).Name} is not supported");
    }
}
```

#### 验收标准
- ✅ 所有 Agent 继承自 `LlmAgent : AIAgent`
- ✅ `MafAgentBase` 标记为 Obsolete
- ✅ 所有 Demo Agent 使用新的继承层次
- ✅ 单元测试全部通过
- ✅ 编译无警告

#### 文件清单
1. `src/Core/Agents/MafAgentBase.cs`（标记为废弃）
2. `src/Core/Agents/LlmAgent.cs`（保持不变）
3. `src/Demos/SmartHome/Agents/LightingAgent.cs`（迁移）
4. `src/Demos/SmartHome/Agents/ClimateAgent.cs`（迁移）
5. `src/Demos/SmartHome/Agents/MusicAgent.cs`（迁移）

---

## 🚀 第二周：P1 重要任务（Days 6-10）

### Task 4: 实现 Qdrant 向量存储 ⭐⭐⭐⭐
**优先级**: P1 - 重要
**工作量**: 2天
**依赖**: 无

#### 目标
实现 `QdrantVectorStore : IVectorStore`，支持语义检索和RAG功能。

#### 接口定义
```csharp
// src/Core/Abstractions/IVectorStore.cs:37
public interface IVectorStore
{
    Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default);
    Task InsertAsync(string collectionName, IEnumerable<VectorPoint> points, CancellationToken ct = default);
    Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] vector, int topK = 10, Dictionary<string, object>? filter = null, CancellationToken ct = default);
    Task DeleteAsync(string collectionName, IEnumerable<string> ids, CancellationToken ct = default);
    Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default);
}
```

#### 实现方案

**方案选择**: Qdrant.Client 1.9.0

**项目结构**:
```
src/Infrastructure/Vectorization/
├── CKY.MAF.Infrastructure.Vectorization.csproj
├── QdrantVectorStore.cs           # 主实现
├── MemoryVectorStore.cs           # 测试用实现
└── QdrantVectorStoreExtensions.cs # 扩展方法
```

**核心实现代码结构**:

```csharp
// QdrantVectorStore.cs
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(
        QdrantClient client,
        ILogger<QdrantVectorStore> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default)
    {
        try
        {
            await _client.CreateCollectionAsync(
                collectionName,
                new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine
                },
                cancellationToken: ct);

            _logger.LogInformation("Created collection: {CollectionName} with vector size: {VectorSize}",
                collectionName, vectorSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection: {CollectionName}", collectionName);
            throw;
        }
    }

    public async Task InsertAsync(string collectionName, IEnumerable<VectorPoint> points, CancellationToken ct = default)
    {
        try
        {
            var pointsList = points.Select(p => new PointStruct
            {
                Id = new PointId { Uuid = p.Id },
                Vectors = new Vectors { Vector = p.Vector.ToArray() },
                Payload = p.Metadata.ToPayload()
            }).ToList();

            await _client.UpsertAsync(
                collectionName,
                pointsList,
                wait: true,
                cancellationToken: ct);

            _logger.LogInformation("Inserted {Count} points into collection: {CollectionName}",
                pointsList.Count, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert points into collection: {CollectionName}", collectionName);
            throw;
        }
    }

    public async Task<List<VectorSearchResult>> SearchAsync(
        string collectionName,
        float[] vector,
        int topK = 10,
        Dictionary<string, object>? filter = null,
        CancellationToken ct = default)
    {
        try
        {
            var searchResult = await _client.SearchAsync(
                collectionName,
                vector,
                limit: (ulong)topK,
                filter: filter?.ToQdrantFilter(),
                cancellationToken: ct);

            return searchResult.Select(r => new VectorSearchResult
            {
                Id = r.Id.Uuid,
                Score = r.Score,
                Metadata = r.Payload.ToDictionary()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search collection: {CollectionName}", collectionName);
            return new List<VectorSearchResult>();
        }
    }

    public async Task DeleteAsync(string collectionName, IEnumerable<string> ids, CancellationToken ct = default)
    {
        try
        {
            var pointIds = ids.Select(id => new PointId { Uuid = id }).ToArray();
            await _client.DeleteAsync(collectionName, pointIds, cancellationToken: ct);

            _logger.LogInformation("Deleted {Count} points from collection: {CollectionName}",
                pointIds.Length, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete points from collection: {CollectionName}", collectionName);
            throw;
        }
    }

    public async Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteCollectionAsync(collectionName, cancellationToken: ct);
            _logger.LogInformation("Deleted collection: {CollectionName}", collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection: {CollectionName}", collectionName);
            throw;
        }
    }
}
```

**依赖注入配置**:
```csharp
// Program.cs
builder.Services.AddSingleton<QdrantClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var host = configuration["Qdrant:Host"] ?? "localhost";
    var port = int.Parse(configuration["Qdrant:Port"] ?? "6334");
    return new QdrantClient(host, port);
});

builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
```

**配置文件**:
```json
{
  "Qdrant": {
    "Host": "localhost",
    "Port": "6334"
  }
}
```

#### 验收标准
- ✅ 所有接口方法实现完成
- ✅ 支持Payload（元数据）的序列化/反序列化
- ✅ 集成测试通过（使用 Testcontainers Qdrant）
- ✅ 向量检索精度测试通过

#### 文件清单
1. `src/Infrastructure/Vectorization/CKY.MAF.Infrastructure.Vectorization.csproj`
2. `src/Infrastructure/Vectorization/QdrantVectorStore.cs`
3. `src/Infrastructure/Vectorization/MemoryVectorStore.cs`
4. `tests/IntegrationTests/Vectorization/QdrantVectorStoreTests.cs`

---

### Task 5: 实现 A2A 通信机制 ⭐⭐⭐⭐
**优先级**: P1 - 核心协作
**工作量**: 1.5天
**依赖**: Task 3（Agent继承统一）

#### 目标
实现 MS Agent Framework 的 Agent-to-Agent (A2A) 通信机制，支持Agent间协作。

#### 设计方案

**A2A 通信模式**:
1. **直接调用**: MainAgent 直接调用 SubAgent 的方法
2. **消息传递**: 通过 MS AF 的消息传递机制
3. **事件驱动**: 基于事件的异步通信

**实现步骤**:

**步骤1**: 定义 A2A 消息格式
```csharp
// src/Core/Models/Agent/AgentMessage.cs
public class AgentMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string FromAgentId { get; set; } = string.Empty;
    public string ToAgentId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty; // Request, Response, Notification
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum AgentMessageType
{
    Request,
    Response,
    Notification,
    Error
}
```

**步骤2**: 实现 A2A 通信服务
```csharp
// src/Services/Communication/A2ACommunicationService.cs
public class A2ACommunicationService : IA2ACommunicationService
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<A2ACommunicationService> _logger;

    public A2ACommunicationService(
        IAgentRegistry agentRegistry,
        ICacheStore cacheStore,
        ILogger<A2ACommunicationService> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentMessage> SendAsync(AgentMessage message, CancellationToken ct = default)
    {
        try
        {
            // 1. 查找目标 Agent
            var targetAgent = await _agentRegistry.GetAgentAsync(message.ToAgentId, ct);
            if (targetAgent == null)
            {
                throw new InvalidOperationException($"Agent {message.ToAgentId} not found");
            }

            // 2. 发送消息（调用Agent的处理方法）
            var response = await targetAgent.ReceiveMessageAsync(message, ct);

            // 3. 记录消息历史
            await _cacheStore.SetAsync(
                $"message:{message.MessageId}",
                message,
                TimeSpan.FromHours(24),
                ct);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message from {From} to {To}",
                message.FromAgentId, message.ToAgentId);
            throw;
        }
    }

    public async Task BroadcastAsync(AgentMessage message, CancellationToken ct = default)
    {
        var agents = await _agentRegistry.GetAllAgentsAsync(ct);

        var tasks = agents.Select(async agent =>
        {
            try
            {
                message.ToAgentId = agent.AgentId;
                await SendAsync(message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send message to {AgentId}", agent.AgentId);
            }
        });

        await Task.WhenAll(tasks);
    }
}
```

**步骤3**: 扩展 LlmAgent 支持消息接收
```csharp
// src/Core/Agents/LlmAgent.cs (扩展现有类)
public abstract class LlmAgent : AIAgent
{
    // ... 现有代码 ...

    /// <summary>
    /// 接收来自其他Agent的消息（A2A通信）
    /// </summary>
    public virtual async Task<AgentMessage> ReceiveMessageAsync(
        AgentMessage message,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Agent {AgentId} received message from {FromAgentId}: {MessageType}",
                AgentId, message.FromAgentId, message.MessageType);

            // 处理不同类型的消息
            switch (message.MessageType)
            {
                case nameof(AgentMessageType.Request):
                    return await HandleRequestAsync(message, ct);

                case nameof(AgentMessageType.Notification):
                    await HandleNotificationAsync(message, ct);
                    return message;

                default:
                    throw new NotSupportedException($"Message type {message.MessageType} is not supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle message: {MessageId}", message.MessageId);
            return new AgentMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                FromAgentId = AgentId,
                ToAgentId = message.FromAgentId,
                MessageType = nameof(AgentMessageType.Error),
                Content = ex.Message
            };
        }
    }

    protected virtual async Task<AgentMessage> HandleRequestAsync(AgentMessage request, CancellationToken ct)
    {
        // 默认实现：将消息内容作为 Prompt 调用 LLM
        var responseText = await ExecuteAsync(
            Config.ModelId,
            request.Content,
            LlmScenario.Chat,
            systemPrompt: "You are a helpful AI agent responding to another agent.",
            ct);

        return new AgentMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            FromAgentId = AgentId,
            ToAgentId = request.FromAgentId,
            MessageType = nameof(AgentMessageType.Response),
            Content = responseText
        };
    }

    protected virtual async Task HandleNotificationAsync(AgentMessage notification, CancellationToken ct)
    {
        // 默认实现：记录日志
        _logger.LogInformation("Received notification: {Content}", notification.Content);
        await Task.CompletedTask;
    }
}
```

**步骤4**: Demo 中实现 MainAgent 协调
```csharp
// src/Demos/SmartHome/SmartHomeMainAgent.cs
public class SmartHomeMainAgent : LlmAgent
{
    private readonly IA2ACommunicationService _communicationService;

    public SmartHomeMainAgent(
        LlmProviderConfig config,
        ILogger<SmartHomeMainAgent> logger,
        IA2ACommunicationService communicationService)
        : base(config, logger)
    {
        _communicationService = communicationService ?? throw new ArgumentNullException(nameof(communicationService));
    }

    public async Task<string> CoordinateAgentsAsync(string userIntent, CancellationToken ct = default)
    {
        // 1. 识别意图（意图识别器）
        var intent = await RecognizeIntentAsync(userIntent, ct);

        // 2. 分配给相应的域 Agent
        var agentMessage = new AgentMessage
        {
            FromAgentId = AgentId,
            ToAgentId = $"lighting-agent", // 根据意图选择
            MessageType = nameof(AgentMessageType.Request),
            Content = userIntent
        };

        var response = await _communicationService.SendAsync(agentMessage, ct);

        // 3. 聚合结果
        return response.Content;
    }
}
```

#### 验收标准
- ✅ A2A 消息格式定义完整
- ✅ 支持点对点和广播通信
- ✅ Demo 中演示 Agent 协作场景
- ✅ 单元测试覆盖消息传递流程
- ✅ 集成测试验证多 Agent 协作

#### 文件清单
1. `src/Core/Models/Agent/AgentMessage.cs`
2. `src/Core/Abstractions/IA2ACommunicationService.cs`
3. `src/Services/Communication/A2ACommunicationService.cs`
4. `src/Core/Agents/LlmAgent.cs`（扩展）
5. `src/Demos/SmartHome/SmartHomeMainAgent.cs`（更新）
6. `tests/UnitTests/Communication/A2ACommunicationServiceTests.cs`

---

### Task 6: 完成 Blazor UI 基础组件 ⭐⭐⭐
**优先级**: P1 - 演示必需
**工作量**: 2天
**依赖**: Task 5（A2A通信）

#### 目标
实现智能家居 Demo 的 Blazor UI 界面，支持用户交互。

#### 设计方案

**UI 组件**:
1. **主页面**:智能家居控制面板
2. **聊天界面**: 与 MainAgent 对话
3. **设备控制**: 灯光、温度、音乐控制
4. **状态展示**: Agent 状态、任务执行状态

**项目结构**:
```
src/Demos/SmartHome/Pages/
├── Index.razor              # 主页
├── Chat.razor               # 聊天界面
├── DeviceControl.razor      # 设备控制
└── AgentStatus.razor        # Agent状态

src/Demos/SmartHome/Shared/
├── MainLayout.razor         # 主布局
├── ChatMessage.razor        # 聊天消息组件
└── DeviceCard.razor         # 设备卡片组件

src/Demos/SmartHome/Services/
├── SmartHomeUIService.cs    # UI服务
└── ChatService.cs           # 聊天服务
```

**核心实现示例**:

```razor
<!-- src/Demos/SmartHome/Pages/Chat.razor -->
@page "/chat"
@inject SmartHomeUIService UIService
@inject ChatService ChatService

<div class="chat-container">
    <div class="chat-messages">
        @foreach (var message in messages)
        {
            <ChatMessage Message="@message" />
        }
    </div>

    <div class="chat-input">
        <input @bind="userInput" placeholder="输入您的指令..." />
        <button @onclick="SendMessage">发送</button>
    </div>
</div>

@code {
    private List<ChatMessageModel> messages = new();
    private string userInput = string.Empty;

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        // 添加用户消息
        messages.Add(new ChatMessageModel
        {
            Role = "User",
            Content = userInput,
            Timestamp = DateTime.Now
        });

        var userMessage = userInput;
        userInput = string.Empty;

        try
        {
            // 调用 MainAgent 处理
            var response = await ChatService.SendMessageAsync(userMessage);

            // 添加 Agent 响应
            messages.Add(new ChatMessageModel
            {
                Role = "Agent",
                Content = response,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            messages.Add(new ChatMessageModel
            {
                Role = "System",
                Content = $"错误: {ex.Message}",
                Timestamp = DateTime.Now
            });
        }
    }
}
```

#### 验收标准
- ✅ 主页面布局完整
- ✅ 聊天界面可正常交互
- ✅ 设备控制功能可用
- ✅ Agent 状态实时展示
- ✅ UI 响应式设计（支持移动端）

#### 文件清单
1. `src/Demos/SmartHome/Pages/Index.razor`
2. `src/Demos/SmartHome/Pages/Chat.razor`
3. `src/Demos/SmartHome/Pages/DeviceControl.razor`
4. `src/Demos/SmartHome/Pages/AgentStatus.razor`
5. `src/Demos/SmartHome/Shared/MainLayout.razor`
6. `src/Demos/SmartHome/Shared/ChatMessage.razor`
7. `src/Demos/SmartHome/Services/SmartHomeUIService.cs`

---

### Task 7: 补充集成测试 ⭐⭐⭐
**优先级**: P1 - 质量保证
**工作量**: 1天
**依赖**: Task 1-6

#### 目标
使用 Testcontainers 补充完整的集成测试，覆盖所有关键组件。

#### 测试策略

**集成测试范围**:
1. **Redis 缓存**: 使用 Testcontainers Redis
2. **Qdrant 向量存储**: 使用 Testcontainers Qdrant
3. **SQLite 数据库**: 使用内存数据库
4. **A2A 通信**: 多 Agent 协作测试
5. **端到端流程**: 完整的用户请求处理

**项目结构**:
```
tests/IntegrationTests/
├── Caching/
│   └── RedisCacheStoreTests.cs
├── Vectorization/
│   └── QdrantVectorStoreTests.cs
├── Communication/
│   └── A2ACommunicationTests.cs
├── Scheduling/
│   └── MafTaskSchedulerIntegrationTests.cs
└── EndToEnd/
    └── SmartHomeScenarioTests.cs
```

**测试示例**:

```csharp
// tests/IntegrationTests/EndToEnd/SmartHomeScenarioTests.cs
public class SmartHomeScenarioTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly QdrantContainer _qdrantContainer;
    private WebApplicationFactory<Program> _appFactory;
    private HttpClient _httpClient;

    public SmartHomeScenarioTests()
    {
        _redisContainer = new RedisBuilder().Build();
        _qdrantContainer = new QdrantBuilder().Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _qdrantContainer.StartAsync();

        _appFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IConnectionMultiplexer>(sp =>
                        ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
                    services.AddSingleton<QdrantClient>(sp =>
                        new QdrantClient(_qdrantContainer.GetHostname()));
                });
            });

        _httpClient = _appFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        await _qdrantContainer.StopAsync();
        _appFactory.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task CompleteScenario_MorningRoutine_ShouldWork()
    {
        // Arrange
        var userIntent = "早上好，帮我开启晨间模式";

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/chat", new
        {
            Message = userIntent
        });

        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("灯光", result.Response);
        Assert.Contains("温度", result.Response);
    }
}
```

#### 验收标准
- ✅ 所有 Infrastructure 组件有集成测试
- ✅ 端到端场景测试通过
- ✅ 测试覆盖率 > 60%（集成测试）
- ✅ 所有测试可在 CI/CD 环境运行

#### 文件清单
1. `tests/IntegrationTests/Caching/RedisCacheStoreTests.cs`
2. `tests/IntegrationTests/Vectorization/QdrantVectorStoreTests.cs`
3. `tests/IntegrationTests/Communication/A2ACommunicationTests.cs`
4. `tests/IntegrationTests/EndToEnd/SmartHomeScenarioTests.cs`

---

## 🔧 第三周：P2 优化任务（Days 11-14）

### Task 8: 实现 SignalR 实时通信 ⭐⭐⭐
**优先级**: P2 - 用户体验
**工作量**: 1.5天

#### 目标
实现 SignalR Hub，支持实时推送任务状态更新和 Agent 响应。

#### 实现方案
```csharp
// src/Infrastructure/Messaging/CKY.MAFHub.cs
public class CKY_MAFHub : Hub
{
    private readonly ILogger<CKY_MAFHub> _logger;

    public CKY_MAFHub(ILogger<CKY_MAFHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task JoinTaskGroup(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task:{taskId}");
        _logger.LogInformation("Connection {ConnectionId} joined task group {TaskId}",
            Context.ConnectionId, taskId);
    }

    public async Task LeaveTaskGroup(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task:{taskId}");
    }

    // 服务器端调用此方法推送更新
    public async Task PushTaskUpdate(string taskId, TaskUpdate update)
    {
        await Clients.Group($"task:{taskId}").SendAsync("ReceiveTaskUpdate", update);
    }
}
```

#### 文件清单
1. `src/Infrastructure/Messaging/CKY.MAFHub.cs`
2. `src/Demos/SmartHome/Pages/Chat.razor`（添加 SignalR 连接）

---

### Task 9: 完善 Prometheus 监控 ⭐⭐
**优先级**: P2 - 可观测性
**工作量**: 1天

#### 目标
完善 Prometheus 指标收集和上报，支持性能监控。

#### 实现方案
```csharp
// src/Services/Monitoring/PrometheusMetricsCollector.cs
public class PrometheusMetricsCollector : IMetricsCollector
{
    private readonly Counter _requestCounter;
    private readonly Histogram _responseTimeHistogram;
    private readonly Gauge _activeTasksGauge;

    public PrometheusMetricsCollector()
    {
        var registry = Metrics.DefaultRegistry;

        _requestCounter = Metrics.CreateCounter(
            "maf_requests_total",
            "Total number of requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "agent", "scenario" }
            });

        _responseTimeHistogram = Metrics.CreateHistogram(
            "maf_response_time_seconds",
            "Response time in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "agent" },
                Buckets = Histogram.ExponentialBuckets(0.01, 2, 10)
            });

        _activeTasksGauge = Metrics.CreateGauge(
            "maf_active_tasks",
            "Number of active tasks");
    }

    public void RecordRequest(string agentId, string scenario)
    {
        _requestCounter.WithLabels(agentId, scenario).Inc();
    }

    public void RecordResponseTime(string agentId, double seconds)
    {
        _responseTimeHistogram.WithLabels(agentId).Observe(seconds);
    }

    public void SetActiveTasks(int count)
    {
        _activeTasksGauge.Set(count);
    }
}
```

#### 文件清单
1. `src/Services/Monitoring/PrometheusMetricsCollector.cs`
2. `src/Demos/SmartHome/Program.cs`（添加 Prometheus 端点）

---

### Task 10: 更新架构文档 ⭐
**优先级**: P2 - 文档同步
**工作量**: 0.5天

#### 目标
更新架构文档，反映 Repository 层的变化。

#### 更新内容
1. 更新 `12-layered-architecture.md`，添加 Repository 层说明
2. 更新 `11-implementation-roadmap.md`，标记已完成任务
3. 创建 `ARCHITECTURE_CHANGES.md`，记录架构变更

#### 文件清单
1. `docs/specs/12-layered-architecture.md`（更新）
2. `docs/specs/11-implementation-roadmap.md`（更新）
3. `docs/ARCHITECTURE_CHANGES.md`（新增）

---

## 📋 依赖关系图

```
Task 1 (Redis缓存)
    ↓
Task 2 (任务调度器) ──────┐
    ↓                     │
Task 3 (Agent继承统一) ────┤
    ↓                     │
Task 4 (Qdrant向量存储)   │
    ↓                     │
Task 5 (A2A通信) ─────────┤
    ↓                     │
Task 6 (Blazor UI) ───────┤
    ↓                     ↓
Task 7 (集成测试) ←───────┘
    ↓
Task 8 (SignalR)
    ↓
Task 9 (Prometheus)
    ↓
Task 10 (文档更新)
```

---

## ✅ 验收标准总结

### P0 任务（Week 1）
- ✅ Redis缓存可正常读写
- ✅ 任务调度器可编排多任务
- ✅ Agent继承层次统一
- ✅ 单元测试覆盖率 > 80%

### P1 任务（Week 2）
- ✅ Qdrant向量存储可检索
- ✅ A2A通信支持Agent协作
- ✅ Blazor UI可正常交互
- ✅ 集成测试覆盖率 > 60%

### P2 任务（Week 3）
- ✅ SignalR实时推送可用
- ✅ Prometheus指标正常上报
- ✅ 文档与代码同步

---

## 📊 预期成果

### 完成后的系统能力
1. **三层存储架构完整**: L1内存 + L2Redis + L3SQLite
2. **语义检索可用**: 基于Qdrant的向量搜索
3. **Agent协作正常**: A2A通信支持多Agent场景
4. **用户体验良好**: Blazor UI + SignalR实时推送
5. **可观测性强**: Prometheus监控 + 完善的日志

### 质量指标
- **代码覆盖率**: 单元测试 85%+，集成测试 65%+
- **性能指标**: 简单任务 < 2s，复杂任务 < 10s
- **文档完整性**: 所有公开API有文档
- **架构一致性**: 遵循DIP原则，依赖关系清晰

---

## 🎯 执行建议

### 团队配置
- **后端开发**: 2人（负责 Task 1-5, 8-9）
- **前端开发**: 1人（负责 Task 6, SignalR集成）
- **测试工程师**: 1人（负责 Task 7）

### 工作流程
1. **每日站会**: 同步进度，识别阻塞
2. **代码审查**: 所有PR需要至少1人审查
3. **持续集成**: 每次提交自动运行测试
4. **文档同步**: 代码和文档同时更新

### 风险管理
- **技术风险**: MS AF API可能变化 → 使用适配器模式
- **进度风险**: 任务延期 → 预留2天缓冲时间
- **质量风险**: 测试覆盖不足 → 强制代码覆盖率要求

---

**文档维护**: CKY.MAF 架构团队
**最后更新**: 2026-03-13
**下次审查**: 2026-03-20（Week 1结束时）

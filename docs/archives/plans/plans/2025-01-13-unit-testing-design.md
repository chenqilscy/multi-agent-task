# CKY.MAF 单元测试设计文档

**文档版本**: 1.0
**创建日期**: 2025-01-13
**作者**: Claude
**状态**: 设计阶段

---

## 1. 概述

### 1.1 目标

为 CKY.MAF 项目的 **Repository 层**和 **Core Services 层**设计并实现全面的单元测试，确保代码质量和系统稳定性。

### 1.2 范围

**包含的组件：**
- **Repository 层**（6 个仓储）：
  - MainTaskRepository
  - SubTaskRepository
  - SchedulePlanRepository
  - ExecutionPlanRepository
  - TaskExecutionResultRepository
  - LlmProviderConfigRepository

- **Services 层**（核心服务）：
  - Scheduling: MafTaskScheduler, MafPriorityCalculator
  - Orchestration: MafTaskDecomposer, MafTaskOrchestrator
  - Resilience: CircuitBreaker, RetryExecutor
  - Factory: MafAiAgentFactory
  - Storage: MafAgentRegistry

**不包含的组件：**
- NLP 层（已有独立测试）
- Demo 应用层
- 性能测试
- 并发测试
- E2E 测试

### 1.3 覆盖率目标

| 层级 | 目标覆盖率 | 测试类型 |
|------|-----------|---------|
| Repository | 80% | SQLite 集成测试 |
| Services | 85% | Moq 单元测试 |
| **总体** | **70-80%** | 混合测试 |

---

## 2. 测试策略

### 2.1 Repository 层测试策略

**技术选型：SQLite 内存数据库**

选择 SQLite 而非 Mock 的原因：
- Repository 是数据访问层，需要验证 EF Core 映射正确性
- 需要真实测试 LINQ 查询（`Include`, `Where`, `OrderBy` 等）
- SQLite 内存数据库速度快，隔离性好
- 每个测试用例独立运行，自动回滚

**测试辅助基类：**

```csharp
// src/tests/UnitTests/Helpers/RepositoryTestBase.cs
public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected MafDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MafDbContext>()
            .UseSqlite("DataSource=:memory:")
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new MafDbContext(options);
        await DbContext.Database.OpenConnectionAsync();
        await DbContext.Database.EnsureCreatedAsync();

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.CloseConnectionAsync();
        await DbContext.DisposeAsync();
    }

    protected virtual async Task SeedTestDataAsync() { }
}
```

### 2.2 Services 层测试策略

**技术选型：Moq 模拟依赖**

Services 层测试原则：
- 纯单元测试，不涉及真实数据库或外部服务
- 所有依赖接口使用 Moq 模拟
- 验证方法调用次数和参数
- 验证返回值和状态变更

**测试模式：**

```csharp
public class MafTaskSchedulerTests
{
    private readonly Mock<IPriorityCalculator> _mockPriorityCalculator;
    private readonly MafTaskScheduler _sut;

    public MafTaskSchedulerTests()
    {
        _mockPriorityCalculator = new Mock<IPriorityCalculator>();
        _sut = new MafTaskScheduler(_mockPriorityCalculator.Object, maxConcurrentTasks: 10);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldCalculatePriorityForAllTasks()
    {
        // Arrange
        var tasks = CreateTestTasks(3);
        _mockPriorityCalculator
            .Setup(x => x.CalculatePriority(It.IsAny<PriorityCalculationRequest>()))
            .Returns(75);

        // Act
        var result = await _sut.ScheduleAsync(tasks);

        // Assert
        result.ScheduledTasks.Should().HaveCount(3);
        _mockPriorityCalculator.Verify(
            x => x.CalculatePriority(It.IsAny<PriorityCalculationRequest>()),
            Times.Exactly(3));
    }
}
```

---

## 3. 测试项目结构

```
src/tests/UnitTests/
├── Helpers/                          # 测试辅助工具
│   ├── RepositoryTestBase.cs         # Repository 测试基类
│   ├── TestDataBuilder.cs            # 测试数据构建器
│   └── ServiceTestBase.cs            # Service 测试基类
│
├── Repository/                       # Repository 层测试
│   ├── MainTaskRepositoryTests.cs
│   ├── SubTaskRepositoryTests.cs
│   ├── SchedulePlanRepositoryTests.cs
│   ├── ExecutionPlanRepositoryTests.cs
│   ├── TaskExecutionResultRepositoryTests.cs
│   └── LlmProviderConfigRepositoryTests.cs
│
├── Services/                         # Services 层测试
│   ├── Scheduling/
│   │   ├── MafTaskSchedulerTests.cs
│   │   └── MafPriorityCalculatorTests.cs
│   ├── Orchestration/
│   │   ├── MafTaskDecomposerTests.cs
│   │   └── MafTaskOrchestratorTests.cs
│   ├── Resilience/
│   │   ├── CircuitBreakerTests.cs
│   │   └── RetryExecutorTests.cs
│   ├── Factory/
│   │   └── MafAiAgentFactoryTests.cs
│   └── Storage/
│       └── MafAgentRegistryTests.cs
│
└── CKY.MAF.Tests.csproj
```

---

## 4. 测试用例设计

### 4.1 Repository 层测试用例

#### 4.1.1 MainTaskRepositoryTests（约 10 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `AddAsync_ShouldAssignIdAndSetCreatedAt` | 添加新任务 | Id 自增，CreatedAt 设置 |
| `GetByIdAsync_WhenTaskExists_ShouldReturnTaskWithSubTasks` | 根据 ID 查询 | 返回任务并包含 SubTasks |
| `GetByIdAsync_WhenTaskNotFound_ShouldReturnNull` | 查询不存在的任务 | 返回 null |
| `GetAllAsync_ShouldReturnAllTasksOrderedByCreatedAt` | 获取所有任务 | 按创建时间倒序 |
| `GetByStatusAsync_ShouldReturnOnlyTasksWithGivenStatus` | 按状态查询 | 只返回匹配状态的任务 |
| `GetHighPriorityTasksAsync_ShouldReturnTasksAboveThreshold` | 查询高优先级任务 | 返回优先级 >= 阈值的 Pending 任务 |
| `UpdateAsync_ShouldModifyTaskProperties` | 更新任务 | 任务属性变更并保存 |
| `DeleteAsync_ShouldRemoveTaskAndCascadeDeleteSubTasks` | 删除任务 | 任务和子任务都被删除 |
| `AddAsync_WithSubTasks_ShouldSaveBoth` | 添加带子任务的任务 | 主任务和子任务都保存 |
| `GetHighPriorityTasksAsync_ShouldOrderByPriorityThenCreatedAt` | 高优先级任务排序 | 优先级降序，创建时间升序 |

#### 4.1.2 SubTaskRepositoryTests（约 8 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `AddAsync_ShouldAssignId` | 添加子任务 | Id 自增 |
| `GetByIdAsync_ShouldIncludeMainTask` | 根据 ID 查询 | 包含关联的 MainTask |
| `GetByMainTaskIdAsync_ShouldReturnSubTasksOrderedByExecutionOrder` | 按主任务 ID 查询 | 按执行顺序排序 |
| `GetAllAsync_ShouldReturnAllSubTasks` | 获取所有子任务 | 返回所有记录 |
| `AddRangeAsync_ShouldAddMultipleSubTasks` | 批量添加 | 所有子任务都保存 |
| `UpdateAsync_ShouldModifySubTask` | 更新子任务 | 属性变更并保存 |
| `DeleteAsync_ShouldRemoveSubTask` | 删除子任务 | 记录被删除 |
| `AddRangeAsync_WithExecutionOrder_ShouldPreserveOrder` | 批量添加带顺序 | 保留执行顺序 |

#### 4.1.3 SchedulePlanRepositoryTests（约 8 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `AddAsync_ShouldAssignIdAndSetCreatedAt` | 添加调度计划 | Id 自增，时间戳设置 |
| `GetByIdAsync_WhenPlanExists_ShouldReturnPlan` | 根据 ID 查询 | 返回调度计划 |
| `GetByPlanIdAsync_ShouldReturnPlanByPlanId` | 根据 PlanId 查询 | 返回调度计划 |
| `GetByStatusAsync_ShouldReturnPlansWithGivenStatus` | 按状态查询 | 返回匹配状态的计划 |
| `GetRecentPlansAsync_ShouldReturnMostRecentPlans` | 查询最近计划 | 返回最近的 N 条记录 |
| `UpdateAsync_ShouldModifyPlanProperties` | 更新计划 | 属性变更并保存 |
| `DeleteAsync_ShouldRemovePlan` | 删除计划 | 记录被删除 |
| `GetByStatusAsync_ShouldOrderByCreatedAtDesc` | 按状态查询排序 | 按创建时间倒序 |

#### 4.1.4 ExecutionPlanRepositoryTests（约 9 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `AddAsync_ShouldAssignIdAndSetCreatedAt` | 添加执行计划 | Id 自增 |
| `GetByIdAsync_WhenPlanExists_ShouldReturnPlan` | 根据 ID 查询 | 返回执行计划 |
| `GetByPlanIdAsync_ShouldReturnPlanByPlanId` | 根据 PlanId 查询 | 返回执行计划 |
| `GetByStatusAsync_ShouldReturnPlansWithGivenStatus` | 按状态查询 | 返回匹配状态计划 |
| `GetByMultipleStatusAsync_ShouldReturnPlansMatchingAnyStatus` | 多状态查询 | 返回匹配任一状态的计划 |
| `GetByMultipleStatusAsync_ShouldLimitToCount` | 多状态查询限制 | 返回指定数量 |
| `UpdateAsync_ShouldModifyPlanProperties` | 更新计划 | 属性变更并保存 |
| `DeleteAsync_ShouldRemovePlan` | 删除计划 | 记录被删除 |
| `GetByMultipleStatusAsync_ShouldOrderByCreatedAtDesc` | 多状态查询排序 | 按创建时间倒序 |

#### 4.1.5 TaskExecutionResultRepositoryTests（约 7 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `AddAsync_ShouldAssignIdAndSetCreatedAt` | 添加执行结果 | Id 自增 |
| `GetByIdAsync_WhenResultExists_ShouldReturnResult` | 根据 ID 查询 | 返回执行结果 |
| `GetByTaskIdAsync_ShouldReturnResultsOrderedByCreatedAt` | 按任务 ID 查询 | 按创建时间倒序 |
| `GetByPlanIdAsync_ShouldReturnResultsOrderedByStartedAt` | 按计划 ID 查询 | 按开始时间排序 |
| `AddRangeAsync_ShouldAddMultipleResults` | 批量添加 | 所有结果都保存 |
| `UpdateAsync_ShouldModifyResultProperties` | 更新结果 | 属性变更并保存 |
| `AddRangeAsync_ShouldCalculateDuration` | 批量添加带时长 | 计算执行时长 |

#### 4.1.6 LlmProviderConfigRepositoryTests（约 10 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `SaveAsync_NewConfig_ShouldInsertAndAssignId` | 保存新配置 | 插入并分配 ID |
| `SaveAsync_ExistingConfig_ShouldUpdate` | 保存已存在配置 | 更新现有记录 |
| `GetByNameAsync_WhenConfigExists_ShouldReturnConfig` | 按名称查询 | 返回配置 |
| `GetByNameAsync_WhenConfigNotFound_ShouldReturnNull` | 查询不存在配置 | 返回 null |
| `GetAllEnabledAsync_ShouldReturnOnlyEnabledConfigs` | 查询启用配置 | 只返回 IsEnabled=true |
| `GetAllEnabledAsync_ShouldOrderByPriority` | 启用配置排序 | 按优先级升序 |
| `GetAllAsync_ShouldReturnAllConfigs` | 获取所有配置 | 返回所有记录 |
| `GetByScenarioAsync_ShouldReturnSupportingConfigs` | 按场景查询 | 返回支持该场景的配置 |
| `DeleteAsync_WhenConfigExists_ShouldReturnTrue` | 删除存在配置 | 返回 true |
| `ExistsAsync_WhenConfigExists_ShouldReturnTrue` | 检查存在性 | 返回 true |

**Repository 层总计：52 个测试用例**

---

### 4.2 Services 层测试用例

#### 4.2.1 MafTaskSchedulerTests（约 12 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `ScheduleAsync_ShouldCalculatePriorityForAllTasks` | 调度多个任务 | 为每个任务计算优先级 |
| `ScheduleAsync_ShouldSortTasksByPriorityScore` | 任务排序 | 按优先级分数降序 |
| `ScheduleAsync_ShouldGroupTasksByPriority` | 任务分组 | 高/中/低优先级分组 |
| `ScheduleAsync_ShouldGenerateExecutionPlan` | 生成执行计划 | 创建 ScheduleExecutionPlan |
| `ScheduleAsync_WithEmptyTaskList_ShouldReturnEmptyResult` | 空任务列表 | 返回空结果 |
| `ExecuteTaskAsync_ShouldUpdateTaskStatusToRunning` | 执行任务 | 状态变为 Running |
| `ExecuteTaskAsync_OnSuccess_ShouldUpdateStatusToCompleted` | 执行成功 | 状态变为 Completed |
| `ExecuteTaskAsync_OnFailure_ShouldUpdateStatusToFailed` | 执行失败 | 状态变为 Failed |
| `ExecuteTaskAsync_ShouldRespectConcurrencyLimit` | 并发控制 | 超过并发数时等待 |
| `ExecuteTaskAsync_WithCancellation_ShouldCancel` | 取消执行 | 抛出 OperationCanceledException |
| `ExecuteTaskAsync_ShouldSetStartedAt` | 开始时间 | 设置开始时间 |
| `ExecuteTaskAsync_ShouldSetCompletedAt` | 完成时间 | 设置完成时间 |

#### 4.2.2 MafPriorityCalculatorTests（约 15 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `CalculatePriority_WithHighBasePriority_ShouldReturnHighScore` | 高基础优先级 | 分数 > 50 |
| `CalculatePriority_WithNormalBasePriority_ShouldReturnMediumScore` | 普通基础优先级 | 分数 30-50 |
| `CalculatePriority_WithLowBasePriority_ShouldReturnLowScore` | 低基础优先级 | 分数 < 30 |
| `CalculatePriority_WithDirectUserInteraction_ShouldIncreaseScore` | 直接用户交互 | 分数增加 |
| `CalculatePriority_WithUrgentTimeFactor_ShouldIncreaseScore` | 紧急时间因素 | 分数增加 |
| `CalculatePriority_WithOverdueTask_ShouldIncreaseSignificantly` | 超期任务 | 分数显著增加 |
| `CalculatePriority_WithHighResourceUsage_ShouldDecreaseScore` | 高资源使用 | 分数降低 |
| `CalculatePriority_WithDependencyTask_ShouldInheritPriority` | 有依赖任务 | 继承优先级 |
| `CalculatePriority_CombinedFactors_ShouldCalculateCorrectly` | 组合因素 | 正确计算 |
| `CalculatePriority_AllMaximumFactors_ShouldReturn100` | 所 factors 最大 | 返回 100 |
| `CalculatePriority_AllMinimumFactors_ShouldReturn0` | 所 factors 最小 | 返回 0 |
| `CalculatePriority_WithNilInteractionType_ShouldUseDefault` | 空交互类型 | 使用默认值 |
| `CalculatePriority_WithNilTimeFactor_ShouldUseDefault` | 空时间因素 | 使用默认值 |
| `CalculatePriority_WithNilResourceUsage_ShouldUseDefault` | 空资源使用 | 使用默认值 |
| `CalculatePriority_ShouldClampToValidRange` | 边界值 | 限制在 0-100 |

#### 4.2.3 CircuitBreakerTests（约 12 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `ExecuteAsync_WhenClosed_ShouldExecuteOperation` | 关闭状态 | 执行操作 |
| `ExecuteAsync_WhenOpen_ShouldThrowException` | 开启状态 | 抛出异常 |
| `ExecuteAsync_AfterFailureThreshold_ShouldTransitionToOpen` | 达到失败阈值 | 转换到 Open |
| `ExecuteAsync_AfterOpenTimeout_ShouldTransitionToHalfOpen` | Open 超时 | 转换到 HalfOpen |
| `ExecuteAsync_InHalfOpenOnSuccess_ShouldTransitionToClosed` | HalfOpen 成功 | 转换到 Closed |
| `ExecuteAsync_InHalfOpenOnFailure_ShouldTransitionToOpen` | HalfOpen 失败 | 转回 Open |
| `State_ShouldStartAsClosed` | 初始状态 | 初始为 Closed |
| `State_AfterFailures_ShouldBeOpen` | 多次失败后 | 状态为 Open |
| `ExecuteAsync_ShouldTrackFailureCount` | 失败计数 | 正确计数 |
| `ExecuteAsync_ShouldResetFailureCountOnSuccess` | 成功重置 | 计数重置 |
| `ExecuteAsync_WithMultipleConcurrentCalls_ShouldBeThreadSafe` | 并发调用 | 线程安全 |
| `ExecuteAsync_ShouldLogStateTransitions` | 状态转换 | 记录日志 |

#### 4.2.4 RetryExecutorTests（约 8 个测试）

| 测试方法 | 场景 | 预期结果 |
|---------|------|---------|
| `ExecuteAsync_WithSuccessOnFirstTry_ShouldReturnResult` | 首次成功 | 返回结果 |
| `ExecuteAsync_WithTransientFailure_ShouldRetryAndSucceed` | 瞬时故障 | 重试后成功 |
| `ExecuteAsync_AfterMaxRetries_ShouldThrowException` | 超过最大重试 | 抛出异常 |
| `ExecuteAsync_ShouldUseExponentialBackoff` | 指数退避 | 延迟时间指数增长 |
| `ExecuteAsync_WithCancellation_ShouldCancelImmediately` | 取消请求 | 立即取消 |
| `ExecuteAsync_ShouldOnlyRetryTransientExceptions` | 瞬时异常 | 只重试特定异常 |
| `ExecuteAsync_WithNonTransientException_ShouldNotRetry` | 非瞬时异常 | 不重试 |
| `ExecuteAsync_ShouldLogRetryAttempts` | 重试日志 | 记录每次重试 |

**Services 层总计：47 个新测试用例 + 已有测试**

---

## 5. 测试工具类设计

### 5.1 TestDataBuilder

```csharp
// src/tests/UnitTests/Helpers/TestDataBuilder.cs
public static class TestDataBuilder
{
    public static MainTask CreateMainTask(Action<MainTask>? configure = null)
    {
        var task = new MainTask
        {
            Title = "Test Task",
            Description = "Test Description",
            Priority = TaskPriority.Normal,
            Status = MafTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(task);
        return task;
    }

    public static SubTask CreateSubTask(Action<SubTask>? configure = null)
    {
        var subTask = new SubTask
        {
            Title = "Test SubTask",
            Description = "Test SubTask Description",
            Status = MafTaskStatus.Pending,
            ExecutionOrder = 1
        };
        configure?.Invoke(subTask);
        return subTask;
    }

    public static SchedulePlanEntity CreateSchedulePlan(Action<SchedulePlanEntity>? configure = null)
    {
        var plan = new SchedulePlanEntity
        {
            PlanId = Guid.NewGuid().ToString(),
            PlanJson = "{}",
            Status = SchedulePlanStatus.Created,
            TotalTasks = 1,
            CreatedAt = DateTime.UtcNow
        };
        configure?.Invoke(plan);
        return plan;
    }

    public static LlmProviderConfig CreateLlmConfig(Action<LlmProviderConfig>? configure = null)
    {
        var config = new LlmProviderConfig
        {
            ProviderName = "test-provider",
            ProviderDisplayName = "Test Provider",
            ApiBaseUrl = "https://api.test.com",
            ApiKey = "test-key-12345678",
            ModelId = "test-model",
            ModelDisplayName = "Test Model",
            IsEnabled = true,
            Priority = 1,
            SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat }
        };
        configure?.Invoke(config);
        return config;
    }

    public static DecomposedTask CreateDecomposedTask(Action<DecomposedTask>? configure = null)
    {
        var task = new DecomposedTask
        {
            TaskId = Guid.NewGuid().ToString(),
            TaskName = "Test Task",
            Intent = "TestIntent",
            Description = "Test Description",
            Priority = TaskPriority.Normal,
            PriorityScore = 50,
            RequiredCapability = "test"
        };
        configure?.Invoke(task);
        return task;
    }
}
```

### 5.2 ServiceTestBase

```csharp
// src/tests/UnitTests/Helpers/ServiceTestBase.cs
public abstract class ServiceTestBase
{
    protected Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    protected Mock<ICacheStore> CreateCacheStoreMock()
    {
        return new Mock<ICacheStore>();
    }

    protected Mock<IVectorStore> CreateVectorStoreMock()
    {
        return new Mock<IVectorStore>();
    }

    protected Mock<IRelationalDatabase> CreateRelationalDatabaseMock()
    {
        return new Mock<IRelationalDatabase>();
    }
}
```

---

## 6. 测试命名约定

遵循以下命名规范：

- **测试类名**：`{ClassName}Tests`
- **测试方法**：`{MethodName}_{Scenario}_{ExpectedResult}`

示例：
- `AddAsync_ShouldAssignIdAndSetCreatedAt`
- `GetByIdAsync_WhenTaskNotFound_ShouldReturnNull`
- `ExecuteAsync_AfterFailureThreshold_ShouldTransitionToOpen`

---

## 7. NuGet 包依赖

### 需要添加到 CKY.MAF.Tests.csproj

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
```

### 已有的包（无需添加）

- xUnit 2.9.3
- FluentAssertions 8.8.0
- Moq 4.20.72
- Microsoft.NET.Test.Sdk 17.14.1

---

## 8. 实施计划

### Phase 1: 基础设施（1 天）

1. 创建测试项目结构
2. 添加 NuGet 包依赖
3. 实现测试辅助类（RepositoryTestBase, TestDataBuilder, ServiceTestBase）
4. 配置测试运行器和代码覆盖率

### Phase 2: Repository 层测试（3-4 天）

按顺序实现各 Repository 测试：
1. MainTaskRepositoryTests
2. SubTaskRepositoryTests
3. SchedulePlanRepositoryTests
4. ExecutionPlanRepositoryTests
5. TaskExecutionResultRepositoryTests
6. LlmProviderConfigRepositoryTests

### Phase 3: Services 层测试（3-4 天）

按顺序实现各 Service 测试：
1. MafPriorityCalculatorTests
2. MafTaskSchedulerTests
3. CircuitBreakerTests
4. RetryExecutorTests
5. 补充和改进已有测试

### Phase 4: 验证和优化（1 天）

1. 运行所有测试，确保通过率 100%
2. 检查代码覆盖率，达到目标 70-80%
3. 修复发现的问题
4. 代码审查和重构

**总计：8-10 个工作日**

---

## 9. 成功标准

项目验收标准：

- [ ] 所有测试用例通过（100% 通过率）
- [ ] 代码覆盖率 ≥ 70%（目标 80%）
- [ ] Repository 层覆盖率 ≥ 80%
- [ ] Services 层覆盖率 ≥ 85%
- [ ] 无测试代码警告
- [ ] 测试运行时间 < 30 秒
- [ ] 所有测试遵循命名约定
- [ ] 测试辅助类已实现并复用

---

## 10. 风险和缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| SQLite 与 PostgreSQL 行为差异 | 测试可能无法发现生产问题 | 关键查询添加集成测试 |
| EF Core InMemory 不支持某些 SQL 特性 | 测试覆盖不全 | 使用 SQLite 而非 InMemory |
| Moq 设置复杂导致测试脆弱 | 维护成本高 | 使用 TestDataBuilder 简化 |
| 测试数据构造复杂 | 测试代码冗长 | 实现完善的 TestDataBuilder |
| 并发测试不稳定 | CI/CD 失败 | 并发测试单独隔离 |

---

## 11. 后续改进

未来可以考虑的增强：

1. **性能测试**：添加 BenchmarkDotNet 基准测试
2. **集成测试**：添加 Testcontainers 集成测试
3. **突变测试**：使用 Stryker 进行突变测试
4. **测试文档**：为每个测试类添加 XML 文档注释
5. **CI/CD 集成**：在 GitHub Actions 中运行测试并生成覆盖率报告

---

## 附录 A：参考文档

- [xUnit 文档](https://xunit.net/)
- [FluentAssertions 文档](https://fluentassertions.com/)
- [Moq 文档](https://github.com/moq/moq4)
- [EF Core Testing 文档](https://docs.microsoft.com/en-us/ef/core/testing/)
- [项目 CLAUDE.md](../../CLAUDE.md)
- [架构文档](../specs/12-layered-architecture.md)

---

**文档变更历史**

| 版本 | 日期 | 变更内容 | 作者 |
|------|------|---------|------|
| 1.0 | 2025-01-13 | 初始版本 | Claude |

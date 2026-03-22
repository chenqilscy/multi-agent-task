# CKY.MAF框架测试指南

> **文档版本**: v1.0
> **创建日期**: 2026-03-13
> **用途**: 核心代码的单测和集成测试设计

---

## 📋 目录

1. [测试策略](#一测试策略)
2. [单元测试设计](#二单元测试设计)
3. [集成测试设计](#三集成测试设计)
4. [测试工具链](#四测试工具链)
5. [测试覆盖率目标](#五测试覆盖率目标)

---

## 一、测试策略

### 1.1 测试金字塔

```
        /\
       /  \        E2E Tests (5%)
      /----\       - 完整场景测试
     /------\      - 端到端流程
    /--------\
   /----------\    Integration Tests (25%)
  /------------\   - 组件协作测试
 /              \  - API测试
/----------------\
Unit Tests (70%)     - 单个类/方法测试
- 逻辑测试            - 边界条件
- 边界测试            - 异常处理
```

### 1.2 测试原则

| 原则 | 说明 | 应用示例 |
|------|------|---------|
| **FIRST** | Fast, Independent, Repeatable, Self-Validating, Timely | 单元测试运行时间<5ms |
| **AAA模式** | Arrange, Act, Assert | 所有测试遵循此结构 |
| **测试隔离** | 测试间不共享状态 | 每个测试独立初始化 |
| **可重复性** | 多次运行结果一致 | 不依赖外部系统状态 |
| **命名规范** | 方法名_Scenario_ExpectedResult | `DecomposeTask_SimpleInput_ReturnsOneTask` |

---

## 二、单元测试设计

### 2.1 核心接口层测试

#### 2.1.1 IIntentRecognizer - 意图识别器

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| IR-01 | 识别简单开灯指令 | "打开客厅灯" | Intent="lighting.control.turn_on", Confidence>0.9 | P0 |
| IR-02 | 识别调温指令 | "把温度调到26度" | Intent="climate.control.set_temperature", Entities={temp:26} | P0 |
| IR-03 | 识别晨间例程 | "我起床了" | Intent="morning_routine.activate", Confidence>0.8 | P0 |
| IR-04 | 未知意图处理 | "随便说点什么的乱七八糟" | Intent="unknown", Confidence<0.3 | P1 |
| IR-05 | 模糊意图识别 | "那个灯" | Intent="unknown", RequiresClarification=true | P1 |
| IR-06 | 多意图识别 | "打开灯并播放音乐" | PrimaryIntent+SecondaryIntents[] | P2 |
| IR-07 | 上下文意图 | "（上下文：讨论客厅）把它打开" | Intent="lighting.control.turn_on", Room="living_room" | P2 |

**示例测试代码**：

```csharp
public class IntentRecognizerTests
{
    [Fact]
    public async Task RecognizeAsync_SimpleLightCommand_ReturnsHighConfidence()
    {
        // Arrange
        var recognizer = new MafIntentRecognizer();
        var input = "打开客厅灯";

        // Act
        var result = await recognizer.RecognizeAsync(input);

        // Assert
        result.PrimaryIntent.Should().Be("lighting.control.turn_on");
        result.Confidence.Should().BeGreaterThan(0.9);
        result.OriginalInput.Should().Be(input);
    }

    [Theory]
    [InlineData("打开客厅灯", "lighting.control.turn_on")]
    [InlineData("把温度调到26度", "climate.control.set_temperature")]
    [InlineData("我起床了", "morning_routine.activate")]
    public async Task RecognizeAsync_KnownCommands_ReturnsCorrectIntent(string input, string expectedIntent)
    {
        // Arrange
        var recognizer = new MafIntentRecognizer();

        // Act
        var result = await recognizer.RecognizeAsync(input);

        // Assert
        result.PrimaryIntent.Should().Be(expectedIntent);
        result.Confidence.Should().BeGreaterThan(0.8);
    }
}
```

---

#### 2.1.2 IEntityExtractor - 实体提取器

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| EE-01 | 提取房间实体 | "打开客厅的灯" | Entities={Room:"客厅", Device:"灯"} | P0 |
| EE-02 | 提取数值参数 | "温度设置为26度" | Entities={Temperature:26} | P0 |
| EE-03 | 提取颜色参数 | "把灯调红色" | Entities={Color:"红色"} | P0 |
| EE-04 | 提取多个实体 | "把客厅温度调到26度" | Entities={Room:"客厅", Temperature:26} | P0 |
| EE-05 | 处理缺失实体 | "打开它" | Entities={}, MissingEntities=["Device"] | P1 |
| EE-06 | 提取时间实体 | "设置早上7点的闹钟" | Entities={Time:"07:00"} | P1 |

**示例测试代码**：

```csharp
public class EntityExtractorTests
{
    [Fact]
    public async Task ExtractAsync_RoomAndDevice_ReturnsBothEntities()
    {
        // Arrange
        var extractor = new MafEntityExtractor();
        var input = "打开客厅的灯";
        var intent = new IntentRecognitionResult { PrimaryIntent = "lighting.control.turn_on" };

        // Act
        var entities = await extractor.ExtractAsync(input, intent);

        // Assert
        entities.Should().ContainKey("Room");
        entities["Room"].Should().Be("客厅");
        entities.Should().ContainKey("Device");
        entities["Device"].Should().Be("灯");
    }
}
```

---

#### 2.1.3 ITaskDecomposer - 任务分解器

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| TD-01 | 简单任务分解 | "打开客厅灯" | 1个SubTask, Priority=Normal | P0 |
| TD-02 | 复合任务分解 | "我起床了" | 4个SubTasks, 包含依赖关系 | P0 |
| TD-03 | 并行任务识别 | "打开灯并播放音乐" | 2个并行SubTasks | P0 |
| TD-04 | 优先级分配 | "紧急：打开所有灯" | Priority=Critical, Score>80 | P0 |
| TD-05 | 依赖关系建立 | "打开灯后再播放音乐" | Task2依赖Task1 | P0 |
| TD-06 | 空输入处理 | "" | 返回空分解或错误 | P1 |
| TD-07 | 循环依赖检测 | 恶意构造的循环依赖 | 抛出CircularDependencyException | P2 |

**示例测试代码**：

```csharp
public class TaskDecomposerTests
{
    [Fact]
    public async Task DecomposeAsync_MorningRoutine_ReturnsFourSubTasks()
    {
        // Arrange
        var decomposer = new MafTaskDecomposer();
        var input = "我起床了";
        var intent = new IntentRecognitionResult
        {
            PrimaryIntent = "morning_routine.activate"
        };

        // Act
        var decomposition = await decomposer.DecomposeAsync(input, intent);

        // Assert
        decomposition.SubTasks.Should().HaveCount(4);

        // 验证任务顺序和依赖
        var firstTask = decomposition.SubTasks.First();
        firstTask.TaskName.Should().Be("打开客厅灯");
        firstTask.PriorityScore.Should().Be(45); // 最高优先级

        var lastTask = decomposition.SubTasks.Last();
        lastTask.TaskName.Should().Be("打开窗帘");
        lastTask.ExecutionStrategy.Should().Be(ExecutionStrategy.Delayed);
    }

    [Fact]
    public async Task DecomposeAsync_CircularDependency_ThrowsException()
    {
        // Arrange
        var decomposer = new MafTaskDecomposer();

        // 构造循环依赖的任务
        var tasks = new List<DecomposedTask>
        {
            CreateTask("1", dependsOn: "3"),
            CreateTask("2", dependsOn: "1"),
            CreateTask("3", dependsOn: "2")
        };

        // Act & Assert
        await Assert.ThrowsAsync<CircularDependencyException>(
            () => decomposer.ValidateDependenciesAsync(tasks));
    }
}
```

---

#### 2.1.4 IPriorityCalculator - 优先级计算器

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| PC-01 | 正常优先级计算 | UserInitiated, Normal, Now | Score=25-40 | P0 |
| PC-02 | 紧急任务计算 | UserInitiated, Critical, Now | Score=80-100 | P0 |
| PC-03 | 延迟任务惩罚 | UserInitiated, Normal, Delayed | Score<20 | P0 |
| PC-04 | 依赖传播 | 依赖高优先级任务 | Score提升10% | P0 |
| PC-05 | 资源利用率调整 | HighResourceUsage | Score降低5-10 | P1 |
| PC-06 | 时间衰减 | OverdueTask | Score提升15% | P1 |

**示例测试代码**：

```csharp
public class PriorityCalculatorTests
{
    [Theory]
    [InlineData(TaskPriority.Critical, UserInteractionType.Active, TimeFactor.Immediate, 90, 100)]
    [InlineData(TaskPriority.High, UserInteractionType.Active, TimeFactor.Immediate, 70, 89)]
    [InlineData(TaskPriority.Normal, UserInteractionType.Active, TimeFactor.Immediate, 40, 69)]
    [InlineData(TaskPriority.Low, UserInteractionType.Active, TimeFactor.Immediate, 10, 39)]
    public void CalculatePriority_VariousInputs_ReturnsExpectedScore(
        TaskPriority basePriority,
        UserInteractionType userInteraction,
        TimeFactor timeFactor,
        int minScore,
        int maxScore)
    {
        // Arrange
        var calculator = new MafTaskPriorityCalculator();
        var request = new PriorityCalculationRequest
        {
            BasePriority = basePriority,
            UserInteraction = userInteraction,
            TimeFactor = timeFactor
        };

        // Act
        var score = calculator.CalculatePriority(request);

        // Assert
        score.Should().BeInRange(minScore, maxScore);
    }

    [Fact]
    public void CalculatePriority_WithHighPriorityDependency_PropagatesScore()
    {
        // Arrange
        var calculator = new MafTaskPriorityCalculator();
        var dependency = new DecomposedTask { PriorityScore = 85 };
        var request = new PriorityCalculationRequest
        {
            BasePriority = TaskPriority.Normal,
            DependencyTask = dependency
        };

        // Act
        var score = calculator.CalculatePriority(request);

        // Assert
        // 依赖传播应该提升分数，但不超过依赖任务
        score.Should().BeGreaterThan(40); // Normal的基准分
        score.Should().BeLessThan(85);    // 不超过依赖任务
        score.Should().BeGreaterThan(85 * 0.9); // 接近依赖任务
    }
}
```

---

#### 2.1.5 ITaskScheduler - 任务调度器

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| TS-01 | 串行调度 | 3个串行任务 | 按顺序执行 | P0 |
| TS-02 | 并行调度 | 3个并行任务 | 同时执行 | P0 |
| TS-03 | 混合调度 | 2个并行组 | 组间串行，组内并行 | P0 |
| TS-04 | 依赖阻塞 | Task2依赖Task1 | Task1完成后Task2才开始 | P0 |
| TS-05 | 资源限制 | 超过MaxConcurrentTasks | 排队等待 | P0 |
| TS-06 | 优先级抢占 | 高优先级任务到达 | 低优先级任务被暂停 | P1 |
| TS-07 | 任务失败处理 | Task1失败 | 依赖Task1的任务被取消 | P0 |

**示例测试代码**：

```csharp
public class TaskSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ParallelTasks_AllExecuteConcurrently()
    {
        // Arrange
        var scheduler = new MafTaskScheduler(maxConcurrentTasks: 3);
        var tasks = new[]
        {
            CreateTask("1", ExecutionStrategy.Parallel),
            CreateTask("2", ExecutionStrategy.Parallel),
            CreateTask("3", ExecutionStrategy.Parallel)
        };

        var executionTimes = new ConcurrentDictionary<string, DateTime>();

        // Act
        var scheduleTask = scheduler.ScheduleAsync(tasks, async (task) =>
        {
            executionTimes[task.TaskId] = DateTime.UtcNow;
            await Task.Delay(100);
        });

        await Task.Delay(50); // 等待所有任务启动

        // Assert
        executionTimes.Should().HaveCount(3);

        // 验证任务几乎同时开始（时间差<50ms）
        var times = executionTimes.Values.OrderBy(t => t).ToList();
        for (int i = 1; i < times.Count; i++)
        {
            (times[i] - times[0]).TotalMilliseconds.Should().BeLessThan(50);
        }
    }

    [Fact]
    public async Task ScheduleAsync_DependencyChain_SequentialExecution()
    {
        // Arrange
        var scheduler = new MafTaskScheduler();
        var task1 = CreateTask("1");
        var task2 = CreateTask("2", dependsOn: "1");
        var task3 = CreateTask("3", dependsOn: "2");

        var executionOrder = new ConcurrentBag<string>();

        // Act
        await scheduler.ScheduleAsync(new[] { task1, task2, task3 }, async (task) =>
        {
            executionOrder.Add(task.TaskId);
            await Task.Delay(50);
        });

        // Assert
        executionOrder.Should().EqualInOrder("1", "2", "3");
    }
}
```

---

### 2.2 存储层测试

#### 2.2.1 IMafSessionStorage - 会话存储

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| SS-01 | 保存会话 | AgentSession | 成功保存到L1/L2 | P0 |
| SS-02 | 读取会话 | SessionId | 返回完整会话 | P0 |
| SS-03 | 缓存回写 | L1未命中 | 从L2读取并回写L1 | P0 |
| SS-04 | TTL过期 | 过期会话 | 返回null或抛出异常 | P0 |
| SS-05 | 并发写入 | 多线程同时写入 | 数据一致性保证 | P1 |
| SS-06 | L3降级 | L1/L2不可用 | 降级到L3 | P1 |

**示例测试代码**：

```csharp
public class MafSessionStorageTests
{
    [Fact]
    public async Task SaveAndLoadAsync_ValidSession_ReturnsSameData()
    {
        // Arrange
        var storage = new MafTieredSessionStorage();
        var session = new AgentSession
        {
            SessionId = "test-session-001",
            AgentId = "test-agent",
            StartTime = DateTime.UtcNow,
            Messages = new List<MessageContext>
            {
                new MessageContext { Role = "User", Content = "测试消息" }
            }
        };

        // Act
        await storage.SaveAsync(session);
        var loaded = await storage.LoadAsync<AgentSession>("test-session-001");

        // Assert
        loaded.Should().NotBeNull();
        loaded.SessionId.Should().Be("test-session-001");
        loaded.Messages.Should().HaveCount(1);
        loaded.Messages[0].Content.Should().Be("测试消息");
    }

    [Fact]
    public async Task LoadAsync_CacheMiss_WritesToL1()
    {
        // Arrange
        var storage = new MafTieredSessionStorage();
        var session = CreateTestSession();
        await storage.SaveAsync(session);

        // 清空L1缓存
        storage.ClearL1Cache();

        // Act
        var loaded = await storage.LoadAsync<AgentSession>(session.SessionId);

        // Assert
        // 验证L1缓存已更新
        storage.L1CacheContains(session.SessionId).Should().BeTrue();
    }
}
```

---

### 2.3 模型层测试

#### 2.3.1 MessageContext - 消息上下文

**测试场景**：

| 场景ID | 测试场景 | 输入 | 预期输出 | 优先级 |
|--------|---------|------|---------|--------|
| MC-01 | 文本消息序列化 | TextMessage | 正确序列化 | P0 |
| MC-02 | 图片消息序列化 | ImageMessage | 包含URL/Base64 | P0 |
| MC-03 | 消息角色提取 | AIClientMessage | Role="User" | P0 |
| MC-04 | 内容类型识别 | StructuredMessage | ContentType="structured" | P0 |
| MC-05 | 反序列化恢复 | MessageContext | 恢复为MS AF消息 | P0 |

---

## 三、集成测试设计

### 3.1 MainAgent完整流程测试

**测试场景**：

| 场景ID | 测试场景 | 描述 | 预期结果 | 优先级 |
|--------|---------|------|---------|--------|
| MA-01 | 完整对话流程 | 用户："我起床了" → MainAgent → 4个SubTask | 所有任务成功执行 | P0 |
| MA-02 | 多轮对话 | 第1轮："打开灯" → 第2轮："把它调红色" | 上下文正确传递 | P0 |
| MA-03 | 错误恢复 | SubTask失败 → MainAgent处理 | 返回友好错误信息 | P0 |
| MA-04 | 超时处理 | SubTask超时 → MainAgent重试 | 重试1-3次 | P1 |
| MA-05 | 结果聚合 | 多SubTask结果 → 统一响应 | 格式化输出 | P0 |

**示例测试代码**：

```csharp
public class MainAgentIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public MainAgentIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteAsync_MorningRoutine_CompletesAllTasks()
    {
        // Arrange
        var mainAgent = _fixture.GetMainAgent();
        var request = new MafTaskRequest
        {
            TaskId = Guid.NewGuid().ToString("N"),
            UserInput = "我起床了",
            ConversationId = "test-conv-001"
        };

        // Act
        var response = await mainAgent.ExecuteAsync(request);

        // Assert
        response.Success.Should().BeTrue();
        response.SubTaskResults.Should().HaveCount(4);
        response.SubTaskResults.Should().OnlyContain(r => r.Success);

        // 验证执行顺序：开灯 → 空调 → 音乐 → 窗帘
        var tasks = response.SubTaskResults.OrderBy(r => r.StartTime).ToList();
        tasks[0].TaskName.Should().Contain("灯");
        tasks[3].TaskName.Should().Contain("窗帘");
    }

    [Fact]
    public async Task ExecuteAsync_MultiTurnConversation_MaintainsContext()
    {
        // Arrange
        var mainAgent = _fixture.GetMainAgent();
        var conversationId = "test-conv-002";

        // 第一轮对话
        var request1 = new MafTaskRequest
        {
            UserInput = "打开客厅的灯",
            ConversationId = conversationId
        };

        // Act
        var response1 = await mainAgent.ExecuteAsync(request1);

        // 第二轮对话（使用"它"指代）
        var request2 = new MafTaskRequest
        {
            UserInput = "把它调暗一点",
            ConversationId = conversationId
        };

        var response2 = await mainAgent.ExecuteAsync(request2);

        // Assert
        response2.Success.Should().BeTrue();
        // 验证"它"被正确解析为"客厅的灯"
        response2.Result.Should().Contain("客厅");
        response2.Result.Should().Contain("调暗");
    }
}
```

---

### 3.2 任务编排集成测试

**测试场景**：

| 场景ID | 测试场景 | 描述 | 预期结果 | 优先级 |
|--------|---------|------|---------|--------|
| TO-01 | 并行执行 | 3个无依赖任务 | 同时完成 | P0 |
| TO-02 | 串行执行 | 3个链式依赖任务 | 顺序完成 | P0 |
| TO-03 | 混合执行 | 2组并行任务 | 组间串行，组内并行 | P0 |
| TO-04 | 失败传播 | Task1失败 → Task2被取消 | Task2状态=Cancelled | P0 |
| TO-05 | 优先级抢占 | 高优先级任务插入 | 低优先级任务暂停 | P1 |

**示例测试代码**：

```csharp
public class TaskOrchestratorIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task OrchestrateAsync_ParallelGroup_AllCompleteConcurrently()
    {
        // Arrange
        var orchestrator = _fixture.GetTaskOrchestrator();
        var tasks = new[]
        {
            CreateParallelTask("1", "开灯"),
            CreateParallelTask("2", "空调"),
            CreateParallelTask("3", "音乐")
        };

        var executionTimes = new ConcurrentDictionary<string, (DateTime start, DateTime end)>();

        // Act
        var results = await orchestrator.OrchestrateAsync(tasks, async (task) =>
        {
            var start = DateTime.UtcNow;
            await Task.Delay(100);
            executionTime[task.TaskId] = (start, DateTime.UtcNow);
            return new ExecutionResult { Success = true };
        });

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.Success);

        // 验证并行执行（时间差<50ms）
        var startTimes = executionTimes.Values.Select(v => v.start).OrderBy(t => t).ToList();
        for (int i = 1; i < startTimes.Count; i++)
        {
            (startTimes[i] - startTimes[0]).TotalMilliseconds.Should().BeLessThan(50);
        }
    }
}
```

---

### 3.3 存储层集成测试

**测试场景**：

| 场景ID | 测试场景 | 描述 | 预期结果 | 优先级 |
|--------|---------|------|---------|--------|
| ST-01 | L1-L2缓存同步 | 写入L1 → 检查L2 | L2也包含数据 | P0 |
| ST-02 | L2-L3持久化 | L1/L2失效 → 从L3读取 | 成功恢复数据 | P0 |
| ST-03 | TTL过期清理 | 过期数据 | 自动删除 | P0 |
| ST-04 | 并发写入 | 10线程同时写入 | 数据一致性 | P1 |
| ST-05 | 容量限制 | 超过MaxSize | LRU淘汰 | P1 |

---

### 3.4 Mock外部依赖

**LLM服务Mock**：

```csharp
public class MockLLMService : ILLMService
{
    public Queue<string> Responses { get; } = new();

    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var response = Responses.Count > 0
            ? Responses.Dequeue()
            : "{\"intent\": \"lighting.control.turn_on\", \"confidence\": 0.95}";
        return Task.FromResult(response);
    }
}

// 使用示例
var mockLLM = new MockLLMService();
mockLLM.Responses.Enqueue("{\"intent\": \"test\", \"confidence\": 0.9}");
```

**设备服务Mock**：

```csharp
public class MockLightingService : ILightingService
{
    public List<string> Operations { get; } = new();

    public Task TurnOnAsync(string deviceName, CancellationToken ct = default)
    {
        Operations.Add($"TurnOn:{deviceName}");
        return Task.CompletedTask;
    }

    public Task SetBrightnessAsync(string deviceName, int brightness, CancellationToken ct = default)
    {
        Operations.Add($"SetBrightness:{deviceName}:{brightness}");
        return Task.CompletedTask;
    }
}
```

---

## 四、测试工具链

### 4.1 单元测试框架

**推荐**：xUnit + FluentAssertions + Moq

```xml
<ItemGroup>
  <!-- 测试框架 -->
  <PackageReference Include="xunit" Version="2.6.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>

  <!-- 断言库 -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />

  <!-- Mock框架 -->
  <PackageReference Include="Moq" Version="4.20.70" />

  <!-- 测试数据生成 -->
  <PackageReference Include="Bogus" Version="35.5.0" />
</ItemGroup>
```

### 4.2 集成测试工具

**推荐**：
- **Docker Compose** - 启动依赖服务（Redis、PostgreSQL、Qdrant）
- **Testcontainers** - 动态管理测试容器
- **WireMock.Net** - Mock外部API

```xml
<ItemGroup>
  <!-- 容器化测试 -->
  <PackageReference Include="Testcontainers.Redis" Version="3.5.0" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.5.0" />

  <!-- API Mock -->
  <PackageReference Include="WireMock.Net" Version="1.5.45" />
</ItemGroup>
```

### 4.3 测试配置文件

**测试Docker Compose** (`docker-compose.test.yml`)：

```yaml
version: '3.8'

services:
  # Redis用于缓存和消息队列
  redis-test:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  # PostgreSQL用于持久化
  postgres-test:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: maf_test
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
    ports:
      - "5432:5432"

  # Qdrant用于向量存储
  qdrant-test:
    image: qdrant/qdrant:v1.8.0
    ports:
      - "6333:6333"
```

---

## 五、测试覆盖率目标

### 5.1 覆盖率要求

| 模块 | 行覆盖率 | 分支覆盖率 | 复杂度 | 优先级 |
|------|---------|-----------|--------|--------|
| **Core.Models** | 90% | 85% | <10 | P0 |
| **Core.Interfaces** | 80% | 75% | <15 | P0 |
| **Services.NLP** | 85% | 80% | <12 | P0 |
| **Services.Scheduling** | 90% | 85% | <10 | P0 |
| **Services.Storage** | 85% | 80% | <12 | P0 |
| **Infrastructure** | 70% | 65% | <20 | P1 |

### 5.2 覆盖率工具

**推荐**：Coverlet + ReportGenerator

```bash
# 收集覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 生成报告
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```

**GitHub Actions配置**：

```yaml
- name: Run tests with coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage" --results-directory:./coverage

- name: Generate coverage report
  run: |
    reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report -reporttypes:Html

- name: Upload coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./coverage/**/coverage.cobertura.xml
```

---

## 六、测试最佳实践

### 6.1 测试命名规范

```csharp
// ✅ 好的命名
public class TaskSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_SingleTask_ReturnsSuccess()

    [Theory]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Low)]
    public void CalculatePriority_WithDifferentPriority_ReturnsCorrectScore(TaskPriority priority)
}

// ❌ 不好的命名
public class Tests
{
    [Fact]
    public async Task Test1()  // 不明确
    [Fact]
    public async Task ScheduleTest()  // 太笼统
}
```

### 6.2 测试数据管理

**使用Bogus生成测试数据**：

```csharp
public class TestDataGenerator
{
    public static MafTaskRequest CreateValidTaskRequest()
    {
        var faker = new Faker();
        return new MafTaskRequest
        {
            TaskId = Guid.NewGuid().ToString("N"),
            UserInput = faker.Lorem.Sentence(),
            ConversationId = Guid.NewGuid().ToString("N"),
            Priority = faker.PickRandom<TaskPriority>(),
            Timestamp = faker.Date.Recent()
        };
    }

    public static List<DecomposedTask> CreateRandomTasks(int count)
    {
        var faker = new Faker();
        return faker.MakeLazy(count, i => new DecomposedTask
        {
            TaskId = Guid.NewGuid().ToString("N"),
            TaskName = faker.Lorem.Word(),
            Priority = faker.PickRandom<TaskPriority>()
        }).ToList();
    }
}
```

### 6.3 异步测试最佳实践

```csharp
// ✅ 正确的异步测试
[Fact]
public async Task ExecuteAsync_ValidInput_ReturnsResponse()
{
    // Arrange
    var service = new TestService();

    // Act
    var result = await service.ExecuteAsync("input");

    // Assert
    result.Should().NotBeNull();
}

// ❌ 错误：忘记await
[Fact]
public async Task ExecuteAsync_ValidInput_ReturnsResponse()
{
    var result = service.ExecuteAsync("input"); // 忘记await
    result.Should().NotBeNull(); // 测试总是通过
}

// ❌ 错误：使用.Result
[Fact]
public void ExecuteAsync_ValidInput_ReturnsResponse()
{
    var result = service.ExecuteAsync("input").Result; // 可能死锁
}
```

---

## 🔗 相关文档

- [核心架构](./00-CORE-ARCHITECTURE.md)
- [实现指南](./01-IMPLEMENTATION-GUIDE.md)
- [任务调度系统](./03-task-scheduling-design.md)

---

**文档版本**: v1.0
**最后更新**: 2026-03-13

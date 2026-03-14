# 如何集成 ILlmService 与 Microsoft Agent Framework

本文档说明如何将 `ILlmService` 与 Microsoft Agent Framework 的 `AIAgent` 正确集成。

## 正确的架构设计

```
┌─────────────────────────────────────────────────────┐
│       Microsoft Agent Framework (AIAgent)          │
│              - 会话管理                              │
│              - 状态追踪                              │
│              - Agent 间通信                          │
└────────────────────┬────────────────────────────────┘
                     │
                     │ 继承
                     ▼
┌─────────────────────────────────────────────────────┐
│              MafAgentBase (增强基类)                 │
│  - 会话存储 (IMafSessionStorage)                    │
│  - 优先级计算 (IPriorityCalculator)                 │
│  - 监控指标 (IMetricsCollector)                     │
└────────────────────┬────────────────────────────────┘
                     │
                     │ 继承 + 实现
                     ▼
┌─────────────────────────────────────────────────────┐
│          ZhipuAILlmAgent : MafAgentBase             │
│          同时实现 ILlmService 接口                    │
│  - ExecuteBusinessLogicAsync: 调用 LLM API          │
│  - 利用 MS AF 的会话管理能力                         │
│  - CompleteAsync: 对外提供的简化接口                 │
└─────────────────────────────────────────────────────┘
```

## 关键设计原则

### ❌ 错误做法（不要这样做）

```csharp
// 错误：直接使用 HttpClient，绕过 MS AF
public class ZhipuAIService : ILlmService
{
    private readonly HttpClient _httpClient; // ❌ 绕过了 MS AF

    public async Task<string> CompleteAsync(string prompt)
    {
        // 直接调用 HTTP API，没有利用 MS AF 的能力
        var response = await _httpClient.PostAsync(...); // ❌ 错误
    }
}
```

### ✅ 正确做法

```csharp
// 正确：基于 MS AF 的 AIAgent
public class ZhipuAILlmAgent : MafAgentBase, ILlmService
{
    // ✅ 继承 MafAgentBase，使用 MS AF 的所有能力

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        IAgentSession session,
        CancellationToken ct)
    {
        // ✅ 在这里调用 LLM API
        // ✅ session 是 MS AF 提供的会话对象
        // ✅ 可以利用会话历史、状态管理等功能

        var response = await CallLlmApi(...);

        // ✅ 将响应记录到会话历史
        session.History.Add(new Message { Role = "assistant", Content = response });

        return new MafTaskResponse { Success = true, Result = response };
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        // ✅ 通过 MS AF 的执行流程来处理
        var request = new MafTaskRequest { UserInput = prompt, ... };
        var response = await ExecuteAsync(request, ct);
        return response.Result;
    }
}
```

## 完整实现示例

### 1. 定义 ILlmService 接口（Core 层）

```csharp
// src/Core/Abstractions/ILlmService.cs
using Microsoft.Agents.AI;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    public interface ILlmService
    {
        Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
        Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);

        // 获取底层的 MS AF Agent 实例
        AIAgent? GetUnderlyingAgent();
    }
}
```

### 2. 实现 LLM Agent（Repository 层）

```csharp
// src/Repository/LLM/ZhipuAILlmAgent.cs
public class ZhipuAILlmAgent : MafAgentBase, ILlmService
{
    private readonly ZhipuAIConfig _config;
    private readonly HttpClient _httpClient; // 仅用于底层 API 调用

    public ZhipuAILlmAgent(
        ZhipuAIConfig config,
        IMafSessionStorage sessionStorage,
        IPriorityCalculator priorityCalculator,
        IMetricsCollector metricsCollector,
        ILogger<ZhipuAILlmAgent> logger)
        : base(sessionStorage, priorityCalculator, metricsCollector, logger)
    {
        _config = config;
        _httpClient = new HttpClient { BaseAddress = new Uri("https://open.bigmodel.cn/api/") };
    }

    // ILlmService 接口实现
    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        // 通过 MS AF 的执行流程
        var request = new MafTaskRequest
        {
            TaskId = Guid.NewGuid().ToString(),
            ConversationId = $"llm-{Guid.NewGuid()}",
            UserInput = prompt
        };

        var response = await ExecuteAsync(request, ct);
        return response.Result ?? throw new InvalidOperationException(response.Error);
    }

    public AIAgent? GetUnderlyingAgent() => this;

    // MafAgentBase 属性实现
    public override string AgentId => "zhipuai-llm-agent";
    public override string Name => "智谱AI LLM Agent";
    public override string Description => "基于 MS AF 的智谱AI GLM-4 模型";
    public override IReadOnlyList<string> Capabilities => new[]
    {
        "文本生成", "意图识别", "任务分解", "自然语言理解"
    };

    // 核心业务逻辑：实际的 LLM API 调用
    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        IAgentSession session,  // ✅ MS AF 提供的会话对象
        CancellationToken ct)
    {
        try
        {
            // 调用智谱AI API
            var apiResponse = await _httpClient.PostAsync("paas/v4/chat/completions", ...);

            var answer = ParseResponse(apiResponse);

            // ✅ 利用 MS AF 的会话管理能力
            session.History.Add(new Message { Role = "assistant", Content = answer });

            // ✅ 利用 MS AF 的监控能力
            Statistics.TotalExecutions++;

            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = true,
                Result = answer
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "LLM API 调用失败");
            return new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Error = ex.Message
            };
        }
    }
}
```

### 3. 在业务 Agent 中使用

```csharp
// src/Demos/SmartHome/Agents/SmartLightingAgent.cs
public class SmartLightingAgent : MafAgentBase
{
    private readonly ILlmService _llmService;

    public SmartLightingAgent(
        ILlmService llmService,
        IMafSessionStorage sessionStorage,
        IPriorityCalculator priorityCalculator,
        IMetricsCollector metricsCollector,
        ILogger logger)
        : base(sessionStorage, priorityCalculator, metricsCollector, logger)
    {
        _llmService = llmService;
    }

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        IAgentSession session,
        CancellationToken ct)
    {
        // ✅ 使用 ILlmService 抽象，不依赖具体实现
        var systemPrompt = "你是智能家居照明助手...";
        var userPrompt = request.UserInput;

        var llmResponse = await _llmService.CompleteAsync(systemPrompt, userPrompt, ct);

        // 处理响应
        var command = ParseCommand(llmResponse);
        await ExecuteLightingCommand(command);

        return new MafTaskResponse { Success = true, Result = $"已执行: {command.Action}" };
    }
}
```

### 4. 注册服务

```csharp
// src/Demos/SmartHome/Program.cs
public static void ConfigureServices(IServiceCollection services)
{
    // 注册 LLM Agent（既是 Agent 也是服务）
    services.AddSingleton<ZhipuAILlmAgent>();
    services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<ZhipuAILlmAgent>());
    services.AddSingleton(new ZhipuAIConfig
    {
        ApiKey = configuration["ZhipuAI:ApiKey"],
        Model = "glm-4"
    });

    // 注册业务 Agent
    services.AddSingleton<SmartLightingAgent>();
}
```

## MS AF 带来的优势

### 1. 会话管理

```csharp
// ✅ MS AF 自动管理会话状态
protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
    MafTaskRequest request,
    IAgentSession session,  // 自动加载会话
    CancellationToken ct)
{
    // ✅ 访问会话历史
    var lastMessage = session.History.LastOrDefault();

    // ✅ 添加新消息到历史
    session.History.Add(new Message { Role = "assistant", Content = answer });

    // ✅ MS AF 自动保存会话（在 ExecuteAsync 中）
}
```

### 2. 状态追踪

```csharp
// ✅ MS AF 提供的状态管理
public override MafAgentStatus Status { get; private set; }
public AgentStatistics Statistics { get; }  // ✅ 自动统计

protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
{
    Status = MafAgentStatus.Busy;  // ✅ 自动更新状态

    try
    {
        // 执行逻辑
        Statistics.TotalExecutions++;  // ✅ 自动计数
        return response;
    }
    finally
    {
        Status = MafAgentStatus.Idle;  // ✅ 自动恢复
    }
}
```

### 3. 监控和日志

```csharp
// ✅ 集成监控指标
protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
{
    var startTime = DateTime.UtcNow;

    try
    {
        var result = await CallLlmApi();

        // ✅ 自动记录执行指标
        await MetricsCollector.RecordExecutionAsync(Name, startTime, true);

        return result;
    }
    catch (Exception ex)
    {
        // ✅ 自动记录错误
        await MetricsCollector.RecordErrorAsync(Name, ex);
        throw;
    }
}
```

### 4. Agent 间通信

```csharp
// ✅ 未来可以扩展为 Agent 间协作
protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
{
    // 可以调用其他 Agent
    var otherAgent = /* 获取其他 Agent */;
    var response = await otherAgent.ExecuteAsync(...);

    return response;
}
```

## 测试

```csharp
public class ZhipuAILlmAgentTests
{
    [Fact]
    public async Task CompleteAsync_ShouldUseMafAgentExecuteFlow()
    {
        // Arrange
        var mockSessionStorage = new Mock<IMafSessionStorage>();
        var mockPriorityCalculator = new Mock<IPriorityCalculator>();
        var mockMetricsCollector = new Mock<IMetricsCollector>();
        var config = new ZhipuAIConfig { ApiKey = "test", Model = "glm-4" };

        var agent = new ZhipuAILlmAgent(
            config,
            mockSessionStorage.Object,
            mockPriorityCalculator.Object,
            mockMetricsCollector.Object,
            NullLogger<ZhipuAILlmAgent>.Instance);

        // Act
        var response = await agent.CompleteAsync("你好");

        // Assert
        response.Should().NotBeNullOrEmpty();
        mockMetricsCollector.Verify(x => x.RecordExecutionAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## 总结

### 核心原则

1. **基于 MS AF**：LLM 服务必须继承 `MafAgentBase`（进而继承 `AIAgent`）
2. **实现接口**：同时实现 `ILlmService` 接口提供简化访问
3. **利用能力**：充分利用 MS AF 的会话管理、状态追踪、监控等功能
4. **抽象访问**：其他 Agent 通过 `ILlmService` 接口使用，不依赖具体实现

### 架构优势

- ✅ **统一管理**：所有 LLM 调用都通过 MS AF 的执行流程
- ✅ **会话持久化**：自动管理对话历史和状态
- ✅ **监控集成**：自动收集指标和日志
- ✅ **易于测试**：可以完整测试 MS AF 的集成
- ✅ **可扩展性**：未来可以轻松添加 Agent 间协作能力

### 与错误做法的对比

| 特性 | ❌ HttpClient 方式 | ✅ MS AF 方式 |
|------|------------------|---------------|
| 会话管理 | ❌ 需要自己实现 | ✅ 自动提供 |
| 状态追踪 | ❌ 需要自己实现 | ✅ 自动提供 |
| 监控指标 | ❌ 需要手动添加 | ✅ 自动收集 |
| Agent 通信 | ❌ 不支持 | ✅ 原生支持 |
| 架构一致性 | ❌ 与其他 Agent 不一致 | ✅ 完全一致 |

这种架构确保 LLM 服务完全融入 MS AF 生态，而不是一个独立的外部组件。

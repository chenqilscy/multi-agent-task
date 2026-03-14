# MafAiAgent 调用方式说明

## 架构层次关系

```
┌─────────────────────────────────────────────────────────────┐
│  Demo Agents (SmartHomeAgent, ClimateAgent, etc.)          │
│  继承: MafBusinessAgentBase                                 │
└────────────────────┬────────────────────────────────────────┘
                     │ 调用业务逻辑方法
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  MafBusinessAgentBase (业务层基类)                           │
│  - 通过组合使用 IMafAiAgentRegistry                          │
│  - 提供 CallLlmAsync/CallLlmChatAsync/CallLlmBatchAsync     │
└────────────────────┬────────────────────────────────────────┘
                     │ 通过 Registry 获取
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  IMafAiAgentRegistry (LLM Agent 注册表)                      │
│  - GetBestAgentAsync(scenario) - 根据场景获取最佳 Agent       │
└────────────────────┬────────────────────────────────────────┘
                     │ 返回
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  MafAiAgent (LLM 层基类，继承 MS AF 的 AIAgent)              │
│  - ExecuteAsync() - 【供 MafBusinessAgentBase 使用】         │
│  - ExecuteBatchAsync() - 【供 MafBusinessAgentBase 使用】    │
│  - RunCoreAsync() - 【MS AF 框架要求，不直接调用】           │
└─────────────────────────────────────────────────────────────┘
```

## MafAiAgent 的方法分类

### ✅ 供 MafBusinessAgentBase 使用的核心方法

#### 1. `ExecuteAsync()` - 单次 LLM 调用
```csharp
public abstract Task<string> ExecuteAsync(
    string modelId,
    string prompt,
    string? systemPrompt = null,
    CancellationToken ct = default);
```

**使用场景**：
- 简单的单轮对话
- 文本生成、总结、翻译
- 意图识别、实体提取

**调用路径**：
```
MafBusinessAgentBase.CallLlmAsync()
  → LlmRegistry.GetBestAgentAsync()
    → MafAiAgent.ExecuteAsync()
      → 具体实现 (ZhipuAiAgent.ExecuteAsync)
```

**示例**：
```csharp
// 在 MafBusinessAgentBase 的子类中
protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
    MafTaskRequest request,
    CancellationToken ct)
{
    var response = await CallLlmAsync(
        prompt: "请分析用户意图",
        scenario: LlmScenario.IntentRecognition,
        systemPrompt: "你是一个意图识别专家",
        ct: ct);

    return new MafTaskResponse { Result = response };
}
```

#### 2. `ExecuteBatchAsync()` - 批量 LLM 调用
```csharp
public virtual async Task<string[]> ExecuteBatchAsync(
    string modelId,
    string[] prompts,
    string? systemPrompt = null,
    CancellationToken ct = default)
```

**使用场景**：
- 并行处理多个请求
- 批量文本分析
- 多路径推理

**调用路径**：
```
MafBusinessAgentBase.CallLlmBatchAsync()
  → LlmRegistry.GetBestAgentAsync()
    → MafAiAgent.ExecuteBatchAsync()
      → 并行调用 ExecuteAsync()
```

**示例**：
```csharp
// 批量处理多个任务
var prompts = new[]
{
    "总结第一段",
    "总结第二段",
    "总结第三段"
};

var responses = await CallLlmBatchAsync(
    prompts,
    LlmScenario.TextGeneration,
    "请简洁总结",
    ct);
```

### 🔄 MS AF 公开 API（可以调用）

#### 3. `RunAsync()` - 非流式执行（MS AF 公开 API）
```csharp
public Task<AgentResponse> RunAsync(
    IEnumerable<ChatMessage> messages,
    AgentSession? session = null,
    AgentRunOptions? options = null,
    CancellationToken cancellationToken = default)
```

**说明**：
- 这是 MS Agent Framework 的**公开 API**，可以直接调用
- 返回完整结果（一次性返回整个响应）
- 内部会调用 `RunCoreAsync()`，然后自动处理会话状态管理
- **可以直接在 Demo Agent 中使用**

**何时使用**：
- 需要完整的 LLM 响应（不需要流式）
- 使用 MS AF 的标准 AgentThread 管理对话
- 需要框架自动管理会话状态和上下文

**示例**：
```csharp
// ✅ 正确：使用 MS AF 的公开 API
var agent = new ZhipuAiAgent(config, logger, sessionStore);
var response = await agent.RunAsync(
    new List<ChatMessage>
    {
        new ChatMessage(ChatRole.User, "你好")
    });

Console.WriteLine(response.Messages.First().Text);
```

#### 4. `RunStreamingAsync()` - 流式执行（MS AF 公开 API）
```csharp
public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
    IEnumerable<ChatMessage> messages,
    AgentSession? session = null,
    AgentRunOptions? options = null,
    CancellationToken cancellationToken = default)
```

**说明**：
- 这是 MS Agent Framework 的**公开 API**，可以直接调用
- 提供流式响应（逐步生成，实时反馈）
- 内部会调用 `RunCoreAsyncStreaming()`，然后自动处理会话状态管理
- **可以直接在 Demo Agent 中使用**

**何时使用**：
- 需要实时显示 LLM 生成过程
- 用户体验要求高（聊天机器人、实时对话）
- 长文本生成（需要逐步显示）

**示例**：
```csharp
// ✅ 正确：使用 MS AF 的流式 API
var agent = new ZhipuAiAgent(config, logger, sessionStore);

await foreach (var update in agent.RunStreamingAsync(
    new List<ChatMessage>
    {
        new ChatMessage(ChatRole.User, "写一首诗")
    }))
{
    // 实时显示生成的内容
    Console.Write(update);
}
```

### ⚙️ MS AF 框架内部方法（不直接调用）

#### 5. `RunCoreAsync()` - 框架内部实现
```csharp
protected override async Task<AgentResponse> RunCoreAsync(
    IEnumerable<ChatMessage> messages,
    AgentSession? session = null,
    AgentRunOptions? options = null,
    CancellationToken cancellationToken = default)
```

**说明**：
- 这是 MS Agent Framework 要求实现的**受保护抽象方法**
- **MafBusinessAgentBase 不应该直接调用此方法**
- 框架会自动调用此方法来处理 AI 对话
- 内部会调用 `ExecuteAsync()` 并处理会话状态管理

**何时使用**：
- 仅由 MS AF 框架自动调用
- 不需要手动调用

**示例**：
```csharp
// ❌ 错误：不应该直接调用
// var response = await llmAgent.RunCoreAsync(messages, session);

// ✅ 正确：使用公开的 RunAsync API
var response = await llmAgent.RunAsync(messages, session);
```

### 🔧 辅助方法（供内部使用）

#### 4. `SupportsScenario()` - 检查场景支持
```csharp
public virtual bool SupportsScenario(LlmScenario scenario)
```

**说明**：
- 检查 Agent 是否支持指定的场景
- 由 `IMafAiAgentRegistry` 内部使用
- MafBusinessAgentBase 通常不需要直接调用

#### 5. `EstimateTokenCount()` - Token 估算
```csharp
protected int EstimateTokenCount(string text)
```

**说明**：
- 估算文本的 Token 数量（粗略估算）
- 用于会话状态管理中的 Token 统计
- MafBusinessAgentBase 可以调用此方法进行成本预估

**示例**：
```csharp
var prompt = "你的提示词";
var estimatedTokens = llmAgent.EstimateTokenCount(prompt);
Logger.LogDebug($"Estimated tokens: {estimatedTokens}");
```

## MafBusinessAgentBase 的封装方法

MafBusinessAgentBase 提供了三个封装方法，简化 LLM 调用：

### 1. `CallLlmAsync()` - 简单 LLM 调用
```csharp
protected async Task<string> CallLlmAsync(
    string prompt,
    LlmScenario scenario = LlmScenario.Chat,
    string? systemPrompt = null,
    CancellationToken ct = default)
```

**使用场景**：单轮对话、简单任务

**示例**：
```csharp
var response = await CallLlmAsync(
    "请识别用户意图：打开空调",
    LlmScenario.IntentRecognition,
    "你是智能家居意图识别专家");
```

### 2. `CallLlmChatAsync()` - 多轮对话
```csharp
protected async Task<string> CallLlmChatAsync(
    IEnumerable<ChatMessage> messages,
    LlmScenario scenario = LlmScenario.Chat,
    CancellationToken ct = default)
```

**使用场景**：多轮对话、需要上下文的任务

**示例**：
```csharp
var messages = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, "你是一个智能助手"),
    new ChatMessage(ChatRole.User, "你好"),
    new ChatMessage(ChatRole.Assistant, "你好！有什么可以帮助你的？"),
    new ChatMessage(ChatRole.User, "今天天气怎么样？")
};

var response = await CallLlmChatAsync(messages, LlmScenario.Chat);
```

### 3. `CallLlmBatchAsync()` - 批量调用
```csharp
protected async Task<string[]> CallLlmBatchAsync(
    string[] prompts,
    LlmScenario scenario = LlmScenario.Chat,
    string? systemPrompt = null,
    CancellationToken ct = default)
```

**使用场景**：并行处理多个请求

**示例**：
```csharp
var prompts = new[]
{
    "总结这段文本",
    "提取关键信息",
    "生成标题"
};

var responses = await CallLlmBatchAsync(prompts, LlmScenario.TextGeneration);
```

## 最佳实践

### ✅ 推荐做法

#### 场景 1: 使用 MafBusinessAgentBase 的封装方法（业务逻辑）

**适用于**：Demo Agent 实现业务逻辑时

```csharp
public class SmartHomeAgent : MafBusinessAgentBase
{
    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        CancellationToken ct)
    {
        // 使用封装方法 - 自动处理 LLM 选择和错误处理
        var response = await CallLlmAsync(
            request.UserMessage,
            LlmScenario.IntentRecognition,
            ct: ct);

        return new MafTaskResponse { Result = response };
    }
}
```

#### 场景 2: 直接使用 MS AF 的 RunAsync（标准对话）

**适用于**：需要使用 MS AF 的 AgentThread 管理对话状态时

```csharp
public class ChatService
{
    private readonly MafAiAgent _agent;
    private readonly AgentThread _thread;

    public async Task<string> ChatAsync(string userMessage)
    {
        // 使用 MS AF 的公开 API
        var response = await _agent.RunAsync(
            new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, userMessage)
            },
            session: _thread.Session); // 自动管理会话状态

        return response.Messages.First().Text;
    }
}
```

#### 场景 3: 使用 RunStreamingAsync（流式对话）

**适用于**：需要实时显示生成过程时

```csharp
public async Task StreamChatAsync(string userMessage)
{
    await foreach (var update in _agent.RunStreamingAsync(
        new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, userMessage)
        }))
    {
        // 实时更新 UI
        OnTextReceived(update.ToString());
    }
}
```

### 📊 API 选择指南

| API 方法 | 使用场景 | 是否自动管理会话 | 返回方式 |
|---------|---------|----------------|---------|
| `CallLlmAsync()` | 业务逻辑处理 | ❌ 需要手动管理 | 完整文本 |
| `CallLlmChatAsync()` | 多轮对话（封装） | ❌ 需要手动管理 | 完整文本 |
| `CallLlmBatchAsync()` | 批量并行处理 | ❌ 需要手动管理 | 文本数组 |
| `RunAsync()` | MS AF 标准对话 | ✅ 自动管理 | AgentResponse |
| `RunStreamingAsync()` | 流式对话 | ✅ 自动管理 | 流式更新 |

### 🎯 选择建议

**使用 MafBusinessAgentBase 封装方法的情况**：
- ✅ 实现具体的业务逻辑（智能家居控制、任务调度等）
- ✅ 需要根据场景自动选择 LLM（意图识别、文本生成等）
- ✅ 不需要 MS AF 的 AgentThread 管理
- ✅ 简单的单次 LLM 调用

**使用 MS AF 公开 API（RunAsync/RunStreamingAsync）的情况**：
- ✅ 需要使用 MS AF 的 AgentThread 管理对话状态
- ✅ 需要使用 MS AF 的会话持久化和恢复
- ✅ 需要流式响应（实时显示生成过程）
- ✅ 需要与 MS AF 的其他功能集成（如 ContextProvider）

### 📌 根据场景选择合适的 LlmScenario

```csharp
// 意图识别
await CallLlmAsync(prompt, LlmScenario.IntentRecognition);

// 文本生成
await CallLlmAsync(prompt, LlmScenario.TextGeneration);

// 代码生成
await CallLlmAsync(prompt, LlmScenario.CodeGeneration);

// 数据分析
await CallLlmAsync(prompt, LlmScenario.DataAnalysis);
```

### ⚠️ 处理异常和重试

```csharp
try
{
    var response = await CallLlmAsync(prompt, scenario, ct: ct);
    return new MafTaskResponse { Success = true, Result = response };
}
catch (Exception ex)
{
    Logger.LogError(ex, "LLM调用失败");
    return new MafTaskResponse { Success = false, Error = ex.Message };
}
```

### ❌ 避免的做法

1. **不要直接调用 `RunCoreAsync()`**
   ```csharp
   // ❌ 错误：RunCoreAsync 是受保护的内部方法
   // var response = await llmAgent.RunCoreAsync(messages, session);

   // ✅ 正确：使用 MS AF 的公开 API
   var response = await llmAgent.RunAsync(messages, session);

   // ✅ 或者使用 MafBusinessAgentBase 的封装方法
   var response = await CallLlmAsync(prompt, scenario);
   ```

2. **不要绕过 Registry 直接创建 Agent（在业务层）**
   ```csharp
   // ❌ 错误：绕过了注册表和场景选择机制
   // var agent = new ZhipuAiAgent(config, logger);
   // var response = await agent.ExecuteAsync(...);

   // ✅ 正确：通过 Registry 获取最佳 Agent
   var agent = await LlmRegistry.GetBestAgentAsync(scenario);
   var response = await agent.ExecuteAsync(...);
   ```

   **例外情况**：如果需要直接使用特定的 Agent（不通过 Registry），可以直接创建：
   ```csharp
   // ✅ 可接受：直接创建 Agent 并使用 RunAsync
   var agent = new ZhipuAiAgent(config, logger, sessionStore);
   var response = await agent.RunAsync(messages);
   ```

3. **不要混淆 ExecuteAsync 和 RunAsync**
   ```csharp
   // ❌ 错误：在业务层直接调用 ExecuteAsync 而不使用 Registry
   // var agent = new ZhipuAiAgent(config, logger);
   // var response = await agent.ExecuteAsync(modelId, prompt);

   // ✅ 正确：使用公开的 RunAsync API
   var agent = new ZhipuAiAgent(config, logger, sessionStore);
   var response = await agent.RunAsync(messages);

   // ✅ 或使用 MafBusinessAgentBase 的封装方法（内部会通过 Registry）
   var response = await CallLlmAsync(prompt, scenario);
   ```

4. **不要在业务层直接操作会话状态**
   ```csharp
   // ❌ 错误：MafBusinessAgentBase 不应该直接管理会话
   // var session = await sessionStore.LoadAsync(sessionId);
   // session.TurnCount++;

   // ✅ 正确：使用 MS AF 的 RunAsync，会话状态自动管理
   var response = await agent.RunAsync(messages, session);

   // ✅ 或者：使用 MafBusinessAgentBase 的封装方法
   // 会话管理由 MafAiAgent 内部处理（如果启用了 SessionStore）
   var response = await CallLlmAsync(prompt, scenario);
   ```

## 总结

### 📋 API 方法对比表

| API 方法 | 类型 | 调用者 | 用途 | 会话管理 |
|---------|------|--------|------|---------|
| **MS AF 公开 API** |
| `RunAsync()` | 公开 | Demo Agent / Service | 标准 MS AF 对话 | ✅ 自动 |
| `RunStreamingAsync()` | 公开 | Demo Agent / Service | 流式 MS AF 对话 | ✅ 自动 |
| **MAF 封装方法** |
| `CallLlmAsync()` | 封装 | Demo Agent | 简单单次 LLM 调用 | ⚠️ 手动（可选） |
| `CallLlmChatAsync()` | 封装 | Demo Agent | 多轮对话 | ⚠️ 手动（可选） |
| `CallLlmBatchAsync()` | 封装 | Demo Agent | 批量并行调用 | ⚠️ 手动（可选） |
| **MAF 内部方法** |
| `ExecuteAsync()` | 抽象 | MafBusinessAgentBase (内部) | 单次 LLM 调用实现 | ❌ 无 |
| `ExecuteBatchAsync()` | 虚方法 | MafBusinessAgentBase (内部) | 批量 LLM 调用实现 | ❌ 无 |
| **MS AF 内部方法** |
| `RunCoreAsync()` | 受保护 | MS AF 框架 (内部) | 框架入口实现 | ✅ 自动 |

### 🎯 使用决策树

```
需要调用 LLM
    │
    ├─ 需要使用 MS AF 的 AgentThread 管理？
    │   ├─ 是 → 使用 RunAsync() / RunStreamingAsync()
    │   └─ 否 → 继续判断
    │
    ├─ 在 Demo Agent 中实现业务逻辑？
    │   ├─ 是 → 使用 CallLlmAsync() / CallLlmChatAsync()
    │   └─ 否 → 继续判断
    │
    ├─ 需要流式响应？
    │   ├─ 是 → 使用 RunStreamingAsync()
    │   └─ 否 → 继续判断
    │
    └─ 直接使用具体 Agent？
        ├─ 是 → 使用 agent.RunAsync()
        └─ 否 → 通过 Registry 获取 Agent
```

### 🔑 核心原则

1. **Demo Agent 层**
   - 优先使用 `MafBusinessAgentBase` 的封装方法
   - 简化 LLM 调用，自动处理场景选择

2. **业务逻辑层**
   - 使用 `CallLlmAsync()` 等封装方法
   - 通过 `IMafAiAgentRegistry` 获取最佳 Agent

3. **MS AF 集成层**
   - 使用 `RunAsync()` / `RunStreamingAsync()` 公开 API
   - 自动管理 AgentThread 和会话状态

4. **框架实现层**
   - `ExecuteAsync()` / `ExecuteBatchAsync()` 供内部调用
   - `RunCoreAsync()` 仅由 MS AF 框架调用

### 💡 实际应用示例

#### 示例 1: 智能家居控制（使用封装方法）
```csharp
public class SmartHomeAgent : MafBusinessAgentBase
{
    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request, CancellationToken ct)
    {
        // 使用封装方法 - 简单直接
        var intent = await CallLlmAsync(
            $"识别意图: {request.UserMessage}",
            LlmScenario.IntentRecognition,
            ct: ct);

        return new MafTaskResponse { Result = intent };
    }
}
```

#### 示例 2: 聊天机器人（使用 MS AF API）
```csharp
public class ChatService
{
    private readonly MafAiAgent _agent;
    private readonly AgentThread _thread;

    public async Task<string> ChatAsync(string message)
    {
        // 使用 MS AF 公开 API - 自动管理会话
        var response = await _agent.RunAsync(
            new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, message)
            },
            session: _thread.Session);

        return response.Messages.First().Text;
    }
}
```

#### 示例 3: 流式对话（使用 RunStreamingAsync）
```csharp
public async Task StreamChatAsync(string message)
{
    // 使用流式 API - 实时反馈
    await foreach (var update in _agent.RunStreamingAsync(
        new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, message)
        }))
    {
        OnTextReceived(update.ToString());
    }
}
```

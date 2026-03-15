# MafAiAgent 方法设计说明

## 问题：为什么需要两套方法？

MafAiAgent 继承了 MS AF 的 `AIAgent`，但仍然提供了 `ExecuteAsync` 和 `ExecuteBatchAsync` 方法。这是为什么？

## 🔄 两套方法对比

### MS AF 的方法（框架标准）

| 方法 | 类型 | 签名 |
|------|------|------|
| `RunAsync()` | 公开 API | `Task<AgentResponse> RunAsync(IEnumerable<ChatMessage>, AgentSession?, ...)` |
| `RunStreamingAsync()` | 公开 API | `IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(...)` |
| `RunCoreAsync()` | 受保护抽象 | `Task<AgentResponse> RunCoreAsync(...)` ← 必须实现 |

### MAF 的方法（自定义接口）

| 方法 | 类型 | 签名 |
|------|------|------|
| `ExecuteAsync()` | 抽象方法 | `Task<string> ExecuteAsync(string modelId, string prompt, string? systemPrompt, ...)` |
| `ExecuteBatchAsync()` | 虚方法 | `Task<string[]> ExecuteBatchAsync(string modelId, string[] prompts, ...)` |

## 🎯 设计原因

### 1. **关注点分离** - MS AF vs MAF

#### MS AF 方法：用于框架集成
```csharp
// 使用 MS AF 的标准 API
var agent = new ZhipuAiAgent(config, logger, sessionStore);

// ✅ 需要使用 MS AF 的类型系统
var response = await agent.RunAsync(
    new List<ChatMessage>  // MS AF 类型
    {
        new ChatMessage(ChatRole.System, "你是一个助手"),
        new ChatMessage(ChatRole.User, "你好")
    },
    session: thread.Session,  // MS AF 会话
    options: new AgentRunOptions { ... });  // MS AF 选项
```

**特点**：
- ✅ 完整的 MS AF 生态系统集成
- ✅ 自动会话管理（AgentThread）
- ✅ 支持 ContextProvider、Tools 等高级功能
- ❌ 复杂的类型系统
- ❌ 需要理解 MS AF 的概念

#### MAF 方法：用于业务逻辑
```csharp
// 使用 MAF 的简化接口
var agent = await LlmRegistry.GetBestAgentAsync(LlmScenario.IntentRecognition);

// ✅ 简单直接的字符串参数
var response = await agent.ExecuteAsync(
    modelId: "glm-4",
    prompt: "识别意图：打开空调",
    systemPrompt: "你是智能家居意图识别专家",
    ct: CancellationToken.None);
```

**特点**：
- ✅ 简单直接，只需要字符串参数
- ✅ 不依赖 MS AF 的复杂类型
- ✅ 适合业务逻辑层调用
- ❌ 不自动管理会话（需要手动管理）

### 2. **架构层次** - 不同层次使用不同方法

```
┌─────────────────────────────────────────────────────────────┐
│  Demo Agent (SmartHomeAgent, ClimateAgent, etc.)          │
│  继承: MafBusinessAgentBase                                 │
│  使用: CallLlmAsync() → ExecuteAsync()                      │
└────────────────────┬────────────────────────────────────────┘
                     │ 简单的字符串参数
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  MafBusinessAgentBase (业务层基类)                           │
│  提供封装方法: CallLlmAsync, CallLlmChatAsync               │
│  内部调用: ExecuteAsync()                                   │
└────────────────────┬────────────────────────────────────────┘
                     │ 直接字符串调用
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  MafAiAgent (LLM 层基类)                                     │
│  提供: ExecuteAsync() ← 抽象方法                            │
│        RunCoreAsync() ← MS AF 要求                          │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│  MS AF 集成路径   │    │  MAF 业务路径    │
│                  │    │                  │
│ RunAsync()       │    │ ExecuteAsync()   │
│   ↓              │    │   ↓              │
│ RunCoreAsync()   │    │ 具体厂商实现     │
│   ↓              │    │ (智谱/通义等)    │
│ ExecuteAsync()   │    │                  │
└──────────────────┘    └──────────────────┘
```

### 3. **具体实现示例**

假设我们有两个厂商实现：

#### 实现 1：使用 MAF 方法（推荐用于业务逻辑）

```csharp
public class ZhipuAiAgent : MafAiAgent
{
    public ZhipuAiAgent(
        LlmProviderConfig config,
        ILogger logger,
        IMafAiSessionStore? sessionStore = null)
        : base(config, logger, sessionStore)
    {
    }

    // ✅ 实现简单的 ExecuteAsync
    public override async Task<string> ExecuteAsync(
        string modelId,
        string prompt,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        var apiKey = GetApiKey();

        // 直接调用智谱 AI API
        var response = await _httpClient.PostAsJsonAsync(
            "https://open.bigmodel.cn/api/paas/v4/chat/completions",
            new
            {
                model = modelId,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt ?? "你是一个有用的AI助手" },
                    new { role = "user", content = prompt }
                }
            },
            ct);

        var result = await response.Content.ReadFromJsonAsync<ZhipuResponse>(ct);
        return result.choices[0].message.content;
    }

    // RunCoreAsync 由 MafAiAgent 基类实现，内部调用 ExecuteAsync
}
```

**使用方式**：
```csharp
// 在 MafBusinessAgentBase 中使用
var intent = await agent.ExecuteAsync(
    "glm-4",
    "识别意图：打开空调",
    "你是智能家居意图识别专家");
```

#### 实现 2：使用 MS AF 方法（推荐用于框架集成）

```csharp
// 直接使用 MS AF 的公开 API
var agent = new ZhipuAiAgent(config, logger, sessionStore);

// ✅ 使用 RunAsync - 自动管理会话
var response = await agent.RunAsync(
    new List<ChatMessage>
    {
        new ChatMessage(ChatRole.User, "你好")
    });

Console.WriteLine(response.Messages.First().Text);
```

### 4. **为什么不能只用一种方法？**

#### ❌ 如果只用 MS AF 方法（RunAsync）

```csharp
// 问题：业务层需要处理 MS AF 的复杂类型
public class SmartHomeAgent : MafBusinessAgentBase
{
    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request, CancellationToken ct)
    {
        // ❌ 业务逻辑需要理解 MS AF 的类型
        var agent = await LlmRegistry.GetBestAgentAsync(LlmScenario.IntentRecognition);

        var response = await agent.RunAsync(  // 返回 AgentResponse
            new List<ChatMessage>  // 需要构建消息列表
            {
                new ChatMessage(ChatRole.User, request.UserMessage)
            });

        var text = response.Messages.First().Text;  // 需要解析响应
        return new MafTaskResponse { Result = text };
    }
}
```

**缺点**：
- 业务层依赖 MS AF 的类型系统
- 每次调用都需要构建 `ChatMessage` 列表
- 需要解析 `AgentResponse` 对象
- 增加了业务层的复杂度

#### ❌ 如果只用 MAF 方法（ExecuteAsync）

```csharp
// 问题：无法使用 MS AF 的会话管理和高级功能
var agent = new ZhipuAiAgent(config, logger);

// ❌ 没有 RunAsync，无法使用 MS AF 的会话管理
// ❌ 没有 AgentThread，无法跨多轮对话保持上下文
// ❌ 没有 ContextProvider，无法使用上下文注入/提取
```

**缺点**：
- 失去了 MS AF 的会话管理能力
- 无法使用 AgentThread
- 无法使用 ContextProvider、Tools 等高级功能
- 与 MS AF 生态系统脱节

### 5. **最佳实践 - 两套方法配合使用**

```csharp
public class ZhipuAiAgent : MafAiAgent
{
    // ✅ 实现 ExecuteAsync - 供业务逻辑层使用
    public override async Task<string> ExecuteAsync(
        string modelId,
        string prompt,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        // 直接调用厂商 API，返回纯文本
        var response = await CallZhipuApiAsync(modelId, prompt, systemPrompt, ct);
        return response;
    }

    // ✅ RunCoreAsync 由基类实现，内部调用 ExecuteAsync
    // 用于 MS AF 的 RunAsync/RunStreamingAsync
}
```

**使用方式 1：业务逻辑（使用 ExecuteAsync）**
```csharp
// 在 MafBusinessAgentBase 中
var intent = await CallLlmAsync(prompt, LlmScenario.IntentRecognition);
// 内部调用: agent.ExecuteAsync(modelId, prompt, systemPrompt)
```

**使用方式 2：MS AF 集成（使用 RunAsync）**
```csharp
// 在需要完整 MS AF 功能的地方
var response = await agent.RunAsync(messages, session);
// 内部调用: RunCoreAsync → ExecuteAsync
```

## 📊 方法调用关系图

```
┌─────────────────────────────────────────────────────────────┐
│                     调用入口                                │
└─────────────────────────────────────────────────────────────┘
         │                           │
         ▼                           ▼
┌──────────────────┐        ┌──────────────────┐
│  业务逻辑层       │        │  MS AF 集成层    │
│                  │        │                  │
│ CallLlmAsync()   │        │ RunAsync()       │
│                  │        │ RunStreamingAsync()│
└────────┬─────────┘        └────────┬─────────┘
         │                           │
         │ 通过 Registry             │ 直接调用
         ▼                           ▼
┌─────────────────────────────────────────────────────────────┐
│  MafAiAgent                                               │
│                                                           │
│  ExecuteAsync() ←──┬── 业务逻辑层直接调用                   │
│                    │                                       │
│  RunCoreAsync() ←──┴── MS AF 框架调用（由 RunAsync 触发）   │
│         │                                                   │
│         └──→ 内部都调用 ExecuteAsync()                      │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  具体厂商实现（ZhipuAiAgent, QwenAgent, 等）              │
│                                                           │
│  ExecuteAsync() 实现                                       │
│  - 调用智谱/通义/文心等 API                                │
│  - 返回纯文本                                             │
└─────────────────────────────────────────────────────────────┘
```

## ✅ 总结

| 方法 | 用途 | 调用者 | 返回类型 | 会话管理 |
|------|------|--------|---------|---------|
| `ExecuteAsync()` | 业务逻辑 LLM 调用 | MafBusinessAgentBase | `string` | 手动（可选） |
| `RunAsync()` | MS AF 标准集成 | Service / Controller | `AgentResponse` | 自动（AgentThread） |
| `RunCoreAsync()` | MS AF 内部实现 | MS AF 框架 | `AgentResponse` | 自动 |

**核心原则**：
1. **ExecuteAsync** - 简单直接的 LLM 调用，供业务逻辑层使用
2. **RunAsync** - MS AF 标准接口，供需要完整框架功能的场景使用
3. 两者配合，既保持简单性，又提供完整的框架能力

**设计优势**：
- ✅ 业务层保持简单（不需要理解 MS AF）
- ✅ 支持完整的 MS AF 生态（AgentThread、ContextProvider）
- ✅ 灵活性高（可以根据场景选择合适的 API）
- ✅ 向后兼容（不影响现有 MS AF 代码）

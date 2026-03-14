# Microsoft Agent Framework - AIContextProvider 参考文档

## 概述

`AIContextProvider` 是 Microsoft Agent Framework (MS AF) 提供的一个抽象接口，用于在 LLM 调用前后进行上下文注入和数据提取。

### 核心定义

```csharp
public interface IAIContextProvider
{
    /// <summary>
    /// 在 LLM 调用前注入上下文信息
    /// </summary>
    Task<AIContext> PrepareContextAsync(AIContext currentContext, CancellationToken cancellationToken);

    /// <summary>
    /// 在 LLM 调用后提取和更新上下文信息
    /// </summary>
    Task<AIContext> ProcessContextAsync(AIContext context, AIResult result, CancellationToken cancellationToken);
}
```

## 使用场景

### 1. RAG (检索增强生成)

**场景描述**: 从向量数据库检索相关文档片段，注入到 LLM 提示词中

```csharp
public class RagContextProvider : IAIContextProvider
{
    private readonly IVectorStore _vectorStore;

    public async Task<AIContext> PrepareContextAsync(AIContext context, CancellationToken ct)
    {
        var query = context.Input.Text;
        var relevantDocs = await _vectorStore.SearchAsync(query, topK: 5, ct);

        // 将检索到的文档注入到上下文中
        context.ContextData["retrieved_docs"] = relevantDocs;
        context.SystemMessage += $"\n\n参考文档：\n{string.Join("\n", relevantDocs)}";

        return context;
    }
}
```

**典型应用**:
- 知识库问答系统
- 企业文档智能搜索
- 技术文档助手

### 2. 对话历史管理

**场景描述**: 维护长期和短期对话历史，支持多轮对话

```csharp
public class ConversationHistoryProvider : IAIContextProvider
{
    private readonly ISessionStorage _sessionStorage;

    public async Task<AIContext> PrepareContextAsync(AIContext context, CancellationToken ct)
    {
        var sessionId = context.ContextData["session_id"] as string;
        var history = await _sessionStorage.GetHistoryAsync(sessionId, ct);

        // 只保留最近 N 轮对话
        var recentHistory = history.TakeLast(10);
        context.Messages.AddRange(recentHistory);

        return context;
    }

    public async Task<AIContext> ProcessContextAsync(AIContext context, AIResult result, CancellationToken ct)
    {
        var sessionId = context.ContextData["session_id"] as string;

        // 保存当前轮次的对话
        await _sessionStorage.AddMessageAsync(sessionId, context.Input, ct);
        await _sessionStorage.AddMessageAsync(sessionId, result.Output, ct);

        return context;
    }
}
```

**典型应用**:
- 智能客服系统
- 个人助理应用
- 教学辅导系统

### 3. 用户偏好注入

**场景描述**: 根据用户历史行为和偏好定制 LLM 响应

```csharp
public class UserPreferenceProvider : IAIContextProvider
{
    private readonly IUserProfileRepository _userProfileRepo;

    public async Task<AIContext> PrepareContextAsync(AIContext context, CancellationToken ct)
    {
        var userId = context.ContextData["user_id"] as string;
        var profile = await _userProfileRepo.GetAsync(userId, ct);

        // 注入用户偏好
        context.ContextData["language"] = profile.PreferredLanguage;
        context.ContextData["tone"] = profile.PreferredTone;
        context.ContextData["expertise_level"] = profile.ExpertiseLevel;

        context.SystemMessage += $@"
用户偏好设置：
- 语言：{profile.PreferredLanguage}
- 语气：{profile.PreferredTone}
- 专业程度：{profile.ExpertiseLevel}
";

        return context;
    }
}
```

**典型应用**:
- 个性化推荐系统
- 自适应学习平台
- 内容定制服务

### 4. 动态工具注入

**场景描述**: 根据当前任务动态选择可用的工具/函数

```csharp
public class DynamicToolProvider : IAIContextProvider
{
    private readonly IToolRegistry _toolRegistry;

    public async Task<AIContext> PrepareContextAsync(AIContext context, CancellationToken ct)
    {
        var intent = await DetectIntentAsync(context.Input.Text, ct);
        var availableTools = await _toolRegistry.GetToolsForIntentAsync(intent, ct);

        // 动态注入可用工具
        context.AvailableTools.Clear();
        foreach (var tool in availableTools)
        {
            context.AvailableTools.Add(tool);
        }

        return context;
    }
}
```

**典型应用**:
- API 调用代理
- 任务自动化系统
- 多步骤工作流引擎

### 5. 上下文压缩与优化

**场景描述**: 当上下文过长时，智能压缩和优化

```csharp
public class ContextCompressionProvider : IAIContextProvider
{
    private const int MaxContextTokens = 4000;

    public async Task<AIContext> PrepareContextAsync(AIContext context, CancellationToken ct)
    {
        var currentTokenCount = EstimateTokenCount(context);

        if (currentTokenCount > MaxContextTokens)
        {
            // 使用 LLM 总结历史对话
            var summary = await SummarizeHistoryAsync(context.Messages, ct);

            context.Messages.Clear();
            context.Messages.Add(new AIMessage(summary));
        }

        return context;
    }

    private async Task<string> SummarizeHistoryAsync(List<AIMessage> history, CancellationToken ct)
    {
        // 调用 LLM 生成摘要
        var summaryPrompt = $"请总结以下对话历史的关键信息：\n{string.Join("\n", history)}";
        // ... 调用 LLM 并返回摘要
    }
}
```

**典型应用**:
- 长对话系统
- 文档分析工具
- 代码审查助手

## CKY.MAF 架构分析

### 架构层次

CKY.MAF 有两种 Agent 基类，使用场景不同：

#### 1. MafAiAgent (基础设施层) ✅ 支持 AIContextProvider

```csharp
public abstract class MafAiAgent : AIAgent  // 继承 MS AF 的基类
{
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,  // ✅ 可以使用 AIContextProvider
        CancellationToken cancellationToken = default)
    {
        // MS AF 框架会自动调用已注册的 AIContextProvider
        // 在调用 LLM 前：PrepareContextAsync()
        // 在调用 LLM 后：ProcessContextAsync()

        // 提取用户消息
        var userMessage = messages.FirstOrDefault(m => m.Role == ChatRole.User);
        var prompt = userMessage?.Text ?? string.Empty;

        // 调用子类实现的 ExecuteAsync 方法
        var responseText = await ExecuteAsync(
            Config.ModelId,
            prompt,
            systemPrompt,
            cancellationToken);

        return new AgentResponse(new[] { new ChatMessage(ChatRole.Assistant, responseText) });
    }

    public abstract Task<string> ExecuteAsync(string modelId, string prompt, string? systemPrompt = null, CancellationToken ct = default);
}
```

**特点**:
- **位置**: `src/Core/Agents/MafAiAgent.cs`
- **继承**: `AIAgent` (MS AF 基类)
- **AIContextProvider 支持**: ✅ **完全支持**
- **用途**: 具体厂商实现的基础类（ZhipuAIAgent、QwenAgent 等）

#### 2. MafAgentBase (业务层) ❌ 不支持 AIContextProvider

```csharp
public abstract class MafAgentBase
{
    private readonly IMafAiAgentRegistry _llmRegistry;  // 组合模式，非继承

    protected async Task<string> CallLlmAsync(
        string prompt,
        LlmScenario scenario,
        string? systemPrompt = null,
        CancellationToken ct = default)
    {
        // 手动调用 LLM，无法使用 MS AF 的上下文注入机制
        var agent = await _llmRegistry.GetBestAgentAsync(scenario, ct);
        return await agent.ExecuteAsync(agent.GetCurrentModelId(), prompt, systemPrompt, ct);
    }
}
```

**特点**:
- **位置**: `src/Core/Agents/MafAgentBase.cs`
- **继承**: 无（使用组合模式）
- **AIContextProvider 支持**: ❌ **不支持**
- **用途**: 业务 Agent 的基础类（IntentRecognitionAgent、DialogueAgent 等）

### 在 MafAiAgent 中使用 AIContextProvider

由于 `MafAiAgent` 继承自 `AIAgent`，可以通过 MS AF 的机制使用 `AIContextProvider`。以下是实现方式：

#### 步骤 1: 创建自定义 AIContextProvider

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Context
{
    /// <summary>
    /// RAG 上下文提供器 - 为 MafAiAgent 提供检索增强功能
    /// </summary>
    public class RagContextProvider : IAIContextProvider
    {
        private readonly IVectorStore _vectorStore;
        private readonly ILogger<RagContextProvider> _logger;

        public RagContextProvider(IVectorStore vectorStore, ILogger<RagContextProvider> logger)
        {
            _vectorStore = vectorStore;
            _logger = logger;
        }

        /// <summary>
        /// 在 LLM 调用前注入检索到的文档
        /// </summary>
        public async Task<AIContext> PrepareContextAsync(AIContext currentContext, CancellationToken cancellationToken)
        {
            try
            {
                // 从上下文中提取用户查询
                var userMessage = currentContext.Input.Text;
                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    return currentContext;
                }

                // 从向量数据库检索相关文档
                var searchResults = await _vectorStore.SearchAsync(
                    userMessage,
                    topK: 5,
                    cancellationToken);

                if (searchResults.Any())
                {
                    // 将检索结果注入到系统消息中
                    var docsContext = string.Join("\n\n", searchResults.Select(r => $"- {r.Content}"));
                    currentContext.SystemMessage += $"\n\n参考文档：\n{docsContext}";

                    _logger.LogInformation("[RAG] 注入了 {Count} 个文档片段", searchResults.Count());
                }

                return currentContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RAG] 上下文准备失败");
                return currentContext; // 降级：返回原始上下文
            }
        }

        /// <summary>
        /// 在 LLM 调用后处理响应（可选）
        /// </summary>
        public async Task<AIContext> ProcessContextAsync(AIContext context, AIResult result, CancellationToken cancellationToken)
        {
            // 可以在这里记录日志、更新向量数据库等
            await Task.CompletedTask;
            return context;
        }
    }
}
```

#### 步骤 2: 在具体 MafAiAgent 实现中启用上下文提供器

```csharp
using CKY.MultiAgentFramework.Core.Agents;
using CKY.MultiAgentFramework.Core.Models.LLM;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Llm
{
    /// <summary>
    /// 智谱 AI LLM Agent 实现
    /// </summary>
    public class ZhipuAIAgent : MafAiAgent
    {
        private readonly HttpClient _httpClient;
        private readonly IEnumerable<IAIContextProvider>? _contextProviders;

        public ZhipuAIAgent(
            LlmProviderConfig config,
            HttpClient httpClient,
            ILogger<ZhipuAIAgent> logger,
            IEnumerable<IAIContextProvider>? contextProviders = null)
            : base(config, logger)
        {
            _httpClient = httpClient;
            _contextProviders = contextProviders;  // 注入上下文提供器
        }

        /// <summary>
        /// 实现抽象方法：调用智谱 AI API
        /// </summary>
        public override async Task<string> ExecuteAsync(
            string modelId,
            string prompt,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            // 检查是否有上下文提供器需要处理
            if (_contextProviders != null && _contextProviders.Any())
            {
                Logger.LogInformation("[ZhipuAI] 使用 {Count} 个上下文提供器", _contextProviders.Count());
            }

            // 调用智谱 AI API
            var apiKey = GetApiKey();
            var request = new
            {
                model = modelId,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt ?? "你是一个有用的AI助手。" },
                    new { role = "user", content = prompt }
                }
            };

            // HTTP 请求实现...
            // (省略具体实现，参考现有的智谱 AI 集成代码)

            return "LLM 响应内容";
        }

        /// <summary>
        /// 重写 RunCoreAsync 以启用 AIContextProvider
        /// 注意：MS AF 框架会自动调用已注册的 AIContextProvider
        /// </summary>
        protected override async Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session = null,
            AgentRunOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // MS AF 框架会在此处自动调用：
            // 1. 所有已注册 AIContextProvider.PrepareContextAsync()
            // 2. 执行 LLM 调用（ExecuteAsync）
            // 3. 所有已注册 AIContextProvider.ProcessContextAsync()

            // 调用基类实现，享受框架的上下文管理
            return await base.RunCoreAsync(messages, session, options, cancellationToken);
        }
    }
}
```

#### 步骤 3: 在 DI 容器中注册 AIContextProvider

```csharp
// Program.cs 或 Startup.cs

// 1. 注册向量数据库
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

// 2. 注册 AIContextProvider
builder.Services.AddSingleton<IAIContextProvider, RagContextProvider>();

// 3. 注册 MafAiAgent（会自动注入已注册的 AIContextProvider）
builder.Services.AddSingleton<MafAiAgent>(sp =>
{
    var config = new LlmProviderConfig
    {
        ProviderName = "ZhipuAI",
        ModelId = "glm-4",
        ApiKey = builder.Configuration["LLM:ZhipuAI:ApiKey"]!,
        BaseUrl = "https://open.bigmodel.cn/api/paas/v4/",
        Priority = 1,
        IsEnabled = true,
        SupportedScenarios = new[] { LlmScenario.Chat, LlmScenario.Intent }
    };

    var httpClient = sp.GetRequiredService<HttpClient>();
    var logger = sp.GetRequiredService<ILogger<ZhipuAIAgent>>();
    var contextProviders = sp.GetServices<IAIContextProvider>();  // 获取所有已注册的提供器

    return new ZhipuAIAgent(config, httpClient, logger, contextProviders);
});
```

### AIContextProvider 工作流程

当使用 MafAiAgent 时，MS AF 框架会按以下顺序执行：

```
1. 用户调用 Agent.InvokeAsync("用户消息")
   ↓
2. MS AF 框架创建 AIContext
   ↓
3. 调用所有已注册的 AIContextProvider.PrepareContextAsync()
   - RagContextProvider: 注入检索到的文档
   - ConversationHistoryProvider: 加载历史对话
   - UserPreferenceProvider: 注入用户偏好
   ↓
4. 调用 MafAiAgent.RunCoreAsync()
   - 提取增强后的上下文
   - 调用子类 ExecuteAsync()
   ↓
5. 调用所有已注册的 AIContextProvider.ProcessContextAsync()
   - ConversationHistoryProvider: 保存当前对话
   - AnalyticsProvider: 记录日志和指标
   ↓
6. 返回 AgentResponse
```

### 为什么 MafAgentBase 不使用 AIContextProvider

#### 关键差异点

1. **继承 vs 组合**:
   - MS AF: Agent 继承自 `AIAgent`，通过框架自动管理上下文
   - CKY.MAF: `MafAgentBase` 不继承 `AIAgent`，使用组合模式访问 LLM

2. **控制粒度**:
   - MS AF: 框架控制上下文注入时机
   - CKY.MAF: 业务代码完全控制 LLM 调用流程

3. **灵活度**:
   - MS AF: 需要符合框架规范
   - CKY.MAF: 可以根据业务需求灵活调整

### CKY.MAF 的等效实现

#### DialogueAgent 的会话管理

```csharp
public class DialogueAgent : MafAgentBase
{
    private readonly Dictionary<string, List<ChatMessage>> _sessionHistories;

    public override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        CancellationToken ct = default)
    {
        var sessionId = GetParameter(request, "sessionId", Guid.NewGuid().ToString());
        var maxHistory = GetParameter(request, "maxHistory", 10);

        // 手动获取历史（相当于 PrepareContextAsync）
        var history = GetOrCreateHistory(sessionId);
        var userMessage = new ChatMessage(ChatRole.User, input);
        history.Add(userMessage);

        // 调用 LLM
        var response = await CallLlmChatAsync(
            history.TakeLast(maxHistory),
            LlmScenario.Chat,
            ct);

        // 手动保存响应（相当于 ProcessContextAsync）
        var assistantMessage = new ChatMessage(ChatRole.Assistant, response);
        history.Add(assistantMessage);

        return new MafTaskResponse { ... };
    }
}
```

### 未来集成建议

如果 CKY.MAF 需要更复杂的上下文管理，可以考虑：

#### 方案 1: 适配器模式

```csharp
public interface IMafContextProvider
{
    Task<List<ChatMessage>> PrepareContextAsync(
        string sessionId,
        string userInput,
        CancellationToken ct);

    Task SaveResponseAsync(
        string sessionId,
        ChatMessage response,
        CancellationToken ct);
}

public class MafAgentBase
{
    private readonly IMafContextProvider? _contextProvider;

    protected async Task<string> CallLlmWithContextAsync(
        string prompt,
        LlmScenario scenario,
        string? sessionId = null,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, prompt)
        };

        // 使用上下文提供器增强消息
        if (_contextProvider != null && sessionId != null)
        {
            messages = await _contextProvider.PrepareContextAsync(sessionId, prompt, ct);
        }

        var response = await CallLlmChatAsync(messages, scenario, ct);

        // 保存响应
        if (_contextProvider != null && sessionId != null)
        {
            await _contextProvider.SaveResponseAsync(
                sessionId,
                new ChatMessage(ChatRole.Assistant, response),
                ct);
        }

        return response;
    }
}
```

#### 方案 2: 管道模式

```csharp
public interface IContextProcessor
{
    Task<ChatMessage> ProcessAsync(
        ChatMessage input,
        ContextProcessorContext context,
        CancellationToken ct);
}

public class ContextProcessorPipeline
{
    private readonly List<IContextProcessor> _processors;

    public async Task<string> ExecuteAsync(
        string userInput,
        string sessionId,
        CancellationToken ct)
    {
        var message = new ChatMessage(ChatRole.User, userInput);
        var context = new ContextProcessorContext { SessionId = sessionId };

        // 依次执行每个处理器
        foreach (var processor in _processors)
        {
            message = await processor.ProcessAsync(message, context, ct);
        }

        return message.Content;
    }
}

// 示例处理器
public class RAGContextProcessor : IContextProcessor
{
    public async Task<ChatMessage> ProcessAsync(
        ChatMessage input,
        ContextProcessorContext context,
        CancellationToken ct)
    {
        var relevantDocs = await _vectorStore.SearchAsync(input.Content, topK: 5, ct);
        input.Content = $@"
参考文档：
{string.Join("\n", relevantDocs)}

用户问题：{input.Content}
";
        return input;
    }
}
```

## 最佳实践

### 1. 上下文数据结构设计

```csharp
public class MafContextData
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<ChatMessage> History { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}
```

### 2. 上下文持久化策略

- **L1 (内存)**: 当前会话数据，快速访问
- **L2 (Redis)**: 近期会话，24小时 TTL
- **L3 (数据库)**: 长期存储，用于分析和训练

### 3. 性能优化

- 并行加载多个上下文源
- 使用缓存减少重复查询
- 异步 I/O 避免阻塞
- 设置合理的超时时间

### 4. 错误处理

```csharp
public async Task<AIContext> PrepareContextAsync(AIContext context, CancellationToken ct)
{
    try
    {
        // 正常逻辑
    }
    catch (Exception ex)
    {
        // 降级：返回最小上下文
        Logger.LogWarning(ex, "Context preparation failed, using minimal context");
        return CreateMinimalContext(context);
    }
}
```

## CKY.MAF 内置 ContextProvider

CKY.MAF 提供了两个开箱即用的 AIContextProvider 实现：

### 1. ConversationHistoryProvider

**文件位置**: `src/Infrastructure/Context/ConversationHistoryProvider.cs`

**功能特性**:
- ✅ 多会话隔离（每个 sessionId 独立维护）
- ✅ 自动清理过期会话（可配置过期时间）
- ✅ 可配置最大历史轮数（默认 20 轮）
- ✅ 线程安全（使用 ConcurrentDictionary）
- ✅ 内存管理（自动清理最旧消息）

**使用示例**:

```csharp
// 1. 注册到 DI 容器
builder.Services.AddSingleton<IAIContextProvider, ConversationHistoryProvider>();

// 2. 在调用 LLM 时传入 sessionId
var context = new AIContext
{
    Input = new AIInput("用户消息"),
    ContextData = new Dictionary<string, object>
    {
        ["session_id"] = "user-123"  // 关键：指定会话 ID
    }
};

// 3. MS AF 框架会自动加载和保存历史
await agent.InvokeAsync(context);
```

**配置选项**:

```csharp
var options = new ConversationHistoryOptions
{
    MaxHistoryTurns = 30,           // 最大历史轮数
    SessionExpiration = TimeSpan.FromHours(12),  // 会话过期时间
    AutoCleanup = true              // 自动清理
};

builder.Services.AddSingleton<IAIContextProvider>(
    sp => new ConversationHistoryProvider(
        sp.GetRequiredService<ILogger<ConversationHistoryProvider>>(),
        options));
```

**手动管理会话**:

```csharp
var provider = sp.GetRequiredService<ConversationHistoryProvider>();

// 查看历史
var history = provider.GetHistory("session-id");

// 清除会话
provider.ClearSession("session-id");

// 清理过期会话
provider.CleanupExpiredSessions();

// 获取活跃会话数
var count = provider.GetActiveSessionCount();
```

---

### 2. ContextCompressionProvider

**文件位置**: `src/Infrastructure/Context/ContextCompressionProvider.cs`

**功能特性**:
- ✅ 智能估算 Token 数量（支持中英文）
- ✅ 分层压缩策略（根据超过程度）
- ✅ 支持 LLM 智能压缩和简单规则压缩
- ✅ 降级策略保证可用性
- ✅ 压缩统计信息

**压缩级别**:

| Token 数量 | 压缩策略 |
|-----------|---------|
| 0-4000 | 不压缩 |
| 4000-6000 | 压缩最旧 50% |
| 6000-8000 | 压缩最旧 70% |
| >8000 | 压缩最旧 90% |

**使用示例**:

```csharp
// 方式 1: 简单模式（仅截断，不使用 LLM）
builder.Services.AddSingleton<IAIContextProvider>(
    sp => new ContextCompressionProvider(
        sp.GetRequiredService<ILogger<ContextCompressionProvider>>(),
        new ContextCompressionOptions
        {
            MaxTokens = 4000,
            Mode = CompressionMode.Simple
        }));

// 方式 2: 智能模式（使用 LLM 总结）
builder.Services.AddSingleton<ILLMCompressionService, LLMCompressionService>();
builder.Services.AddSingleton<IAIContextProvider>(
    sp => new ContextCompressionProvider(
        sp.GetRequiredService<ILogger<ContextCompressionProvider>>(),
        sp.GetRequiredService<ILLMCompressionService>(),
        new ContextCompressionOptions
        {
            MaxTokens = 4000,
            Mode = CompressionMode.Smart
        }));
```

**配置选项**:

```csharp
var options = new ContextCompressionOptions
{
    MaxTokens = 4000,                   // Token 阈值
    EnableCompression = true,           // 启用压缩
    Mode = CompressionMode.Smart,       // 压缩模式
    MinMessagesToKeep = 2               // 最少保留消息数
};
```

---

### 3. 组合使用多个 ContextProvider

可以同时注册多个 ContextProvider，MS AF 会按注册顺序依次调用：

```csharp
// 注册多个提供器
builder.Services.AddSingleton<IAIContextProvider, ConversationHistoryProvider>();
builder.Services.AddSingleton<IAIContextProvider, RagContextProvider>();
builder.Services.AddSingleton<IAIContextProvider, ContextCompressionProvider>();

// 执行顺序：
// 1. ConversationHistoryProvider.PrepareContextAsync() - 加载历史
// 2. RagContextProvider.PrepareContextAsync() - 注入 RAG 文档
// 3. ContextCompressionProvider.PrepareContextAsync() - 压缩上下文
// 4. 执行 LLM 调用
// 5. ContextCompressionProvider.ProcessContextAsync() - 记录压缩统计
// 6. RagContextProvider.ProcessContextAsync() - 记录 RAG 日志
// 7. ConversationHistoryProvider.ProcessContextAsync() - 保存对话
```

---

### 内置 vs 自定义

**使用内置 ContextProvider**：适用于大多数场景
- ✅ 开箱即用，无需开发
- ✅ 经过测试，稳定可靠
- ✅ 可配置选项丰富

**自定义 ContextProvider**：适用于特殊需求
- 自定义数据源（如从 Redis 加载历史）
- 特殊的业务逻辑（如注入用户偏好）
- 集成第三方服务（如调用外部 API）

## 参考资料

- [Microsoft Agent Framework 官方文档](https://learn.microsoft.com/zh-cn/agent-framework/)
- [AIContextProvider API 参考](https://learn.microsoft.com/zh-cn/agent-framework/agents/conversations/context-providers)
- [CKY.MAF 架构文档](../specs/12-layered-architecture.md)
- [对话管理设计](../specs/06-interface-design-spec.md)
- [ConversationHistoryProvider 源码](../../src/Infrastructure/Context/ConversationHistoryProvider.cs)
- [ContextCompressionProvider 源码](../../src/Infrastructure/Context/ContextCompressionProvider.cs)

## 总结

`AIContextProvider` 是 MS AF 提供的强大上下文管理机制，适用于：
- RAG 应用
- 多轮对话系统
- 个性化服务
- 动态工具调用

CKY.MAF 当前采用手动管理方式，在 `MafAgentBase` 层面控制上下文，提供了更大的灵活性。如果未来需要更复杂的上下文管理，可以考虑引入适配器模式或管道模式来实现类似功能。

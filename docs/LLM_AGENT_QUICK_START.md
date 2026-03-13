# LLM Agent 实现快速指南

## 架构概览

```
LlmAgent : AIAgent (抽象基类)
├── ExecuteAsync() - 核心抽象方法
├── ExecuteBatchAsync() - 批量调用
├── SupportsScenario() - 场景支持检查
└── 使用 LlmResiliencePipeline (重试+超时)

具体实现:
├── ZhipuAIAgent : LlmAgent (智谱AI)
├── QwenAIAgent : LlmAgent (通义千问)
└── [未来] DeepSeekAIAgent, ErnieAIAgent 等
```

## 快速开始

### 1. 创建 LlmAgent 实例

```csharp
// 配置
var config = new LlmProviderConfig
{
    ProviderName = "zhipuai",
    ProviderDisplayName = "智谱AI",
    ModelId = "glm-4",
    ApiKey = "your-api-key",
    ApiBaseUrl = "https://open.bigmodel.cn/api/paas/v4",
    SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat },
    Temperature = 0.7,
    MaxTokens = 2000
};

// 验证配置（自动）
config.Validate();

// 创建 Agent（通过依赖注入）
services.AddHttpClient<ZhipuAIAgent>(client =>
{
    client.BaseAddress = new Uri("https://open.bigmodel.cn/api/paas/v4/chat/completions");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
});
services.AddSingleton<LlmResiliencePipeline>();

// 使用
var agent = new ZhipuAIAgent(config, logger, httpClient, resiliencePipeline);
```

### 2. 基本调用

```csharp
var response = await agent.ExecuteAsync(
    modelId: "glm-4",
    prompt: "你好",
    scenario: LlmScenario.Chat,
    systemPrompt: "你是一个友好的助手",
    ct: CancellationToken.None
);

Console.WriteLine(response);
```

## 弹性保护（自动）

所有 LLM 调用都通过 `LlmResiliencePipeline` 自动保护：
- ✅ **重试机制**: 最多 3 次，指数退避
- ✅ **超时保护**: 默认 30 秒超时
- ✅ **异常转换**: 自动区分速率限制、认证失败、网络错误

## 已修复的关键问题

- ✅ C-1: HttpClient 资源泄漏（使用 DI）
- ✅ C-2: 弹性模式（重试+超时）
- ✅ C-3: 自定义异常（区分错误类型）
- ✅ I-3: 配置验证（启动时检查）
- ✅ I-4: API Key 脱敏（日志安全）

## 生产就绪清单

### 必须项（已完成 ✅）
- [x] HttpClient 通过 DI 注入
- [x] 弹性重试机制
- [x] 超时保护
- [x] 自定义异常处理
- [x] 配置验证
- [x] API Key 脱敏

### 推荐项（待完成）
- [ ] ILlmAgentRegistry 实现
- [ ] L2/L3 缓存集成
- [ ] 单元测试覆盖
- [ ] 熔断器模式完整实现
- [ ] 流式响应支持

## 性能提示

- HttpClient 必须使用单例模式（通过 DI）
- 批量调用使用 `ExecuteBatchAsync()` 并行执行
- L2 缓存命中目标：>85%
- P95 响应时间目标：<3s（简单任务）

## 安全提示

- ⚠️ API Key 永远不要硬编码
- ⚠️ 日志中自动脱敏（使用 `GetApiKeyForLogging()`）
- ⚠️ 使用 HTTPS 端点
- ⚠️ 实施速率限制（每提供商独立）

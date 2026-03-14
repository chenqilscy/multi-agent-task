# LLM Agent Factory 使用指南

本文档介绍如何使用 `MafAiAgentFactory` 根据场景和优先级动态创建 LLM Agent 实例。

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                     MafAiAgentFactory                         │
│                   (工厂模式核心)                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │      ILlmProviderConfigRepository        │
        │         (数据库配置仓储)                  │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │          MafDbContext                    │
        │   (EF Core - LlmProviderConfigs 表)      │
        └─────────────────────────────────────────┘
```

## 核心组件

### 1. 数据模型

**LlmProviderConfig** (领域模型)
- `ProviderName`: 提供商唯一标识（如 zhipuai, tongyi）
- `SupportedScenarios`: 支持的场景列表
- `Priority`: 优先级（数字越小越高）
- `IsEnabled`: 是否启用
- 其他配置参数...

**LlmProviderConfigEntity** (数据库实体)
- 用于 EF Core 持久化
- 支持与领域模型的双向转换

### 2. 核心接口

**IMafAiAgentFactory** - 工厂接口
```csharp
// 根据配置创建 Agent
Task<MafAiAgent> CreateAgentAsync(LlmProviderConfig config, LlmScenario scenario);

// 根据提供商名称创建 Agent
Task<MafAiAgent> CreateAgentByProviderAsync(string providerName, LlmScenario scenario);

// 创建最佳 Agent（自动选择优先级最高的）
Task<MafAiAgent> CreateBestAgentForScenarioAsync(LlmScenario scenario);

// 批量创建所有支持该场景的 Agent
Task<List<MafAiAgent>> CreateAllAgentsForScenarioAsync(LlmScenario scenario);

// 创建带 Fallback 能力的 Agent
Task<MafAiAgent> CreateAgentWithFallbackAsync(LlmScenario scenario);

// 检查提供商是否支持场景
Task<bool> IsScenarioSupportedAsync(string providerName, LlmScenario scenario);
```

## 使用示例

### 示例 1: 基本用法 - 根据场景创建最佳 Agent

```csharp
// 在 Startup 或 Program.cs 中注册服务
services.AddScoped<ILlmProviderConfigRepository, LlmProviderConfigRepository>();
services.AddScoped<IMafAiAgentFactory, MafAiAgentFactory>();
services.AddDbContext<MafDbContext>(options =>
    options.UseNpgsql("YourConnectionString"));

// 在业务代码中使用
public class ChatService
{
    private readonly IMafAiAgentFactory _agentFactory;

    public ChatService(IMafAiAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    public async Task<string> ChatAsync(string message)
    {
        // 自动选择最佳 Agent（优先级最高且支持 Chat 场景）
        var agent = await _agentFactory.CreateBestAgentForScenarioAsync(
            LlmScenario.Chat);

        return await agent.ExecuteAsync(
            agent.GetCurrentModelId(),
            message,
            LlmScenario.Chat,
            "你是一个有用的AI助手。");
    }
}
```

### 示例 2: 使用 Fallback 模式

```csharp
public class RobustChatService
{
    private readonly IMafAiAgentFactory _agentFactory;

    public RobustChatService(IMafAiAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    public async Task<string> ChatWithFallbackAsync(string message)
    {
        // 创建带 Fallback 的 Agent
        // 如果主 Agent 失败，自动尝试下一个优先级的 Agent
        var agent = await _agentFactory.CreateAgentWithFallbackAsync(
            LlmScenario.Chat);

        return await agent.ExecuteAsync(
            agent.GetCurrentModelId(),
            message,
            LlmScenario.Chat);
    }
}
```

### 示例 3: 指定提供商

```csharp
public async Task<string> ChatWithZhipuAIAsync(string message)
{
    // 指定使用智谱AI
    var agent = await _agentFactory.CreateAgentByProviderAsync(
        "zhipuai",
        LlmScenario.Chat);

    return await agent.ExecuteAsync(
        "glm-4-plus",
        message,
        LlmScenario.Chat);
}
```

### 示例 4: 保存新配置

```csharp
public class ConfigManagementService
{
    private readonly ILlmProviderConfigRepository _configRepository;

    public ConfigManagementService(ILlmProviderConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    public async Task AddZhipuAIConfigAsync()
    {
        var config = new LlmProviderConfig
        {
            ProviderName = "zhipuai",
            ProviderDisplayName = "智谱AI",
            ApiBaseUrl = "https://open.bigmodel.cn/api/paas/v4/",
            ApiKey = "your-api-key",
            ModelId = "glm-4-plus",
            ModelDisplayName = "GLM-4 Plus",
            SupportedScenarios = new List<LlmScenario>
            {
                LlmScenario.Chat,
                LlmScenario.Embed,
                LlmScenario.Intent
            },
            MaxTokens = 8000,
            Temperature = 0.7,
            IsEnabled = true,
            Priority = 1, // 高优先级
            CostPer1kTokens = 0.05m
        };

        await _configRepository.SaveAsync(config);
    }
}
```

## 数据库表结构

**LlmProviderConfigs 表**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| ProviderName | string(50) | 提供商唯一标识（唯一索引） |
| ProviderDisplayName | string(100) | 显示名称 |
| ApiBaseUrl | string(500) | API 基础 URL |
| ApiKey | string(500) | API 密钥（加密存储） |
| ModelId | string(100) | 模型 ID |
| ModelDisplayName | string(100) | 模型显示名称 |
| SupportedScenariosJson | string(200) | 支持的场景（JSON数组） |
| MaxTokens | int | 最大 token 数 |
| Temperature | double | 温度参数 |
| IsEnabled | bool | 是否启用 |
| Priority | int | 优先级 |
| CostPer1kTokens | decimal | 成本 |
| AdditionalParametersJson | string(2000) | 附加参数（JSON） |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime? | 更新时间 |
| LastUsedAt | DateTime? | 最后使用时间 |
| Notes | string(500) | 备注 |

## Fallback 机制

`FallbackMafAiAgent` 实现了自动故障转移：

1. **按优先级尝试**: 从主 Agent 开始，依次尝试备用 Agent
2. **场景支持**: 只尝试支持当前场景的 Agent
3. **历史记录**: 记录每次 Fallback 的详细历史
4. **统计信息**: 提供 Fallback 率、成功率等统计数据

```csharp
// 获取 Fallback 统计
if (agent is FallbackMafAiAgent fallbackAgent)
{
    var stats = fallbackAgent.GetStatistics();
    Console.WriteLine($"Fallback Rate: {stats.FallbackRate:P2}");
    Console.WriteLine($"Success Rate: {stats.SuccessRate:P2}");

    foreach (var (agentId, count) in stats.AgentUsageCounts)
    {
        Console.WriteLine($"  {agentId}: {count} times");
    }
}
```

## 支持的场景

| 场景 | 枚举值 | 说明 |
|------|--------|------|
| Chat | 1 | 聊天对话 |
| Embed | 2 | 文本嵌入（向量化） |
| Intent | 3 | 意图识别 |
| Image | 4 | 图像生成 |
| Video | 5 | 视频生成 |
| Code | 6 | 代码生成 |
| Summarization | 7 | 摘要提取 |
| Translation | 8 | 翻译 |

## 扩展新提供商

要添加新的 LLM 提供商：

1. **实现 Agent 类**（继承 `MafAiAgent`）
```csharp
public class NewProviderMafAiAgent : MafAiAgent
{
    public NewProviderMafAiAgent(LlmProviderConfig config, ILogger logger)
        : base(config, logger)
    {
    }

    public override async Task<string> ExecuteAsync(
        string modelId,
        string prompt,
        LlmScenario scenario,
        string? systemPrompt,
        CancellationToken ct)
    {
        // 实现具体的 API 调用逻辑
    }
}
```

2. **在工厂中注册**
```csharp
// 在 MafAiAgentFactory.CreateAgentAsync 中添加 case
"newprovider" => await CreateNewProviderAgentAsync(config, ct),

// 添加创建方法
private async Task<MafAiAgent> CreateNewProviderAgentAsync(
    LlmProviderConfig config,
    CancellationToken ct)
{
    await Task.CompletedTask;
    var logger = _loggerFactory.CreateLogger<NewProviderMafAiAgent>();
    return new NewProviderMafAiAgent(config, logger);
}
```

## 最佳实践

1. **使用 Fallback**: 生产环境建议使用 `CreateAgentWithFallbackAsync`
2. **监控统计**: 定期检查 Fallback 统计，了解 Agent 健康状况
3. **优先级配置**: 合理设置优先级，主要服务商设为低值（高优先级）
4. **场景匹配**: 只配置模型实际支持的场景
5. **成本优化**: 根据场景选择合适的模型（如简单任务用低成本模型）

## 依赖注入配置

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加 DbContext
builder.Services.AddDbContext<MafDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册仓储和工厂
builder.Services.AddScoped<ILlmProviderConfigRepository, LlmProviderConfigRepository>();
builder.Services.AddScoped<IMafAiAgentFactory, MafAiAgentFactory>();

// 添加 LoggerFactory（可选，用于日志）
builder.Services.AddLogging();
```

## 故障排查

### 问题 1: No agent available for scenario
**原因**: 没有启用支持该场景的提供商
**解决**:
```sql
-- 检查数据库中的配置
SELECT * FROM "LlmProviderConfigs" WHERE "IsEnabled" = true;

-- 检查 SupportedScenariosJson 是否包含场景 ID
```

### 问题 2: Provider not found in database
**原因**: 提供商配置不存在
**解决**: 先保存配置到数据库
```csharp
await _configRepository.SaveAsync(config);
```

### 问题 3: All agents failed
**原因**: 所有 Agent 都返回错误
**解决**:
1. 检查 API 密钥是否正确
2. 检查网络连接
3. 查看 Fallback 历史获取详细错误信息

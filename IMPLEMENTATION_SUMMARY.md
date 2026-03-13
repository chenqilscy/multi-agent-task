# CKY.MAF 架构重构总结 - 基于 RC4 版本

## 实施日期
2026-03-13

## 最终架构方案

### 核心组件层次

```
LlmAgent (抽象基类)
├─ ExecuteAsync() - 核心抽象方法
├─ ExecuteBatchAsync() - 批量调用
└─ SupportsScenario() - 场景支持检查

具体实现（继承 LlmAgent）:
├─ ZhipuAIAgent : LlmAgent
├─ QwenAIAgent : LlmAgent
├─ DeepSeekAIAgent : LlmAgent
└─ 其他厂商实现

Filters（基于 RC4 模式）:
├─ MafChatHistoryFilter - L2 + L3 聊天历史管理
├─ MafMonitoringFilter - Prometheus 监控指标
└─ MafVectorSearchFilter - Qdrant 向量搜索（待实现）
```

### 技术栈版本

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-rc4" />
<PackageReference Include="Microsoft.Extensions.AI" Version="10.4.0" />
```

## ✅ 已完成的工作

### 1. 创建 LlmAgent 抽象基类
- **位置**: [src/Core/Agents/LlmAgent.cs](G:/work/agent/multi-agent-task/src/Core/Agents/LlmAgent.cs)
- **功能**:
  - 继承自 `Microsoft.Agents.AI.AIAgent`
  - 统一的 LLM 调用接口
  - 支持多种场景（chat/embed/intent/image/video）
  - 批量调用支持
- **抽象方法**: `ExecuteAsync(modelId, prompt, scenario, systemPrompt, ct)`
- **已实现的 AIAgent 抽象方法**:
  - `CreateSessionCoreAsync()` - 创建会话
  - `SerializeSessionCoreAsync()` - 序列化会话
  - `DeserializeSessionCoreAsync()` - 反序列化会话
  - `RunCoreAsync()` - 非流式运行（桥接到 ExecuteAsync）
  - `RunCoreStreamingAsync()` - 流式运行（暂不支持）

### 2. 创建具体 LLM Agent 实现

#### ZhipuAIAgent（智谱AI）
- **位置**: [src/Core/Agents/ZhipuAIAgent.cs](G:/work/agent/multi-agent-task/src/Core/Agents/ZhipuAIAgent.cs)
- **支持的模型**:
  - `glm-4-plus` - 最强版本
  - `glm-4` - 标准版（推荐）
  - `glm-4-air` - 轻量版
  - `glm-4-flash` - 极速版
  - `glm-3-turbo` - 旧版
- **API Endpoint**: `https://open.bigmodel.cn/api/paas/v4/chat/completions`
- **实现特性**:
  - 完整的 ExecuteAsync 实现
  - 请求体构建（支持不同场景）
  - 响应内容提取
  - 错误处理和日志记录

#### QwenAIAgent（通义千问）
- **位置**: [src/Core/Agents/QwenAIAgent.cs](G:/work/agent/multi-agent-task/src/Core/Agents/QwenAIAgent.cs)
- **支持的模型**:
  - `qwen-max` - 最强版本
  - `qwen-plus` - 标准版（推荐）
  - `qwen-turbo` - 高速版
  - `qwen-long` - 长文本
  - `qwen-vl-max` - 视觉理解（最强）
  - `qwen-vl-plus` - 视觉理解（标准版）
- **API Endpoint**: `https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation`
- **实现特性**:
  - 完整的 ExecuteAsync 实现
  - 支持意图识别场景
  - 支持图像/视频场景
  - 通义千问格式的响应解析

### 3. 创建 Filter 实现（基于 RC4）

#### MafChatHistoryFilter
- **位置**: [src/Core/Filters/MafChatHistoryFilter.cs](G:/work/agent/multi-agent-task/src/Core/Filters/MafChatHistoryFilter.cs)
- **功能**: L2 Redis + L3 PostgreSQL 聊天历史管理
- **方法**: `ProcessAsync(sessionId, next, ct)`

#### MafMonitoringFilter
- **位置**: [src/Core/Filters/MafMonitoringFilter.cs](G:/work/agent/multi-agent-task/src/Core/Filters/MafMonitoringFilter.cs)
- **功能**: Prometheus 监控指标收集
- **指标**:
  - `maf_agent_invocations_total` - 调用计数
  - `maf_agent_duration_seconds` - 执行时长
  - `maf_llm_api_latency_seconds` - LLM API 延迟
- **方法**: `ProcessAsync(sessionId, agentName, next, ct)`

### 3. 更新依赖和接口

#### ILlmAgentRegistry
- **更新**: 所有 `MafAgentBase` 引用改为 `LlmAgent`
- **位置**: [src/Core/Abstractions/ILlmAgentRegistry.cs](G:/work/agent/multi-agent-task/src/Core/Abstractions/ILlmAgentRegistry.cs)

#### CKY.MAF.Core.csproj
- **Microsoft.Agents.AI**: `1.0.0-rc4`
- **Microsoft.Extensions.AI**: `10.4.0`

### 4. 项目编译状态

```
已成功生成。
0 个警告
0 个错误
```

**注意**: 在实现过程中遇到了以下技术挑战并已解决：
1. ✅ `AgentSession` 是抽象类型 - 创建了 `SimpleAgentSession` 内部类
2. ✅ `RunCoreAsync` 返回类型不匹配 - 改为返回 `AgentResponse`
3. ✅ `RunCoreStreamingAsync` 返回类型不匹配 - 改为返回 `IAsyncEnumerable<AgentResponseUpdate>`

## 📋 后续开发任务

### 高优先级
1. **实现更多 LLM Agent**
   - 创建 `DeepSeekAIAgent : LlmAgent` - DeepSeek
   - 创建 `ErnieAIAgent : LlmAgent` - 百度文心一言
   - 创建 `SparkAIAgent : LlmAgent` - 讯飞星火
   - 实现 Embedding API 支持（用于向量搜索）

2. **完善 Filter 功能**
   - 完成 `MafChatHistoryFilter` 的 L2/L3 存储逻辑
   - 创建 `MafVectorSearchFilter`（Qdrant 集成）
   - 实现 Embedding API 集成

### 中优先级
3. **实现 ILlmAgentRegistry**
   - Agent 注册和发现
   - 优先级计算
   - 故障转移

4. **创建 Services 层**
   - TaskScheduler（任务调度）
   - TaskOrchestrator（任务编排）
   - IntentRecognizer（意图识别）

## 📂 项目结构

```
src/Core/
├── Agents/
│   ├── LlmAgent.cs (抽象基类) ✅
│   ├── ZhipuAIAgent.cs (智谱AI实现) ✅
│   ├── QwenAIAgent.cs (通义千问实现) ✅
│   └── [未来添加] DeepSeekAIAgent.cs, ErnieAIAgent.cs 等
├── Filters/
│   ├── MafChatHistoryFilter.cs ✅
│   ├── MafMonitoringFilter.cs ✅
│   └── [待实现] MafVectorSearchFilter.cs
├── Abstractions/
│   ├── ILlmAgentRegistry.cs ✅
│   ├── ICacheStore.cs
│   ├── IRelationalDatabase.cs
│   └── IVectorStore.cs
└── Models/
    ├── LLM/
    │   ├── LlmScenario.cs
    │   └── LlmProviderConfig.cs
    └── ...
```

## 🎯 使用示例

### 创建 LlmAgent 实例

```csharp
// 1. 创建配置
var config = new LlmProviderConfig
{
    ProviderName = "zhipuai",
    ProviderDisplayName = "智谱AI",
    ModelId = "glm-4",
    ApiKey = "your-api-key",
    ApiBaseUrl = "https://open.bigmodel.cn/api/paas/v4",
    SupportedScenarios = new List<LlmScenario> { LlmScenario.Chat, LlmScenario.Intent }
};

// 2. 创建 Agent 实例
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ZhipuAIAgent>();
var agent = new ZhipuAIAgent(config, logger);

// 3. 调用 LLM
var response = await agent.ExecuteAsync(
    modelId: "glm-4",
    prompt: "你好，请介绍一下自己",
    scenario: LlmScenario.Chat,
    systemPrompt: "你是一个友好的AI助手"
);

Console.WriteLine(response); // 输出: LLM 的响应
```

### 使用 ILlmAgentRegistry 管理多个 Agent

```csharp
var chatFilter = new MafChatHistoryFilter(l2Cache, l3Db, logger);
var monitoringFilter = new MafMonitoringFilter(logger);

// 执行 Agent 调用
await chatFilter.ProcessAsync(sessionId, async () =>
{
    await monitoringFilter.ProcessAsync(sessionId, agentName, async () =>
    {
        // 核心 LLM 调用
        var response = await llmAgent.ExecuteAsync(modelId, prompt);
        return response;
    }, ct);
}, ct);
```

## 关键设计决策

### 为什么不继承 AIAgent？
1. **API 不稳定性**: RC4 版本的 `AIContextProvider` 方法不是 virtual
2. **架构不匹配**: 文档示例与实际 API 不一致
3. **灵活性**: 独立设计避免受 MS AF API 变化影响

### 为什么使用 Filter 模式？
1. **符合 RC4 架构**: 适配 `IAgentRunFilter` 接口
2. **职责分离**: 每个 Filter 专注单一功能
3. **易于测试**: 可独立测试每个 Filter

## 总结

✅ **核心架构已建立**:
- `LlmAgent` 抽象基类
- `ILlmAgentRegistry` 接口
- Filter 模式框架
- 项目编译通过

🚀 **准备就绪**:
- 可以开始实现具体的 LLM Agent（智谱AI、通义千问等）
- 可以完善 Filter 的具体功能
- 基于 RC4 版本稳定开发

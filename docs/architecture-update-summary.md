# 架构文档更新说明

> **更新日期**: 2026-03-13
> **更新内容**: 反映基于 Microsoft Agent Framework 的 LLM 集成架构

---

## 更新概述

本次更新修正了架构文档中关于 LLM 集成的描述，确保与实际实现一致：**LLM 服务必须继承 MS AF 的 `AIAgent`，不使用 SemanticKernel**。

## 更新的文档

### 1. [06-interface-design-spec.md](./06-interface-design-spec.md)

**更新内容**：
- 修改了"核心依赖"部分，明确 LLM 服务通过继承 `AIAgent` 并实现 `ILlmService` 接口
- 更新了"1.2 LLM集成接口"章节，说明正确的架构原则
- 添加了"2.2 LLM服务Agent示例"章节，展示 `ZhipuAILlmAgent` 的实现
- 更新了"2.3 业务Agent示例"章节，展示如何在业务 Agent 中使用 `ILlmService`

**关键变更**：
```diff
- ❌ ILLMService | ✅ IChatClient | LLM调用接口
+ ✅ ILlmService (必须继承AIAgent) | LLM服务抽象接口
```

### 2. [01-architecture-overview.md](./01-architecture-overview.md)

**更新内容**：
- 修改了"关键关系"部分，说明 LLM 服务不使用 SemanticKernel
- 更新了技术栈部分，明确 LLM 集成方式

**关键变更**：
```diff
- LLM: Microsoft.Extensions.AI (MS AF集成)
+ LLM: 自定义ILlmService接口，实现类继承AIAgent
+      智谱AI (GLM-4/GLM-4-Plus) - 主力模型
+      通义千问、文心一言、讯飞星火 - 备选模型
+      **重要**: 不使用SemanticKernel，基于MS AF原生能力
```

## 正确的架构理解

### LLM 服务架构层次

```
┌─────────────────────────────────────────────────────┐
│       业务 Agent (LightingAgent, ClimateAgent)      │
│       依赖: ILlmService 接口                           │
└────────────────────┬────────────────────────────────┘
                     │ 使用
                     ▼
┌─────────────────────────────────────────────────────┐
│           ILlmService 接口 (抽象层)                  │
│       - CompleteAsync(string prompt)                  │
│       - CompleteAsync(system, user)                   │
│       - GetUnderlyingAgent()                          │
└────────────────────┬────────────────────────────────┘
                     │ 实现
                     ▼
┌─────────────────────────────────────────────────────┐
│     ZhipuAILlmAgent : MafAgentBase : AIAgent          │
│       - ExecuteBusinessLogicAsync: 调用 LLM API      │
│       - 利用 MS AF 的会话管理、状态追踪               │
└────────────────────┬────────────────────────────────┘
                     │ 继承
                     ▼
┌─────────────────────────────────────────────────────┐
│        Microsoft Agent Framework (AIAgent)          │
│       - 会话管理                                      │
│       - 状态追踪                                      │
│       - A2A 通信                                      │
└─────────────────────────────────────────────────────┘
```

### 关键设计原则

1. **依赖倒置**: 业务 Agent 依赖 `ILlmService` 抽象，不依赖具体实现
2. **基于 MS AF**: 所有 LLM 服务必须继承 `AIAgent`
3. **不绕过框架**: 不直接使用 HttpClient，而是通过 MS AF 的执行流程
4. **充分利用能力**: 使用 MS AF 的会话管理、状态追踪、监控等功能

### 实现示例

**ZhipuAILlmAgent** (正确的 LLM 服务实现):
```csharp
public class ZhipuAILlmAgent : MafAgentBase, ILlmService
{
    // ✅ 继承 MafAgentBase (进而继承 AIAgent)
    // ✅ 实现 ILlmService 接口

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(
        MafTaskRequest request,
        IAgentSession session,  // ✅ MS AF 提供的会话对象
        CancellationToken ct)
    {
        // 调用智谱AI API
        var response = await CallZhipuAIApi(...);

        // ✅ 利用 MS AF 的会话管理
        session.History.Add(new Message { Role = "assistant", Content = response });

        // ✅ 自动统计（MS AF 能力）
        Statistics.TotalExecutions++;

        return new MafTaskResponse { Success = true, Result = response };
    }
}
```

**LightingAgent** (使用 LLM 服务的业务 Agent):
```csharp
public class LightingAgent : MafAgentBase
{
    private readonly ILlmService _llmService;

    public LightingAgent(
        ILlmService llmService,  // ✅ 注入抽象接口
        // ... 其他依赖
    )
    {
        _llmService = llmService;
    }

    protected override async Task<MafTaskResponse> ExecuteBusinessLogicAsync(...)
    {
        // ✅ 使用 ILlmService 抽象
        var response = await _llmService.CompleteAsync(systemPrompt, userPrompt);

        // 处理响应...
    }
}
```

## 与错误做法的对比

| 特性 | ❌ 错误做法 | ✅ 正确做法 |
|------|-----------|-----------|
| 基类 | 不继承 AIAgent | 继承 MafAgentBase |
| LLM 调用 | 直接使用 HttpClient | 在 ExecuteBusinessLogicAsync 中调用 |
| 会话管理 | 需要自己实现 | MS AF 自动提供 |
| 状态追踪 | 需要自己实现 | MS AF 自动提供 |
| 框架一致性 | 与其他 Agent 不一致 | 完全融入 MS AF 生态 |
| 依赖性 | 绑定具体实现 | 依赖抽象接口 |

## 相关文档

- **接口设计**: [06-interface-design-spec.md](./06-interface-design-spec.md)
- **架构概览**: [01-architecture-overview.md](./01-architecture-overview.md)
- **集成指南**: [../how-to-integrate-llm-with-agent-framework.md](../how-to-integrate-llm-with-agent-framework.md)
- **配置加载**: [../how-to-load-llm-config.md](../how-to-load-llm-config.md)

## 实现文件

- **ILlmService 接口**: [src/Core/Abstractions/ILlmService.cs](../src/Core/Abstractions/ILlmService.cs)
- **智谱AI 实现**: [src/Repository/LLM/ZhipuAILlmAgent.cs](../src/Repository/LLM/ZhipuAILlmAgent.cs)
- **单元测试**: [src/tests/UnitTests/LLM/ZhipuAILlmAgentTests.cs](../src/tests/UnitTests/LLM/ZhipuAILlmAgentTests.cs)

## 总结

本次更新确保了架构文档与实际实现完全一致，强调了以下核心原则：

1. **基于 MS AF**: LLM 服务必须继承 `AIAgent`
2. **不使用 SemanticKernel**: 绕过此框架，直接使用 MS AF 的能力
3. **抽象优先**: 通过 `ILlmService` 接口解耦
4. **充分利用**: 使用 MS AF 的会话管理、状态追踪、监控等能力

这种架构确保 LLM 服务完全融入 Microsoft Agent Framework 生态，而不是独立的外部组件。

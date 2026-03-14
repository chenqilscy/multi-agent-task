# ExecuteAsync 访问级别修复总结

## 问题描述

用户反馈：ExecuteAsync 和 ExecuteStreamingAsync 为什么是 public 而不是 protected？

## 问题分析

### 设计原则
根据架构文档，ExecuteAsync 应该仅仅是厂商 API 调用封装，不应该作为主要接口对外暴露。

### 调用链路
正确的调用链应该是：
```
外部调用
  ↓
MS AF 公开 API (RunAsync/RunStreamingAsync)
  ↓
MS AF 内部调用 (RunCoreAsync/RunCoreStreamingAsync)
  ↓
ExecuteAsync/ExecuteStreamingAsync - 仅仅是厂商 API 调用
  ↓
厂商 API (智谱/通义/文心)
```

### 访问级别冲突
如果改为 `protected`，会导致 MafBusinessAgentBase 无法调用（因为不是继承关系）。

## 解决方案

将 ExecuteAsync 和 ExecuteStreamingAsync 改为 `internal`：
- ✅ 不是 public（外部程序集无法访问）
- ✅ 不是 protected（同一程序集内的类可以访问）
- ✅ 保持了封装性
- ✅ 不破坏现有调用关系

## 修改文件

### 1. src/Core/Agents/MafAiAgent.cs
```csharp
// 修改前
public abstract Task<string> ExecuteAsync(...)
public abstract IAsyncEnumerable<string> ExecuteStreamingAsync(...)
public virtual async Task<string[]> ExecuteBatchAsync(...)

// 修改后
internal abstract Task<string> ExecuteAsync(...)
internal abstract IAsyncEnumerable<string> ExecuteStreamingAsync(...)
internal virtual async Task<string[]> ExecuteBatchAsync(...)
```

### 2. src/Core/Agents/ZhipuAIAgent.cs
```csharp
// 修改访问级别
internal override async Task<string> ExecuteAsync(...)

// 添加流式方法实现
internal override async IAsyncEnumerable<string> ExecuteStreamingAsync(...)
{
    // TODO: 实现真正的流式支持
    var result = await ExecuteAsync(modelId, prompt, systemPrompt, ct);
    yield return result;
}
```

### 3. src/Core/Agents/QwenAIAgent.cs
```csharp
// 修改访问级别
internal override async Task<string> ExecuteAsync(...)

// 添加流式方法实现
internal override async IAsyncEnumerable<string> ExecuteStreamingAsync(...)
{
    // TODO: 实现真正的流式支持
    var result = await ExecuteAsync(modelId, prompt, systemPrompt, ct);
    yield return result;
}
```

### 4. src/Core/Agents/FallbackLlmAgent.cs
```csharp
// 修改访问级别
internal override async Task<string> ExecuteAsync(...)

// 添加流式方法实现
internal override async IAsyncEnumerable<string> ExecuteStreamingAsync(...)
{
    // TODO: 实现流式支持
    var result = await ExecuteAsync(modelId, prompt, systemPrompt, ct);
    yield return result;
}
```

### 5. src/Core/Models/Session/MafAgentSession.cs
```csharp
// 修复命名空间冲突
using Task = System.Threading.Tasks.Task;

// 修复会话 ID 生成
public MafAiAgentSession MafSession
{
    get
    {
        if (_mafSession == null)
        {
            _mafSession = new MafAiAgentSession
            {
                SessionId = Guid.NewGuid().ToString(), // 使用 GUID 而不是 Id
                ...
            };
        }
        return _mafSession;
    }
}

// 修复 Task 返回类型
public async System.Threading.Tasks.Task<bool> LoadAsync(...)
public async System.Threading.Tasks.Task SaveAsync(...)
```

### 6. src/Core/Agents/MafAiAgent.cs - RunCoreAsync/RunCoreStreamingAsync
```csharp
// 修复会话 ID 访问（移除对 session.Id 的依赖）
MafAgentSession? mafAgentSession = null;
if (session is MafAgentSession mafSession)
{
    mafAgentSession = mafSession;

    // 检查是否已有会话数据
    if (!string.IsNullOrEmpty(mafAgentSession.MafSession.SessionId))
    {
        // 会话已存在，更新活动
        mafAgentSession.UpdateActivity();
        mafAgentSession.IncrementTurn();
    }
    else
    {
        // 新会话，生成会话 ID
        mafAgentSession.MafSession.SessionId = Guid.NewGuid().ToString();
    }
}
```

## 构建结果

```
已成功生成。
1 个警告
0 个错误
```

## 影响范围

### ✅ 不受影响
- MS AF 公开 API (RunAsync/RunStreamingAsync) - 仍然可以正常使用
- MafBusinessAgentBase.CallLlmAsync() - 内部调用不受影响（同一程序集）
- 具体厂商实现（ZhipuAiAgent, QwenAIAgent 等）- 已更新为 internal

### 🔒 已保护
- ExecuteAsync/ExecuteStreamingAsync - 外部程序集无法直接调用
- 强制使用 MS AF 的标准 API 或 MafBusinessAgentBase 的封装方法

## 后续工作

### 待实现
1. **真正的流式支持**
   - 当前 ExecuteStreamingAsync 只是简单调用 ExecuteAsync
   - 需要根据厂商 API 实现真正的流式响应

2. **流式 Token 统计**
   - RunCoreStreamingAsync 中的 TODO: 更新会话状态（流式场景下的 Token 统计）

3. **流式更新内容**
   - RunCoreStreamingAsync 中的 TODO: 正确设置流式更新内容
   - 需要根据 MS AF 的 AgentResponseUpdate 结构来设置

## 总结

通过将 ExecuteAsync 和 ExecuteStreamingAsync 改为 `internal`，我们：
- ✅ 保持了架构的一致性和封装性
- ✅ 强制外部使用 MS AF 的标准 API
- ✅ 不破坏同一程序集内的正常调用关系
- ✅ 为未来的流式支持预留了接口

这次修改符合用户的架构设计原则："深度使用 AiAgent，基于它的能力进行扩展，不是重写它的能力"。

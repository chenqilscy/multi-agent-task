# 客服系统 Chat API 使用指南

> **版本**: v1.0  
> **更新日期**: 2026-03-23  
> **适用范围**: CustomerService Demo — REST API + 服务层

---

## 概述

CustomerService Demo 提供 REST API 端点，允许外部系统通过 HTTP 接口与客服 Agent 进行交互。架构采用三层解耦设计：

```
HTTP 请求 → ChatApiEndpoints (Minimal API) → IChatService → LeaderAgent → 业务 Agent
```

### 核心组件

| 组件 | 路径 | 职责 |
|------|------|------|
| `IChatService` | Services/IChatService.cs | 服务层接口 |
| `ChatService` | Services/Implementations/ChatService.cs | 消息处理编排 |
| `ChatApiEndpoints` | Api/ChatApiEndpoints.cs | REST 端点定义 |
| `ConversationManager` | Models/CustomerServiceModels.cs | 多轮上下文管理 |

---

## API 端点

### POST /api/chat/send

发送用户消息并获取 Agent 回复。

**请求体**:
```json
{
  "userId": "user-001",
  "sessionId": "session-abc",
  "message": "查询订单 ORD-2024-001"
}
```

**成功响应** (200):
```json
{
  "content": "📦 订单 ORD-2024-001 的状态为...",
  "needsClarification": false,
  "clarificationOptions": []
}
```

**澄清响应** (200):
```json
{
  "content": "请问您是要查询哪方面的信息？",
  "needsClarification": true,
  "clarificationOptions": ["订单状态", "物流信息", "退款进度"]
}
```

**参数校验失败** (400):
```
"userId, sessionId, message 不能为空"
```

### POST /api/chat/handoff

请求转人工客服，自动创建工单。

**请求体**:
```json
{
  "userId": "user-001",
  "sessionId": "session-abc"
}
```

**成功响应** (200):
```json
{
  "content": "✅ 工单已创建成功...",
  "needsClarification": false,
  "clarificationOptions": []
}
```

---

## 消息处理流程

`ChatService.SendMessageAsync` 执行以下步骤：

1. **输入校验** — 验证 userId、sessionId、message 非空
2. **消息截断** — 超过 500 字符自动截断
3. **持久化用户消息** — 通过 `IChatHistoryService`（可选）
4. **多轮上下文追踪** — 通过 `ConversationManager` 记录对话轮次
5. **指代消解** — 从上下文推断缺失实体（如"它"→之前提到的订单号）
6. **Agent 调用** — 构造 `MafTaskRequest` 交给 `CustomerServiceLeaderAgent`
7. **结果封装** — 根据是否需要澄清返回不同格式
8. **持久化回复** — 保存助手回复到聊天历史

### Agent 路由

LeaderAgent 根据意图将请求路由到不同 Agent：

| 意图 | 路由 Agent | 示例输入 |
|------|-----------|---------|
| 订单查询 | OrderStatusAgent | "查询订单 ORD-123" |
| 投诉 | ComplaintAgent | "我要投诉物流太慢" |
| 知识库 | KnowledgeBaseAgent | "退货政策是什么" |
| 未识别 | LLM Fallback → 工单建议 | "今天天气怎么样" |

### LLM Fallback 链路

当 KnowledgeBaseAgent 无法回答时，LeaderAgent 尝试 LLM 直接对话作为兜底：

```
用户消息 → 意图识别失败 → KnowledgeBaseAgent → 答案不满意
  → TryLlmFallbackAsync (通过 IMafAiAgentRegistry)
  → 若 LLM 也不可用 → 建议提交工单
```

LLM Fallback 有如下约束：
- 受降级管理器控制（`DegradationManager.IsFeatureEnabled("llm")`）
- 回复限制在 200 字符以内
- 禁止编造数据，超出范围引导提交工单

---

## DI 注册

在 `Program.cs` 中自动注册：

```csharp
// 持久化模式 — ChatService 为 Scoped（跟随请求生命周期）
builder.Services.AddScoped<IChatService, ChatService>();

// 非持久化模式 — ChatService 为 Singleton
builder.Services.AddSingleton<IChatService, ChatService>();

// 映射 API 路由
app.MapChatApi();
```

---

## 多轮上下文

`ConversationManager` 提供以下能力：

- **线程安全** — 内部使用 `ConcurrentDictionary`
- **自动压缩** — 超过 20 轮对话自动压缩历史
- **实体追踪** — 记录 `ImportantEntities`（订单号、产品名等）
- **话题追踪** — 记录 `CurrentTopic`（当前讨论主题）
- **指代消解** — `InferMissingEntities()` 从历史上下文补充缺失实体

Chat.razor UI 中展示上下文面板，显示当前话题和已识别实体。

---

## 测试

| 测试文件 | 数量 | 覆盖范围 |
|---------|------|---------|
| ChatServiceTests.cs | 15 | 消息发送、持久化、截断、空值校验、路由、异常恢复、人工转接 |
| ChatApiEndpointTests.cs | 14 | 有效/无效请求参数组合、澄清选项 |

### 运行测试

```bash
dotnet test tests/UnitTests --filter "FullyQualifiedName~ChatService"
```

---

## 使用示例

### C# HttpClient

```csharp
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5013") };

var response = await client.PostAsJsonAsync("/api/chat/send", new
{
    userId = "demo-user",
    sessionId = Guid.NewGuid().ToString(),
    message = "你好，我想查询订单"
});

var result = await response.Content.ReadFromJsonAsync<ChatServiceResponse>();
Console.WriteLine(result.Content);
```

### PowerShell

```powershell
$body = @{userId="demo-user"; sessionId="sess-001"; message="退货政策"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5013/api/chat/send" `
  -Method Post -ContentType "application/json" `
  -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
```

### curl

```bash
curl -X POST http://localhost:5013/api/chat/send \
  -H "Content-Type: application/json" \
  -d '{"userId":"demo-user","sessionId":"sess-001","message":"查询订单"}'
```

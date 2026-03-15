# CS-ESCAL-001 自动升级到人工客服

---
metadata:
  case_id: CS-ESCAL-001
  journey: 问题升级
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [escalation, ticket-creation, context-handover]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 30
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ESCAL-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

AI无法解决的复杂问题，自动升级到人工客服并传递上下文。

## 📝 执行流程

**第1轮**:
- 用户："我要投诉你们的隐私政策，怎么把我的信息泄露了？"
- 系统（判断为敏感问题，需人工处理）：
```
这个问题非常重要，我需要为您转接专业的人工客服来处理。

📋 转接准备：
  - 已创建工单：TK-20260315-007
  - 问题类别：隐私安全
  - 优先级：🔴 高

📞 正在为您转接人工客服...
预计等待时间：约1分钟

💡 已将您的对话记录同步给人工客服，
无需重复描述问题。

请稍候... ⏳
```

## ✅ 预期结果

- [x] 敏感问题自动识别
- [x] 升级工单创建
- [x] 对话上下文传递给人工
- [x] 等待时间预估

## 🔬 详细对话交互示例（与代码对齐）

**用户输入**: `"我要投诉你们的隐私政策，怎么把我的信息泄露了？"`

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `CreateTicket`: "投诉" YES → score = 1/6 ≈ 0.167
- `PrimaryIntent = "CreateTicket"`, `Confidence ≈ 0.167`

**Step 2 — 实体提取** (`IntentDrivenEntityExtractor`):
- 无可提取实体（无订单号/商品名等）

**Step 3 — 路由** (`RouteToAgentAsync`):
```csharp
// intent.PrimaryIntent is "CreateTicket" → true
// → 路由到 TicketAgent
// 备选: ContainsAny(["投诉", ...]) → "投诉" 命中 → 也会路由到 TicketAgent
```

**Step 4 — TicketAgent 业务逻辑** (`ExecuteBusinessLogicAsync`):
```csharp
// 不含 "查询工单"/"我的工单"/"处理进度" → 进入创建工单分支
var title = ExtractTitle(userInput);
// → "我要投诉你们的隐私政策，怎么把我的信息泄露了？" (len=22 < 50, 不截断)

var category = DetectCategory(userInput);
// → 不含 "订单"/"快递"/"退款"/"产品" → category = "other"

var ticketId = await _ticketService.CreateTicketAsync(new TicketCreateRequest {
    UserId = "anonymous",
    Title = "我要投诉你们的隐私政策，怎么把我的信息泄露了？",
    Description = userInput,
    Category = "other",
    Priority = "normal",  // ⚠️ 隐私问题应为 "high"，当前代码未做优先级提升
}, ct);
// → ticketId = "TKT-20260315-001"
```

**Step 5 — TicketAgent 响应**:
```csharp
return new MafTaskResponse {
    Success = true,
    Result = "✅ 工单已创建成功\n• 工单编号：TKT-20260315-001\n• 类别：other\n• 我们会在24小时内处理您的工单，请耐心等待。",
    Data = new { TicketId = "TKT-20260315-001" },
};
```

**Step 6 — 行为记录** (`IUserBehaviorService.RecordAsync`):
```csharp
new UserBehaviorRecord {
    UserId = request.UserId,
    Intent = "CreateTicket",
    TaskSucceeded = true,
    ResponseTime = elapsed,
}
```

> ⚠️ **代码差异**:
> 1. 当前 `TicketAgent` 未实现敏感问题自动识别（隐私/安全类），统一创建普通工单
> 2. 工单优先级固定为 `"normal"`，隐私问题应提升为 `"high"` 或 `"urgent"`
> 3. 未实现上下文传递给人工客服的机制
> 4. `DetectCategory` 不识别 "隐私" 关键词，归类为 `"other"`

**系统输出** (理想行为):
```
这个问题非常重要，我需要为您转接专业的人工客服来处理。

📋 转接准备：
  - 已创建工单：TKT-20260315-001
  - 问题类别：隐私安全
  - 优先级：🔴 高

📞 正在为您转接人工客服...
预计等待时间：约1分钟

💡 已将您的对话记录同步给人工客服，
无需重复描述问题。

请稍候... ⏳
```

## 🧪 测试要点

- [ ] 升级触发条件检测
- [ ] 上下文序列化传递
- [ ] 等待时间预估准确

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

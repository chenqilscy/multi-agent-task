# CS-TICKET-001 提交新工单

---
metadata:
  case_id: CS-TICKET-001
  journey: 工单跟进
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [intent-recognition, ticket-creation]
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

- **用例ID**: CS-TICKET-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户主动提交问题工单，系统收集信息并创建工单。

## 📝 执行流程

**第1轮**:
- 用户："我想提交一个问题反馈"
- 系统：
```
好的，我来帮您创建一个工单。请描述您遇到的问题：

📋 请提供：
  1. 问题类别（商品/物流/账户/其他）
  2. 详细问题描述
  3. 相关订单号（如有）
```

**第2轮**:
- 用户："账户问题，我的优惠券消失了，之前有一张满200减50的券"
- 系统：
```
📝 工单已创建：

  工单编号：TK-20260315-005
  问题类别：账户问题
  问题描述：优惠券丢失（满200减50）
  优先级：🟡 中
  处理时效：48小时内回复

📌 处理流程：
  1. 技术团队核实优惠券记录
  2. 确认后恢复或补发
  3. 处理结果将通过消息通知您

工单号请妥善保存，您可随时查询进度 😊
```

## ✅ 预期结果

- [x] 信息收集完整
- [x] 工单自动创建
- [x] 处理时效承诺
- [x] 工单号返回

## 🧪 测试要点

- [ ] TicketAgent工单创建
- [ ] 优先级自动分配
- [ ] 工单持久化存储

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - 工单编号格式: 代码生成 `TKT-{yyyyMMdd}-{序号:D3}`（非文档的 `TK-`）
> - TicketAgent 没有"请先描述问题"的引导轮次，直接用 userInput 创建工单
> - 优先级固定为 `"normal"`，无自动分配逻辑

### 示例: 直接提交反馈

```
用户: "我想提交一个问题反馈"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `CreateTicket`: "反馈" YES → score = 1/6 ≈ 0.167
- `GeneralFaq`: "我想" YES → score = 1/5 = 0.2
- **PrimaryIntent = "GeneralFaq"**, Confidence = 0.2

**Step 2 — 路由** (`RouteToAgentAsync`):
- `"GeneralFaq"` 不在订单/工单 intent 列表
- `ContainsAny(["投诉", "工单", "反馈", "建议", "举报", "人工"])` → "反馈" YES
- **→ 路由到 TicketAgent** ✓

**Step 3 — TicketAgent 处理** (`ExecuteBusinessLogicAsync`):
- `userInput.Contains("查询工单")` NO, `Contains("我的工单")` NO, `Contains("处理进度")` NO
- → 进入创建工单分支
- `ExtractTitle("我想提交一个问题反馈")` → "我想提交一个问题反馈"（<50字，不截断）
- `DetectCategory("我想提交一个问题反馈")` → 无特定关键词 → `"other"`
- `userId` 从 `request.Parameters["userId"]` 或默认 `"anonymous"`

**Step 4 — 工单创建** (`SimulatedTicketService.CreateTicketAsync`):
```csharp
TicketCreateRequest {
    UserId = "anonymous",
    Title = "我想提交一个问题反馈",
    Description = "我想提交一个问题反馈",
    Category = "other",
    Priority = "normal",
}
// 生成 ticketId = "TKT-20260315-001"
```

**Step 5 — 实际响应**:
```
✅ 工单已创建成功
• 工单编号：TKT-20260315-001
• 类别：other
• 我们会在24小时内处理您的工单，请耐心等待。
```

### 示例: 带订单信息的投诉

```
用户: "我要投诉，订单 ORD-2024-001 快递太慢了"
```

**Step 1 — 意图识别**:
- `CreateTicket`: "投诉" YES → score = 1/6 ≈ 0.167
- `TrackShipping`: "快递" YES → score = 1/6 ≈ 0.167
- `QueryOrder`: "订单" 不在关键词("查询订单"等)中，"ORD-" YES → score = 1/6 ≈ 0.167
- 三者并列 → 按 OrderByDescending 取第一个（字典序不确定）

**Step 2 — 路由**:
- 不管 intent 结果如何，`ContainsAny(["订单"])` → YES → **路由到 OrderAgent**

> ⚠️ 用户想投诉但因包含"订单"关键词被路由到 OrderAgent 而非 TicketAgent。
> 这是一个路由优先级问题：订单关键词判断在工单关键词之前。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

# CS-ORDER-002 模糊查询

---
metadata:
  case_id: CS-ORDER-002
  journey: 订单查询
  journey_order: 2
  case_type: multi-turn
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [intent-recognition, order-query, clarification]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 30
  depends_on: [CS-ORDER-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ORDER-002
- **用例类型**: 多轮对话
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户未提供订单号，系统引导用户提供必要信息后完成查询。

## 📝 执行流程

**第1轮**:
- 用户："我想查一下我的订单"
- 意图识别：order-query（缺少orderId）
- 系统：
```
好的，请提供以下任一信息以便查询：
  1️⃣ 订单号（如 ORD-XXXXXXXX-XXX）
  2️⃣ 手机号（下单时使用的手机号）
  3️⃣ 商品名称 + 大致下单时间
```

**第2轮**:
- 用户："手机号 138XXXX1234，上周买的"
- 系统：
```
📦 查询到2笔近期订单：

1. ORD-20260310-023
   - 智能台灯 × 1 — ¥89.00
   - 状态：✅ 已签收（03-12）

2. ORD-20260312-045
   - 无线耳机 × 1 — ¥199.00
   - 状态：🚚 配送中（预计03-16到达）

请问您想查看哪笔订单的详情？
```

## ✅ 预期结果

- [x] 识别到缺少订单号
- [x] 引导提供替代查询信息
- [x] 多条结果列表展示
- [x] 支持进一步选择

## 🧪 测试要点

- [ ] 缺少关键实体检测
- [ ] 多查询条件组合支持
- [ ] 多结果分条展示

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - `IOrderService` 无手机号查询方法，文档中"手机号查询"在当前代码中不可实现
> - 缺少 orderId 时 OrderAgent 返回 `NeedsClarification`，不支持手机号/商品名查询
> - `GetUserOrdersAsync(userId)` 按 userId 查询，不按手机号

### 示例: 模糊查询无订单号

```
用户: "我想查一下我的订单"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `QueryOrder`: "我的订单" YES, "查一下订单" — 原文是"查一下我的订单"不完全匹配
  但 `userInput.Contains("我的订单")` = YES → score = 1/6 ≈ 0.167
- `GeneralFaq`: "我想" YES → score = 1/5 = 0.2
- **PrimaryIntent = "GeneralFaq"**, Confidence = 0.2

**Step 2 — 路由** (`RouteToAgentAsync`):
- `"GeneralFaq"` 不在订单 intent 列表
- `ContainsAny(["订单"])` → YES → **路由到 OrderAgent** ✓

**Step 3 — OrderAgent 处理** (`ExecuteBusinessLogicAsync`):
- `ExtractOrderId`: `parameters["orderId"]` 无值，正则无匹配 → `orderId = null`
- `userInput.Contains("查一下")` → YES → `HandleOrderQueryAsync(null, ...)`

**Step 4 — HandleOrderQueryAsync(null, ...)**:
```csharp
// orderId 为空 → 返回澄清
return new MafTaskResponse {
    Success = false,
    NeedsClarification = true,
    ClarificationQuestion = "请提供您的订单号（格式如 ORD-2024-001），我来帮您查询。",
    Result = "请提供订单号以便为您查询。",
};
```

**Step 5 — 实际响应**:
```
请提供订单号以便为您查询。
```
（`NeedsClarification = true`, `ClarificationQuestion = "请提供您的订单号（格式如 ORD-2024-001），我来帮您查询。"`）

### 第2轮: 用户提供订单号

```
用户: "ORD-2024-001"
```
- 正则匹配 "ORD-2024-001" → `HandleOrderQueryAsync("ORD-2024-001")`
- 返回完整订单信息（同 CS-ORDER-001 示例）

> ⚠️ 文档中的手机号查询流程 (`138XXXX1234`) 需要先在 `IOrderService` 中添加
> `Task<List<OrderInfo>> SearchOrdersAsync(string phoneNumber, ...)` 方法才能实现。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

# CS-COMPLAIN-002 产品缺陷投诉

---
metadata:
  case_id: CS-COMPLAIN-002
  journey: 投诉建议
  journey_order: 2
  case_type: variant
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent]
  capabilities: [intent-recognition, ticket-creation, order-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 35
  depends_on: [CS-COMPLAIN-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-COMPLAIN-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

产品缺陷投诉，关联订单并触发质量工单。

## 📝 执行流程

**用户输入**: "订单 ORD-20260310-015 收到的耳机左边没声音，这质量也太差了"

**系统响应**:
```
非常抱歉遇到产品质量问题 😔

📦 已关联订单：ORD-20260310-015
  - 商品：无线蓝牙耳机  — ¥299.00

📝 质量投诉工单已创建：
  工单编号：TK-20260315-002
  投诉类型：产品缺陷
  问题描述：左耳无声音
  优先级：🔴 高

📌 处理方案：
  1. 优先安排换货（1个工作日内审核）
  2. 或选择全额退款
  3. 质量部门将跟进调查

请问您倾向换货还是退款？
```

## ✅ 预期结果

- [x] 订单自动关联
- [x] 质量工单高优先级
- [x] 同时提供换货和退款选项

## 🧪 测试要点

- [ ] OrderAgent订单关联
- [ ] TicketAgent高优先级工单
- [ ] 处理方案完整

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - 文档描述的 OrderAgent + TicketAgent 协作在当前代码中未实现
> - 路由是互斥的（订单关键词优先于工单关键词）
> - 订单号 `ORD-20260310-015` 在 mock 数据中不存在
> - 工单编号格式: `TKT-yyyyMMdd-NNN`（非 `TK-`）

### 示例: 产品缺陷投诉（实际路由行为）

```
用户: "订单 ORD-2024-001 收到的耳机左边没声音，这质量也太差了"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `QueryOrder`: "ORD-" YES → score = 1/6 ≈ 0.167
- `ProductQuery`: "质量" 不在其关键词中（其keywords是["产品","商品","规格","参数","功能","使用方法","说明书"]） → 0
- **PrimaryIntent = "QueryOrder"**, Confidence ≈ 0.167

**Step 2 — 路由** (`RouteToAgentAsync`):
- `intent.PrimaryIntent is "QueryOrder"` → YES → **OrderAgent**

> ⚠️ 用户想投诉产品质量，但因包含"订单"关键词被路由到 OrderAgent。
> 文档预期的 TicketAgent 协作不会触发。

**Step 3 — OrderAgent 处理**:
- `ExtractOrderId` → "ORD-2024-001"
- `userInput.Contains("查询")` NO, `Contains("快递")` NO, `Contains("取消")` NO
- `Contains("退款")` NO, `Contains("退货")` NO, `Contains("退钱")` NO
- 兜底: `HandleOrderQueryAsync("ORD-2024-001", ...)`

**实际响应**:
```
📦 订单 ORD-2024-001
• 状态：已发货，运输中
• 商品：无线蓝牙耳机
• 金额：¥299.00
• 下单时间：2026-03-12 ...
```

> ⚠️ 代码仅返回订单信息，未创建质量投诉工单。
> 实现此用例需要:
> 1. 路由增加"质量投诉"识别（嗨反为"质量"+"订单"的组合意图）
> 2. OrderAgent + TicketAgent 的组合调用逻辑
> 3. 优先级自动提升机制

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

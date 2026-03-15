# CS-COMPLAIN-003 物流延误投诉

---
metadata:
  case_id: CS-COMPLAIN-003
  journey: 投诉建议
  journey_order: 3
  case_type: variant
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent]
  capabilities: [intent-recognition, ticket-creation, order-query, logistics-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 30
  depends_on: [CS-COMPLAIN-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-COMPLAIN-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

物流延误投诉，查询物流状态并创建催件工单。

## 📝 执行流程

**用户输入**: "我的快递都5天了还没到！订单 ORD-20260310-020"

**系统响应**:
```
抱歉让您久等了 😔 我来帮您查一下。

📦 订单 ORD-20260310-020 物流信息：
  - 快递公司：中通快递
  - 运单号：ZT9876543210
  - 最新状态：03-12 到达中转站（已停滞3天）

⚠️ 确认物流异常：快递已停滞超过48小时

📝 已为您处理：
  1. ✅ 创建催件工单：TK-20260315-003
  2. ✅ 已联系快递公司催促
  3. 🕐 预计24小时内更新物流

如果24小时内仍无进展，我们将安排补发或退款。
```

## ✅ 预期结果

- [x] 物流状态自动查询
- [x] 异常检测（停滞超48小时）
- [x] 自动创建催件工单
- [x] 承诺后续处理方案

## 🧪 测试要点

- [ ] OrderAgent物流查询
- [ ] 异常停滞自动检测
- [ ] 催件工单创建

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我的快递都5天了还没到！订单 ORD-20260310-020 |
| 1 | 🤖 系统 | _(IntentRecognizer: "快递" → TrackShipping; "ORD-" → QueryOrder)_<br>_(DetectEmotionLevel: 无情绪关键词 → Neutral)_<br>_(路由: 输入包含"快递"/"物流" → OrderAgent)_ |
| 1 | 🤖 系统 | _(OrderAgent: "快递"/"物流" → 物流查询分支)_<br>🚚 订单 ORD-20260310-020 物流信息：<br>快递单号: SF1234567890<br>状态: 运输中<br>位置: 已到达当地分拣中心<br>预计送达: 明天 |
| 2 | 👤 用户 | 太慢了，我要投诉物流 |
| 2 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(路由: 输入包含"投诉" → TicketAgent)_<br>📝 投诉工单已创建！<br>工单号: TK-20260315-004<br>类别: shipping<br>优先级: 普通<br>24小时内处理 |

### 代码执行追踪

```
Turn 1:
OrderAgent.ExecuteBusinessLogicAsync()
  ├─ 提取订单号: ORD-20260310-020
  ├─ "快递"/"物流"/"到哪" → 物流查询分支
  └─ IOrderService.GetOrderAsync() → 返回订单+物流信息

Turn 2:
TicketAgent: "投诉" → 工单创建
  ├─ DetectPriority() → "normal"
  ├─ category: "shipping" (输入含"物流")
  └─ 创建工单 TK-20260315-004
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 催件功能 | 自动创建催件工单 | TicketAgent创建普通工单，无催件专用流程 |
| 物流关联 | 工单自动关联订单物流 | TicketAgent不主动关联订单信息 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

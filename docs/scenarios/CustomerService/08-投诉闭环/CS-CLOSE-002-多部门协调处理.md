# CS-CLOSE-002 多部门协调处理

---
metadata:
  case_id: CS-CLOSE-002
  journey: 投诉闭环
  journey_order: 2
  case_type: multi-agent
  domain: customerservice
  complexity: L4
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent, KnowledgeBaseAgent]
  capabilities: [cross-department-coordination, ticket-lifecycle, order-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 45
  depends_on: [CS-CLOSE-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-CLOSE-002
- **用例类型**: 多Agent协作
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

复杂投诉需要多部门（售后+物流+品控）协调处理。

## 📝 执行流程

**用户输入**: "我买的手机屏幕有坏点，而且快递盒也是压扁的，不知道是出厂问题还是快递的问题"

**系统响应**:
```
📝 已创建多部门协调工单：TK-20260315-012

📋 问题分解：
  1. 📦 物流部门：核查运输环节（包裹外损）
  2. 🔧 品控部门：检查屏幕坏点（出厂质量）
  3. 💰 售后部门：准备补偿方案

⏱️ 各部门处理时效：
  - 物流核查：24小时
  - 品控鉴定：48小时
  - 综合方案：48小时内给出

我们会在各环节完成后主动通知您进展。
请问还有其他需要补充的信息吗？
```

## ✅ 预期结果

- [x] 问题自动分解到多部门
- [x] 各部门时效分别承诺
- [x] 进展主动通知

## 🧪 测试要点

- [ ] 问题分解逻辑
- [ ] 多部门工单创建
- [ ] 进展聚合通知

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我买的手机屏幕有坏点，而且快递箱也被压扁了 |
| 1 | 🤖 系统 | _(IntentRecognizer: "快递" → TrackShipping)_<br>_(路由: 输入包含"快递" → OrderAgent)_ |
| 1 | 🤖 系统 | _(OrderAgent: "快递" → 物流追踪分支)_<br>请提供您的订单号，我来查询物流信息。 |
| 2 | 👤 用户 | 订单号 ORD-20260310-005，手机有坏点我要投诉 |
| 2 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(路由: TicketAgent)_<br>📝 投诉工单已创建！<br>工单号: TK-20260315-009<br>类别: product<br>优先级: 普通<br>我们会协调品控和物流部门处理。 |

### 代码执行追踪

```
Turn 1:
OrderAgent: "快递" → 物流追踪分支

Turn 2:
MainAgent.RouteToAgent("订单号 ORD-20260310-005，手机有坏点我要投诉")
  ├─ "投诉" → TicketAgent
  ├─ TicketAgent: 工单创建, category=自动检测"手机"→"product"
  └─ DetectPriority: 无紧急关键词 → "normal"
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 多部门协调 | 工单自动分发到品控+物流 | 创建单一工单，无多部门分发 |
| 进展聚合 | 多部门进展聚合显示 | 无多部门进展追踪 |
| 双问题 | 同时处理品质+包装问题 | 单次路由只匹配一个Agent |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

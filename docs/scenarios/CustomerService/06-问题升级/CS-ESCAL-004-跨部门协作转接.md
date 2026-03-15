# CS-ESCAL-004 跨部门协作转接

---
metadata:
  case_id: CS-ESCAL-004
  journey: 问题升级
  journey_order: 4
  case_type: multi-agent
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent, KnowledgeBaseAgent]
  capabilities: [cross-department-routing, ticket-creation, context-handover]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 35
  depends_on: [CS-ESCAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ESCAL-004
- **用例类型**: 多Agent协作
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

问题涉及多部门（如售后+物流+财务），系统协调多部门处理。

## 📝 执行流程

**用户输入**: "我退货寄回去了，但物流显示已签收，退款还没到，你们帮我查一下"

**系统响应**（多Agent协作查询）:
```
📋 为您综合查询中...

📦 物流信息（OrderAgent）：
  - 退货运单：SF9876543210
  - 状态：✅ 仓库已签收（03-14 10:00）

🎫 退货工单（TicketAgent）：
  - 工单：TK-20260312-015
  - 状态：退货审核中

💰 退款信息（OrderAgent）：
  - 退款状态：⏳ 待审核
  - 原因：仓库签收后需1个工作日验货

📌 综合结论：
  - 仓库已收到退货，正在验货中
  - 预计验货完成：2026-03-15 18:00
  - 验货通过后退款将在3-5个工作日到账

已创建跟进工单 TK-20260315-010 确保流程推进。
```

## ✅ 预期结果

- [x] 多Agent并行查询不同系统
- [x] 信息聚合综合分析
- [x] 给出明确时间节点
- [x] 自动创建跟进工单

## 🧪 测试要点

- [ ] 多Agent并行调度
- [ ] 数据聚合准确
- [ ] 跨部门信息一致性

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我退货寄回去了，但物流显示已签收，退款还没到 |
| 1 | 🤖 系统 | _(IntentRecognizer: "退货"/"退款" → RequestRefund; "物流" → TrackShipping)_<br>_(路由: 输入包含"退款"/"退货" → OrderAgent)_ |
| 1 | 🤖 系统 | _(OrderAgent: "退货"/"退款" → 退款分支)_<br>您的退货已确认签收，退款正在审核中。<br>预计 1-3 个工作日到账。 |
| 2 | 👤 用户 | 都一周了还没到，我要投诉 |
| 2 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(路由: TicketAgent)_<br>📝 投诉工单已创建！<br>工单号: TK-20260315-007<br>类别: refund<br>优先级: 普通<br>我们会协调物流和退款部门尽快处理。 |

### 代码执行追踪

```
Turn 1:
OrderAgent: "退货"/"退款" → 退款分支
  └─ IOrderService.RequestRefundAsync() → 返回审核状态

Turn 2:
TicketAgent: "投诉" → 工单创建
  ├─ DetectPriority() → "normal"
  ├─ category: "refund"
  └─ 创建工单 TK-20260315-007
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 跨部门协调 | 自动协调物流+退款部门 | TicketAgent创建单一工单，无跨部门流转 |
| 物流关联 | 自动关联退货物流信息 | 工单与物流信息不关联 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

# CS-TICKET-004 工单分配和进度查询

---
metadata:
  case_id: CS-TICKET-004
  journey: 工单跟进
  journey_order: 4
  case_type: multi-agent
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent]
  capabilities: [ticket-query, order-query, agent-coordination]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 25
  depends_on: [CS-TICKET-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-TICKET-004
- **用例类型**: 多Agent协作
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

复杂工单需要多Agent协作查询，TicketAgent获取工单信息，OrderAgent关联订单数据。

## 📝 执行流程

**用户输入**: "我的退款工单 TK-20260315-004 处理得怎么样了？"

**系统响应**（TicketAgent + OrderAgent协作）:
```
🎫 工单综合进展：

  工单：TK-20260315-004
  类型：退款加急
  状态：🟢 处理完成

📦 关联订单：ORD-20260305-008
  - 退款金额：¥599.00
  - 退款状态：✅ 已退款
  - 到账时间：2026-03-15 16:30
  - 退款渠道：支付宝

📋 处理时间线：
  - 03-15 10:00 ️工单创建（加急）
  - 03-15 11:00 财务部门处理
  - 03-15 16:30 退款到账

工单已关闭。感谢您的耐心等待 😊
```

## ✅ 预期结果

- [x] TicketAgent查询工单状态
- [x] OrderAgent关联退款信息
- [x] 综合数据聚合展示

## 🧪 测试要点

- [ ] 多Agent数据聚合
- [ ] 工单与订单关联
- [ ] 退款状态实时查询

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我的退款工单 TK-20260315-004 处理得怎么样了？ |
| 1 | 🤖 系统 | _(IntentRecognizer: "工单" → QueryTicket)_<br>_(路由: TicketAgent)_ |
| 1 | 🤖 系统 | _(TicketAgent: "处理进度" → 工单查询分支)_<br>📝 工单 TK-20260315-004：<br>状态: 已分配给退款处理组<br>处理人: 张三<br>进度: 退款审核中<br>预计完成: 12小时内 |
| 2 | 👤 用户 | 退款多久能到账？ |
| 2 | 🤖 系统 | _(IntentRecognizer: "退款" → RequestRefund)_<br>_(路由: 输入包含"退款" → OrderAgent)_<br>一般审核通过后1-3个工作日到账，具体取决于支付方式。 |

### 代码执行追踪

```
TicketAgent.ExecuteBusinessLogicAsync("我的退款工单 TK-20260315-004 处理得怎么样了？")
  ├─ "处理"/"工单" → 工单查询分支
  └─ 返回工单状态和分配信息
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 处理人信息 | 显示工单分配人和处理进度 | TicketAgent返回基本状态，无分配人详情 |
| 退款状态 | 实时查询退款到账状态 | 无专用退款状态API |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

# CS-TICKET-002 查询工单状态

---
metadata:
  case_id: CS-TICKET-002
  journey: 工单跟进
  journey_order: 2
  case_type: single-agent
  domain: customerservice
  complexity: L1
  agents: [TicketAgent]
  capabilities: [ticket-query]
  coverage: { single-agent: true, multi-agent: false, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 15
  depends_on: [CS-TICKET-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-TICKET-002
- **用例类型**: 单Agent
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

查询已创建工单的处理状态。

## 📝 执行流程

**用户输入**: "查一下工单 TK-20260315-005 的进度"

**系统响应**:
```
🎫 工单状态：

  工单编号：TK-20260315-005
  问题类别：账户问题
  提交时间：2026-03-15 10:30
  当前状态：🔵 处理中
  处理人：技术支持组-张工
  预计完成：2026-03-16 18:00

📋 处理记录：
  - 03-15 10:30 工单创建
  - 03-15 11:00 已分配至技术支持组
  - 03-15 14:00 正在核实优惠券记录

请耐心等候，有进展会及时通知您 😊
```

## ✅ 预期结果

- [x] 工单状态完整展示
- [x] 处理时间线清晰
- [x] 预计完成时间

## 🧪 测试要点

- [ ] TicketAgent工单查询
- [ ] 状态时间线完整
- [ ] 工单号格式匹配

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 查一下工单 TK-20260315-005 的进度 |
| 1 | 🤖 系统 | _(IntentRecognizer: "工单状态"/"处理进度"/"我的工单" → QueryTicket)_<br>_(路由: 输入包含"工单" → TicketAgent)_ |
| 1 | 🤖 系统 | _(TicketAgent: "查询工单"/"我的工单"/"处理进度" → 工单查询分支)_<br>📝 工单 TK-20260315-005：<br>状态: 处理中<br>标题: 订单物流延误投诉<br>创建时间: 2026-03-15<br>预计完成: 24小时内 |

### 代码执行追踪

```
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("查一下工单 TK-20260315-005 的进度")
  ├─ IntentRecognizer: "工单" → QueryTicket
  ├─ 路由: 输入包含"工单" → TicketAgent
  └─ TicketAgent.ExecuteBusinessLogicAsync()
      ├─ "查询工单"/"进度" → 工单查询分支
      └─ ITicketService.GetTicketAsync("TK-20260315-005") → 返回工单信息
```

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

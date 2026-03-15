# CS-TICKET-005 工单处理超时处理

---
metadata:
  case_id: CS-TICKET-005
  journey: 工单跟进
  journey_order: 5
  case_type: exception
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [ticket-query, timeout-detection, escalation]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: [CS-TICKET-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-TICKET-005
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

工单超过承诺时效未处理，系统自动升级并通知用户。

## 📝 执行流程

**用户输入**: "工单 TK-20260313-010 说48小时回复，现在都3天了"

**系统响应**:
```
🎫 工单 TK-20260313-010 状态：

  提交时间：2026-03-13 09:00
  承诺时效：48小时
  当前状态：⚠️ 已超时（超出24小时）

❌ 确认处理超时，已执行以下操作：

  1. 🔴 工单优先级提升为"紧急"
  2. 📧 已通知主管介入处理
  3. 🎟️ 已为您生成投诉工单：TK-20260315-006

非常抱歉处理延迟！主管将在4小时内联系您。
作为补偿，已为您发放一张满100减20优惠券。
```

## ✅ 预期结果

- [x] 超时自动检测
- [x] 优先级自动升级
- [x] 主管通知
- [x] 补偿方案提供

## 🧪 测试要点

- [ ] 超时计算准确
- [ ] 自动升级逻辑
- [ ] 补偿方案自动发放

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 工单 TK-20260313-010 说48小时回复，现在都3天了 |
| 1 | 🤖 系统 | _(IntentRecognizer: "工单" → QueryTicket)_<br>_(DetectEmotionLevel: 无情绪关键词 → Neutral)_<br>_(路由: TicketAgent)_ |
| 1 | 🤖 系统 | 📝 工单 TK-20260313-010：<br>状态: 处理中 (已超时)<br>创建时间: 3天前<br><br>⚠️ 该工单确实超过承诺时间，我们已升级处理优先级。<br>抱歉给您带来不便。 |
| 2 | 👤 用户 | 这也太慢了，我很不满意 |
| 2 | 🤖 系统 | _(DetectEmotionLevel: "不满意" → Frustrated)_<br>很抱歉让您感到失望，我理解您的感受。我会尽力帮您解决问题。<br><br>我们会优先处理您的工单，确保尽快解决。 |

### 代码执行追踪

```
Turn 2:
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("这也太慢了，我很不满意")
  ├─ DetectEmotionLevel: "不满意" → Frustrated (level 2)
  ├─ 情绪前缀: "很抱歉让您感到失望..."
  ├─ 路由: 无明确意图关键词 → KnowledgeBaseAgent
  └─ KnowledgeBaseAgent: 语义搜索 → 返回超时处理流程说明
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 超时检测 | 自动检测工单超时并升级 | TicketAgent无超时检测机制 |
| 补偿方案 | 自动发放优惠券补偿 | 无补偿自动发放逻辑 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

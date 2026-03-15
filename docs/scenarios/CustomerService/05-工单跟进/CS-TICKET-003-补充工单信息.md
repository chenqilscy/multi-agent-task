# CS-TICKET-003 补充工单信息

---
metadata:
  case_id: CS-TICKET-003
  journey: 工单跟进
  journey_order: 3
  case_type: multi-turn
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [ticket-update, context-tracking]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 25
  depends_on: [CS-TICKET-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-TICKET-003
- **用例类型**: 多轮对话
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

用户补充已创建工单的信息（截图、补充描述等）。

## 📝 执行流程

**第1轮**:
- 用户："我想给工单 TK-20260315-005 补充一些信息"
- 系统：
```
🎫 已找到工单 TK-20260315-005（账户问题-优惠券丢失）

请提供需要补充的信息：
```

**第2轮**:
- 用户："那张优惠券是上周三领的，当时在活动页面领取的，活动名叫'春季特惠'"
- 系统：
```
✅ 工单信息已更新：

  工单：TK-20260315-005
  新增信息：
  - 优惠券领取时间：上周三（约2026-03-12）
  - 领取渠道：活动页面
  - 活动名称：春季特惠

已通知处理人员查看更新信息。
```

## ✅ 预期结果

- [x] 工单查询并关联
- [x] 补充信息追加到工单
- [x] 通知处理人员

## 🧪 测试要点

- [ ] 工单更新操作
- [ ] 追加信息格式化
- [ ] 上下文关联准确

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我想给工单 TK-20260315-005 补充一些信息 |
| 1 | 🤖 系统 | _(IntentRecognizer: "工单" → QueryTicket)_<br>_(路由: TicketAgent)_<br>📝 请提供您要补充的信息，我会添加到工单 TK-20260315-005 中。 |
| 2 | 👤 用户 | 问题是收到的商品外包装严重破损，内部商品也有划痕 |
| 2 | 🤖 系统 | _(TicketAgent: 补充信息已记录)_<br>✅ 补充信息已添加到工单 TK-20260315-005。<br>工单状态: 处理中 |

### 代码执行追踪

```
TicketAgent.ExecuteBusinessLogicAsync("我想给工单 TK-20260315-005 补充一些信息")
  ├─ "查询工单"/"我的工单" → 工单查询分支 (部分匹配)
  └─ 实际可能进入工单创建分支 (非查询关键词)
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 补充信息 | 工单补充信息API | TicketAgent无专用补充信息分支，可能创建新工单 |
| 关联工单 | 自动识别工单号关联 | TicketAgent仅匹配查询/创建两个分支 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

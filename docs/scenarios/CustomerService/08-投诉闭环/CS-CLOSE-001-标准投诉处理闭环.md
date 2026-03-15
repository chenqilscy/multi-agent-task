# CS-CLOSE-001 标准投诉处理闭环

---
metadata:
  case_id: CS-CLOSE-001
  journey: 投诉闭环
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L4
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent, KnowledgeBaseAgent]
  capabilities: [ticket-lifecycle, order-query, satisfaction-survey, knowledge-update]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 60
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-CLOSE-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

展示从投诉受理→处理→确认→回访的完整闭环流程。

## 📝 执行流程

**阶段1 - 受理**:
- 用户："我3天前投诉的快递破损问题，有结果了吗？"
- 系统：
```
🎫 工单 TK-20260312-020 进展：

  投诉内容：快递包裹破损
  状态：✅ 已处理完成

📋 处理结果：
  1. 已确认包裹在运输中破损
  2. 全额退款 ¥199.00 已于昨日到账
  3. 赠送优惠券：满100减20 × 2

请问您对处理结果满意吗？
  ⭐⭐⭐⭐⭐ 非常满意
  ⭐⭐⭐⭐ 满意
  ⭐⭐⭐ 一般
  ⭐⭐ 不满意
  ⭐ 非常不满意
```

**阶段2 - 确认**:
- 用户："满意，退款已经收到了"
- 系统：
```
感谢您的反馈！很高兴问题得到解决 😊

📝 工单已关闭：
  - 工单号：TK-20260312-020
  - 满意度：⭐⭐⭐⭐ 满意
  - 关闭时间：2026-03-15

感谢您的耐心和理解，如有任何问题随时联系我们！
```

## ✅ 预期结果

- [x] 处理结果完整展示
- [x] 满意度评价收集
- [x] 工单正式关闭
- [x] 闭环数据记录

## 🧪 测试要点

- [ ] 工单状态流转完整
- [ ] 满意度评价入库
- [ ] 闭环记录持久化

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我三天前投诉的问题有结果了吗？ |
| 1 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(路由: 输入包含"投诉" → TicketAgent)_ |
| 1 | 🤖 系统 | _(TicketAgent: 无查询关键词 → 工单创建分支)_<br>📝 工单已创建！<br>工单号: TK-20260315-008<br>类别: other<br>优先级: 普通 |
| 2 | 👤 用户 | 不是创建新工单，我要查之前的工单进度 |
| 2 | 🤖 系统 | _(IntentRecognizer: "工单" + "进度" → QueryTicket)_<br>_(路由: TicketAgent)_<br>📝 您近期的工单：<br>TK-20260312-003: 已解决 ✅<br>处理结果: 已为您补发商品并赠送5元优惠券 |

### 代码执行追踪

```
Turn 1:
TicketAgent.ExecuteBusinessLogicAsync("我三天前投诉的问题有结果了吗？")
  ├─ 查询关键词匹配: "投诉" ≠ "查询工单"/"我的工单"/"处理进度"
  └─ 进入工单创建分支 (误判)

Turn 2:
TicketAgent.ExecuteBusinessLogicAsync("不是创建新工单，我要查之前的工单进度")
  ├─ 查询关键词匹配: "工单" + "进度" ⇒ 正确进入查询分支
  └─ 返回工单列表
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 闭环查询 | 自动关联历史工单并显示处理结果 | "投诉"关键词导向创建而非查询 |
| 闭环记录 | 持久化全部处理过程 | 无闭环状态追踪机制 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

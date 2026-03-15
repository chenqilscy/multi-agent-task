# CS-CLOSE-003 处理结果用户确认

---
metadata:
  case_id: CS-CLOSE-003
  journey: 投诉闭环
  journey_order: 3
  case_type: multi-turn
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [ticket-update, user-confirmation]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 25
  depends_on: [CS-CLOSE-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-CLOSE-003
- **用例类型**: 多轮对话
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

处理完成后请用户确认，用户不满意则重新处理。

## 📝 执行流程

**系统主动通知**:
```
📢 工单处理完成通知

🎫 工单 TK-20260314-025 处理结果：

  问题：商品瑕疵投诉
  处理方案：补发全新商品 + 赠送20元优惠券
  新订单号：ORD-20260315-060（已发货）

请确认您是否接受此处理方案？
  ✅ 接受
  ❌ 不接受（需要其他方案）
```

**用户回复**: "不接受，我想直接退款"

**系统响应**:
```
理解您的诉求。已为您更新处理方案：

📋 变更内容：
  - 原方案：补发 + 优惠券 → 取消
  - 新方案：全额退款 ¥259.00
  - 新订单已拦截取消

🔄 退款将在3-5个工作日到账。
工单已更新，后续有问题请随时联系。
```

## ✅ 预期结果

- [x] 处理结果主动通知
- [x] 用户确认/拒绝选项
- [x] 拒绝后方案调整
- [x] 新方案自动执行

## 🧪 测试要点

- [ ] 确认/拒绝流程分支
- [ ] 方案变更操作
- [ ] 新旧方案状态同步

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(工单处理完成，发送确认通知)_ |
| 0 | 🤖 系统 | 📝 您的工单 TK-20260312-003 已处理完成！<br>处理方案: 补发新商品 + 5元优惠券<br><br>您是否接受此方案？<br>1️⃣ 接受<br>2️⃣ 不接受，要求重新处理 |
| 1 | 👤 用户 | 我不接受，希望直接退款 |
| 1 | 🤖 系统 | _(IntentRecognizer: "退款" → RequestRefund)_<br>_(路由: OrderAgent)_<br>您的退款申请已提交，预计1-3个工作日到账。 |

### 代码执行追踪

```
系统触发 (工单完成通知):
  └─ 当前无工单完成自动通知机制

Turn 1:
MainAgent.RouteToAgent("我不接受，希望直接退款")
  ├─ "退款" → RequestRefund
  └─ 路由: OrderAgent → 退款分支
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 完成通知 | 工单完成后自动发送确认 | 无工单完成自动通知机制 |
| 方案替换 | 拒绝后自动切换备选方案 | 用户需自行提出新请求 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

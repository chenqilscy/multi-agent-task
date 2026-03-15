# CS-PROACTIVE-001 发货延迟主动通知

---
metadata:
  case_id: CS-PROACTIVE-001
  journey: 主动服务
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [logistics-monitoring, proactive-notification]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 20
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-PROACTIVE-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

系统检测到物流延迟，主动通知用户并提供处理方案。

## 📝 执行流程

**触发条件**: 物流停滞超过48小时

**系统主动推送**:
```
📢 物流通知

尊敬的用户，您有一笔订单物流延迟：

📦 订单：ORD-20260312-030
  - 商品：智能手表
  - 最后更新：03-13 中转站发出
  - 已停滞：52小时

⚠️ 可能原因：区域性物流拥堵

📌 我们已为您：
  1. ✅ 联系快递公司催促
  2. ✅ 创建跟踪工单

💡 您可以选择：
  1️⃣ 继续等待（预计1-2天内更新）
  2️⃣ 申请退款
  3️⃣ 联系客服了解详情
```

## ✅ 预期结果

- [x] 物流异常自动检测
- [x] 主动推送通知
- [x] 已采取措施说明
- [x] 用户可选处理方案

## 🧪 测试要点

- [ ] 物流停滞检测阈值
- [ ] 主动推送触发
- [ ] 用户响应选项处理

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(IProactiveEventBus.PublishAsync: type="shipping_delay")_<br>_(物流停滞48小时，自动触发事件)_ |
| 0 | 🤖 系统 | _(ShippingDelayEventHandler.HandleAsync())_<br>📦 您的订单 ORD-20260310-001 物流发生延迟，<br>当前物流已停滞超过48小时。<br>我们正在积极协调，预计新的送达时间2-3天。<br>您可以选择：<br>1️⃣ 继续等待<br>2️⃣ 申请补偿<br>3️⃣ 取消订单 |
| 1 | 👤 用户 | 申请补偿 |
| 1 | 🤖 系统 | _(路由: 无明确关键词匹配 → KnowledgeBaseAgent)_<br>补偿申请已收到，我们会尽快处理。 |

### 代码执行追踪

```
系统触发:
InMemoryProactiveEventBus.PublishAsync(event: {type="shipping_delay", data={orderId, delay}})
  ├─ 查找 handlers → ShippingDelayEventHandler
  └─ ShippingDelayEventHandler.HandleAsync(event)
       └─ 返回 ProactiveNotification {Title, Message, Actions}

用户响应:
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("申请补偿")
  ├─ IntentRecognizer: 无匹配 → GeneralFaq
  └─ 路由: KnowledgeBaseAgent (default)
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 事件触发 | 物流停滞48h自动触发 | EventBus已实现，但无自动监控触发源 |
| 用户响应 | 点击选项后自动处理 | 用户响应回到普通MainAgent路由 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

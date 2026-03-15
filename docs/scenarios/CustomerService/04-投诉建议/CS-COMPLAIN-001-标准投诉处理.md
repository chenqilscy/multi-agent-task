# CS-COMPLAIN-001 标准投诉处理

---
metadata:
  case_id: CS-COMPLAIN-001
  journey: 投诉建议
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent]
  capabilities: [intent-recognition, ticket-creation, order-query, emotion-detection]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 40
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-COMPLAIN-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

完整的投诉处理流程：收集投诉信息→创建工单→告知处理时效。

## 📝 执行流程

**第1轮**:
- 用户："我要投诉！你们的服务太差了"
- 意图：complaint, emotion=negative
- 系统：
```
非常抱歉给您带来了不好的体验 😔
我会认真记录您的投诉并尽快处理。

请问您是针对哪方面的投诉？
  1️⃣ 商品质量
  2️⃣ 物流配送
  3️⃣ 售后服务
  4️⃣ 其他
```

**第2轮**:
- 用户："售后服务态度很差，上次找人工客服等了半小时才接"
- 系统：
```
📝 投诉已记录：

  投诉类型：售后服务
  投诉内容：人工客服等待时间过长（约30分钟）
  工单编号：TK-20260315-001
  处理时效：24小时内回复

我们会认真调查此事并尽快改进。
给您带来不便深表歉意！是否需要补偿方案？
```

## ✅ 预期结果

- [x] 情绪检测并安抚
- [x] 投诉分类收集
- [x] 工单自动创建
- [x] 处理时效承诺

## 🧪 测试要点

- [ ] 负面情绪检测准确
- [ ] TicketAgent工单创建
- [ ] 安抚语言自然

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要投诉！你们的服务太差了 |
| 1 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(DetectEmotionLevel: "太差" → Frustrated)_<br>_(情绪安抚: "很抱歉让您感到失望，我理解您的感受。我会尽力帮您解决问题。")_<br>_(路由: 输入包含"投诉" → TicketAgent)_ |
| 1 | 🤖 系统 | 很抱歉让您感到失望，我理解您的感受。我会尽力帮您解决问题。<br><br>📝 投诉工单已创建！<br>工单号: TK-20260315-003<br>类别: other<br>优先级: 普通<br>24小时内处理 |
| 2 | 👤 用户 | 我的订单 ORD-20260310-015 到现在都没发货 |
| 2 | 🤖 系统 | _(IntentRecognizer: "订单"/"ORD-" → QueryOrder)_<br>_(路由: OrderAgent → 订单查询)_<br>📦 订单 ORD-20260310-015：<br>状态: 待发货<br>商品: 智能音箱<br>下单时间: 5天前<br><br>ℹ️ 发货确实较慢，已在工单中记录，将优先处理。 |

### 代码执行追踪

```
Turn 1:
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("我要投诉！你们的服务太差了")
  ├─ IntentRecognizer: "投诉" → CreateTicket
  ├─ DetectEmotionLevel(): "太差" → Frustrated (level 2)
  ├─ 情绪前缀: "很抱歉让您感到失望..."
  ├─ 路由: 输入包含"投诉" → TicketAgent
  └─ TicketAgent.ExecuteBusinessLogicAsync()
      ├─ 工单创建分支 (非查询)
      ├─ DetectPriority() → "normal" (无紧急关键词)
      ├─ category: "other" (无明确分类关键词)
      └─ 创建工单 → TK-20260315-003
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 情绪升级 | Frustrated情绪自动提升工单优先级 | 仅Angry级别传递emotionEscalation=true，Frustrated不会升级 |
| 关联订单 | 投诉自动关联历史订单 | TicketAgent不主动查询订单，需用户另行查询 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

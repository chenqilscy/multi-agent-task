# CS-INITIAL-003 多个连续问题

---
metadata:
  case_id: CS-INITIAL-003
  journey: 初次咨询
  journey_order: 3
  case_type: variant
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [intent-recognition, knowledge-query, context-tracking]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 40
  depends_on: [CS-INITIAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-INITIAL-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户连续提出多个不同问题，系统逐一回答并保持上下文。

## 📝 执行流程

**第1轮**:
- 用户："你们支持哪些支付方式？"
- 系统：
```
💳 支持的支付方式：
  - 支付宝
  - 微信支付
  - 银联在线
  - 信用卡/借记卡
  - 花呗分期（满200元可用）

请问还有其他问题吗？
```

**第2轮**:
- 用户："运费怎么算？"
- 系统：
```
📦 运费政策：
  - 订单满99元：包邮
  - 订单未满99元：普通快递 8元
  - 偏远地区（西藏、新疆等）：加收 5元
  - 加急配送：额外 15元

请问还需要了解什么？
```

**第3轮**:
- 用户："发货后多久能到？"
- 系统：
```
🚚 配送时效：
  - 普通快递：2-5个工作日
  - 加急配送：1-2个工作日
  - 偏远地区：5-7个工作日

📌 下单后可在"我的订单"中查看物流信息

还有其他问题吗？
```

## ✅ 预期结果

- [x] 每次问题独立正确识别
- [x] 知识库分别检索不同主题
- [x] 上下文自然衔接
- [x] 每次追问语略有变化

## 🧪 测试要点

- [ ] 连续多次知识库查询性能
- [ ] 意图无交叉干扰
- [ ] 追问语表达多样化

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 你们支持哪些支付方式？ |
| 1 | 🤖 系统 | _(IntentRecognizer: "支付" → PaymentQuery)_<br>_(MainAgent路由: PaymentQuery → KnowledgeBaseAgent)_<br>我们支持以下支付方式：支付宝、微信支付、银联卡...<br>📚 来源：FAQ-010 |
| 2 | 👤 用户 | 可以用花呗吗？ |
| 2 | 🤖 系统 | _(IntentRecognizer: "吗" → GeneralFaq)_<br>支持花呗分期付款，3/6/12期可选...<br>📚 来源：FAQ-011 |
| 3 | 👤 用户 | 那发票怎么开？ |
| 3 | 🤖 系统 | _(IntentRecognizer: "发票"+"怎么" → PaymentQuery/GeneralFaq)_<br>您可以在订单详情页点击"申请发票"，支持电子发票和纸质发票...<br>📚 来源：FAQ-012 |
| 4 | 👤 用户 | 还有，优惠券怎么用？ |
| 4 | 🤖 系统 | _(IntentRecognizer: "优惠券" → PaymentQuery)_<br>优惠券可在结算时自动折扣，每笔订单限用一张...<br>📚 来源：FAQ-013 |

### 代码执行追踪

```
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("你们支持哪些支付方式？")
  ├─ IntentRecognizer: "支付" → PaymentQuery
  ├─ DetectEmotionLevel() → Neutral
  ├─ 路由: PaymentQuery → KnowledgeBaseAgent (默认路径)
  └─ KnowledgeBaseAgent: 语义搜索 "支付方式" → FAQ匹配
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 连续问题上下文 | 多轮问题自动关联上下文 | 每轮独立处理，DialogStateManager存储但未用于意图关联 |
| 追问多样化 | "还有""另外"追问表达 | IntentRecognizer按关键词匹配，不识别追问意图 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

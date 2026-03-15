# CS-INITIAL-004 问题不明确

---
metadata:
  case_id: CS-INITIAL-004
  journey: 初次咨询
  journey_order: 4
  case_type: multi-turn
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [intent-recognition, clarification, knowledge-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 35
  depends_on: [CS-INITIAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-INITIAL-004
- **用例类型**: 多轮对话
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户表述模糊时，系统主动澄清意图，引导用户明确需求。

## 📝 执行流程

**第1轮 - 模糊提问**:
- 用户："我想问一下那个事情"
- 系统：
```
当然可以！不过您能具体描述一下想了解哪方面的信息吗？

我可以帮您：
  1️⃣ 查询订单状态
  2️⃣ 了解退换货政策
  3️⃣ 咨询商品信息
  4️⃣ 其他问题

请选择或直接描述您的问题 😊
```

**第2轮 - 用户澄清**:
- 用户："就是退货的事"
- 意图识别（第二次）：knowledge-query, topic="退货"
- 系统：
```
📋 关于退货：

1. **退货期限**：收到商品后7天内
2. **退货条件**：商品未使用、未拆封，保留包装
3. **退货流程**：
   - 在"我的订单"中提交退货申请
   - 等待审核（1个工作日内）
   - 审核通过后寄回商品
   - 收到商品后3-5个工作日退款

需要我帮您发起退货申请吗？
```

## ✅ 预期结果

- [x] 识别到意图不明确
- [x] 提供选项引导用户
- [x] 用户澄清后正确识别
- [x] 最终准确回答

## 🧪 测试要点

- [ ] 模糊意图触发澄清流程
- [ ] 选项设计覆盖常见场景
- [ ] 澄清后意图识别准确

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - 当前代码无"澄清引导"的独立逻辑，MainAgent 不会主动提供多选项
> - `PrimaryIntent = "Unknown"` 时直接路由到 KnowledgeBaseAgent
> - 知识库无匹配时返回固定降级文案，非交互式选项

### 第1轮: 模糊提问

```
用户: "我想问一下那个事情"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `GeneralFaq`: "我想" YES → score = 1/5 = 0.2
- 其他意图无匹配
- **PrimaryIntent = "GeneralFaq"**, Confidence = 0.2

**Step 2 — 路由**: 默认 → **KnowledgeBaseAgent**

**Step 3 — 知识库检索** (`SearchAsync("我想问一下那个事情", topK: 3)`):
- 所有 FAQ 无关键词命中 → score = 0 → `RelevantFaqs = []`

**Step 4 — 实际响应**:
```
抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。

💡 您也可以说「提交工单」，我们会安排专属客服为您解决。
```

> ⚠️ 代码无「提供多选项引导」的逻辑，直接返回知识库无结果的降级文案。
> 文档预期的"订单查询 / 退换货政策 / 商品信息 / 其他"多选项引导需要额外实现。

### 第2轮: 用户漄清后命中 FAQ

```
用户: "就是退货的事"
```

**意图识别**: `RequestRefund` → "退货" YES → score = 1/5 = 0.2
**路由**: `intent.PrimaryIntent is "RequestRefund"` → **OrderAgent** (非 KnowledgeBaseAgent)
**OrderAgent**: "退货" → `HandleRefundAsync(null, ...)` → orderId 为空

**实际响应**:
```
请提供订单号。
```

> ⚠️ 文档预期返回退货政策信息（知识库回答），但代码实际路由到 OrderAgent 并要求提供订单号。
> 这是一个重要的路由优先级问题: “政策咨询”与“退货操作”意图未区分。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

# CS-INITIAL-001 标准咨询流程

---
metadata:
  case_id: CS-INITIAL-001
  journey: 初次咨询
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [intent-recognition, knowledge-query, greeting]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 45
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-INITIAL-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

完整展示从问候到知识库查询回答再到满意度确认的标准客服流程。

## 📝 执行流程

**第1轮 - 用户问候**:
- 用户："你好"
- 系统："您好！我是智能客服小助手，请问有什么可以帮您？"

**第2轮 - 咨询问题**:
- 用户："你们的退换货政策是怎样的？"
- 意图识别：咨询类 → knowledge-query
- 实体提取：topic="退换货政策"
- 系统：
```
📋 关于退换货政策：

1. **退货期限**：收到商品后7天内可无理由退货
2. **换货期限**：收到商品后15天内可申请换货
3. **退货条件**：
   - 商品未使用、未拆封
   - 保留完整包装和标签
   - 附带购买凭证
4. **退款方式**：原路退回，3-5个工作日到账

📌 特殊商品（生鲜、定制品）不适用无理由退货

请问还有其他问题吗？
```

**第3轮 - 结束**:
- 用户："没有了，谢谢"
- 系统："不客气！如有需要随时找我。祝您生活愉快！😊"

## ✅ 预期结果

- [x] 问候语自然友好
- [x] 意图识别准确（knowledge-query）
- [x] 实体提取正确（退换货政策）
- [x] 知识库返回完整政策信息
- [x] 主动追问是否还有问题
- [x] 结束语礼貌

## 🧪 测试要点

- [ ] MainAgent正确路由到KnowledgeBaseAgent
- [ ] 知识库检索召回率
- [ ] 多轮对话上下文保持
- [ ] 响应格式清晰美观

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **路由修正说明**: 当前用户输入"退换货政策是怎样的？"包含"退货"关键词，
> `RuleBasedIntentRecognizer` 会命中 `RequestRefund`（score=1/5=0.2），
> 同时 `RouteToAgentAsync` 的 `ContainsAny(["退款", "退货"])` 为 true 导致路由到 **OrderAgent** 而非 KnowledgeBaseAgent。
> 以下示例使用不触发订单/工单关键词的措辞来确保正确路由到知识库。

### 示例 A: 正确路由到知识库的版本

```
用户: "你们的售后服务有哪些？"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- 扫描 `CustomerServiceIntentKeywordProvider` 全部 9 个 intent
- 匹配结果: `GeneralFaq` → "是什么" 不命中, "帮我" 不命中 → score=0
- 无任何 intent 命中 → `PrimaryIntent = "Unknown"`, `Confidence = 0.0`

**Step 2 — 实体提取** (`IntentDrivenEntityExtractor`):
- `_mapping.GetProviderType("Unknown")` → null → 返回空 `EntityExtractionResult`

**Step 3 — 路由** (`RouteToAgentAsync`):
- `intent.PrimaryIntent` 不是订单/工单类 intent ✗
- `ContainsAny(["订单", "快递", "物流", "退款", "退货"])` → false ✗
- `ContainsAny(["投诉", "工单", "反馈", "建议", "举报", "人工"])` → false ✗
- **→ 默认路由到 `KnowledgeBaseAgent`** ✓

**Step 4 — 知识库检索** (`SimulatedKnowledgeBaseService.SearchAsync`):
- FAQ-005 "产品质量问题怎么处理？" keywords: ["质量", "质量问题", "坏了", "损坏", "不好用", "有问题"] → 无命中
- 所有 FAQ Relevance = 0 → `Confidence = 0`
- `GeneratedAnswer = null`, `RelevantFaqs = []`

**Step 5 — KnowledgeBaseAgent 响应**:
```csharp
// Confidence 0 ≤ 0.6, RelevantFaqs.Count == 0
return new MafTaskResponse {
    Success = false,
    Result = "抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。",
    Data = new { ShouldEscalate = true },
};
```

**Step 6 — MainAgent 追加提示**:
```
抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。

💡 您也可以说「提交工单」，我们会安排专属客服为您解决。
```

### 示例 B: 命中 FAQ 的版本（推荐演示用）

```
用户: "如何申请退款？"
```

**Step 1 — 意图识别**:
- `RequestRefund`: "退款" YES, "申请退款" YES → score = 2/5 = 0.4
- `PrimaryIntent = "RequestRefund"`, `Confidence = 0.4`

**Step 2 — 路由**: `intent.PrimaryIntent is "RequestRefund"` → **OrderAgent**

> 注意: 此查询实际路由到 OrderAgent（因为含"退款"关键词），不走知识库。
> 如需演示知识库查询，应使用不含订单/退款关键词的问题。

### 示例 C: 正确触发知识库且 FAQ 命中（推荐演示用）

```
用户: "产品有质量问题怎么处理？"
```

**Step 1 — 意图识别**:
- `ProductQuery`: "产品" YES → score = 1/7 ≈ 0.143
- `PrimaryIntent = "ProductQuery"`, `Confidence ≈ 0.143`

**Step 2 — 路由**: `"ProductQuery"` 不在订单/工单 intent 列表中
- `ContainsAny(["订单", "快递", "物流", "退款", "退货"])` → false
- `ContainsAny(["投诉", "工单", "反馈", "建议", "举报", "人工"])` → false
- **→ 默认路由到 `KnowledgeBaseAgent`** ✓

**Step 3 — 知识库检索** (`SimulatedKnowledgeBaseService.SearchAsync(topK: 3)`):
- FAQ-005: "质量" YES (+0.3), "质量问题" YES (+0.3), "有问题" YES (+0.3) → Relevance = 0.9
- `Confidence = 0.9 > 0.7` → `GeneratedAnswer = FAQ-005.Answer`

**Step 4 — KnowledgeBaseAgent 响应**:
```csharp
// Confidence 0.9 > 0.6, GeneratedAnswer 有值
return new MafTaskResponse {
    Success = true,
    Result = "如果您收到的商品存在质量问题：\n1. 拍摄照片或视频记录问题\n2. 联系我们提交工单\n3. 我们会在24小时内给您答复\n\n质量问题在7天内可申请退款或换货。",
    Data = new { Sources = [], Confidence = 0.9 },
};
```

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

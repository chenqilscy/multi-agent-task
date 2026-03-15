# CS-INITIAL-005 知识库无匹配答案

---
metadata:
  case_id: CS-INITIAL-005
  journey: 初次咨询
  journey_order: 5
  case_type: exception
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [intent-recognition, knowledge-query, fallback-handling]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: true }
  demo_value: 3
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 25
  depends_on: [CS-INITIAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-INITIAL-005
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

知识库无法匹配用户问题时，提供降级方案（建议转人工或记录问题）。

## 📝 执行流程

**用户输入**: "你们能不能帮我安装软件？"

**系统响应**（知识库无匹配）:
```
抱歉，关于"安装软件"的问题暂时不在我们的服务范围内 😅

为您推荐以下方案：
  1️⃣ 转接人工客服（等待约2分钟）
  2️⃣ 留下联系方式，稍后回复
  3️⃣ 查看帮助中心：help.example.com

请选择或描述其他问题。
```

## ✅ 预期结果

- [x] 知识库无匹配时不编造答案
- [x] 提供降级方案选择
- [x] 语气友好不生硬
- [x] 引导到其他服务渠道

## 🧪 测试要点

- [ ] KnowledgeBaseAgent返回空结果处理
- [ ] 降级方案完整
- [ ] 不产生幻觉回答

## 🔬 详细对话交互示例（与代码对齐）

### 示例: 超出服务范围的问题

```
用户: "你们能不能帮我安装软件？"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- 扫描全部 9 个 intent，"安装软件" 不命中任何关键词
- **PrimaryIntent = "Unknown"**, Confidence = 0.0

**Step 2 — 路由** (`RouteToAgentAsync`):
- `"Unknown"` 不在订单/工单 intent 列表
- `ContainsAny(["订单","快递","物流","退款","退货"])` → false
- `ContainsAny(["投诉","工单","反馈","建议","举报","人工"])` → false
- **→ 默认路由到 KnowledgeBaseAgent** ✓

**Step 3 — 知识库检索** (`SimulatedKnowledgeBaseService.SearchAsync("你们能不能帮我安装软件？", topK: 3)`):
- FAQ-001 keywords ["查询","订单","查单","订单状态"]: 0 命中 → score = 0
- FAQ-002 keywords ["退款","退货","退钱","申请退款"]: 0 命中 → score = 0
- FAQ-003 keywords ["取消","取消订单","不要了","撤单"]: 0 命中 → score = 0
- FAQ-004 keywords ["快递","物流","送到哪","到哪了","派送"]: 0 命中 → score = 0
- FAQ-005 keywords ["质量","质量问题","坏了","损坏","不好用","有问题"]: 0 命中 → score = 0
- **全部 score = 0** → `RelevantFaqs = []`, `Confidence = 0`

**Step 4 — KnowledgeBaseAgent 响应**:
```csharp
// Confidence 0 ≤ 0.6 且 RelevantFaqs.Count == 0
return new MafTaskResponse {
    Success = false,
    Result = "抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。",
    Data = new { ShouldEscalate = true },
};
```

**Step 5 — MainAgent 追加提示** (`RouteToAgentAsync` 尾部逻辑):
```csharp
// kbResponse.Success == false
// kbResponse.Data["ShouldEscalate"] == true
kbResponse.Result += "\n\n💡 您也可以说「提交工单」，我们会安排专属客服为您解决。";
```

**最终响应**:
```
抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。

💡 您也可以说「提交工单」，我们会安排专属客服为您解决。
```

> 注意: 文档原始响应中的"转接人工客服"、"留下联系方式"等选项在代码中不存在，
> 代码仅提供"提交工单"的引导文案。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

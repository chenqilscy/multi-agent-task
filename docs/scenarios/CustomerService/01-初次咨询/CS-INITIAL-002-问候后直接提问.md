# CS-INITIAL-002 问候后直接提问

---
metadata:
  case_id: CS-INITIAL-002
  journey: 初次咨询
  journey_order: 2
  case_type: variant
  domain: customerservice
  complexity: L1
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [intent-recognition, knowledge-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: [CS-INITIAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-INITIAL-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户跳过问候直接提问，系统同时完成问候和回答。

## 📝 执行流程

**用户输入**: "你好，请问你们营业时间是几点到几点？"

**系统响应**:
```
您好！很高兴为您服务 😊

🕐 我们的营业时间：
  - 周一至周五：09:00 - 18:00
  - 周六：10:00 - 16:00
  - 周日及法定节假日：休息

📞 客服在线时间：09:00 - 21:00（全年无休）

请问还有其他问题吗？
```

## ✅ 预期结果

- [x] 问候和回答合并在一次响应中
- [x] 知识库正确匹配"营业时间"
- [x] 信息完整清晰

## 🧪 测试要点

- [ ] 复合输入（问候+提问）正确处理
- [ ] 不重复问候

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 你好，请问你们营业时间是几点到几点？ |
| 1 | 🤖 系统 | _(IntentRecognizer: "是什么" → GeneralFaq)_<br>_(情绪检测: Neutral)_<br>_(MainAgent路由: GeneralFaq → KnowledgeBaseAgent)_ |
| 1 | 🤖 系统 | 您好！我们的客服营业时间是每天 9:00-21:00。<br>📚 来源：FAQ-001 |
| 2 | 👤 用户 | 那周末也可以咨询吗？ |
| 2 | 🤖 系统 | _(IntentRecognizer: "吗" → GeneralFaq)_<br>_(KnowledgeBaseAgent: 语义搜索 "周末咨询")_<br>是的，周末也提供在线客服服务，时间为 10:00-18:00。<br>📚 来源：FAQ-002 |

### 代码执行追踪

```
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("你好，请问你们营业时间是几点到几点？")
  ├─ IntentRecognizer: "是什么" → GeneralFaq
  ├─ DetectEmotionLevel() → Neutral (无情绪关键词)
  ├─ 路由: GeneralFaq ≠ Order/Ticket意图 → KnowledgeBaseAgent
  └─ KnowledgeBaseAgent.ExecuteBusinessLogicAsync()
      ├─ 语义搜索 "营业时间" → topK=3
      └─ Confidence > 0.6 → 返回 FAQ 答案
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 问候识别 | 识别"你好"为问候不重复 | IntentRecognizer无问候专用意图，按关键词匹配 |
| 上下文关联 | 追问关联到前一轮营业时间 | DialogStateManager可存储，但KnowledgeBaseAgent每次独立搜索 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

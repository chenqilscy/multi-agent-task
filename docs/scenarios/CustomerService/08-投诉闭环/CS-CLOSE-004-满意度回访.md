# CS-CLOSE-004 满意度回访

---
metadata:
  case_id: CS-CLOSE-004
  journey: 投诉闭环
  journey_order: 4
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [satisfaction-survey, proactive-notification]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: [CS-CLOSE-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-CLOSE-004
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

投诉关闭后主动回访，收集满意度数据和改进建议。

## 📝 执行流程

**触发条件**: 投诉工单关闭后3天

**系统主动推送**:
```
📢 服务回访

您好！3天前您向我们反馈的问题（TK-20260312-020）已处理完毕。

为了提升服务质量，想请您花1分钟做个简单评价：

1️⃣ 处理速度：⭐⭐⭐⭐⭐
2️⃣ 处理结果：⭐⭐⭐⭐⭐
3️⃣ 服务态度：⭐⭐⭐⭐⭐
4️⃣ 整体满意度：⭐⭐⭐⭐⭐

还有什么建议想告诉我们吗？（可选）
```

**用户回复**: "处理速度4分，其他5分，建议退款能快一点"

**系统响应**:
```
感谢您的宝贵反馈！🙏

📊 评价已记录：
  - 处理速度：⭐⭐⭐⭐
  - 处理结果：⭐⭐⭐⭐⭐
  - 服务态度：⭐⭐⭐⭐⭐
  - 整体满意度：⭐⭐⭐⭐⭐

💡 您的建议"退款加速"已记录，我们会持续改进！

感谢您的支持 😊
```

## ✅ 预期结果

- [x] 定时回访触发
- [x] 多维度评价收集
- [x] 开放式建议收集
- [x] 反馈数据入库

## 🧪 测试要点

- [ ] 回访触发时机
- [ ] 评分解析准确
- [ ] 建议文本提取

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(工单关闭后24小时，触发满意度回访)_ |
| 0 | 🤖 系统 | 📊 您的工单 TK-20260312-003 已处理完成！<br>请对本次服务进行评价：<br>⭐⭐⭐⭐⭐ 非常满意<br>⭐⭐⭐⭐ 满意<br>⭐⭐⭐ 一般<br>⭐⭐ 不满意<br>⭐ 非常不满意 |
| 1 | 👤 用户 | 4星，希望处理速度还能再快一点 |
| 1 | 🤖 系统 | _(路由: 无明确关键词 → KnowledgeBaseAgent)_<br>感谢您的评价和宝贵建议！<br>我们会努力提升处理速度，感谢您的支持。 |

### 代码执行追踪

```
系统触发 (满意度回访):
  └─ 当前无满意度回访自动触发机制

Turn 1:
MainAgent.RouteToAgent("4星，希望处理速度还能再快一点")
  ├─ DetectEmotionLevel: 无情绪关键词 → Neutral
  └─ 路由: KnowledgeBaseAgent (default)
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 满意度调查 | 自动发送评价表并收集 | 无满意度回访自动触发 |
| 建议提取 | NLP提取建议关键词 | 无建议文本分析功能 |
| 评分统计 | 聚合统计CSAT分数 | 无评分存储和统计 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

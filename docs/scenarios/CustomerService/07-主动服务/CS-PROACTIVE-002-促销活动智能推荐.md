# CS-PROACTIVE-002 促销活动智能推荐

---
metadata:
  case_id: CS-PROACTIVE-002
  journey: 主动服务
  journey_order: 2
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [user-profiling, recommendation, knowledge-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 15
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-PROACTIVE-002
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

基于用户行为数据，智能推荐相关促销活动。

## 📝 执行流程

**触发条件**: 用户浏览商品后进入客服

**系统主动推送**:
```
💡 温馨提示：

我注意到您最近在浏览智能家居产品，
目前有一个优惠活动可能对您有帮助：

🎉 春季智能家居节
  - 智能音箱 → 直降50元
  - 智能灯泡 → 买3送1
  - 全场满500减80

⏰ 活动时间：2026-03-15 ~ 2026-03-20

需要了解更多活动详情吗？
```

## ✅ 预期结果

- [x] 基于浏览记录推荐
- [x] 活动信息准确
- [x] 推荐不过度打扰

## 🧪 测试要点

- [ ] 用户行为分析
- [ ] KnowledgeBaseAgent活动查询
- [ ] 推荐频率控制

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(用户浏览无线耳机品类后，系统触发推荐事件)_ |
| 0 | 🤖 系统 | 🎁 您好！注意到您最近在浏览无线耳机，当前有以下优惠：<br>🎧 品牌 A 降价 30%<br>🎧 品牌 B 买一送一<br>需要了解详情吗？ |
| 1 | 👤 用户 | 品牌A耳机怎么样？ |
| 1 | 🤖 系统 | _(IntentRecognizer: "产品" → ProductQuery)_<br>_(路由: KnowledgeBaseAgent)_<br>品牌A 无线耳机参数：<br>- 降噪: ANC主动降噪<br>- 续航: 30小时<br>- 原价: ¥899，活动价: ¥629 |

### 代码执行追踪

```
系统触发 (无对应EventHandler):
  └─ 当前无“促销推荐”类型的EventHandler

Turn 1:
KnowledgeBaseAgent: 语义搜索 "品牌A耳机"
  └─ RAG topK=3 → 产品信息
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 浏览行为触发 | 根据浏览历史自动推荐 | EventBus无"促销推荐"类型Handler |
| 频率控制 | 限制推荐频次避免骚扰 | 无推荐频率控制机制 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

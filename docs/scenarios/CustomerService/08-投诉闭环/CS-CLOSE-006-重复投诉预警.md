# CS-CLOSE-006 重复投诉预警

---
metadata:
  case_id: CS-CLOSE-006
  journey: 投诉闭环
  journey_order: 6
  case_type: exception
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent]
  capabilities: [duplicate-detection, escalation, ticket-correlation]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: true }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 30
  depends_on: [CS-CLOSE-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-CLOSE-006
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

同一用户对同一问题重复投诉，系统自动检测并升级处理。

## 📝 执行流程

**用户输入**: "我上次投诉的退款问题还是没有解决！都第三次了！"

**系统响应**（检测到重复投诉）:
```
⚠️ 检测到重复投诉

📋 历史投诉记录：
  1. TK-20260308-005（03-08）退款延迟 → 已关闭
  2. TK-20260311-012（03-11）退款仍未到 → 已关闭
  3. 本次（03-15）第三次投诉

🔴 已触发重复投诉升级机制：

  1. ✅ 优先级提升至"最高"
  2. ✅ 直接升级至主管处理
  3. ✅ 关联历史工单（完整上下文）
  4. 📞 主管将在15分钟内亲自联系您

非常抱歉问题至今未解决。
作为补偿，已为您发放50元无门槛优惠券。

我们会确保此次彻底解决您的问题。
```

## ✅ 预期结果

- [x] 重复投诉自动检测
- [x] 历史工单关联展示
- [x] 自动升级至主管
- [x] 补偿方案提供

## 🧪 测试要点

- [ ] 重复投诉检测算法（用户+主题+时间窗口）
- [ ] 历史工单关联查询
- [ ] 自动升级触发（第N次预警）
- [ ] 补偿方案自动化

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 退款问题我已经第三次投诉了，还是没解决！ |
| 1 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket; "退款" → RequestRefund)_<br>_(DetectEmotionLevel: 无关键词 → Neutral)_<br>_(路由: 输入包含"投诉" → TicketAgent)_ |
| 1 | 🤖 系统 | _(TicketAgent: 工单创建分支)_<br>_(DetectPriority: 无紧急关键词 → "normal")_<br>📝 投诉工单已创建！<br>工单号: TK-20260315-011<br>类别: refund<br>优先级: 普通 |
| 2 | 👤 用户 | 我已经投诉过两次了，怎么还是普通优先级？我太不满意了！ |
| 2 | 🤖 系统 | _(DetectEmotionLevel: "不满意" → Frustrated)_<br>很抱歉让您感到失望，我理解您的感受。我会尽力帮您解决问题。<br><br>_(路由: 输入包含"投诉" → TicketAgent)_<br>我们已将您的工单升级处理。 |

### 代码执行追踪

```
Turn 1:
MainAgent.ExecuteBusinessLogicAsync("退款问题我已经第三次投诉了，还是没解决！")
  ├─ DetectEmotionLevel: 无情绪关键词匹配 → Neutral
  ├─ 路由: "投诉" → TicketAgent
  └─ TicketAgent: DetectPriority → "normal" (无重复检测)

Turn 2:
MainAgent.ExecuteBusinessLogicAsync("我已经投诉过两次了...")
  ├─ DetectEmotionLevel: "不满意" → Frustrated
  ├─ 情绪前缀: "很抱歉让您感到失望..."
  └─ 路由: TicketAgent
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 重复检测 | 检测同一用户多次投诉自动升级 | 无历史工单关联，每次创建新工单 |
| 自动升级 | 重复投诉自动提升优先级 | DetectPriority无历史工单感知 |
| 补偿自动化 | 自动发放补偿方案 | 无自动补偿机制 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

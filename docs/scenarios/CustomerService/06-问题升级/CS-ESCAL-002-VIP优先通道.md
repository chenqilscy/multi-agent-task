# CS-ESCAL-002 VIP优先通道

---
metadata:
  case_id: CS-ESCAL-002
  journey: 问题升级
  journey_order: 2
  case_type: variant
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [user-identification, vip-routing, escalation]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: [CS-ESCAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ESCAL-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

VIP用户享受优先转接通道，缩短等待时间。

## 📝 执行流程

**用户输入**: "我是你们的金卡会员，我要找经理投诉"

**系统响应**（识别VIP身份）:
```
🌟 尊敬的金卡会员，您好！

已为您开通VIP优先通道：
  - 会员等级：🥇 金卡
  - 专属通道：VIP热线
  - 预计等待：<30秒

📋 已创建优先工单：TK-20260315-008
  优先级：🔴 最高（VIP加权）

📞 正在为您转接VIP专属客服经理...
```

## ✅ 预期结果

- [x] VIP身份自动识别
- [x] 优先通道路由
- [x] 等待时间大幅缩短
- [x] 专属客服分配

## 🧪 测试要点

- [ ] VIP身份检测
- [ ] 优先级加权计算
- [ ] VIP路由通道

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我是你们的金卡会员，我要找经理投诉 |
| 1 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(DetectEmotionLevel: 无情绪关键词 → Neutral)_<br>_(路由: 输入包含"投诉" → TicketAgent)_ |
| 1 | 🤖 系统 | 📝 投诉工单已创建！<br>工单号: TK-20260315-006<br>类别: other<br>优先级: 普通<br>24小时内处理 |
| 2 | 👤 用户 | 我是VIP，能不能优先处理？ |
| 2 | 🤖 系统 | _(路由: 无VIP关键词匹配 → KnowledgeBaseAgent)_<br>我们的VIP会员享有优先处理权益，您的工单将加急处理。 |

### 代码执行追踪

```
Turn 1:
TicketAgent.ExecuteBusinessLogicAsync()
  ├─ 非查询关键词 → 工单创建分支
  ├─ DetectPriority("我是你们的金卡会员，我要找经理投诉")
  │   └─ 无紧急关键词 → "normal"
  └─ 创建工单: category="other", priority="normal"
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| VIP识别 | 自动识别用户VIP等级 | 无用户等级识别，所有用户同等处理 |
| 优先通道 | VIP自动走优先处理队列 | DetectPriority仅基于关键词，不考虑用户等级 |
| 转人工 | 自动连接VIP专属客服 | 无人工转接功能 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

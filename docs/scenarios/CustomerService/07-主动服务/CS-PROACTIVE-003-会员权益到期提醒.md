# CS-PROACTIVE-003 会员权益到期提醒

---
metadata:
  case_id: CS-PROACTIVE-003
  journey: 主动服务
  journey_order: 3
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, KnowledgeBaseAgent]
  capabilities: [membership-monitoring, proactive-notification]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 15
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-PROACTIVE-003
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

会员权益即将到期，系统主动提醒并提供续费方案。

## 📝 执行流程

**触发条件**: 会员到期前7天

**系统主动推送**:
```
📢 会员权益提醒

尊敬的金卡会员，您的会员将于7天后到期：

🌟 当前权益：
  - 全场95折
  - 免费极速配送
  - 专属客服通道
  - 每月优惠券包

⏰ 到期时间：2026-03-22

💡 续费方案：
  - 年卡：¥199/年（原价¥299，当前7折优惠）
  - 月卡：¥29/月

需要我帮您办理续费吗？
```

## ✅ 预期结果

- [x] 到期前主动提醒
- [x] 当前权益汇总
- [x] 续费优惠方案

## 🧪 测试要点

- [ ] 到期检测定时任务
- [ ] 会员信息查询
- [ ] 续费优惠计算

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(IProactiveEventBus.PublishAsync: type="member_expiry")_<br>_(会员到期前7天，自动发送提醒)_ |
| 0 | 🤖 系统 | _(MemberExpiryEventHandler.HandleAsync())_<br>👑 尊敬的会员，您的金卡会员将于7天后到期。<br>会员权益包括：专属优惠、积分加倍、优先客服。<br>续费享 8 折优惠，是否需要续费？ |
| 1 | 👤 用户 | 怎么续费？ |
| 1 | 🤖 系统 | _(路由: 无明确关键词 → KnowledgeBaseAgent)_<br>您可以通过 “我的-会员中心-续费” 进行续费，<br>支持支付宝、微信支付。 |

### 代码执行追踪

```
系统触发:
InMemoryProactiveEventBus.PublishAsync(event: {type="member_expiry", data={userId, expiryDate}})
  ├─ 查找 handlers → MemberExpiryEventHandler
  └─ MemberExpiryEventHandler.HandleAsync(event)
       └─ 返回 ProactiveNotification {Title="会员到期提醒", Message, Actions=["renew","details"]}

用户响应:
KnowledgeBaseAgent: RAG搜索"续费" → 返回续费流程说明
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 到期提醒 | 自动监控会员到期触发 | EventBus已实现Handler，但无定时监控触发源 |
| 续费优惠 | 自动计算个性化优惠 | Handler返回固定优惠信息 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

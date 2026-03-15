# CS-PROACTIVE-004 生日祝福和优惠券

---
metadata:
  case_id: CS-PROACTIVE-004
  journey: 主动服务
  journey_order: 4
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent]
  capabilities: [user-profiling, proactive-notification, coupon-distribution]
  coverage: { single-agent: true, multi-agent: false, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 15
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-PROACTIVE-004
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户生日当天发送祝福和专属优惠券，增强用户黏性。

## 📝 执行流程

**触发条件**: 用户生日当天

**系统主动推送**:
```
🎂 生日快乐！

亲爱的用户，今天是您的生日！🎉
祝您生日快乐，幸福安康！

🎁 专属生日礼物：
  - 🎫 满100减30 生日券 × 1
  - 🎫 满200减80 生日券 × 1
  - 🎫 免运费券 × 3

⏰ 有效期：2026-03-15 ~ 2026-03-22（7天）

优惠券已自动发放至您的账户 🎊
```

**用户回复**: "谢谢！"

**系统响应**:
```
不客气！祝您生日愉快！
有任何需要随时找我 😊🎂
```

## ✅ 预期结果

- [x] 生日祝福温暖
- [x] 优惠券自动发放
- [x] 有效期合理

## 🧪 测试要点

- [ ] 生日检测定时任务
- [ ] 优惠券发放接口
- [ ] 不重复发送

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(IProactiveEventBus.PublishAsync: type="birthday")_<br>_(用户生日当天，自动发送祝福)_ |
| 0 | 🤖 系统 | _(BirthdayEventHandler.HandleAsync())_<br>🎂 生日快乐！感谢您一直以来的支持！<br>我们为您准备了一份生日礼物：<br>🎁 50元无门槛优惠券（有效期7天）<br>🌟 双倍积分奔取（当日有效） |
| 1 | 👤 用户 | 谢谢！优惠券怎么用？ |
| 1 | 🤖 系统 | _(路由: 无明确关键词 → KnowledgeBaseAgent)_<br>优惠券已自动发放到您的账户，<br>下单时在支付页面选择优惠券即可使用。 |

### 代码执行追踪

```
系统触发:
InMemoryProactiveEventBus.PublishAsync(event: {type="birthday", data={userId, name}})
  ├─ 查找 handlers → BirthdayEventHandler
  └─ BirthdayEventHandler.HandleAsync(event)
       └─ 返回 ProactiveNotification {Title="生日祝福", Message, Actions=["use_coupon","view_details"]}

用户响应:
KnowledgeBaseAgent: RAG搜索"优惠券使用" → 返回使用说明
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 生日触发 | 自动监控生日触发 | BirthdayHandler已实现，无自动监控源 |
| 去重控制 | 速当日不重复发送 | Handler无去重检查逻辑 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

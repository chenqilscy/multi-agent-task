# CS-PROACTIVE-005 异常交易主动核实

---
metadata:
  case_id: CS-PROACTIVE-005
  journey: 主动服务
  journey_order: 5
  case_type: primary
  domain: customerservice
  complexity: L4
  agents: [CustomerServiceMainAgent, OrderAgent, TicketAgent]
  capabilities: [anomaly-detection, proactive-notification, security-verification]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: true }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 30
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-PROACTIVE-005
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

系统检测到异常交易行为，主动联系用户核实，保障账户安全。

## 📝 执行流程

**触发条件**: 检测到异常交易模式（异地登录+大额订单）

**系统主动推送**:
```
🚨 安全提醒

尊敬的用户，我们检测到您的账户有一笔异常交易：

⚠️ 异常信息：
  - 订单号：ORD-20260315-088
  - 金额：¥3,299.00
  - 下单时间：2026-03-15 03:30
  - 收货地址：与常用地址不同
  - 登录设备：新设备（IP：异地）

🔒 为保护您的账户安全，该订单已暂时冻结。

请确认：
  1️⃣ 是我本人操作（解冻订单）
  2️⃣ 不是我操作（冻结账户+拦截订单）
```

**用户回复**: "不是我操作的！"

**系统响应**:
```
🔒 安全措施已执行：

  1. ✅ 账户已冻结
  2. ✅ 异常订单已拦截取消
  3. ✅ 强制修改密码（短信验证）
  4. 📋 安全工单已创建：TK-20260315-011

📞 安全专员将在10分钟内联系您
请注意接听来电。
```

## ✅ 预期结果

- [x] 异常交易模式检测
- [x] 订单自动冻结
- [x] 用户确认机制
- [x] 否认后快速安全响应

## 🧪 测试要点

- [ ] 异常模式检测算法
- [ ] 冻结/拦截操作安全性
- [ ] 用户确认流程
- [ ] 安全响应时效

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚙️ 系统触发 | _(检测到异常交易: 非常用IP地址登录，妥费30分钟内三笔大额支付)_ |
| 0 | 🤖 系统 | ⚠️ 安全提醒：您的账户检测到异常交易。<br>异常信息：<br>- 非常用登录IP<br>- 30分钟内三笔大额支付<br><br>是否是本人操作？<br>1️⃣ 是我本人<br>2️⃣ 不是我，立即冻结 |
| 1 | 👤 用户 | 不是我的操作，赶快冻结账号！ |
| 1 | 🤖 系统 | _(DetectEmotionLevel: 无情绪关键词 → Neutral)_<br>_(IntentRecognizer: 无匹配)_<br>_(路由: 输入包含"投诉"无匹配 → KnowledgeBaseAgent)_<br>✅ 账户已临时冻结，异常交易已拦截。<br>请您尽快修改密码并联系官方客服进一步处理。 |

### 代码执行追踪

```
系统触发:
  └─ 当前EventBus无"异常交易"类型的EventHandler
     (只有shipping_delay, member_expiry, birthday三种)

用户响应:
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("不是我的操作，赶快冻结账号！")
  ├─ DetectEmotionLevel: 无情绪关键词 → Neutral
  └─ 路由: KnowledgeBaseAgent (default)
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 异常检测 | 实时监控异常交易模式 | EventBus无"异常交易"Handler |
| 账户冻结 | 一键冻结账户 | 无账户冻结API集成 |
| 安全响应 | 5分钟内响应 | 无SLA时效控制 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

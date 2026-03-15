# CS-COMPLAIN-004 情绪安抚和降级处理

---
metadata:
  case_id: CS-COMPLAIN-004
  journey: 投诉建议
  journey_order: 4
  case_type: exception
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent]
  capabilities: [emotion-detection, escalation, ticket-creation]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: true }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 45
  depends_on: [CS-COMPLAIN-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-COMPLAIN-004
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

用户情绪激动时，系统进行情绪安抚，安抚无效时自动升级到人工。

## 📝 执行流程

**第1轮**:
- 用户："你们什么破公司！退款都一个星期了还没到账！骗子！"
- 情绪检测：🔴 强烈负面
- 系统：
```
非常理解您的心情，退款延迟确实让人着急 😔
请您放心，我会立即帮您跟进这个问题。

请提供一下您的订单号，我马上帮您查询退款状态。
```

**第2轮**:
- 用户："ORD-20260305-008！你们就是在拖！"
- 系统：
```
📦 退款状态查询：
  订单：ORD-20260305-008
  退款金额：¥599.00
  申请时间：2026-03-08
  当前状态：退款处理中（已超出正常时效）

⚠️ 确认退款超时，已紧急处理：
  1. ✅ 已标记为加急退款
  2. ✅ 已创建紧急工单：TK-20260315-004
  3. 📞 正在为您转接人工客服...

人工客服将在30秒内接入，请稍候。
给您带来的不便深表歉意！
```

## ✅ 预期结果

- [x] 强烈负面情绪检测
- [x] 安抚语言得体
- [x] 超时问题自动加急
- [x] 自动升级到人工客服

## 🧪 测试要点

- [ ] 情绪强度分级检测
- [ ] 安抚语言不激化矛盾
- [ ] 自动升级触发条件
- [ ] 人工转接流程

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - 情绪检测功能 **未实现**，当前代码无 emotion-detection 模块
> - 自动升级到人工客服 **未实现**
> - 工单编号格式: 代码生成 `TKT-yyyyMMdd-NNN`（非文档的 `TK-`）
> - 用户提及的订单 `ORD-20260305-008` 在 mock 数据中不存在

### 示例: 第1轮 — 愤怒投诉

```
用户: "你们什么破公司！退款都一个星期了还没到账！骗子！"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `RequestRefund`: "退款" YES → score = 1/5 = 0.2
- `CreateTicket`: "投诉" 不在输入中（"破公司"≠"投诉"）→ score = 0
- **PrimaryIntent = "RequestRefund"**, Confidence = 0.2

**Step 2 — 路由** (`RouteToAgentAsync`):
- `intent.PrimaryIntent is "RequestRefund"` → YES → **OrderAgent**

> ⚠️ 注意: 根据代码逻辑，此投诉被路由到 OrderAgent（因为含"退款"），而非 TicketAgent。
> 文档预期的"情绪检测→安抚→升级"流程在当前代码中不存在。

**Step 3 — OrderAgent 处理**:
- `userInput.Contains("退款")` → YES → `HandleRefundAsync(null, ...)`
- `ExtractOrderId`: 无 ORD- 格式 → `orderId = null`

**Step 4 — 实际响应**:
```csharp
return new MafTaskResponse {
    Success = false,
    NeedsClarification = true,
    ClarificationQuestion = "请提供您要申请退款的订单号。",
    Result = "请提供订单号。",
};
```

```
请提供订单号。
```

### 示例: 第2轮 — 提供不存在的订单号

```
用户: "ORD-2024-001！你们就是在拖！"
```

- `ExtractOrderId`: 正则匹配 "ORD-2024-001"
- `userInput.Contains("退款")` NO, `Contains("退货")` NO, `Contains("退钱")` NO
- `Contains("取消")` NO, `Contains("查询")` NO, `Contains("快递")` NO
- 兜底: `HandleOrderQueryAsync("ORD-2024-001", ...)`

**查询结果**: 订单存在，status = "shipped"
```
📦 订单 ORD-2024-001
• 状态：已发货，运输中
• 商品：无线蓝牙耳机
• 金额：¥299.00
• 下单时间：2026-03-12 ...
```

> ⚠️ 代码仅返回订单信息，无法处理"退款到账查询"。
> 实现此用例需要: 1) 情绪检测模块 2) 退款状态查询 API 3) 升级到人工的工作流。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

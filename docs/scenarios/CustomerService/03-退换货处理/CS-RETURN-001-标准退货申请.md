# CS-RETURN-001 标准退货申请

---
metadata:
  case_id: CS-RETURN-001
  journey: 退换货处理
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, OrderAgent, KnowledgeBaseAgent]
  capabilities: [intent-recognition, order-query, return-processing, policy-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 45
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-RETURN-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

完整的退货申请流程：验证订单→检查退货条件→提交申请→确认退款。

## 📝 执行流程

**第1轮**:
- 用户："我要退货，订单号 ORD-20260313-010"
- 意图：return-request, orderId="ORD-20260313-010"
- 系统（OrderAgent验证订单）：
```
📦 已找到订单 ORD-20260313-010：
  - 商品：蓝牙键盘 × 1 — ¥159.00
  - 下单时间：2026-03-13
  - 状态：已签收（2026-03-14）

✅ 该订单在退货期限内（7天内）

请问退货原因是？
  1️⃣ 不想要了/不喜欢
  2️⃣ 商品质量问题
  3️⃣ 商品与描述不符
  4️⃣ 其他原因
```

**第2轮**:
- 用户："质量有问题，按键有时不灵"
- 系统：
```
📋 退货申请已提交：

  订单号：ORD-20260313-010
  退货原因：商品质量问题 - 按键不灵
  退款金额：¥159.00
  退款方式：原路退回

📌 后续步骤：
  1. 审核（1个工作日内）
  2. 审核通过后请寄回商品
  3. 收到商品后3-5个工作日退款

退货申请编号：RET-20260315-001
如有问题请随时联系 😊
```

## ✅ 预期结果

- [x] 订单验证通过
- [x] 退货条件检查（时限内）
- [x] 退货原因收集
- [x] 申请提交成功
- [x] 后续步骤说明

## 🧪 测试要点

- [ ] OrderAgent订单查询和验证
- [ ] 退货期限自动计算
- [ ] KnowledgeBaseAgent提供退货政策
- [ ] 退货申请状态持久化

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - 退款编号格式: 代码生成 `REF-{Guid前8位}`（非文档的 `RET-`）
> - 退货流程: 代码中 OrderAgent 直接调用 `RequestRefundAsync`，无"退货期限检查"独立逻辑
> - Mock 数据: 仅 `ORD-2024-001`(shipped) 和 `ORD-2024-002`(delivered) 可用

### 示例: 退款申请 ORD-2024-001（shipped 状态）

```
用户: "我要退货，订单号 ORD-2024-001"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `RequestRefund`: "退货" YES → score = 1/5 = 0.2
- `QueryOrder`: "ORD-" YES → score = 1/6 ≈ 0.167
- **PrimaryIntent = "RequestRefund"**, Confidence = 0.2

**Step 2 — 路由** (`RouteToAgentAsync`):
- `intent.PrimaryIntent is "RequestRefund"` → YES → **OrderAgent**

**Step 3 — OrderAgent 处理** (`ExecuteBusinessLogicAsync`):
- `ExtractOrderId`: 正则匹配 "ORD-2024-001" → `orderId = "ORD-2024-001"`
- `userInput.Contains("退货")` → YES → `HandleRefundAsync("ORD-2024-001", ...)`

**Step 4 — 退款申请** (`SimulatedOrderService.RequestRefundAsync`):
```csharp
// 查找订单: ORD-2024-001 exists, TotalAmount = 299.00
// request.Amount = 0 → refundAmount = order.TotalAmount = 299.00
return new RefundResult {
    Success = true,
    RefundId = "REF-a1b2c3d4",       // Guid 前8位，每次不同
    Message = "退款申请已提交，预计3-5个工作日内退回到您的账户",
    RefundAmount = 299.00m,
    EstimatedDays = 5,
};
```

**Step 5 — 实际响应**:
```
✅ 退款申请已提交
• 退款单号：REF-a1b2c3d4
• 退款金额：¥299.00
• 退款申请已提交，预计3-5个工作日内退回到您的账户
```

### 示例: 退款已签收订单 ORD-2024-002

```
用户: "ORD-2024-002 退钱"
```
- 意图: `RequestRefund` ("退钱" YES, "ORD-" 触发 QueryOrder 但分低)
- OrderAgent: "退钱" → `HandleRefundAsync`
- `RequestRefundAsync("ORD-2024-002", ...)`: 订单存在(delivered), TotalAmount=599.00
- **返回成功** (注意: 代码不检查 delivered 状态是否允许退款)

```
✅ 退款申请已提交
• 退款单号：REF-e5f6g7h8
• 退款金额：¥599.00
• 退款申请已提交，预计3-5个工作日内退回到您的账户
```

> ⚠️ 代码中 `RequestRefundAsync` 对任何已存在的订单都返回成功，不检查状态。
> 但 `CancelOrderAsync` 对 `delivered` 状态返回 false。这是一个潜在的业务逻辑差异。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

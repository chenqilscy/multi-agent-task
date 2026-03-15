# CS-RETURN-004 缺少必要信息

---
metadata:
  case_id: CS-RETURN-004
  journey: 退换货处理
  journey_order: 4
  case_type: multi-turn
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [intent-recognition, clarification, order-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 35
  depends_on: [CS-RETURN-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-RETURN-004
- **用例类型**: 多轮对话
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户退货申请缺少必要信息（订单号、原因等），系统逐步引导补充。

## 📝 执行流程

**第1轮**:
- 用户："我要退货"
- 系统：
```
好的，请提供您的订单号以便处理退货。
订单号通常以 ORD- 开头，可在"我的订单"页面查看。
```

**第2轮**:
- 用户："ORD-20260314-020"
- 系统：
```
📦 订单 ORD-20260314-020：
  - 充电宝 10000mAh × 1 — ¥79.00
  - 签收时间：2026-03-15

✅ 在退货期限内。请问退货原因？
  1️⃣ 不想要了
  2️⃣ 质量问题
  3️⃣ 与描述不符
  4️⃣ 其他
```

**第3轮**:
- 用户："不想要了"
- 系统：
```
📋 退货申请已提交：
  退货原因：不想要了
  退款金额：¥79.00（运费需自付）
  退货申请编号：RET-20260315-002

📌 请在3天内将商品寄回。
```

## ✅ 预期结果

- [x] 逐步引导收集必要信息
- [x] 每步反馈清晰
- [x] 信息收集完整后自动提交

## 🧪 测试要点

- [ ] 多轮信息收集流程完整
- [ ] 上下文在多轮间保持
- [ ] 非质量问题运费提示

## 🔬 详细对话交互示例（与代码对齐）

> ⚠️ **代码差异说明**:
> - OrderAgent 无多轮信息收集流程，缺 orderId 时直接返回 `NeedsClarification`
> - 退货原因收集、运费提示等逻辑未实现
> - 退款编号格式为 `REF-{Guid前8位}`（非 `RET-`）

### 第1轮: 用户说"我要退货"（无订单号）

```
用户: "我要退货"
```

**意图识别**: `RequestRefund` → "退货" YES → score = 1/5 = 0.2
**路由**: `intent.PrimaryIntent is "RequestRefund"` → OrderAgent
**OrderAgent**: "退货" → `HandleRefundAsync(null, ...)` → orderId 为空

**实际响应**:
```
请提供订单号。
```
(`NeedsClarification = true`, `ClarificationQuestion = "请提供您要申请退款的订单号。"`)

### 第2轮: 用户提供订单号

```
用户: "ORD-2024-001"
```

**OrderAgent**: `ExtractOrderId` → "ORD-2024-001"
- `userInput` 无 "查询"/"快递"/"取消"/"退款"/"退货"/"退钱" 关键词
- 兜底: `HandleOrderQueryAsync("ORD-2024-001", ...)`

**实际响应**:
```
📦 订单 ORD-2024-001
• 状态：已发货，运输中
• 商品：无线蓝牙耳机
• 金额：¥299.00
• 下单时间：2026-03-12 ...
```

> ⚠️ **问题**: 第2轮用户仅输入订单号，无"退货"关键词，导致 OrderAgent 足底到查询而非退货。
> 多轮对话上下文（"2轮前用户说了要退货"）未传递给 OrderAgent。
> 实现此用例需要: `ConversationManager.InferMissingEntities()` 与意图保持机制。

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

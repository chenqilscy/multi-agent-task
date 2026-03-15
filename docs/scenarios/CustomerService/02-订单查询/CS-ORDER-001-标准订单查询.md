# CS-ORDER-001 标准订单查询

---
metadata:
  case_id: CS-ORDER-001
  journey: 订单查询
  journey_order: 1
  case_type: primary
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [intent-recognition, order-query, entity-extraction]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 20
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ORDER-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

用户提供订单号查询订单状态，系统返回详细订单信息。

## 📝 执行流程

**用户输入**: "帮我查一下订单 ORD-20260315-001"

**意图识别**: order-query, 实体: orderId="ORD-20260315-001"

**系统响应**:
```
📦 订单查询结果：

订单号：ORD-20260315-001
状态：🚚 配送中

📋 订单详情：
  - 商品：智能音箱 Pro × 1
  - 下单时间：2026-03-13 14:30
  - 支付金额：¥299.00

📦 物流信息：
  - 快递公司：顺丰速运
  - 运单号：SF1234567890
  - 当前位置：杭州分拨中心
  - 预计到达：2026-03-16

还需要了解其他信息吗？
```

## ✅ 预期结果

- [x] 订单号实体正确提取
- [x] OrderAgent查询返回完整信息
- [x] 订单状态、物流信息清晰展示

## 🧪 测试要点

- [ ] 订单号格式识别（多种格式）
- [ ] OrderAgent查询响应时间
- [ ] 物流信息展示完整

## 🔬 详细对话交互示例（与代码对齐）

> 以下示例使用 `SimulatedOrderService` 中实际存在的 mock 数据。

### 示例: 查询已发货订单 ORD-2024-001

```
用户: "帮我查一下订单 ORD-2024-001"
```

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `QueryOrder` keywords: ["查询订单", "查一下订单", "订单状态", "我的订单", "查单", "ORD-"]
- "查一下订单" → `userInput.Contains("查一下订单")` = false（输入是"查一下订单 ORD-2024-001"，包含"查一下订单"?  不，原文是"帮我查一下订单"）
- "ORD-" → YES
- score = 1/6 ≈ 0.167
- 同时 `GeneralFaq`: "帮我" YES → score = 1/5 = 0.2

> ⚠️ `GeneralFaq`(0.2) > `QueryOrder`(0.167)，`PrimaryIntent = "GeneralFaq"`

**Step 2 — 路由** (`RouteToAgentAsync`):
- `intent.PrimaryIntent` = "GeneralFaq" → 不在订单/工单 intent 列表
- `ContainsAny(["订单", "快递", "物流", "退款", "退货"])` → "订单" YES → **路由到 OrderAgent** ✓

**Step 3 — 实体提取** (`IntentDrivenEntityExtractor`):
- 识别出 intent → 查找 Provider → 提取实体到 `request.Parameters`

**Step 4 — OrderAgent 处理** (`ExecuteBusinessLogicAsync`):
- `ExtractOrderId`: 正则 `@"ORD-\d{4}-\d{3,}"` 匹配 "ORD-2024-001" → `orderId = "ORD-2024-001"`
- `userInput.Contains("查一下")` → YES → `HandleOrderQueryAsync("ORD-2024-001", ...)`

**Step 5 — 订单查询** (`SimulatedOrderService.GetOrderAsync("ORD-2024-001")`):
```csharp
// Mock 数据返回:
OrderInfo {
    OrderId = "ORD-2024-001",
    UserId = "user-001",
    Status = "shipped",              // → TranslateStatus → "已发货，运输中"
    TotalAmount = 299.00m,
    TrackingNumber = "SF1234567890",
    Items = [{ ProductName = "无线蓝牙耳机", Quantity = 1, UnitPrice = 299.00 }],
    CreatedAt = DateTime.Now.AddDays(-3),
}
```

**Step 6 — 响应组装**:
```
📦 订单 ORD-2024-001
• 状态：已发货，运输中
• 商品：无线蓝牙耳机
• 金额：¥299.00
• 下单时间：2026-03-12 14:30
```

### 可用 Mock 数据速查

| 订单号 | Status | 金额 | 商品 | 快递单号 | 可取消 |
|--------|--------|------|------|---------|--------|
| `ORD-2024-001` | shipped | ¥299.00 | 无线蓝牙耳机 | SF1234567890 | ✅ 是 |
| `ORD-2024-002` | delivered | ¥599.00 | 智能手环 | SF0987654321 | ❌ 否 |

> 文档原示例使用 `ORD-20260315-001` 在 mock 数据中不存在，
> 实际演示会返回 `"未找到订单 ORD-20260315-001，请检查订单号是否正确。"`

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

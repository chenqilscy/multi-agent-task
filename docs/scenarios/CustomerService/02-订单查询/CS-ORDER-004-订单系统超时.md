# CS-ORDER-004 订单系统超时

---
metadata:
  case_id: CS-ORDER-004
  journey: 订单查询
  journey_order: 4
  case_type: exception
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [order-query, error-handling, retry, degradation]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 3
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 25
  depends_on: [CS-ORDER-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ORDER-004
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

订单系统超时或不可用时，展示重试和降级策略。

## 📝 执行流程

**用户输入**: "查订单 ORD-20260315-001"

**系统响应**（订单服务超时）:
```
⏳ 订单系统响应较慢，正在重试...

（3秒后）

抱歉，订单系统暂时繁忙，无法查询 😔

🔄 已为您安排：
  1. 系统恢复后自动查询并推送结果
  2. 您也可以稍后再试

📞 如需紧急处理，可以：
  - 转接人工客服
  - 拨打客服热线 400-XXX-XXXX

给您带来不便深表歉意！
```

## ✅ 预期结果

- [x] 超时后自动重试（至少1次）
- [x] 重试失败后友好告知
- [x] 提供替代方案
- [x] 不暴露技术错误信息

## 🔬 详细对话交互示例（与代码对齐）

**用户输入**: `"查订单 ORD-2024-001"`

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `QueryOrder`: "查一下订单" 不完全匹配, "ORD-" YES → score = 1/6 ≈ 0.167
- `PrimaryIntent = "QueryOrder"`, `Confidence ≈ 0.167`

**Step 2 — 实体提取** (`IntentDrivenEntityExtractor`):
- `OrderIdEntityProvider` 正则 `ORD-\d{4}-\d{3,}` → 提取 `orderId = "ORD-2024-001"`

**Step 3 — 路由** (`RouteToAgentAsync`):
- `intent.PrimaryIntent is "QueryOrder"` → true → **OrderAgent**

**Step 4 — OrderAgent 业务逻辑** (`ExecuteBusinessLogicAsync`):
```csharp
// 提取订单ID
string? orderId = ExtractOrderId(userInput, request.Parameters);
// → orderId = "ORD-2024-001" (from parameters or regex)

// "查" 关键词命中
return await HandleOrderQueryAsync("ORD-2024-001", request, ct);
```

**Step 5 — 模拟超时场景**:
```csharp
// SimulatedOrderService.GetOrderAsync("ORD-2024-001")
// ⚠️ 当前模拟实现无超时机制，直接返回数据
// 超时需通过故障注入实现：
//   方案A: Task.Delay + CancellationToken 超时
//   方案B: 注入 IOrderService 的超时装饰器

// 正常返回:
_orders.TryGetValue("ORD-2024-001", out var order);
// → order = { Status: "shipped", TotalAmount: 299.00, Items: ["无线蓝牙耳机"] }
```

**Step 6 — OrderAgent 响应** (正常情况):
```csharp
return new MafTaskResponse {
    Success = true,
    Result = "📦 订单 ORD-2024-001\n• 状态：已发货，运输中\n• 商品：无线蓝牙耳机\n• 金额：¥299.00\n• 下单时间：...",
    Data = order,
};
```

**Step 7 — 若超时** (需故障注入，当前代码路径):
```csharp
// MainAgent.ExecuteBusinessLogicAsync catch block:
catch (Exception ex) {
    return new MafTaskResponse {
        Success = false,
        Result = "非常抱歉，系统暂时遇到问题，请稍后重试或联系人工客服。",
        Error = ex.Message,
    };
}
```

> ⚠️ **代码差异**: 当前 `SimulatedOrderService` 无超时模拟。超时场景需要：
> 1. 为 `IOrderService` 添加超时装饰器 / 故障注入接口
> 2. 或在测试中通过 `Task.Delay` + `CancellationTokenSource` 模拟
> 3. 重试逻辑尚未在 Agent 层实现，需在 `HandleOrderQueryAsync` 中添加

**系统输出** (超时降级):
```
⏳ 订单系统响应较慢，正在重试...

（3秒后）

抱歉，订单系统暂时繁忙，无法查询 😔

🔄 已为您安排：
  1. 系统恢复后自动查询并推送结果
  2. 您也可以稍后再试

📞 如需紧急处理，可以：
  - 转接人工客服
  - 拨打客服热线 400-XXX-XXXX

给您带来不便深表歉意！
```

## 🧪 测试要点

- [ ] 超时检测和自动重试
- [ ] 重试次数限制（不超过3次）
- [ ] 降级方案有效
- [ ] 错误信息不泄露技术细节

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

# CS-ORDER-003 订单不存在

---
metadata:
  case_id: CS-ORDER-003
  journey: 订单查询
  journey_order: 3
  case_type: exception
  domain: customerservice
  complexity: L1
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [order-query, error-handling]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: true }
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: [CS-ORDER-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ORDER-003
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

订单号不存在时，友好提示并引导重新查询。

## 📝 执行流程

**用户输入**: "查订单 ORD-99999999-999"

**系统响应**:
```
抱歉，未查询到订单 ORD-99999999-999 😔

可能的原因：
  - 订单号输入有误
  - 该订单可能已取消或过期

💡 建议：
  1️⃣ 请核实订单号后重新查询
  2️⃣ 提供手机号查询关联订单
  3️⃣ 转接人工客服协助查询

需要我帮您做什么？
```

## ✅ 预期结果

- [x] 不存在时友好提示
- [x] 提供可能原因
- [x] 给出替代方案

## 🧪 测试要点

- [ ] OrderAgent返回空结果处理
- [ ] 提示信息友好不生硬

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 查订单 ORD-99999999-999 |
| 1 | 🤖 系统 | _(IntentRecognizer: "查询订单"/"ORD-" → QueryOrder)_<br>_(MainAgent路由: QueryOrder → OrderAgent)_<br>_(OrderAgent: 提取订单号 ORD-99999999-999)_<br>_(IOrderService.GetOrderAsync() → null)_ |
| 1 | 🤖 系统 | 抱歉，未找到订单号 ORD-99999999-999。<br>📝 请确认订单号是否正确，或提供其他查询信息。 |
| 2 | 👤 用户 | 那帮我查查 ORD-2026-001 |
| 2 | 🤖 系统 | _(OrderAgent: 提取 ORD-2026-001)_<br>_(IOrderService.GetOrderAsync() → 订单存在)_<br>📦 订单 ORD-2026-001：<br>状态: 已发货<br>商品: 智能手表 x1<br>金额: ¥299.00 |

### 代码执行追踪

```
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("查订单 ORD-99999999-999")
  ├─ IntentRecognizer: "ORD-" → QueryOrder
  ├─ DetectEmotionLevel() → Neutral
  ├─ 路由: QueryOrder → OrderAgent
  └─ OrderAgent.ExecuteBusinessLogicAsync()
      ├─ 提取订单号: Regex "ORD-\d+-\d+" → ORD-99999999-999
      ├─ "查询"/"查" → 订单查询分支
      ├─ IOrderService.GetOrderAsync("ORD-99999999-999") → null
      └─ 返回: "未找到订单"
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 替代查询 | 提供手机号/收件人查询 | OrderAgent仅支持订单号查询 |
| 输入纠错 | 智能纠正订单号格式 | 仅Regex匹配，无模糊纠错 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

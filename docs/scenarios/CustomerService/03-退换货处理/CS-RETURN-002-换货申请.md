# CS-RETURN-002 换货申请

---
metadata:
  case_id: CS-RETURN-002
  journey: 退换货处理
  journey_order: 2
  case_type: variant
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, OrderAgent]
  capabilities: [intent-recognition, order-query, exchange-processing]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 35
  depends_on: [CS-RETURN-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-RETURN-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户申请换货而非退货，需确认换货商品和差价处理。

## 📝 执行流程

**第1轮**:
- 用户："订单 ORD-20260312-008 的衣服尺码不对，能换吗？"
- 系统：
```
📦 订单 ORD-20260312-008：
  - 商品：休闲外套 M码 × 1 — ¥399.00
  - 状态：已签收（2026-03-14）

✅ 在换货期限内。请问想换成什么尺码？
  - 可选：S / L / XL
```

**第2轮**:
- 用户："换L码"
- 系统：
```
📋 换货申请已提交：

  订单号：ORD-20260312-008
  原商品：休闲外套 M码
  换货商品：休闲外套 L码
  差价：¥0.00（同款不同码）

📌 后续步骤：
  1. 审核通过后寄回原商品
  2. 收到后1-2个工作日发出新商品

换货申请编号：EXC-20260315-001
```

## ✅ 预期结果

- [x] 换货意图识别
- [x] 可选项列表展示
- [x] 差价自动计算
- [x] 换货流程提交

## 🧪 测试要点

- [ ] 换货与退货意图区分
- [ ] 库存可选项查询
- [ ] 差价计算逻辑

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 订单 ORD-20260312-008 的衣服尺码不对，能换吗？ |
| 1 | 🤖 系统 | _(IntentRecognizer: "订单"/"ORD-" → QueryOrder + "换" → 订单相关)_<br>_(路由: OrderAgent → 提取订单号 ORD-20260312-008)_<br>_("换"/"换货" → 退款分支)_ |
| 1 | 🤖 系统 | 📦 订单 ORD-20260312-008 信息：<br>商品: 休闲外套 M码<br>状态: 已签收<br><br>换货申请已提交，退款单号: RF-20260315-001<br>请将商品寄回后我们将为您发出新尺码。 |
| 2 | 👤 用户 | 我想换成L码，有差价吗？ |
| 2 | 🤖 系统 | _(KnowledgeBaseAgent: 语义搜索"换货差价")_<br>同款不同尺码换货无差价。如换不同款式，差价将在新订单中多退少补。 |

### 代码执行追踪

```
CustomerServiceMainAgent.ExecuteBusinessLogicAsync("订单 ORD-20260312-008 的衣服尺码不对，能换吗？")
  ├─ IntentRecognizer: "订单"/"ORD-" → QueryOrder
  ├─ 路由: 输入包含"订单" → OrderAgent
  └─ OrderAgent.ExecuteBusinessLogicAsync()
      ├─ 提取订单号: ORD-20260312-008
      ├─ "换" → 退款/退货分支 (关键词"退货"未匹配，实际可能走订单查询分支)
      └─ IOrderService.GetOrderAsync("ORD-20260312-008") → 返回订单信息
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 换货流程 | 专用换货申请流程 | OrderAgent无专用"换货"分支，可能归入退款分支 |
| 差价计算 | 自动计算不同款式差价 | 无差价计算逻辑，需人工处理 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

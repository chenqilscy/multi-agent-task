# CS-RETURN-005 特殊商品退货限制

---
metadata:
  case_id: CS-RETURN-005
  journey: 退换货处理
  journey_order: 5
  case_type: exception
  domain: customerservice
  complexity: L2
  agents: [CustomerServiceMainAgent, OrderAgent, KnowledgeBaseAgent]
  capabilities: [order-query, policy-query, error-handling]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: true }
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 25
  depends_on: [CS-RETURN-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-RETURN-005
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

特殊商品（生鲜、定制品等）不支持退货，系统说明限制并提供替代方案。

## 📝 执行流程

**用户输入**: "我要退订单 ORD-20260314-030 的生鲜水果"

**系统响应**:
```
📦 订单 ORD-20260314-030：
  - 进口车厘子 2kg × 1 — ¥128.00
  - 签收时间：2026-03-15

❌ 抱歉，生鲜类商品不适用无理由退货政策

📋 特殊商品退货限制：
  - 🥬 生鲜食品：不支持无理由退货
  - 🎨 定制商品：不支持退货
  - 💿 已拆封数码产品：不支持退货

💡 如果商品存在以下问题仍可申请：
  1️⃣ 商品变质/损坏（需提供照片）
  2️⃣ 商品与订单不符
  3️⃣ 联系人工客服特殊处理

需要我帮您申请质量问题售后吗？
```

## ✅ 预期结果

- [x] 特殊商品类别正确识别
- [x] 退货限制说明清晰
- [x] 质量问题仍可售后
- [x] 提供替代方案

## 🧪 测试要点

- [ ] 商品类别判断准确
- [ ] KnowledgeBaseAgent返回退货限制政策
- [ ] 替代方案引导完整

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要退订单 ORD-20260314-030 的生鲜水果 |
| 1 | 🤖 系统 | _(IntentRecognizer: "退" → RequestRefund; "ORD-" → QueryOrder)_<br>_(路由: OrderAgent → 退款分支)_ |
| 1 | 🤖 系统 | 抱歉，生鲜水果属于特殊商品，不支持无理由退货。<br><br>💡 如商品存在质量问题，您可以：<br>1. 拍照上传质量问题照片<br>2. 提交工单申请特殊处理 |
| 2 | 👤 用户 | 水果到的时候就坏了一半，我要投诉 |
| 2 | 🤖 系统 | _(IntentRecognizer: "投诉" → CreateTicket)_<br>_(情绪检测: Neutral)_<br>_(路由: CreateTicket → TicketAgent)_<br>📝 投诉工单已创建！<br>工单号: TK-20260315-002<br>类别: product<br>优先级: 普通<br>24小时内处理 |

### 代码执行追踪

```
Turn 1:
OrderAgent: "退"/"退货" → 退款分支
  └─ IOrderService.RequestRefundAsync("ORD-20260314-030")
      └─ 特殊商品限制 → 拒绝退款 + 提供替代方案

Turn 2:
TicketAgent: "投诉" → 工单创建分支
  ├─ DetectPriority("水果到的时候就坏了...") → "normal"
  └─ 创建工单: category="product"
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 特殊商品识别 | 自动识别生鲜/易耗品 | IOrderService返回商品类型，需服务层判断 |
| 照片上传 | 支持拍照上传质量问题 | 无图片上传功能 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

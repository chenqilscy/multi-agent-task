# CS-RETURN-003 超过退货期限

---
metadata:
  case_id: CS-RETURN-003
  journey: 退换货处理
  journey_order: 3
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

- **用例ID**: CS-RETURN-003
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

订单超过退货期限，系统说明原因并提供替代方案。

## 📝 执行流程

**用户输入**: "我要退货，订单 ORD-20260201-005"

**系统响应**（超过7天退货期限）:
```
📦 订单 ORD-20260201-005：
  - 商品：运动手环 × 1 — ¥249.00
  - 签收时间：2026-02-03

❌ 抱歉，该订单已超过7天无理由退货期限（已42天）

💡 替代方案：
  1️⃣ 如商品存在质量问题，可申请售后维修
  2️⃣ 联系人工客服申请特殊处理
  3️⃣ 查看保修政策

需要我帮您安排哪项服务？
```

## ✅ 预期结果

- [x] 退货期限自动计算并判断
- [x] 明确告知超期天数
- [x] 提供替代方案
- [x] 不直接拒绝，语气友好

## 🧪 测试要点

- [ ] 退货期限计算准确
- [ ] 超期原因说明清晰
- [ ] 替代方案有效

## 🔬 详细对话交互示例（与代码对齐）

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要退货，订单 ORD-20260201-005 |
| 1 | 🤖 系统 | _(IntentRecognizer: "退货" → RequestRefund; "订单"/"ORD-" → QueryOrder)_<br>_(路由: 输入包含"退货" → OrderAgent)_<br>_(OrderAgent: "退货" → 退款申请分支)_ |
| 1 | 🤖 系统 | 抱歉，订单 ORD-20260201-005 已超过退货期限（7天）。<br><br>💡 您可以尝试：<br>1. 联系品牌售后申请保修<br>2. 提交工单申请特殊处理 |
| 2 | 👤 用户 | 那帮我提交个工单吧 |
| 2 | 🤖 系统 | _(IntentRecognizer: "提交工单" → CreateTicket)_<br>_(路由: CreateTicket → TicketAgent)_<br>📝 工单已创建！<br>工单号: TK-20260315-001<br>类别: refund<br>优先级: 普通<br>承诺: 24小时内处理 |

### 代码执行追踪

```
Turn 1:
OrderAgent.ExecuteBusinessLogicAsync()
  ├─ 提取订单号: ORD-20260201-005
  ├─ "退货"/"退款" → 退款申请分支
  └─ IOrderService.RequestRefundAsync() → 返回超期提示

Turn 2:
TicketAgent.ExecuteBusinessLogicAsync()
  ├─ 提取标题: "那帮我提交个工单吧" → 截取前50字
  ├─ DetectPriority() → "normal" (无紧急关键词)
  └─ 创建工单 → TK-20260315-001
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 期限检查 | 自动检查订单日期是否超过7天 | OrderAgent退款分支可能直接调用服务，期限检查在服务层 |
| 替代方案 | 自动提供保修/特殊处理建议 | 需手动引导用户提交工单 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

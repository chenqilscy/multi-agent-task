# CS-ESCAL-003 紧急问题快速响应

---
metadata:
  case_id: CS-ESCAL-003
  journey: 问题升级
  journey_order: 3
  case_type: variant
  domain: customerservice
  complexity: L3
  agents: [CustomerServiceMainAgent, TicketAgent, OrderAgent]
  capabilities: [urgency-detection, fast-escalation, order-query]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 20
  depends_on: [CS-ESCAL-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: CS-ESCAL-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

紧急问题（如账户安全、资金异常）跳过常规流程直接快速响应。

## 📝 执行流程

**用户输入**: "我的账户被盗了！有人在用我的账号下单！"

**系统响应**（紧急安全事件）:
```
🚨 紧急安全事件！已启动快速响应：

  ⏱️ 即时处理（已执行）：
  1. 🔒 账户已临时冻结（防止进一步损失）
  2. 📋 紧急工单已创建：TK-20260315-009（最高优先级）

  📦 可疑订单排查：
  - ORD-20260315-050 ⚠️ 可疑（非常用地址，已拦截）
  - ORD-20260315-051 ⚠️ 可疑（已拦截）

  📞 安全专员将在5分钟内联系您
  📱 已发送验证短信至尾号 *1234

请确认：是否需要立即修改密码？
```

## ✅ 预期结果

- [x] 紧急事件即时检测
- [x] 账户自动冻结保护
- [x] 可疑订单自动拦截
- [x] 安全专员快速介入

## 🔬 详细对话交互示例（与代码对齐）

**用户输入**: `"我的账户被盗了！有人在用我的账号下单！"`

**Step 1 — 意图识别** (`RuleBasedIntentRecognizer`):
- `QueryOrder`: "订单" 不命中直接, "下单" 不在关键词中 → score = 0
- `CreateTicket`: "投诉" 不命中, "反馈" 不命中, "举报" 不命中 → score = 0
- `GeneralFaq`: "帮我" 不命中 → score = 0
- 无 intent 命中 → `PrimaryIntent = "Unknown"`, `Confidence = 0.0`

**Step 2 — 实体提取** (`IntentDrivenEntityExtractor`):
- intent="Unknown" → 无对应 ProviderType → 返回空实体列表

**Step 3 — 路由** (`RouteToAgentAsync`):
```csharp
// intent.PrimaryIntent 不在订单/工单 intent 列表中
// ContainsAny(["订单", "快递", "物流", "退款", "退货"]) → false
//   ("下单" ≠ "订单"，不命中)
// ContainsAny(["投诉", "工单", "反馈", "建议", "举报", "人工"]) → false
// → 默认路由到 KnowledgeBaseAgent
```

**Step 4 — KnowledgeBaseAgent 检索** (`SimulatedKnowledgeBaseService.SearchAsync`):
```csharp
// "我的账户被盗了！有人在用我的账号下单！"
// FAQ 关键词匹配:
// FAQ-001: ["查询","订单","查单","订单状态"] → 无命中
// FAQ-002: ["退款","退货","退钱","申请退款"] → 无命中
// FAQ-003: ["取消","取消订单","不要了","撤单"] → 无命中
// FAQ-004: ["快递","物流","送到哪","到哪了","派送"] → 无命中
// FAQ-005: ["质量","质量问题","坏了","损坏","不好用","有问题"] → 无命中
// → Confidence = 0, RelevantFaqs = []
```

**Step 5 — KnowledgeBaseAgent 响应**:
```csharp
return new MafTaskResponse {
    Success = false,
    Result = "抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。",
    Data = new { ShouldEscalate = true },
};
```

**Step 6 — MainAgent 追加升级提示**:
```csharp
// ShouldEscalate = true → 追加提示
kbResponse.Result += "\n\n💡 您也可以说「提交工单」，我们会安排专属客服为您解决。";
```

> ⚠️ **代码差异**（重要）:
> 1. **紧急关键词检测缺失**: 当前代码无 "被盗"/"账户安全" 等紧急关键词识别
> 2. **快速响应通道缺失**: 不存在跳过常规流程的紧急处理逻辑
> 3. **账户冻结能力缺失**: 无 `IAccountService.FreezeAsync()` 接口
> 4. **可疑订单排查缺失**: 无订单异常检测机制
> 5. **实际行为**: 路由到 KnowledgeBaseAgent → 返回"无法解答" + 建议工单
> 6. **改进建议**: 在 `RouteToAgentAsync` 中添加紧急关键词优先检测:
>    ```csharp
>    if (ContainsAny(userInput, ["被盗", "盗号", "安全", "异常登录"]))
>        return await HandleEmergencyAsync(request, ct);
>    ```

**实际系统输出** (当前代码):
```
抱歉，我暂时无法解答您的问题。建议您提交工单，由专属客服为您处理。

💡 您也可以说「提交工单」，我们会安排专属客服为您解决。
```

**理想系统输出** (用例设计目标):
```
🚨 紧急安全事件！已启动快速响应：

  ⏱️ 即时处理（已执行）：
  1. 🔒 账户已临时冻结（防止进一步损失）
  2. 📋 紧急工单已创建：TKT-20260315-009（最高优先级）

  📦 可疑订单排查：
  - ORD-20260315-050 ⚠️ 可疑（非常用地址，已拦截）
  - ORD-20260315-051 ⚠️ 可疑（已拦截）

  📞 安全专员将在5分钟内联系您
  📱 已发送验证短信至尾号 *1234

请确认：是否需要立即修改密码？
```

## 🧪 测试要点

- [ ] 紧急关键词检测（"被盗""安全"）
- [ ] 账户冻结操作
- [ ] 可疑订单排查
- [ ] 快速升级通道

---

**文档版本**: v1.1 | **最后更新**: 2026-03-15

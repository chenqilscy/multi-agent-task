# CustomerService 用例文档与 Demo 代码交叉验证报告

**验证日期**: 2026-03-15  
**验证范围**: 12 个 P0 用例 vs. `src/Demos/CustomerService/` 源码  
**结论**: 发现 **7 类不一致**，涉及 10 个用例文档

---

## 🔴 严重不一致（会导致测试失败或演示异常）

### 1. 工单 ID 格式不匹配

| 项目 | 文档写法 | 代码实际格式 |
|------|---------|------------|
| 工单编号 | `TK-20260315-005` | `TKT-{yyyyMMdd}-{序号:D3}` 如 `TKT-20260315-001` |
| 影响用例 | CS-TICKET-001, CS-COMPLAIN-002, CS-COMPLAIN-004, CS-ESCAL-001 | |

**代码依据** (`SimulatedTicketService.CreateTicketAsync`):
```csharp
var ticketId = $"TKT-{DateTime.Now:yyyyMMdd}-{_tickets.Count + 1:D3}";
```

**修复建议**: 文档中所有 `TK-` 前缀改为 `TKT-`。

---

### 2. 退款单号格式不匹配

| 项目 | 文档写法 | 代码实际格式 |
|------|---------|------------|
| 退货申请编号 | `RET-20260315-001` | `REF-{Guid前8位}` 如 `REF-a1b2c3d4` |
| 影响用例 | CS-RETURN-001, CS-RETURN-004 | |

**代码依据** (`SimulatedOrderService.RequestRefundAsync`):
```csharp
RefundId = $"REF-{Guid.NewGuid():N[..8]}",
```

**修复建议**: 文档退款编号从 `RET-` 前缀改为 `REF-` 前缀。

---

### 3. 意图名称不一致

| 文档意图名 | 代码实际意图名 | 影响用例 |
|-----------|--------------|---------|
| `order-query` | `QueryOrder` | CS-ORDER-001, CS-ORDER-002 |
| `return-request` | `RequestRefund` | CS-RETURN-001, CS-RETURN-004 |
| `knowledge-query` | `GeneralFaq` / `ProductQuery` | CS-INITIAL-001, CS-INITIAL-004 |
| `咨询类` / `complaint` | `CreateTicket` | CS-COMPLAIN-002, CS-COMPLAIN-004 |

**代码依据** (`CustomerServiceIntentKeywordProvider`):
```
QueryOrder, CancelOrder, TrackShipping, RequestRefund,
CreateTicket, QueryTicket, ProductQuery, PaymentQuery, GeneralFaq
```

**修复建议**: 文档统一使用代码定义的意图名。

---

### 4. 订单 ID 与模拟数据不匹配

| 文档使用的订单号 | 模拟数据中存在的订单号 |
|----------------|---------------------|
| `ORD-20260315-001` | ❌ 不存在 |
| `ORD-20260313-010` | ❌ 不存在 |
| `ORD-20260310-015` | ❌ 不存在 |
| `ORD-20260314-020` | ❌ 不存在 |
| `ORD-20260305-008` | ❌ 不存在 |
| — | `ORD-2024-001` ✅ (shipped, ¥299, 无线蓝牙耳机) |
| — | `ORD-2024-002` ✅ (delivered, ¥599, 智能手环) |

**影响**: 所有用例中使用的订单号在 `SimulatedOrderService` 中均**查不到**，演示将全部返回"未找到订单"。

**修复建议**: 对话示例中使用 `ORD-2024-001` / `ORD-2024-002`，或在 `SimulatedOrderService` 中补充更多mock数据。

---

## 🟡 中等不一致（逻辑差异，不影响编译但影响功能预期）

### 5. 路由逻辑覆盖问题

**问题 A**: CS-INITIAL-001 中用户问"退换货政策是怎样的？"

- **文档预期**: 路由到 KnowledgeBaseAgent  
- **代码实际**: "退换货政策" 包含 "退货" → `RequestRefund` intent (score=0.2)；同时 `RouteToAgentAsync` 的关键词判断 `ContainsAny(["退款", "退货"])` 为 true → **路由到 OrderAgent**
- **后果**: OrderAgent 尝试处理退款请求但无订单号，返回 `NeedsClarification: "请提供您要申请退款的订单号"`
- **影响用例**: CS-INITIAL-001

**问题 B**: 手机号查询不支持

- **文档预期** (CS-ORDER-002): 用户提供手机号 → 返回订单列表  
- **代码实际**: `IOrderService` 没有按手机号查询的方法，`GetUserOrdersAsync(string userId)` 只接受 userId
- **影响用例**: CS-ORDER-002

---

### 6. 功能缺失

| 文档描述的功能 | 代码实现状态 | 影响用例 |
|-------------|-----------|---------|
| 情绪检测 (emotion-detection) | ❌ 未实现 | CS-COMPLAIN-004 |
| 敏感问题自动识别 | ❌ 未实现 | CS-ESCAL-001 |
| 订单系统超时重试 | ❌ 模拟服务无超时 | CS-ORDER-004 |
| 自动升级到人工客服 | ❌ 逻辑未实现 | CS-ESCAL-001 |
| 退货期限自动计算 | ❌ 未实现 | CS-RETURN-001, CS-RETURN-004 |

---

## 🟢 轻微不一致

### 7. 响应文案差异

| 用例 | 文档文案 | 代码实际文案模式 |
|------|---------|----------------|
| CS-ORDER-001 查询成功 | 自定义富文本 | `📦 订单 {id}\n• 状态：{status}\n• 商品：{items}\n• 金额：¥{amount}\n• 下单时间：{date}` |
| CS-TICKET-001 创建成功 | 自定义富文本 | `✅ 工单已创建成功\n• 工单编号：{id}\n• 类别：{category}\n• 我们会在24小时内处理您的工单，请耐心等待。` |
| OrderAgent 取消成功 | — | `✅ 订单 {id} 已成功取消。如已付款，退款将在3-5个工作日内到账。` |
| KnowledgeBase 无结果 | 自定义 | `抱歉，我暂时无法解答您的问题。建议您提交工单...` + MainAgent 追加 `💡 您也可以说「提交工单」...` |

---

## 📊 验证总结

| 级别 | 数量 | 说明 |
|------|------|------|
| 🔴 严重 | 4 项 | ID 格式、意图名、mock 数据不匹配 |
| 🟡 中等 | 2 项 | 路由逻辑偏差、功能缺失 |
| 🟢 轻微 | 1 项 | 文案格式差异 |
| **总计** | **7 项** | 影响 10/12 个 P0 用例 |

## ✅ 建议修复优先级

1. **P0 - 立即修复**: 订单 mock 数据补充或文档订单号对齐（否则所有订单相关演示失败）
2. **P0 - 立即修复**: 工单ID前缀 `TK-` → `TKT-`，退款ID `RET-` → `REF-`
3. **P1 - 优先修复**: 意图名对齐（文档中的英文意图名统一为代码中的 PascalCase）  
4. **P1 - 优先修复**: CS-INITIAL-001 路由逻辑——"退换货政策"类查询应先走知识库
5. **P2 - 后续修复**: 补充缺失功能（情绪检测、敏感问题识别、超时重试）

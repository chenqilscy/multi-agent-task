# CKY.MAF Demo 场景用例总索引

> 全部 **74个** 场景用例的完整索引，覆盖 SmartHome（36个）和 CustomerService（38个）两个 Demo 项目。

---

## 📊 统计概览

| 维度 | 分布 |
|------|------|
| **用例总数** | 74 |
| **SmartHome** | 36（8个用户旅程 × 4-5个用例） |
| **CustomerService** | 38（8个用户旅程 × 4-6个用例） |
| **涉及Agent** | 9个（SmartHome 6 + CustomerService 4） |
| **测试优先级** | P0: 24 / P1: 36 / P2: 14 |
| **复杂度** | L1: 0 / L2: 13 / L3: 45 / L4: 16 |
| **文档重要性** | Critical: 12 / High: 26 / Medium: 27 / Low: 9 |

---

## 🏠 SmartHome — 全部 36 个用例

### 01-早晨唤醒（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 1 | SH-MORNING-001 | 标准晨起唤醒 | primary | L3 | P0 | high |
| 2 | SH-MORNING-002 | 工作日晨起模式 | variant | L3 | P1 | medium |
| 3 | SH-MORNING-003 | 周末懒觉模式 | variant | L3 | P1 | medium |
| 4 | SH-MORNING-004 | 天气服务异常 | exception | L3 | P0 | high |
| 5 | SH-MORNING-005 | 设备离线处理 | exception | L3 | P0 | high |

### 02-离家准备（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 6 | SH-DEPART-001 | 标准离家模式 | primary | L3 | P0 | high |
| 7 | SH-DEPART-002 | 远距离出行 | variant | L3 | P1 | medium |
| 8 | SH-DEPART-003 | 设备关闭失败 | exception | L3 | P0 | high |
| 9 | SH-DEPART-004 | 网络不稳定 | exception | L3 | P1 | medium |

### 03-回家放松（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 10 | SH-HOME-001 | 标准回家模式 | primary | L3 | P0 | high |
| 11 | SH-HOME-002 | 夏天回家 | variant | L3 | P1 | medium |
| 12 | SH-HOME-003 | 冬天回家 | variant | L3 | P1 | medium |
| 13 | SH-HOME-004 | 澄清真请求 | variant | L3 | P1 | medium |
| 14 | SH-HOME-005 | 音乐服务故障 | exception | L3 | P0 | high |

### 04-睡眠准备（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 15 | SH-SLEEP-001 | 标准睡眠模式 | primary | L3 | P1 | medium |
| 16 | SH-SLEEP-002 | 工作日早睡模式 | variant | L2 | P2 | low |
| 17 | SH-SLEEP-003 | 周末熬夜模式 | variant | L2 | P2 | low |
| 18 | SH-SLEEP-004 | 灯光调暗失败 | exception | L3 | P1 | medium |

### 05-会客模式（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 19 | SH-GUEST-001 | 标准会客模式 | primary | L3 | P1 | high |
| 20 | SH-GUEST-002 | 派对氛围模式 | variant | L3 | P2 | medium |
| 21 | SH-GUEST-003 | 商务洽谈模式 | variant | L3 | P2 | low |
| 22 | SH-GUEST-004 | 多房间灯光协调 | variant | L3 | P1 | high |

### 06-外出期间（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 23 | SH-AWAY-001 | 开启外出监控 | primary | L3 | P1 | high |
| 24 | SH-AWAY-002 | 远程查询家中状态 | variant | L2 | P2 | medium |
| 25 | SH-AWAY-003 | 模拟有人在家 | variant | L3 | P1 | medium |
| 26 | SH-AWAY-004 | 异常入侵检测响应 | exception | L3 | P0 | critical |

### 07-紧急情况（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 27 | SH-EMERG-001 | 火警烟雾检测响应 | primary | L4 | P0 | critical |
| 28 | SH-EMERG-002 | 燃气泄漏处理 | primary | L4 | P0 | critical |
| 29 | SH-EMERG-003 | 陌生人闯入响应 | primary | L4 | P0 | critical |
| 30 | SH-EMERG-004 | 紧急求助模式 | primary | L4 | P0 | high |
| 31 | SH-EMERG-005 | 多设备联动应急 | variant | L4 | P1 | critical |

### 08-个性化场景（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 32 | SH-PERS-001 | 阅读专注模式 | primary | L3 | P2 | medium |
| 33 | SH-PERS-002 | 电影观影模式 | variant | L3 | P1 | high |
| 34 | SH-PERS-003 | 运动健身模式 | variant | L3 | P2 | low |
| 35 | SH-PERS-004 | 工作学习模式 | variant | L3 | P2 | low |
| 36 | SH-PERS-005 | 聚餐准备模式 | variant | L3 | P1 | high |

---

## 🎧 CustomerService — 全部 38 个用例

### 01-初次咨询（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 37 | CS-INITIAL-001 | 标准咨询流程 | primary | L2 | P0 | critical |
| 38 | CS-INITIAL-002 | 问候后直接提问 | variant | L2 | P1 | medium |
| 39 | CS-INITIAL-003 | 多个连续问题 | variant | L2 | P1 | medium |
| 40 | CS-INITIAL-004 | 问题不明确 | exception | L2 | P0 | high |
| 41 | CS-INITIAL-005 | 知识库无匹配答案 | exception | L2 | P0 | high |

### 02-订单查询（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 42 | CS-ORDER-001 | 标准订单查询 | primary | L2 | P0 | critical |
| 43 | CS-ORDER-002 | 模糊查询 | variant | L2 | P0 | high |
| 44 | CS-ORDER-003 | 订单不存在 | exception | L2 | P1 | medium |
| 45 | CS-ORDER-004 | 订单系统超时 | exception | L2 | P0 | high |

### 03-退换货处理（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 46 | CS-RETURN-001 | 标准退货申请 | primary | L3 | P0 | critical |
| 47 | CS-RETURN-002 | 换货申请 | variant | L3 | P1 | medium |
| 48 | CS-RETURN-003 | 超过退货期限 | exception | L3 | P1 | medium |
| 49 | CS-RETURN-004 | 缺少必要信息 | exception | L3 | P0 | high |
| 50 | CS-RETURN-005 | 特殊商品退货限制 | boundary | L3 | P1 | medium |

### 04-投诉建议（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 51 | CS-COMPLAIN-001 | 标准投诉处理 | primary | L3 | P1 | high |
| 52 | CS-COMPLAIN-002 | 产品缺陷投诉 | variant | L3 | P0 | high |
| 53 | CS-COMPLAIN-003 | 物流延误投诉 | variant | L3 | P1 | medium |
| 54 | CS-COMPLAIN-004 | 情绪安抚和降级处理 | exception | L3 | P0 | critical |

### 05-工单跟进（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 55 | CS-TICKET-001 | 提交新工单 | primary | L2 | P1 | high |
| 56 | CS-TICKET-002 | 查询工单状态 | primary | L2 | P1 | medium |
| 57 | CS-TICKET-003 | 补充工单信息 | variant | L2 | P2 | low |
| 58 | CS-TICKET-004 | 工单分配和进度查询 | variant | L3 | P1 | high |
| 59 | CS-TICKET-005 | 工单处理超时处理 | exception | L3 | P1 | medium |

### 06-问题升级（4个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 60 | CS-ESCAL-001 | 自动升级到人工客服 | primary | L3 | P0 | high |
| 61 | CS-ESCAL-002 | VIP优先通道 | variant | L3 | P1 | medium |
| 62 | CS-ESCAL-003 | 紧急问题快速响应 | exception | L3 | P0 | critical |
| 63 | CS-ESCAL-004 | 跨部门协作转接 | variant | L3 | P1 | medium |

### 07-主动服务（5个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 64 | CS-PROACTIVE-001 | 发货延迟主动通知 | primary | L3 | P1 | high |
| 65 | CS-PROACTIVE-002 | 促销活动智能推荐 | variant | L3 | P2 | low |
| 66 | CS-PROACTIVE-003 | 会员权益到期提醒 | variant | L2 | P2 | low |
| 67 | CS-PROACTIVE-004 | 生日祝福和优惠券 | variant | L2 | P1 | medium |
| 68 | CS-PROACTIVE-005 | 异常交易主动核实 | exception | L3 | P2 | critical |

### 08-投诉闭环（6个）

| # | 用例ID | 用例名称 | 类型 | 复杂度 | 优先级 | 文档重要性 |
|---|--------|----------|------|--------|--------|-----------|
| 69 | CS-CLOSE-001 | 标准投诉处理闭环 | primary | L4 | P1 | critical |
| 70 | CS-CLOSE-002 | 多部门协调处理 | variant | L4 | P1 | high |
| 71 | CS-CLOSE-003 | 处理结果用户确认 | primary | L3 | P1 | medium |
| 72 | CS-CLOSE-004 | 满意度回访 | variant | L3 | P1 | medium |
| 73 | CS-CLOSE-005 | 投诉数据分析和优化 | variant | L3 | P2 | low |
| 74 | CS-CLOSE-006 | 重复投诉预警 | exception | L3 | P2 | high |

---

## 🔗 多维索引导航

| 索引维度 | 链接 | 说明 |
|----------|------|------|
| 按用户旅程 | [SmartHome](SmartHome/README.md) / [CustomerService](CustomerService/README.md) | 按业务流程浏览 |
| 按Agent | [by-agent](by-agent/README.md) | 查看Agent职责和相关用例 |
| 按复杂度 | [by-complexity](by-complexity/README.md) | L1→L4 循序渐进 |
| 按测试优先级 | [by-test-priority](by-test-priority/README.md) | P0/P1/P2 分级 |
| 按演示价值 | [by-demo-value](by-demo-value/README.md) | 5星→3星评级 |
| 按文档重要性 | [by-doc-importance](by-doc-importance/README.md) | Critical/High/Medium/Low |

---

**最后更新**: 2026-03-15

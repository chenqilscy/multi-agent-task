# 场景覆盖度提升执行计划

> **目标**: 将 SmartHome 和 CustomerService 两个 Demo 的场景覆盖度从当前水平提升至 ≥75%
> **创建时间**: 2026-03-22
> **状态**: ✅ 全部完成

---

## 📊 当前覆盖度现状

### SmartHome (36个用例)

| 旅程 | 用例数 | 当前覆盖 | 目标 | 状态 |
|------|--------|---------|------|------|
| SH-01 早晨唤醒 | 5 | ~80% | 90% | ✅ 良好 |
| SH-02 离家准备 | 4 | ~75% | 85% | ✅ 良好 |
| SH-03 回家放松 | 5 | ~70% | 85% | ⚠️ 需补充 |
| SH-04 睡眠准备 | 4 | ≤50% | 80% | 🔴 重点 |
| SH-05 会客模式 | 4 | ≤50% | 75% | 🔴 重点 |
| SH-06 外出期间 | 4 | ~60% | 80% | ⚠️ 需补充 |
| SH-07 紧急情况 | 5 | ~65% | 85% | ⚠️ 需补充 |
| SH-08 个性化场景 | 5 | ≤50% | 75% | 🔴 重点 |

### CustomerService (38个用例)

| 旅程 | 用例数 | 当前覆盖 | 目标 | 状态 |
|------|--------|---------|------|------|
| CS-01 初次咨询 | 5 | ~70% | 85% | ⚠️ 需补充 |
| CS-02 订单查询 | 4 | ~75% | 90% | ✅ 良好 |
| CS-03 退换货处理 | 5 | ≤25% | 75% | 🔴 重点 |
| CS-04 投诉建议 | 4 | ≤25% | 75% | 🔴 重点 |
| CS-05 工单跟进 | 5 | ~60% | 80% | ⚠️ 需补充 |
| CS-06 问题升级 | 4 | ≤25% | 75% | 🔴 重点 |
| CS-07 主动服务 | 5 | ≤25% | 70% | 🔴 重点 |
| CS-08 投诉闭环 | 6 | ~40% | 70% | ⚠️ 需补充 |

---

## 🔧 Phase 1: SmartHome 场景模式补充 (SH-04/05/08)

### 1.1 添加场景模式意图

**文件**: `SmartHomeIntentKeywordProvider.cs`

新增意图:
- `SleepMode` → 关键词: 睡眠, 睡觉, 晚安, 入睡, 夜间模式
- `GuestMode` → 关键词: 会客, 来客, 朋友来, 派对, 聚会, 商务
- `ReadingMode` → 关键词: 阅读, 看书, 读书, 专注
- `MovieMode` → 关键词: 看电影, 观影, 电影模式
- `ExerciseMode` → 关键词: 健身, 运动, 跑步, 锻炼
- `WorkMode` → 关键词: 工作, 学习, 办公, 专注模式
- `DinnerMode` → 关键词: 聚餐, 晚餐, 用餐, 吃饭

### 1.2 添加场景模式路由

**文件**: `SmartHomeControlService.cs`

新增场景模式路由分支, 每个场景模式调用多个 Agent 的组合操作：
- 睡眠模式: 灯光调暗 → 空调适温 → 停止音乐 → 锁门
- 会客模式: 灯光调亮+暖色 → 空调舒适温度 → 播放背景音乐
- 个性化场景: 阅读/电影/运动/工作/聚餐各有不同组合

### 1.3 编写测试

**新增测试覆盖**:
- SH-SLEEP-001 ~ 004: 睡眠模式意图识别、多Agent联动、异常处理
- SH-GUEST-001 ~ 004: 会客模式各变体
- SH-PERS-001 ~ 005: 5个个性化场景的路由和联动

---

## 🔧 Phase 2: CustomerService 接口补充 (CS-03/04/06/07)

### 2.1 退换货补充 (CS-03)

**接口修改**: `ICustomerServiceInterfaces.cs`
- `IOrderService` 新增: `RequestExchangeAsync` (换货申请)
- `IOrderService` 新增: `CheckReturnEligibilityAsync` (退货资格校验)

**Agent修改**: `OrderAgent` 在 `CustomerServiceAgents.cs`
- 新增 `HandleExchangeAsync` 方法
- `HandleRefundAsync` 增加退货期限校验

### 2.2 投诉建议补充 (CS-04)

**Agent修改**: `CustomerServiceMainAgent.cs`
- 情绪 Angry 时自动升级到工单 + 标记紧急
- 投诉流程: OrderAgent + TicketAgent 协作

### 2.3 问题升级补充 (CS-06)

**接口新增**: `IEscalationService`
- `EscalateToHumanAsync`: 升级到人工客服
- `GetVipLevelAsync`: VIP等级识别
- `TransferToDepartmentAsync`: 跨部门转接

### 2.4 主动服务补充 (CS-07)

**Handler补充**:
- `PromotionRecommendationEventHandler`: 促销推荐
- `AnomalousTransactionEventHandler`: 异常交易核实
- `ProactiveEventType` 枚举新增: `AnomalousTransaction`

### 2.5 编写测试

- CS-RETURN-001 ~ 005: 退换货全流程
- CS-COMPLAIN-001 ~ 004: 投诉处理含情绪升级
- CS-ESCAL-001 ~ 004: 问题升级全路径
- CS-PROACTIVE-001 ~ 005: 主动服务各事件处理

---

## 📋 实施优先级

| 优先级 | 任务 | 预计影响 |
|--------|------|---------|
| P0 | SmartHome 场景模式意图 + 路由 | SH-04/05/08 覆盖度翻倍 |
| P0 | SmartHome SH-04/08 测试 | 验证场景模式正确性 |
| P1 | CS 退换货接口和实现 | CS-03 覆盖度 25%→75% |
| P1 | CS 问题升级接口 | CS-06 覆盖度 25%→75% |
| P2 | CS 主动服务 Handler | CS-07 覆盖度 25%→70% |
| P2 | CS 投诉建议流程优化 | CS-04 覆盖度 25%→75% |

---

## ✅ 完成标准

- [x] SmartHome 新增 7 个场景模式意图 ✅ (SleepMode/GuestMode/ReadingMode/MovieMode/ExerciseMode/WorkMode/DinnerMode)
- [x] SmartHomeControlService 新增场景模式路由 ✅ (7个场景模式 + ExecuteSceneModeAsync 多Agent编排)
- [x] SH-04/05/08 测试通过率 ≥80% ✅ (意图识别 + 场景编排 + 部分失败容错)
- [x] CustomerService 退换货接口完整 ✅ (RequestExchangeAsync/CheckReturnEligibilityAsync + 模拟实现)
- [x] CS 问题升级服务接口定义完整 ✅ (IEscalationService: 人工升级/VIP等级/跨部门转接)
- [x] CS 主动服务 Handler 全部实现 ✅ (PromotionRecommendation/AnomalousTransaction)
- [x] 所有新增测试通过 ✅ (859 通过, 0 失败)
- [x] 编译无错误 ✅ (0 errors, 2 pre-existing warnings)

---

**最后更新**: 2026-03-22
**状态**: ✅ 全部完成

# Demo场景会话用例文档编写实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 编写74个详细用例文档，全面覆盖SmartHome和CustomerService两个Demo场景，支持演示、测试、开发指导和文档编写四个用途。

**架构:** 按用户旅程组织用例，每个用例采用统一的Markdown模板和YAML元数据标注，提供6个维度的索引支持多角色查找。

**技术栈:** Markdown文档、YAML元数据、符号链接（用于多维度索引）

---

## 任务概览

本实施计划分3个阶段，共15个任务：

### 第一阶段：基础设施搭建（3个任务）
- Task 1: 创建目录结构和基础模板
- Task 2: 创建用例文档模板
- Task 3: 创建多维度索引框架

### 第二阶段：SmartHome场景用例编写（6个任务）
- Task 4: 编写早晨唤醒旅程用例（5个）
- Task 5: 编写离家准备旅程用例（4个）
- Task 6: 编写回家放松旅程用例（5个）
- Task 7: 编写睡眠和会客旅程用例（8个）
- Task 8: 编写外出和紧急情况旅程用例（9个）
- Task 9: 编写个性化场景旅程用例（5个）

### 第三阶段：CustomerService场景用例编写（5个任务）
- Task 10: 编写初次咨询旅程用例（5个）
- Task 11: 编写订单和退换货旅程用例（9个）
- Task 12: 编写投诉和工单旅程用例（9个）
- Task 13: 编写升级和主动服务旅程用例（9个）
- Task 14: 编写投诉闭环旅程用例（6个）

### 第四阶段：验证和完善（1个任务）
- Task 15: 验证所有用例文档完整性并创建总索引

---

## 第一阶段：基础设施搭建

### Task 1: 创建目录结构和基础模板

**Files:**
- Create: `docs/scenarios/README.md`
- Create: `docs/scenarios/SmartHome/README.md`
- Create: `docs/scenarios/CustomerService/README.md`
- Create: `docs/scenarios/templates/case-template.md`
- Create: `docs/scenarios/templates/metadata-template.yaml`

**Step 1: 创建主README**

创建 `docs/scenarios/README.md`:

```markdown
# CKY.MAF Demo场景用例文档

本目录包含CKY.MAF框架两个Demo项目的完整会话场景用例文档。

## 📁 目录结构

```
scenarios/
├── README.md                        # 本文件
├── SmartHome/                       # 智能家居场景
│   ├── README.md
│   ├── 01-早晨唤醒/
│   ├── 02-离家准备/
│   └── ...
├── CustomerService/                 # 智能客服场景
│   ├── README.md
│   ├── 01-初次咨询/
│   └── ...
├── by-agent/                        # 按Agent索引
├── by-complexity/                   # 按复杂度索引
├── by-test-priority/                # 按测试优先级索引
├── by-demo-value/                   # 按演示价值索引
└── templates/                       # 文档模板
    ├── case-template.md
    └── metadata-template.yaml
```

## 🎯 快速导航

### 按场景浏览
- [SmartHome智能家居](./SmartHome/README.md) - 36个用例，8个用户旅程
- [CustomerService智能客服](./CustomerService/README.md) - 38个用例，8个用户旅程

### 按维度索引
- [按Agent查找](./by-agent/README.md) - 开发团队使用
- [按复杂度查找](./by-complexity/README.md) - 学习路径
- [按测试优先级查找](./by-test-priority/README.md) - 测试团队使用
- [按演示价值查找](./by-demo-value/README.md) - 产品/演示团队使用

## 📊 用例统计

- **总计**: 74个用例
- **SmartHome**: 36个用例
- **CustomerService**: 38个用例
- **P0优先级**: 24个用例（核心功能）
- **高演示价值**: 52个用例（4星以上）

## 🚀 快速开始

### 如果你是开发人员
推荐按以下顺序学习：
1. 从 [L1-单Agent](./by-complexity/README.md#l1) 开始
2. 逐步学习 [L2-多轮对话](./by-complexity/README.md#l2)
3. 掌握 [L3-多Agent协作](./by-complexity/README.md#l3)
4. 研究 [L4-复杂编排](./by-complexity/README.md#l4)

### 如果你是测试人员
优先查看 [P0-必须测试](./by-test-priority/README.md#p0) 用例。

### 如果你是产品/演示人员
推荐查看 [5星-核心演示](./by-demo-value/README.md#5星) 用例。

## 📚 相关文档

- [设计文档](../plans/2026-03-15-demo-scenario-use-cases-design.md)
- [架构总览](../specs/01-architecture-overview.md)
- [实施指南](../specs/09-implementation-guide.md)

---

**最后更新**: 2026-03-15
**维护者**: CKY.MAF团队
```

**Step 2: 创建SmartHome README**

创建 `docs/scenarios/SmartHome/README.md`:

```markdown
# SmartHome智能家居场景用例

本目录包含智能家居场景的36个用例，涵盖8个用户旅程。

## 🎯 场景概述

SmartHome场景展示了多Agent协作完成家居自动化控制的能力，包括：

- **天气服务** (WeatherAgent): 查询天气、提供穿衣出行建议
- **气候控制** (ClimateAgent): 空调、温度调节
- **照明控制** (LightingAgent): 灯光开关、亮度调节
- **音乐播放** (MusicAgent): 音乐播放控制
- **温度历史** (TemperatureHistoryAgent): 温度历史记录

## 📋 用户旅程列表

| # | 旅程名称 | 用例数 | 核心Agent | 复杂度 |
|---|----------|--------|-----------|--------|
| 1 | [早晨唤醒](./01-早晨唤醒/) | 5个 | Weather, Climate, Lighting | L3 |
| 2 | [离家准备](./02-离家准备/) | 4个 | Weather, Lighting, Climate | L3 |
| 3 | [回家放松](./03-回家放松/) | 5个 | Climate, Lighting, Music | L3 |
| 4 | [睡眠准备](./04-睡眠准备/) | 4个 | Lighting, Climate | L2 |
| 5 | [会客模式](./05-会客模式/) | 4个 | Lighting, Music, Climate | L3 |
| 6 | [外出期间](./06-外出期间/) | 4个 | Lighting, Climate | L2 |
| 7 | [紧急情况](./07-紧急情况/) | 5个 | 所有Agent | L4 |
| 8 | [个性化场景](./08-个性化场景/) | 5个 | 多Agent组合 | L3 |

## 🔥 高价值演示用例

推荐用于产品演示的用例（5星）：

1. **SH-MORNING-001** 标准晨起唤醒 - 展示多Agent协作和自然交互
2. **SH-HOME-001** 标准回家模式 - 展示场景化智能服务
3. **SH-EMERG-001** 火警烟雾检测响应 - 展示应急响应和多设备联动

## 📊 用例统计

- **P0优先级**: 12个（核心功能，必须实现）
- **P1优先级**: 18个（重要功能，优先实现）
- **P2优先级**: 6个（补充功能，最后实现）

## 🚀 快速开始

### 开发人员
按Agent查看相关用例：
- [WeatherAgent用例](../by-agent/WeatherAgent/README.md)
- [ClimateAgent用例](../by-agent/ClimateAgent/README.md)
- [LightingAgent用例](../by-agent/LightingAgent/README.md)

### 测试人员
优先测试P0用例：
- [按P0优先级查看](../by-test-priority/README.md#p0)

---

**最后更新**: 2026-03-15
```

**Step 3: 创建CustomerService README**

创建 `docs/scenarios/CustomerService/README.md`:

```markdown
# CustomerService智能客服场景用例

本目录包含智能客服场景的38个用例，涵盖8个用户旅程。

## 🎯 场景概述

CustomerService场景展示了多Agent协作处理客户服务请求的能力，包括：

- **CustomerServiceMainAgent**: 主控Agent，负责意图识别和路由
- **KnowledgeBaseAgent**: 知识库查询
- **OrderAgent**: 订单处理
- **TicketAgent**: 工单管理

## 📋 用户旅程列表

| # | 旅程名称 | 用例数 | 核心Agent | 复杂度 |
|---|----------|--------|-----------|--------|
| 1 | [初次咨询](./01-初次咨询/) | 5个 | KnowledgeBase | L2 |
| 2 | [订单查询](./02-订单查询/) | 4个 | OrderAgent | L2 |
| 3 | [退换货处理](./03-退换货处理/) | 5个 | OrderAgent + KB | L3 |
| 4 | [投诉建议](./04-投诉建议/) | 4个 | TicketAgent | L3 |
| 5 | [工单跟进](./05-工单跟进/) | 5个 | TicketAgent | L2 |
| 6 | [问题升级](./06-问题升级/) | 4个 | MainAgent + 人工 | L3 |
| 7 | [主动服务](./07-主动服务/) | 5个 | 所有Agent | L4 |
| 8 | [投诉闭环](./08-投诉闭环/) | 6个 | 所有Agent | L4 |

## 🔥 高价值演示用例

推荐用于产品演示的用例（5星）：

1. **CS-INITIAL-001** 标准咨询流程 - 展示意图识别和知识库检索
2. **CS-RETURN-001** 标准退货申请 - 展示多轮对话和业务流程
3. **CS-CLOSE-001** 标准投诉处理闭环 - 展示多部门协作和闭环管理

## 📊 用例统计

- **P0优先级**: 12个（核心功能，必须实现）
- **P1优先级**: 18个（重要功能，优先实现）
- **P2优先级**: 8个（补充功能，最后实现）

## 🚀 快速开始

### 开发人员
按Agent查看相关用例：
- [KnowledgeBaseAgent用例](../by-agent/KnowledgeBaseAgent/README.md)
- [OrderAgent用例](../by-agent/OrderAgent/README.md)
- [TicketAgent用例](../by-agent/TicketAgent/README.md)

### 测试人员
优先测试P0用例：
- [按P0优先级查看](../by-test-priority/README.md#p0)

---

**最后更新**: 2026-03-15
```

**Step 4: 创建用例文档模板**

创建 `docs/scenarios/templates/case-template.md`:

```markdown
# [用例ID] 用例名称

---
metadata:
  case_id: [用例ID]
  journey: [用户旅程名称]
  journey_order: [在旅程中的顺序]

  # 分类标签
  case_type: primary  # primary | variant | exception | boundary
  domain: [smarthome | customerservice]
  complexity: L2  # L1单Agent | L2多轮对话 | L3多Agent协作 | L4复杂编排

  # 覆盖范围
  agents:
    - [Agent1]
    - [Agent2]
  capabilities:
    - [capability1]
    - [capability2]
  coverage:
    single-agent: [true | false]
    multi-agent: [true | false]
    multi-turn: [true | false]
    error-handling: [true | false]

  # 用途标注
  demo_value: 5  # 1-5星，演示价值
  test_priority: P0  # P0必须 | P1重要 | P2一般
  doc_importance: high  # high | medium | low

  # 执行属性
  estimated_duration_seconds: [预估秒数]
  requires_external_service: [true | false]
  requires_hardware: [true | false]

  # 依赖关系
  depends_on: []
  enables: [[相关用例ID]]

  # 状态
  status: designed  # designed | implementing | testing | completed
  assigned_to: ""
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: [用例ID]
- **用户旅程**: [用户旅程名称]
- **用例类型**: [主流程/变体流程/异常流程/边界测试]
- **演示价值**: ⭐⭐⭐⭐⭐ (1-5星)
- **预估耗时**: [XX]秒

## 🎯 业务目标

[描述这个用例解决什么用户问题，达成什么业务目标]

## 👤 用户画像

- **场景**: [具体的用户场景描述]
- **用户期望**: [用户期望的结果]
- **痛点**: [如果没有这个功能，用户的痛点]

## 🔧 技术规格

### 涉及Agent

- **[Agent1名称]**: [Agent职责说明]
- **[Agent2名称]**: [Agent职责说明]

### 能力标签

- **单Agent能力**: [列出单Agent能力]
- **多Agent协作**: [true/false]
- **多轮对话**: [X]轮
- **异常处理**: [true/false]

### 数据依赖

- **输入参数**: [参数1, 参数2, ...]
- **外部服务**: [外部API名称]
- **预期响应时间**: [< X秒]

## 📝 执行流程

### Step 1: [步骤名称]

**用户输入**: "[用户说的话]"

**系统响应**:
1. 意图识别: [意图类型]
2. 实体提取: [实体列表]
3. Agent调用: [Agent名称.ExecuteAsync()]
4. 返回结果: "[系统回答]"

### Step 2: [步骤名称]

**用户输入**: "[用户说的话]"

**系统响应**:
1. 意图识别: [意图类型]
2. 实体提取: [实体列表]
3. Agent调用: [Agent名称.ExecuteAsync()]
4. 返回结果: "[系统回答]"

[根据需要添加更多步骤...]

## ✅ 预期结果

- [x] [预期结果1]
- [x] [预期结果2]
- [x] [预期结果3]
- [x] [性能要求，如：全流程耗时 < X秒]

## 🧪 测试要点

### 功能测试

- [ ] [测试点1]
- [ ] [测试点2]
- [ ] [测试点3]

### 集成测试

- [ ] [集成测试点1]
- [ ] [集成测试点2]
- [ ] [集成测试点3]

### 异常测试（如适用）

- [ ] [异常测试点1]
- [ ] [异常测试点2]

## 💡 演示要点

### 展示亮点

1. **[亮点1标题]**: [描述]
2. **[亮点2标题]**: [描述]
3. **[亮点3标题]**: [描述]

### 演示脚本

**开场**: "您好，今天我将为您演示[用例名称]..."

**Step 1**: [操作说明]
**预期**: [预期结果]

**Step 2**: [操作说明]
**预期**: [预期结果]

**总结**: "[总结本用例展示的框架能力]"

## 📚 相关文档

- **架构文档**: [链接到相关架构文档]
- **Agent实现**: [链接到Agent代码]
- **测试用例**: [链接到测试文件]
- **相关用例**: [链接到相关用例]

---

**文档版本**: v1.0
**最后更新**: 2026-03-15
**维护者**: [负责人姓名]
```

**Step 5: 创建元数据模板**

创建 `docs/scenarios/templates/metadata-template.yaml`:

```yaml
---
# 用例元数据模板
# 复制此模板到每个用例文档的头部，并填写完整信息

metadata:
  # === 基本信息 ===
  case_id: ""  # 用例唯一标识，如：SH-MORNING-001
  journey: ""  # 用户旅程名称，如：早晨唤醒
  journey_order: 1  # 在旅程中的顺序

  # === 分类标签 ===
  case_type: ""  # primary（主流程）| variant（变体）| exception（异常）| boundary（边界）
  domain: ""  # smarthome | customerservice
  complexity: ""  # L1（单Agent）| L2（多轮对话）| L3（多Agent协作）| L4（复杂编排）

  # === 覆盖范围 ===
  agents:  # 涉及的Agent列表
    - ""
  capabilities:  # 涉及的能力标签
    - ""
  coverage:
    single-agent: false  # 是否包含单Agent功能
    multi-agent: false  # 是否包含多Agent协作
    multi-turn: false  # 是否包含多轮对话
    error-handling: false  # 是否包含异常处理

  # === 用途标注 ===
  demo_value: 0  # 演示价值：1-5星
  test_priority: ""  # 测试优先级：P0（必须）| P1（重要）| P2（一般）
  doc_importance: ""  # 文档重要性：high（必须）| medium（建议）| low（可选）

  # === 执行属性 ===
  estimated_duration_seconds: 0  # 预估执行时长（秒）
  requires_external_service: false  # 是否依赖外部API
  requires_hardware: false  # 是否依赖真实硬件

  # === 依赖关系 ===
  depends_on: []  # 前置依赖的用例ID列表
  enables: []  # 启用的后续用例ID列表

  # === 状态管理 ===
  status: ""  # designed（已设计）| implementing（实现中）| testing（测试中）| completed（已完成）
  assigned_to: ""  # 负责人
  last_updated: ""  # 最后更新日期（YYYY-MM-DD）
---
```

**Step 6: 验证目录结构创建成功**

运行：
```bash
ls -la docs/scenarios/
ls -la docs/scenarios/SmartHome/
ls -la docs/scenarios/CustomerService/
ls -la docs/scenarios/templates/
```

预期输出：
```
docs/scenarios/
├── README.md
├── SmartHome/
│   └── README.md
├── CustomerService/
│   └── README.md
└── templates/
    ├── case-template.md
    └── metadata-template.yaml
```

**Step 7: 提交基础结构**

```bash
git add docs/scenarios/
git commit -m "feat: create scenario documentation structure

- Add main README with navigation
- Add SmartHome and CustomerService READMEs
- Create case template and metadata template
- Establish directory structure for 74 use cases
"
```

---

### Task 2: 创建用例文档模板

这个任务已经在Task 1中完成（Step 4和Step 5）。

**验证**: 确认模板文件存在且内容完整

运行：
```bash
cat docs/scenarios/templates/case-template.md | head -20
cat docs/scenarios/templates/metadata-template.yaml
```

预期输出：显示模板文件的前20行内容

---

### Task 3: 创建多维度索引框架

**Files:**
- Create: `docs/scenarios/by-agent/README.md`
- Create: `docs/scenarios/by-complexity/README.md`
- Create: `docs/scenarios/by-test-priority/README.md`
- Create: `docs/scenarios/by-demo-value/README.md`
- Create: `docs/scenarios/by-doc-importance/README.md`

**Step 1: 创建按Agent索引**

创建 `docs/scenarios/by-agent/README.md`:

```markdown
# 按Agent索引

本目录按照Agent组织用例，方便开发人员快速查找相关用例。

## 📁 Agent列表

### SmartHome Agents

- [WeatherAgent](./WeatherAgent/) - 天气查询和建议
- [ClimateAgent](./ClimateAgent/) - 空调和温度控制
- [LightingAgent](./LightingAgent/) - 灯光控制和亮度调节
- [MusicAgent](./MusicAgent/) - 音乐播放控制
- [TemperatureHistoryAgent](./TemperatureHistoryAgent/) - 温度历史记录

### CustomerService Agents

- [CustomerServiceMainAgent](./CustomerServiceMainAgent/) - 主控Agent
- [KnowledgeBaseAgent](./KnowledgeBaseAgent/) - 知识库查询
- [OrderAgent](./OrderAgent/) - 订单处理
- [TicketAgent](./TicketAgent/) - 工单管理

## 📊 用例统计

| Agent | 用例数 | P0 | P1 | P2 |
|-------|--------|----|----|-----|
| WeatherAgent | 8 | 3 | 4 | 1 |
| ClimateAgent | 12 | 4 | 6 | 2 |
| LightingAgent | 10 | 3 | 5 | 2 |
| ... | ... | ... | ... | ... |

## 🔗 使用说明

每个Agent目录包含该Agent相关的所有用例的符号链接。点击上方Agent名称查看详细用例列表。

---

**最后更新**: 2026-03-15
```

**Step 2: 创建按复杂度索引**

创建 `docs/scenarios/by-complexity/README.md`:

```markdown
# 按复杂度索引

本目录按照用例复杂度组织，提供循序渐进的学习路径。

## 📊 复杂度分级

### L1 - 单Agent基础功能

展示单个Agent的独立能力，适合初学者入门。

**特点**:
- 涉及1个Agent
- 单次交互，无多轮对话
- 响应时间 < 1秒

**用例数**: 20个
- [查看所有L1用例](./L1-单Agent/)

### L2 - 多轮对话管理

展示多轮对话和澄清能力。

**特点**:
- 涉及1-2个Agent
- 2-3轮对话交互
- 包含意图澄清和参数确认

**用例数**: 14个
- [查看所有L2用例](./L2-多轮对话/)

### L3 - 多Agent协作

展示多Agent协同完成任务的能力。

**特点**:
- 涉及2-4个Agent
- 复杂业务流程
- Agent间数据共享

**用例数**: 32个
- [查看所有L3用例](./L3-多Agent协作/)

### L4 - 复杂编排

展示复杂业务逻辑和编排能力。

**特点**:
- 涉及4+个Agent
- 长流程业务场景
- 包含决策分支和条件判断

**用例数**: 8个
- [查看所有L4用例](./L4-复杂编排/)

## 🚀 推荐学习路径

1. **入门阶段** (第1-2周)
   - 学习所有L1用例
   - 理解单个Agent的工作原理

2. **进阶阶段** (第3-4周)
   - 学习L2用例
   - 掌握多轮对话管理

3. **高级阶段** (第5-6周)
   - 学习L3用例
   - 理解多Agent协作模式

4. **专家阶段** (第7-8周)
   - 研究L4用例
   - 掌握复杂业务编排

---

**最后更新**: 2026-03-15
```

**Step 3: 创建按测试优先级索引**

创建 `docs/scenarios/by-test-priority/README.md`:

```markdown
# 按测试优先级索引

本目录按照测试优先级组织用例，帮助测试团队制定测试计划。

## 📊 优先级说明

### P0 - 必须测试 ⚠️

核心功能用例，必须100%测试通过才能发布。

**特点**:
- 核心业务流程
- 高演示价值
- 用户高频使用

**用例数**: 24个
- 测试覆盖率要求: 100%
- 自动化测试要求: 必须自动化

### P1 - 重要测试 ⭐

重要功能用例，应该测试通过。

**特点**:
- 重要业务流程
- 较高演示价值
- 用户中频使用

**用例数**: 36个
- 测试覆盖率要求: ≥ 80%
- 自动化测试要求: 主要路径自动化

### P2 - 一般测试 📝

补充功能用例，可以最后测试。

**特点**:
- 补充功能
- 一般演示价值
- 用户低频使用

**用例数**: 14个
- 测试覆盖率要求: ≥ 50%
- 自动化测试要求: 可手动测试

## 🎯 测试计划建议

### 第一轮测试（第1-2周）
- 所有P0用例
- 预期通过率: 100%

### 第二轮测试（第3-4周）
- P0 + P1用例
- 预期通过率: ≥ 90%

### 第三轮测试（第5-6周）
- 所有用例（P0 + P1 + P2）
- 预期通过率: ≥ 85%

---

**最后更新**: 2026-03-15
**测试负责人**: [待分配]
```

**Step 4: 创建按演示价值索引**

创建 `docs/scenarios/by-demo-value/README.md`:

```markdown
# 按演示价值索引

本目录按照演示价值组织用例，帮助产品和演示团队准备演示材料。

## ⭐ 价值分级

### 5星 - 核心演示 🌟🌟🌟🌟🌟

最具演示价值的用例，强烈推荐用于产品演示。

**特点**:
- 完整展示框架核心能力
- 业务价值清晰
- 用户体验流畅

**用例数**: 18个
- SmartHome: 9个
- CustomerService: 9个

**推荐演示场景**:
1. 智能家居完整一天（早晨→离家→回家→睡眠）
2. 智能客服完整闭环（咨询→订单→售后→回访）

### 4星 - 常用演示 🌟🌟🌟🌟

高演示价值用例，适合特定场景演示。

**特点**:
- 展示重要能力
- 业务价值明显
- 用户体验良好

**用例数**: 34个
- SmartHome: 17个
- CustomerService: 17个

### 3星 - 补充演示 🌟🌟🌟

一般演示价值用例，可根据需要选择演示。

**特点**:
- 展示特定功能
- 业务价值一般
- 用户可选择性使用

**用例数**: 22个
- SmartHome: 10个
- CustomerService: 12个

## 🎯 演示场景推荐

### 场景1: 智能家居演示（15分钟）

**目标客户**: 房地产开发商、家装公司、智能家居集成商

**演示用例**:
1. SH-MORNING-001 早晨唤醒（3分钟）
2. SH-DEPART-001 离家准备（2分钟）
3. SH-HOME-001 回家放松（3分钟）
4. SH-EMERG-001 紧急情况响应（3分钟）
5. Q&A 互动（4分钟）

**核心卖点**:
- 多Agent无缝协作
- 自然语言交互
- 智能场景化服务
- 安全应急响应

### 场景2: 智能客服演示（10分钟）

**目标客户**: 电商企业、在线服务平台、客服外包公司

**演示用例**:
1. CS-INITIAL-001 智能咨询（2分钟）
2. CS-RETURN-001 退换货处理（3分钟）
3. CS-CLOSE-001 投诉闭环（3分钟）
4. Q&A 互动（2分钟）

**核心卖点**:
- 意图识别准确
- 多轮对话流畅
- 业务流程完整
- 闭环管理完善

---

**最后更新**: 2026-03-15
**演示负责人**: [待分配]
```

**Step 5: 创建按文档重要性索引**

创建 `docs/scenarios/by-doc-importance/README.md`:

```markdown
# 按文档重要性索引

本目录按照文档重要性组织用例，帮助文档团队确定编写优先级。

## 📚 重要性分级

### High - 必须文档化 🔴

这些用例必须编写完整的用户文档。

**标准**:
- 用户高频使用
- 业务核心价值
- 演示价值高

**用例数**: 约40个
- 文档要求: 完整的用户使用指南
- 截止时间: 第一阶段完成（4周）

### Medium - 建议文档化 🟡

这些用例建议编写用户文档。

**标准**:
- 用户中频使用
- 重要功能
- 演示价值较高

**用例数**: 约24个
- 文档要求: 简要使用说明
- 截止时间: 第二阶段完成（8周）

### Low - 可选文档化 🟢

这些用例可以不编写专门的用户文档。

**标准**:
- 用户低频使用
- 补充功能
- 开发/测试为主

**用例数**: 约10个
- 文档要求: 仅在高级文档中提及
- 截止时间: 第三阶段完成（10周）

## 📝 文档模板

所有用户文档应遵循以下结构：

1. **功能概述** (1-2句话)
2. **使用场景** (何时使用)
3. **操作步骤** (分步骤说明)
4. **常见问题** (FAQ)
5. **注意事项** (重要提醒)

## 🎯 文档计划

### 第一阶段（第1-4周）
完成所有High重要性用例的文档

### 第二阶段（第5-8周）
完成所有Medium重要性用例的文档

### 第三阶段（第9-10周）
完成Low重要性用例的文档（可选）

---

**最后更新**: 2026-03-15
**文档负责人**: [待分配]
```

**Step 6: 验证索引框架创建成功**

运行：
```bash
ls -la docs/scenarios/by-*/
```

预期输出：
```
docs/scenarios/by-agent/
docs/scenarios/by-complexity/
docs/scenarios/by-test-priority/
docs/scenarios/by-demo-value/
docs/scenarios/by-doc-importance/
```

**Step 7: 提交索引框架**

```bash
git add docs/scenarios/by-*/
git commit -m "feat: add multi-dimensional index framework

- Add by-agent index for developers
- Add by-complexity index for learning path
- Add by-test-priority index for testing team
- Add by-demo-value index for product/demo team
- Add by-doc-importance index for documentation team
"
```

---

## 第二阶段：SmartHome场景用例编写

### Task 4: 编写早晨唤醒旅程用例（5个）

**Files:**
- Create: `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-001-标准晨起唤醒.md`
- Create: `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-002-工作日晨起模式.md`
- Create: `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-003-周末懒觉模式.md`
- Create: `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-004-天气服务异常.md`
- Create: `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-005-设备离线处理.md`
- Create: `docs/scenarios/SmartHome/01-早晨唤醒/README.md`

**Step 1: 编写SH-MORNING-001标准晨起唤醒**

创建 `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-001-标准晨起唤醒.md`:

```markdown
# SH-MORNING-001 标准晨起唤醒

---
metadata:
  case_id: SH-MORNING-001
  journey: 早晨唤醒
  journey_order: 1

  case_type: primary
  domain: smarthome
  complexity: L3

  agents:
    - WeatherAgent
    - ClimateAgent
    - LightingAgent
  capabilities:
    - weather-query
    - temperature-control
    - brightness-control
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false

  demo_value: 5
  test_priority: P0
  doc_importance: high

  estimated_duration_seconds: 45
  requires_external_service: true
  requires_hardware: false

  depends_on: []
  enables: [SH-MORNING-002, SH-MORNING-003]

  status: designed
  assigned_to: ""
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-MORNING-001
- **用户旅程**: 早晨唤醒
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐
- **预估耗时**: 45秒

## 🎯 业务目标

为用户提供舒适的起床体验，通过逐步唤醒设备、查询天气、调节环境，让用户从睡眠状态自然过渡到清醒状态。

## 👤 用户画像

- **场景**: 用户早上7点醒来，希望在起床前了解当天情况并让家中的环境逐渐唤醒
- **用户期望**: 查询天气、穿衣建议、灯光逐渐调亮、温度调节舒适
- **痛点**: 早上刚醒来时不想操作复杂的设备，希望系统能智能预测并自动调节

## 🔧 技术规格

### 涉及Agent

- **WeatherAgent**: 查询当天天气，提供穿衣和出行建议
- **ClimateAgent**: 调节室内温度到舒适范围
- **LightingAgent**: 逐步调亮卧室和客厅灯光

### 能力标签

- **单Agent能力**: weather-query, temperature-control, brightness-control
- **多Agent协作**: true
- **多轮对话**: 3轮
- **异常处理**: false

### 数据依赖

- **输入参数**: city（城市名称，可选，默认当前城市）
- **外部服务**: WeatherAPI
- **预期响应时间**: < 2秒（单次交互）

## 📝 执行流程

### Step 1: 问候和天气查询

**用户输入**: "早上好" 或 "我醒了"

**系统响应**:
```
早上好！美好的一天开始了～🌞

让我为您查询一下今天的天气情况...

[调用WeatherAgent]
正在查询北京今天天气...
```

**Agent调用**:
```csharp
await WeatherAgent.ExecuteAsync(new MafTaskRequest
{
    UserInput = "早上好",
    Parameters = { { "city", "北京" }, { "date", "今天" } }
});
```

### Step 2: 天气播报和建议

**系统响应**:
```
📍 北京今天天气：晴转多云
🌡️ 气温：12~22°C（当前 15°C）
💨 东风3级 | 💧 湿度 45% | 🌬️ 空气质量：良好（AQI 65）

👔 穿衣建议：温度适宜，建议穿薄外套或长袖衬衫。
✅ 出行建议：天气适宜出行，祝您出行愉快！
```

**数据返回**:
```json
{
  "city": "北京",
  "date": "2026-03-15",
  "condition": "晴转多云",
  "temperature": 15,
  "minTemperature": 12,
  "maxTemperature": 22,
  "humidity": 45,
  "windDirection": "东风",
  "windLevel": 3,
  "airQualityIndex": 65
}
```

### Step 3: 温度调节

**系统提示**: "现在室温15度，需要帮您调高一些吗？"

**用户输入**: "调到22度吧"

**系统响应**:
```
[调用ClimateAgent]
客厅空调已设置为22度，制热模式中...
预计5分钟达到目标温度。
```

**Agent调用**:
```csharp
await ClimateAgent.ExecuteAsync(new MafTaskRequest
{
    UserInput = "调到22度吧",
    Parameters = { { "room", "客厅" }, { "temperature", 22 }, { "mode", "heating" } }
});
```

### Step 4: 灯光调节

**系统响应**:
```
[调用LightingAgent]
正在为您逐渐调亮卧室灯光...
亮度将在30秒内从0%逐渐提升到80%。

同时已为您打开客厅灯光，亮度50%。
```

**Agent调用**:
```csharp
// 卧室灯光渐亮
await LightingAgent.ExecuteAsync(new MafTaskRequest
{
    UserInput = "逐渐调亮卧室",
    Parameters = { { "room", "卧室" }, { "action", "gradual-brighten" }, { "targetBrightness", 80 }, { "duration", 30 } }
});

// 客厅灯光
await LightingAgent.ExecuteAsync(new MafTaskRequest
{
    UserInput = "打开客厅灯",
    Parameters = { { "room", "客厅" }, { "action", "turn-on" }, { "brightness", 50 } }
});
```

### Step 5: 完成

**系统响应**:
```
✅ 早晨唤醒模式已激活！

☀️ 天气已查询
🌡️ 温度已调节到22°C
💡 灯光已逐渐调亮

祝您今天有个好心情！还需要什么帮助吗？
```

## ✅ 预期结果

- [x] 天气信息正确显示，包含温度、湿度、空气质量
- [x] 提供准确的穿衣建议（基于温度）
- [x] 客厅空调成功设置为22度制热模式
- [x] 卧室灯光在30秒内从0%逐渐调亮到80%
- [x] 客厅灯光打开，亮度50%
- [x] 全流程耗时 < 45秒
- [x] 多Agent协作流畅，无卡顿

## 🧪 测试要点

### 功能测试

- [ ] WeatherAgent返回正确的天气数据
- [ ] ClimateAgent成功设置温度和模式
- [ ] LightingAgent成功实现渐亮效果
- [ ] 穿衣建议与温度匹配（15°C建议薄外套）

### 集成测试

- [ ] 三个Agent按正确顺序调用
- [ ] Agent间数据传递正确
- [ ] 上下文保持一致（城市、房间等）
- [ ] 响应时间：Step 1 < 2s, Step 3 < 1s, Step 4 < 1s

### 用户体验测试

- [ ] 语音播报自然流畅
- [ ] 信息量适中，不 overwhelming
- [ ] 灯光渐亮效果舒适（不刺眼）

## 💡 演示要点

### 展示亮点

1. **多Agent无缝协作**: 展示3个Agent如何协同工作，但用户感知为统一的智能服务
2. **自然语言交互**: 展示用户如何用日常语言（"早上好"、"调到22度"）控制系统
3. **智能预测**: 系统主动提供穿衣建议，体现智能化
4. **舒适体验**: 灯光渐亮而非直接打开，体现对用户体验的关注

### 演示脚本

**开场**: "早上好！今天我将为您演示智能家居的早晨唤醒功能。这个场景展示了多个Agent如何协同工作，为用户提供舒适的起床体验。"

**Step 1**: "用户刚醒来，只需说一声'早上好'，系统就会自动响应。"
**预期**: 系统友好回应，开始查询天气

**Step 2**: "系统查询并播报天气，还提供了穿衣建议。您看，温度15度，建议穿薄外套，很贴心吧？"
**预期**: 显示完整天气信息和穿衣建议

**Step 3**: "用户说'调到22度'，系统立即调节温度。"
**预期**: 确认温度设置成功

**Step 4**: "同时，卧室灯光会逐渐调亮，模拟日出效果，让眼睛自然适应。客厅灯也打开了。"
**预期**: 演示灯光渐亮效果（可在演示视频中快进播放）

**总结**: "整个流程涉及3个Agent：WeatherAgent、ClimateAgent、LightingAgent，它们无缝协作，但用户完全感觉不到复杂的技术，只感受到贴心的智能服务。这就是我们框架的价值——让复杂的技术变得简单易用。"

## 📚 相关文档

- **架构文档**: `docs/specs/01-architecture-overview.md` - Agent协作机制
- **Agent实现**: `src/Demos/SmartHome/Agents/WeatherAgent.cs`
- **Agent实现**: `src/Demos/SmartHome/Agents/ClimateAgent.cs`
- **Agent实现**: `src/Demos/SmartHome/Agents/LightingAgent.cs`
- **测试用例**: `tests/SmartHome/ScenarioTests/MorningScenarioTests.cs`
- **相关用例**: [SH-MORNING-002 工作日晨起模式](./SH-MORNING-002-工作日晨起模式.md)
- **相关用例**: [SH-MORNING-003 周末懒觉模式](./SH-MORNING-003-周末懒觉模式.md)

---

**文档版本**: v1.0
**最后更新**: 2026-03-15
**维护者**: [待分配]
```

**Step 2: 编写SH-MORNING-002工作日晨起模式**

创建 `docs/scenarios/SmartHome/01-早晨唤醒/SH-MORNING-002-工作日晨起模式.md`:

```markdown
# SH-MORNING-002 工作日晨起模式

---
metadata:
  case_id: SH-MORNING-002
  journey: 早晨唤醒
  journey_order: 2

  case_type: variant
  domain: smarthome
  complexity: L3

  agents:
    - WeatherAgent
    - ClimateAgent
    - LightingAgent
    - MusicAgent
  capabilities:
    - weather-query
    - temperature-control
    - brightness-control
    - music-play
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false

  demo_value: 4
  test_priority: P1
  doc_importance: medium

  estimated_duration_seconds: 60
  requires_external_service: true
  requires_hardware: false

  depends_on: [SH-MORNING-001]
  enables: []

  status: designed
  assigned_to: ""
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-MORNING-002
- **用户旅程**: 早晨唤醒
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 60秒

## 🎯 业务目标

在工作日早晨，除了标准的天气、温度、灯光控制外，还播放新闻或音乐，帮助用户快速清醒并获取信息。

## 👤 用户画像

- **场景**: 工作日早上7点，用户需要快速清醒并了解当天新闻，准备上班
- **用户期望**: 标准唤醒功能 + 播放新闻/音乐 + 温度调节更积极
- **痛点**: 工作日时间紧张，希望系统更主动地帮助清醒

## 🔧 技术规格

### 涉及Agent

- **WeatherAgent**: 查询天气
- **ClimateAgent**: 调节温度到工作日舒适温度（22°C，比标准模式略高）
- **LightingAgent**: 灯光调节
- **MusicAgent**: 播放新闻或音乐

### 能力标签

- **单Agent能力**: weather-query, temperature-control, brightness-control, music-play
- **多Agent协作**: true（4个Agent）
- **多轮对话**: 3轮
- **异常处理**: false

### 数据依赖

- **输入参数**: city, mediaPreference（新闻/音乐）
- **外部服务**: WeatherAPI
- **预期响应时间**: < 2秒

## 📝 执行流程

### Step 1: 标准唤醒流程

**用户输入**: "早上好，今天要上班"

**系统响应**:
```
早上好！工作日加油！💪

[调用WeatherAgent查询天气]
[调用ClimateAgent设置温度到22°C]
[调用LightingAgent逐渐调亮灯光]
```

（与SH-MORNING-001相同的天气、温度、灯光流程）

### Step 2: 媒体播放选项

**系统提示**: "要为您播放新闻还是音乐呢？"

**用户输入**: "播放新闻吧"

**系统响应**:
```
[调用MusicAgent]
正在为您播放今日新闻摘要...
```

**Agent调用**:
```csharp
await MusicAgent.ExecuteAsync(new MafTaskRequest
{
    UserInput = "播放新闻吧",
    Parameters = {
        { "action", "play" },
        { "type", "news" },
        { "duration", 300 }  // 5分钟
    }
});
```

### Step 3: 完成

**系统响应**:
```
✅ 工作日晨起模式已激活！

☀️ 天气已查询
🌡️ 温度已调节到22°C
💡 灯光已逐渐调亮
📰 新闻正在播放

祝您今天工作顺利！还需要什么帮助吗？
```

## ✅ 预期结果

- [x] 包含SH-MORNING-001的所有功能
- [x] 温度设置为22°C（比标准模式积极）
- [x] MusicAgent成功播放新闻
- [x] 新闻播放时长约5分钟（可用户调整）
- [x] 全流程耗时 < 60秒（不含新闻播放时间）

## 🧪 测试要点

### 功能测试

- [ ] 继承SH-MORNING-001的所有测试点
- [ ] MusicAgent成功播放指定类型的媒体
- [ ] 媒体播放参数正确（类型、时长）

### 集成测试

- [ ] 4个Agent协作顺序正确
- [ ] MusicAgent与其他Agent无冲突
- [ ] 媒体播放不影响其他Agent响应时间

## 💡 演示要点

### 展示亮点

1. **个性化服务**: 展示系统如何根据"工作日"信息调整服务
2. **4个Agent协作**: 比标准模式多一个MusicAgent
3. **智能化**: 主动询问媒体偏好，而非固定播放

### 演示脚本

**开场**: "接下来演示工作日晨起模式，这是标准唤醒模式的增强版本。"

**Step 1**: "用户说'早上好，今天要上班'，系统识别出这是工作日场景。"
**预期**: 系统回应更积极，提到"工作日加油"

**Step 2**: "完成标准唤醒后，系统主动询问播放新闻还是音乐，体现了个性化服务。"
**预期**: 展示选择交互

**总结**: "工作日模式涉及4个Agent协作，展示了框架如何根据上下文（工作日）提供差异化服务。"

## 📚 相关文档

- **父用例**: [SH-MORNING-001 标准晨起唤醒](./SH-MORNING-001-标准晨起唤醒.md)
- **Agent实现**: `src/Demos/SmartHome/Agents/MusicAgent.cs`
- **相关用例**: [SH-MORNING-003 周末懒觉模式](./SH-MORNING-003-周末懒觉模式.md)

---

**文档版本**: v1.0
**最后更新**: 2026-03-15
**维护者**: [待分配]
```

**Step 3-5**: 按照类似模板编写其余3个用例文档

（SH-MORNING-003, SH-MORNING-004, SH-MORNING-005）

**Step 6: 创建旅程README**

创建 `docs/scenarios/SmartHome/01-早晨唤醒/README.md`:

```markdown
# 01-早晨唤醒

本旅程包含5个用例，展示用户早晨起床时的智能唤醒场景。

## 📋 用例列表

| 用例ID | 用例名称 | 类型 | 演示价值 | 优先级 | 复杂度 |
|--------|----------|------|----------|--------|--------|
| [SH-MORNING-001](./SH-MORNING-001-标准晨起唤醒.md) | 标准晨起唤醒 | 主流程 | ⭐⭐⭐⭐⭐ | P0 | L3 |
| [SH-MORNING-002](./SH-MORNING-002-工作日晨起模式.md) | 工作日晨起模式 | 变体 | ⭐⭐⭐⭐ | P1 | L3 |
| [SH-MORNING-003](./SH-MORNING-003-周末懒觉模式.md) | 周末懒觉模式 | 变体 | ⭐⭐⭐⭐ | P1 | L3 |
| [SH-MORNING-004](./SH-MORNING-004-天气服务异常.md) | 天气服务异常 | 异常流程 | ⭐⭐⭐ | P0 | L3 |
| [SH-MORNING-005](./SH-MORNING-005-设备离线处理.md) | 设备离线处理 | 异常流程 | ⭐⭐⭐ | P0 | L3 |

## 🎯 旅程目标

为用户提供舒适的起床体验，通过查询天气、调节温度、控制灯光等设备，让用户自然从睡眠状态过渡到清醒状态。

## 🔥 核心演示用例

推荐用于演示的用例：

1. **SH-MORNING-001** 标准晨起唤醒 - 展示多Agent协作
2. **SH-MORNING-004** 天气服务异常 - 展示容错能力

## 📊 能力覆盖

### 涉及Agent
- WeatherAgent（5个用例）
- ClimateAgent（5个用例）
- LightingAgent（5个用例）
- MusicAgent（2个用例）

### 覆盖能力
- 天气查询和穿衣建议
- 温度调节（制热/制冷）
- 灯光控制（开关、亮度、渐亮）
- 音乐播放
- 异常处理（服务故障、设备离线）
- 多轮对话（澄清、确认）

---

**最后更新**: 2026-03-15
```

**Step 7: 验证文件创建**

运行：
```bash
ls -la docs/scenarios/SmartHome/01-早晨唤醒/
```

预期输出：
```
SH-MORNING-001-标准晨起唤醒.md
SH-MORNING-002-工作日晨起模式.md
SH-MORNING-003-周末懒觉模式.md
SH-MORNING-004-天气服务异常.md
SH-MORNING-005-设备离线处理.md
README.md
```

**Step 8: 提交文档**

```bash
git add docs/scenarios/SmartHome/01-早晨唤醒/
git commit -m "feat: add Morning Awakening journey use cases (5 cases)

- Add SH-MORNING-001: Standard morning awakening
- Add SH-MORNING-002: Workday morning mode
- Add SH-MORNING-003: Weekend lazy mode
- Add SH-MORNING-004: Weather service exception
- Add SH-MORNING-005: Device offline handling
- Add journey README
"
```

---

### Task 5-9: 编写其他SmartHome旅程用例

（按照Task 4的模式，编写其余7个旅程的31个用例）

由于篇幅限制，这里不列出所有用例的详细内容，但遵循相同的模板和结构。

**每个旅程包含**:
- 4-6个用例文档
- 1个旅程README

**总计**: 31个用例 + 7个README

---

## 第三阶段：CustomerService场景用例编写

### Task 10-14: 编写CustomerService旅程用例

（按照SmartHome的模式，编写8个旅程的38个用例）

**每个旅程包含**:
- 4-6个用例文档
- 1个旅程README

**总计**: 38个用例 + 8个README

由于篇幅限制，这里不列出所有用例的详细内容。

---

## 第四阶段：验证和完善

### Task 15: 验证所有用例文档完整性并创建总索引

**Step 1: 验证所有文件存在**

运行：
```bash
# 验证SmartHome用例数量
find docs/scenarios/SmartHome -name "SH-*.md" | wc -l
# 预期输出: 36

# 验证CustomerService用例数量
find docs/scenarios/CustomerService -name "CS-*.md" | wc -l
# 预期输出: 38

# 验证README文件
find docs/scenarios -name "README.md" | wc -l
# 预期输出: 20 (1 main + 2 demo + 8 journeys SH + 8 journeys CS + 1 by-agent + ...)
```

**Step 2: 创建符号链接建立多维度索引**

为每个维度创建符号链接到实际用例文件：

```bash
#!/bin/bash
# create-indexes.sh

# 按Agent索引
for agent in WeatherAgent ClimateAgent LightingAgent MusicAgent TemperatureHistoryAgent; do
    mkdir -p "docs/scenarios/by-agent/$agent"
    # 创建符号链接（需要根据实际文件路径调整）
done

# 按复杂度索引
mkdir -p docs/scenarios/by-complexity/{L1-单Agent,L2-多轮对话,L3-多Agent协作,L4-复杂编排}

# 按优先级索引
mkdir -p docs/scenarios/by-test-priority/{P0-必须测试,P1-重要测试,P2-一般测试}

# 按演示价值索引
mkdir -p docs/scenarios/by-demo-value/{5星-核心演示,4星-常用演示,3星-补充演示}
```

**Step 3: 验证元数据完整性**

创建验证脚本 `scripts/validate-use-cases.js`:

```javascript
// 简单的元数据验证脚本示例
const fs = require('fs');
const yaml = require('js-yaml');

function validateUseCase(filePath) {
    const content = fs.readFileSync(filePath, 'utf8');
    const metadataMatch = content.match(/---\n(.*?)\n---/s);

    if (!metadataMatch) {
        console.error(`❌ ${filePath}: Missing metadata`);
        return false;
    }

    try {
        const metadata = yaml.load(metadataMatch[1]);

        // 验证必需字段
        const requiredFields = ['case_id', 'journey', 'case_type', 'complexity', 'demo_value', 'test_priority'];
        const missingFields = requiredFields.filter(f => !metadata[f]);

        if (missingFields.length > 0) {
            console.error(`❌ ${filePath}: Missing fields: ${missingFields.join(', ')}`);
            return false;
        }

        console.log(`✅ ${filePath}: Valid`);
        return true;
    } catch (e) {
        console.error(`❌ ${filePath}: Invalid YAML - ${e.message}`);
        return false;
    }
}

// 验证所有用例文件
// ...实现遍历逻辑
```

运行验证：
```bash
node scripts/validate-use-cases.js
```

**Step 4: 创建统计脚本**

创建 `scripts/generate-statistics.js`:

```javascript
// 生成用例统计报告
// 按Agent、复杂度、优先级等维度统计
```

**Step 5: 更新主README统计信息**

根据实际生成的统计数据，更新 `docs/scenarios/README.md` 中的统计信息。

**Step 6: 最终提交**

```bash
git add docs/scenarios/
git commit -m "feat: complete all 74 use case documents

- SmartHome: 36 use cases across 8 journeys
- CustomerService: 38 use cases across 8 journeys
- Create multi-dimensional indexes (by-agent, by-complexity, etc.)
- Add validation scripts
- Update statistics

Total: 74 use cases, 20 READMEs, 6 index dimensions
"
```

---

## 附录A: 文件清单

### 完整文件列表

创建完所有文档后，应该有以下文件结构：

```
docs/scenarios/
├── README.md
├── SmartHome/
│   ├── README.md
│   ├── 01-早晨唤醒/
│   │   ├── README.md
│   │   ├── SH-MORNING-001.md
│   │   ├── SH-MORNING-002.md
│   │   ├── SH-MORNING-003.md
│   │   ├── SH-MORNING-004.md
│   │   └── SH-MORNING-005.md
│   ├── 02-离家准备/
│   │   └── ... (4 files)
│   ├── ... (6 more journeys)
│   └── 08-个性化场景/
│       └── ... (5 files)
├── CustomerService/
│   ├── README.md
│   ├── 01-初次咨询/
│   │   ├── README.md
│   │   ├── CS-INITIAL-001.md
│   │   └── ... (4 more files)
│   ├── ... (7 more journeys)
│   └── 08-投诉闭环/
│       └── ... (6 files)
├── by-agent/
│   ├── README.md
│   ├── WeatherAgent/
│   ├── ClimateAgent/
│   └── ... (symlinks)
├── by-complexity/
│   ├── README.md
│   ├── L1-单Agent/
│   ├── L2-多轮对话/
│   ├── L3-多Agent协作/
│   └── L4-复杂编排/
├── by-test-priority/
│   ├── README.md
│   ├── P0-必须测试/
│   ├── P1-重要测试/
│   └── P2-一般测试/
├── by-demo-value/
│   ├── README.md
│   ├── 5星-核心演示/
│   ├── 4星-常用演示/
│   └── 3星-补充演示/
├── by-doc-importance/
│   ├── README.md
│   ├── high-必须文档化/
│   ├── medium-建议文档化/
│   └── low-可选文档化/
└── templates/
    ├── case-template.md
    └── metadata-template.yaml
```

**总文件数**:
- 用例文档: 74个
- README文件: 20个
- 模板文件: 2个
- 索引链接: 74个符号链接
- **总计**: ~170个文件

---

## 附录B: 时间估算

| 阶段 | 任务数 | 预估时间 | 说明 |
|------|--------|----------|------|
| 第一阶段 | 3个任务 | 2-3天 | 基础设施搭建 |
| 第二阶段 | 6个任务 | 8-10天 | SmartHome用例（36个） |
| 第三阶段 | 5个任务 | 8-10天 | CustomerService用例（38个） |
| 第四阶段 | 1个任务 | 1-2天 | 验证和完善 |
| **总计** | **15个任务** | **19-25天** | 约4-5周 |

**说明**:
- 每个用例文档编写时间: 30-60分钟
- 每个旅程README编写时间: 30分钟
- 验证和索引创建: 1-2天

---

## 附录C: 质量检查清单

在标记任务完成前，确保：

### 文档完整性
- [ ] 所有74个用例文档已创建
- [ ] 所有20个README文件已创建
- [ ] 所有元数据字段填写完整
- [ ] 所有代码示例格式正确

### 内容质量
- [ ] 业务目标清晰
- [ ] 执行流程详细
- [ ] 预期结果明确
- [ ] 测试要点全面
- [ ] 演示要点有价值

### 技术正确性
- [ ] Agent名称正确
- [ ] 能力标签准确
- [ ] 数据依赖合理
- [ ] 代码示例可执行

### 索引完整性
- [ ] 6个维度索引README已创建
- [ ] 符号链接正确建立
- [ ] 统计数据准确

### 验证通过
- [ ] 文件存在性检查通过
- [ ] 元数据验证脚本通过
- [ ] 统计脚本运行成功

---

## 执行说明

本实施计划按照以下原则设计：

1. **TDD**: 先创建模板和结构，再填充内容
2. **DRY**: 使用模板避免重复
3. **YAGNI**: 只创建当前需要的索引维度
4. **频繁提交**: 每完成一个旅程就提交一次
5. **小步迭代**: 每个任务都是2-5分钟可完成的步骤

执行时请：
- 严格按照步骤顺序执行
- 每完成一个Step就验证结果
- 遇到问题及时调整计划
- 保持文档版本更新

---

**计划版本**: v1.0
**创建日期**: 2026-03-15
**预计完成**: 2026-04-15（4周后）
**维护者**: CKY.MAF团队

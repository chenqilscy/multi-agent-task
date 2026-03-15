# [用例ID] 用例名称

---
metadata:
  case_id: [用例ID]
  journey: [用户旅程名称]
  journey_order: [在旅程中的顺序]

  case_type: primary  # primary | variant | exception | boundary
  domain: [smarthome | customerservice]
  complexity: L2  # L1单Agent | L2多轮对话 | L3多Agent协作 | L4复杂编排

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

  demo_value: 5  # 1-5星
  test_priority: P0  # P0必须 | P1重要 | P2一般
  doc_importance: high  # high | medium | low

  estimated_duration_seconds: [预估秒数]
  requires_external_service: [true | false]
  requires_hardware: [true | false]

  depends_on: []
  enables: [[相关用例ID]]

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

## ✅ 预期结果

- [x] [预期结果1]
- [x] [预期结果2]
- [x] [预期结果3]
- [x] [性能要求，如：全流程耗时 < X秒]

## 🧪 测试要点

### 功能测试
- [ ] [测试点1]
- [ ] [测试点2]

### 集成测试
- [ ] [集成测试点1]
- [ ] [集成测试点2]

### 异常测试（如适用）
- [ ] [异常测试点1]
- [ ] [异常测试点2]

## 💡 演示要点

### 展示亮点
1. **[亮点1标题]**: [描述]
2. **[亮点2标题]**: [描述]

### 演示脚本

**开场**: "您好，今天我将为您演示[用例名称]..."
**Step 1**: [操作说明] → **预期**: [预期结果]
**Step 2**: [操作说明] → **预期**: [预期结果]
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

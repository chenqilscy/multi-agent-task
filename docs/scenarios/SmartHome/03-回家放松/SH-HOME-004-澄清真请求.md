# SH-HOME-004 澄清真请求

---
metadata:
  case_id: SH-HOME-004
  journey: 回家放松
  journey_order: 4
  case_type: variant
  domain: smarthome
  complexity: L3
  agents: [ClimateAgent, LightingAgent]
  capabilities: [temperature-control, brightness-control, clarification]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 45
  requires_external_service: false
  requires_hardware: false
  depends_on: [SH-HOME-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-HOME-004
- **用例类型**: 多轮对话
- **演示价值**: ⭐⭐⭐
- **预估耗时**: 45秒

## 🎯 业务目标

用户意图不明确时，系统通过多轮对话澄清真实需求。

## 📝 执行流程

### Step 1: 模糊输入

**用户输入**: "帮我调一下"

**系统响应**:
```
请问您想调节什么呢？
1. 💡 灯光亮度
2. 🌡️ 空调温度
3. 🎵 音乐音量
```

### Step 2: 用户澄清

**用户输入**: "温度"

**系统响应**:
```
好的，您想把温度调到多少度？当前客厅温度24°C。
```

### Step 3: 具体指令

**用户输入**: "低一点，22度"

**系统响应**:
```
🌡️ 客厅空调已调至22°C ✅
```

## ✅ 预期结果

- [x] 模糊输入触发澄清对话
- [x] 提供明确选项（非开放式问题）
- [x] 3轮内完成意图确认
- [x] 最终正确执行用户意图

## 🧪 测试要点

- [ ] 低置信度（<0.3）触发澄清
- [ ] 选项覆盖主要设备类型
- [ ] 上下文保持跨轮次一致
- [ ] 历史实体自动填充（如房间）

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 帮我调一下 |
| 1 | 🤖 系统 | _(意图: GeneralQuery("帮我") score=0.2, 低于0.3 → 低置信度)_<br>请问您想做什么呢？<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 温度 |
| 2 | 🤖 系统 | _(意图: AdjustClimate("温度") score=1/7=0.143, 低于0.3)_<br>_(仍然低置信度)_ 请问您想做什么呢？... |
| 2 | 🤖 系统（理想） | 好的，您想把温度调到多少度？当前客厅温度24°C。 |
| 3 | 👤 用户 | 低一点，22度 |
| 3 | 🤖 系统 | _(意图: AdjustClimate("度"不在关键词中) → 可能匹配失败)_<br>_(若能到达 ClimateAgent)_<br>🌡️ 客厅空调已调至22°C ✅ |

### 代码执行追踪

**Turn 1 实际代码路径（✅ 与设计基本一致）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("帮我调一下")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("帮我调一下")
  │   ├─ GeneralQuery: "帮我" → matchCount=1, score=1/5=0.2
  │   ├─ 其余意图: 0 匹配
  │   └─ 结果: PrimaryIntent="GeneralQuery", Confidence=0.2
  │
  ├─ Step 2: Confidence 0.2 < 0.3 阈值
  │   └─ BuildLowConfidenceResponse(taskId, intent)
  │       ├─ candidateHints = [
  │       │   "控制设备（如：打开客厅的灯）",
  │       │   "查询天气（如：今天北京天气怎么样）",
  │       │   "查看温度历史（如：客厅最近的温度变化）",
  │       │   "播放音乐（如：播放轻音乐）"
  │       │ ]
  │       └─ 返回 MafTaskResponse { Success=false, NeedsClarification=true }
  │
  └─ ✅ 此步骤与设计预期一致：模糊输入触发澄清选项
```

**Turn 2 实际代码路径（"温度"）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("温度")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("温度")
  │   ├─ AdjustClimate: "温度" → matchCount=1, score=1/7=0.143
  │   ├─ QueryWeather: "温度怎么样" → "温度" 不完全匹配 "温度怎么样" → 0
  │   └─ 结果: PrimaryIntent="AdjustClimate", Confidence=0.143
  │
  ├─ Step 2: Confidence 0.143 < 0.3 阈值
  │   └─ 再次触发 BuildLowConfidenceResponse()
  │
  └─ ⚠️ 问题: 用户在澄清后回答"温度"，系统应理解上下文而非重新独立识别
      当前代码每次调用 ExecuteBusinessLogicAsync 都是独立的意图识别
      DialogStateManager 中有 PreviousIntent 字段，但未用于提升二次识别置信度
```

**Turn 3 实际代码路径（"低一点，22度"）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("低一点，22度")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("低一点，22度")
  │   ├─ 所有意图: 无关键词匹配
  │   │   （"度"不是 AdjustClimate 的关键词，关键词是"温度"）
  │   └─ 结果: PrimaryIntent="Unknown", Confidence=0.0
  │
  ├─ Step 2: Confidence 0.0 < 0.3
  │   └─ BuildLowConfidenceResponse()
  │
  └─ ⚠️ 完全丢失上下文，无法关联到上一轮的温度调节请求

  若通过 SmartHomeControlService 路由:
  ├─ ContainsKeywords("低一点，22度", ["空调","温度","制热","制冷","度"])
  │   → "度" 匹配 → 路由到 ClimateAgent ✅
  └─ ClimateAgent:
      ├─ Contains("冷") → false
      ├─ Contains("热") → false
      ├─ Contains("度") → true
      │   → ExtractTemperature("低一点，22度") → 22
      │   → 22 >= 16 && 22 <= 30 → 有效
      │   → IClimateService.SetTemperatureAsync("客厅", 22) → ✅
      └─ 返回: "客厅温度已设置为22度"
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 低置信度澄清 | ✅ "帮我调一下" 触发澄清 | ✅ GeneralQuery(0.2) < 0.3 → BuildLowConfidenceResponse — **与设计一致** |
| 多轮上下文 | 澄清后"温度"应在上下文中理解为AdjustClimate | 每次独立识别，"温度"单独score=0.143 < 0.3 → 再次低置信度 |
| 槽位继承 | 第3轮"低一点，22度"应继承 room="客厅" | DialogContext.HistoricalSlots 有此能力，但需意图先通过阈值 |
| 选项展示 | 提供明确选项（灯光/温度/音量） | BuildLowConfidenceResponse 提供4个通用选项，非针对"调节"的细分选项 |
| 所需改进 | ① 多轮对话中利用 PreviousIntent 提升后续轮次置信度<br>② 澄清回复后降低阈值或跳过阈值检查<br>③ BuildLowConfidenceResponse 可根据部分匹配生成更精确的选项 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

# SH-HOME-002 夏天回家

---
metadata:
  case_id: SH-HOME-002
  journey: 回家放松
  journey_order: 2
  case_type: variant
  domain: smarthome
  complexity: L3
  agents: [ClimateAgent, LightingAgent, MusicAgent, TemperatureHistoryAgent]
  capabilities: [temperature-control, brightness-control, music-play, temperature-history]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 40
  requires_external_service: false
  requires_hardware: false
  depends_on: [SH-HOME-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-HOME-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 40秒

## 🎯 业务目标

夏天回家场景：优先制冷，参考室内温度历史数据智能调温。

## 📝 执行流程

**用户输入**: "我回来了，好热"

**系统响应**:
```
欢迎回家！检测到室温较高...🌡️

📊 当前室温32°C（过去3小时上升了5°C）
🌡️ 空调已开启制冷模式，目标24°C
💡 客厅灯光已打开（冷色调，亮度60%）
🎵 轻音乐播放中

预计10分钟室温降到26°C，15分钟达到24°C。
```

## ✅ 预期结果

- [x] 识别"好热"关键词启用制冷模式
- [x] TemperatureHistoryAgent提供温度趋势
- [x] 灯光使用冷色调（夏天更凉爽）
- [x] 提供降温时间预估

## 🧪 测试要点

- [ ] 温度历史数据正确查询
- [ ] 制冷模式参数正确
- [ ] 灯光色调根据季节调整

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我回来了，好热 |
| 1 | 🤖 系统 | _(意图: AdjustClimate("热") score=0.143, 低于0.3)_<br>_(低置信度响应)_ 请问您想做什么呢？<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 1 | 🤖 系统（理想） | 欢迎回家！检测到室温较高...🌡️<br><br>📊 当前室温32°C（过去3小时上升了5°C）<br>🌡️ 空调已开启制冷模式，目标24°C<br>💡 客厅灯光已打开（冷色调，亮度60%）<br>🎵 轻音乐播放中<br><br>预计10分钟室温降到26°C，15分钟达到24°C。 |
| 2 | 👤 用户 | 再冷一点，调到22度 |
| 2 | 🤖 系统 | 🌡️ 客厅温度已调至22°C ✅ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("我回来了，好热")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("我回来了，好热")
  │   ├─ AdjustClimate: "热" → matchCount=1, score=1/7=0.143
  │   ├─ 其余意图: 0 匹配
  │   └─ 结果: PrimaryIntent="AdjustClimate", Confidence=0.143
  │
  ├─ Step 2: Confidence 0.143 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │
  └─ 流程终止，未进入 ClimateAgent

注意：如果阈值降低到 0.1，则会进入完整流程:
  ├─ EntityExtractor → { room: "客厅"(default) }
  ├─ CheckRequiredEntities("AdjustClimate", {}) → null（不强制澄清）
  ├─ TaskDecomposer → SubTasks
  └─ ClimateAgent.ExecuteBusinessLogicAsync()
```

**ClimateAgent 如果被调用的执行路径**:
```
ClimateAgent.ExecuteBusinessLogicAsync(request{UserInput: "我回来了，好热"})
  │
  ├─ room = request.Parameters["room"] ?? "客厅" → "客厅"
  ├─ userInput.Contains("冷") → false
  ├─ userInput.Contains("热") → true ✅
  │   └─ IClimateService.SetModeAsync("客厅", "heating")  ← ⚠️ BUG: 应为 "cooling"!
  │      实际代码: 匹配 "热" 后设置为 "heating"（制热），但用户说"好热"需要制冷
  │
  └─ 返回: "客厅空调已切换到制热模式"  ← ⚠️ 语义错误
```

**Turn 2 实际代码路径（如果第二轮正常执行）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("再冷一点，调到22度")
  │
  ├─ IntentRecognizer:
  │   ├─ AdjustClimate: "冷"→1匹配, "度"→ 不在关键词中（关键词是"制冷"不是"冷"）
  │   │   wait, "冷" IS a keyword → matchCount=1, score=1/7=0.143
  │   └─ 结果: AdjustClimate, Confidence=0.143 → < 0.3 → 低置信度
  │
  └─ 若能到达 ClimateAgent:
      ├─ userInput.Contains("冷") → true
      │   → IClimateService.SetModeAsync("客厅", "cooling") ✅
      ├─ userInput.Contains("度") → true
      │   → ExtractTemperature("再冷一点，调到22度") → 22
      │   → IClimateService.SetTemperatureAsync("客厅", 22) ✅
      └─ 注意: 代码先匹配"冷"返回，不会继续到"度"分支（if-else链）
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 回家意图 | "我回来了"触发回家模式 | 无"回家"意图关键词，"热"仅匹配 AdjustClimate(0.143) < 0.3 |
| 制冷/制热判断 | "好热" → 制冷(cooling) | ⚠️ ClimateAgent 的 "热" 分支调用 `SetModeAsync("heating")` — 语义错误，用户说热应该制冷 |
| 温度历史 | TemperatureHistoryAgent 提供室温趋势 | 理想路径需 TaskDecomposer 同时调用 TemperatureHistoryAgent |
| 冷色灯光 | LightingAgent 设置冷色60% | LightingAgent 无 SetColor API 调用（SimulatedLightingService.SetColorAsync 存在但未被 Agent 调用） |
| 制冷+调温 | 同时设模式和目标温度 | ClimateAgent 使用 if-else 链，匹配"冷"后直接返回，不会继续匹配"度"设温度 |
| 所需改进 | ① 修复 ClimateAgent "热"分支: "热"→cooling, "冷"→heating<br>② 支持同时设模式+温度<br>③ LightingAgent 增加色温控制分支<br>④ 增加"回家"复合场景意图 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

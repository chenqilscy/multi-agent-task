# SH-HOME-003 冬天回家

---
metadata:
  case_id: SH-HOME-003
  journey: 回家放松
  journey_order: 3
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

- **用例ID**: SH-HOME-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 40秒

## 🎯 业务目标

冬天回家场景：优先制热，暖色灯光营造温馨氛围。

## 📝 执行流程

**用户输入**: "我回来了，好冷"

**系统响应**:
```
欢迎回家！正在为您暖起来...🔥

📊 当前室温12°C（过去2小时下降了3°C）
🌡️ 空调已开启制热模式，目标26°C
💡 客厅灯光已打开（暖色调，亮度80%）
🎵 轻音乐播放中

预计15分钟室温升到22°C，20分钟达到26°C。
建议先喝杯热水暖暖身子☕
```

## ✅ 预期结果

- [x] 识别"好冷"启用制热模式
- [x] 目标温度26°C（比夏天高）
- [x] 暖色调灯光（营造温馨感）
- [x] 提供贴心建议

## 🧪 测试要点

- [ ] 制热模式参数正确
- [ ] 灯光暖色调设置
- [ ] 与夏天回家模式参数差异验证

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我回来了，好冷 |
| 1 | 🤖 系统 | _(意图: AdjustClimate("冷") score=0.143, 低于0.3)_<br>_(低置信度响应)_ 请问您想做什么呢？<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 1 | 🤖 系统（理想） | 欢迎回家！正在为您暖起来...🔥<br><br>📊 当前室温12°C（过去2小时下降了3°C）<br>🌡️ 空调已开启制热模式，目标26°C<br>💡 客厅灯光已打开（暖色调，亮度80%）<br>🎵 轻音乐播放中<br><br>预计15分钟室温升到22°C，20分钟达到26°C。<br>建议先喝杯热水暖暖身子☕ |
| 2 | 👤 用户 | 再暖一点，调到28度 |
| 2 | 🤖 系统 | 🌡️ 温度范围限制在16\~30°C，已调至28°C ✅ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("我回来了，好冷")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("我回来了，好冷")
  │   ├─ AdjustClimate: "冷" → matchCount=1, score=1/7=0.143
  │   ├─ 其余意图: 0 匹配
  │   └─ 结果: PrimaryIntent="AdjustClimate", Confidence=0.143
  │
  ├─ Step 2: Confidence 0.143 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │
  └─ 流程终止
```

**ClimateAgent 如果被调用的执行路径**:
```
ClimateAgent.ExecuteBusinessLogicAsync(request{UserInput: "我回来了，好冷"})
  │
  ├─ room = request.Parameters["room"] ?? "客厅" → "客厅"
  ├─ userInput.Contains("冷") → true ✅
  │   └─ IClimateService.SetModeAsync("客厅", "cooling")  ← ⚠️ 语义问题:
  │      用户说"好冷"想制热，但"冷"关键词映射到 "cooling"（制冷）
  │
  └─ 返回: "客厅空调已切换到制冷模式"  ← ⚠️ 与用户需求相反

  对比 SH-HOME-002 "好热"→"heating" 的问题:
  ClimateAgent 关键词映射:
    "冷"/"制冷" → SetModeAsync(room, "cooling")  ← 动作描述词: 制冷
    "热"/"制热"/"暖" → SetModeAsync(room, "heating") ← 动作描述词: 制热

  问题: 用户表达的是状态("好冷"=我感觉冷)，代码按动作处理("冷"=制冷)
  修复方案: 区分状态词("好冷"→制热) 和 动作词("制冷"→制冷)
```

**Turn 2 理想路径（"再暖一点，调到28度"）**:
```
ClimateAgent.ExecuteBusinessLogicAsync(request{UserInput: "再暖一点，调到28度"})
  │
  ├─ userInput.Contains("冷") → false
  ├─ userInput.Contains("热") → false
  ├─ userInput.Contains("暖") → true
  │   └─ IClimateService.SetModeAsync("客厅", "heating") → ✅ 正确
  │   └─ 直接返回，不继续检查温度值
  │
  └─ 注意: "28度"中的28不会被解析，因为匹配"暖"后已返回

  若走到 "度" 分支:
  ├─ ExtractTemperature("再暖一点，调到28度") → 28
  ├─ 28 >= 16 && 28 <= 30 → 有效
  └─ IClimateService.SetTemperatureAsync("客厅", 28) → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| "好冷"语义 | "好冷" → 用户感觉冷 → 制热(heating) | ⚠️ "冷" → `SetModeAsync("cooling")` — 语义反转，应为 heating |
| 暖色灯光 | LightingAgent 设置暖色80% | LightingAgent 无色温控制分支，SimulatedLightingService 有 SetColorAsync 未被调用 |
| 温度+模式 | 同时设制热模式和目标26°C | ClimateAgent if-else 链匹配"暖"后返回，不会继续解析温度值 |
| 温度历史 | TemperatureHistoryAgent 提供"过去2小时下降3°C"分析 | 需 TaskDecomposer 同时调用多个 Agent，当前单意图路径不支持 |
| 贴心提示 | "建议喝杯热水" | ResultAggregator 无生活建议生成逻辑 |
| 所需改进 | ① **关键修复**: 区分状态词/动作词 — "好冷"→heating, "制冷"→cooling<br>② ClimateAgent 支持同时设模式+温度<br>③ LightingAgent 增加暖色/冷色控制 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

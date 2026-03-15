# SH-HOME-001 标准回家模式

---
metadata:
  case_id: SH-HOME-001
  journey: 回家放松
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [ClimateAgent, LightingAgent, MusicAgent]
  capabilities: [temperature-control, brightness-control, music-play]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 5
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 40
  requires_external_service: false
  requires_hardware: false
  depends_on: []
  enables: [SH-HOME-002, SH-HOME-003]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-HOME-001
- **用户旅程**: 回家放松
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐
- **预估耗时**: 40秒

## 🎯 业务目标

用户到家后一句话营造舒适放松环境：开灯、调温、播放音乐。

## 📝 执行流程

### Step 1: 触发回家模式

**用户输入**: "我回来了"

**系统响应**:
```
欢迎回家！🏠 正在为您营造舒适环境...

💡 客厅灯光已打开，暖色调亮度70%
🌡️ 空调已设置为24°C，舒适模式
🎵 正在播放您喜欢的轻音乐

✅ 回家模式已激活！放松一下吧～
```

### Step 2: 个性化调整（可选）

**用户输入**: "灯光再暗一点"

**系统响应**:
```
💡 客厅灯光已调至50%亮度。
```

## 🗣️ 详细对话交互示例

### Turn 1: 触发回家模式

```
用户: 我回来了
```

**内部处理链**:
```
1. SmartHomeMainAgent.ExecuteBusinessLogicAsync("我回来了")
2. IntentRecognizer → 场景匹配: 回家模式
3. 编排决策: 并行调用 LightingAgent + ClimateAgent + MusicAgent
```

**Agent 调用详情**:
```
[LightingAgent] 打开客厅灯光
  → ILightingService.TurnOnAsync("客厅")
  → ILightingService.SetBrightnessAsync("客厅", 70)
  → ILightingService.SetColorAsync("客厅", "#FFD700")  // 暖色调
  → ✅

[ClimateAgent] 设置舒适温度
  → IClimateService.GetCurrentTemperatureAsync("客厅") → 返回: 18
  → IClimateService.SetTemperatureAsync("客厅", 24)
  → IClimateService.SetModeAsync("客厅", "auto")
  → ✅

[MusicAgent] 播放轻音乐
  → 模拟播放: { action: "播放", genre: "轻音乐" }
  → ✅
```

**系统输出**:
```
欢迎回家！🏠 正在为您营造舒适环境...

💡 客厅灯光已打开，暖色调亮度70%
🌡️ 空调已设置为24°C，舒适模式（当前室温18°C）
🎵 正在播放轻音乐

✅ 回家模式已激活！放松一下吧～
```

### Turn 2: 个性化调整

```
用户: 灯光再暗一点
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "ControlLight"（匹配"灯光""暗"）
2. EntityExtractor → { room: "客厅"(上下文继承), action: "调暗" }
3. LightingAgent.ExecuteAsync({command: "调暗", room: "客厅"})
   → ILightingService.SetBrightnessAsync("客厅", 30)  // 当前70% → 30%
```

**系统输出**:
```
💡 客厅灯光已调至30%亮度。
```

### Turn 3: 继续调整

```
用户: 换首歌
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "PlayMusic"（匹配"换歌"）
2. MusicAgent.ExecuteAsync({command: "下一首"})
```

**系统输出**:
```
🎵 正在为您切换下一首...
当前播放：《Spring Breeze》
```

## ✅ 预期结果

- [x] 客厅灯光打开（暖色调70%）
- [x] 空调设为24°C舒适模式
- [x] 音乐自动播放
- [x] 支持后续个性化微调
- [x] 全流程 < 40秒

## 🧪 测试要点

- [ ] 3个Agent并行执行
- [ ] 灯光暖色调参数正确
- [ ] 音乐播放不阻塞其他Agent
- [ ] 后续微调命令正确执行

## 📚 相关文档

- **相关用例**: [SH-HOME-002 夏天回家](./SH-HOME-002-夏天回家.md)

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

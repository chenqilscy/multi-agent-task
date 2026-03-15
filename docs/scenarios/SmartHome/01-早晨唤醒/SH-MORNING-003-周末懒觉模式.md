# SH-MORNING-003 周末懒觉模式

---
metadata:
  case_id: SH-MORNING-003
  journey: 早晨唤醒
  journey_order: 3
  case_type: variant
  domain: smarthome
  complexity: L3
  agents: [WeatherAgent, ClimateAgent, LightingAgent, MusicAgent]
  capabilities: [weather-query, temperature-control, brightness-control, music-play]
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
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-MORNING-003
- **用户旅程**: 早晨唤醒
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 60秒

## 🎯 业务目标

周末慢节奏唤醒：灯光更柔和、温度更舒适、播放轻音乐，营造放松氛围。

## 📝 执行流程

### Step 1: 周末问候

**用户输入**: "早上好，今天周末"

**系统响应**: 识别周末场景，降低唤醒强度。

```
早上好！周末好好休息～☕
```

### Step 2: 柔和唤醒

**系统自动执行**:
- WeatherAgent: 查询天气（简短播报）
- ClimateAgent: 温度设为24°C（比工作日高2度，更舒适）
- LightingAgent: 灯光渐亮到50%（比工作日低，更柔和），渐亮时间60秒
- MusicAgent: 自动播放轻音乐

```
📍 今天天气：晴，12~22°C，适合出门散步。

🌡️ 温度已调至24°C
💡 灯光柔和渐亮中（60秒到50%）
🎵 正在播放轻音乐...

享受慵懒的周末吧！☀️
```

## ✅ 预期结果

- [x] 温度设为24°C（高于工作日）
- [x] 灯光亮度50%（低于工作日80%），渐亮60秒
- [x] 自动播放轻音乐（无需用户选择）
- [x] 天气播报更简短

## 🧪 测试要点

### 功能测试
- [ ] 系统识别"周末"关键词并调整参数
- [ ] 温度、亮度、渐亮时间与工作日模式不同
- [ ] 音乐自动播放（无需询问）

## 📚 相关文档

- **父用例**: [SH-MORNING-001](./SH-MORNING-001-标准晨起唤醒.md)
- **对比用例**: [SH-MORNING-002 工作日模式](./SH-MORNING-002-工作日晨起模式.md)

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 早上好，今天周末 |
| 1 | 🤖 系统 | _(意图未明确匹配，触发低置信度响应)_ 早上好！请问您需要什么帮助？<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 帮我开个灯，放点音乐 |
| 2 | 🤖 系统（理想） | 早上好！周末好好休息～☕<br><br>💡 灯光柔和渐亮中（50%亮度）<br>🌡️ 温度已调至24°C<br>📍 今天天气：晴，12\~22°C，适合出门散步<br>🎵 正在播放轻音乐...<br><br>享受慵懒的周末吧！☀️ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("早上好，今天周末")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("早上好，今天周末")
  │   ├─ 扫描所有意图关键词:
  │   │   ControlLight: ["灯","照明","亮","暗","开灯","关灯"] → 0 匹配
  │   │   AdjustClimate: ["温度","空调","冷","热","暖","制冷","制热"] → 0 匹配
  │   │   PlayMusic: ["音乐","播放","歌曲","歌","音频"] → 0 匹配
  │   │   SecurityControl: ["门","锁","安全","门锁","摄像头"] → 0 匹配
  │   │   QueryWeather: ["天气","气温",...] → 0 匹配
  │   │   GeneralQuery: ["查询","状态","怎么","什么","帮我"] → 0 匹配
  │   └─ 结果: PrimaryIntent="Unknown", Confidence=0.0
  │
  ├─ Step 2: Confidence 0.0 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │       → 返回候选选项列表
  │
  └─ 流程终止，未进入 TaskDecomposer
```

**Turn 2 理想代码路径（需场景识别支持）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("帮我开个灯，放点音乐")
  │
  ├─ IntentRecognizer → 匹配结果:
  │   ├─ ControlLight: "灯" → score=1/6=0.167
  │   ├─ PlayMusic: "音乐"未匹配（"放"不在关键词中），但"播放"不在输入中
  │   ├─ GeneralQuery: "帮我" → score=1/5=0.2
  │   └─ 最高分: GeneralQuery 0.2 → 仍 < 0.3 → BuildLowConfidenceResponse
  │
  └─ 【理想路径】TaskDecomposer 分解为周末模式:
      ├─ SubTask 1: LightingAgent → SetBrightness("客厅", 50)  // 周末柔和
      ├─ SubTask 2: ClimateAgent → SetTemperature("客厅", 24)  // 比工作日高2°C
      ├─ SubTask 3: WeatherAgent → GetWeather(城市, today) // 简短播报
      └─ SubTask 4: MusicAgent → 播放("轻音乐")  // 自动播放，无需询问
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 周末识别 | 识别"周末"关键词，调整参数（亮度50%、温度24°C） | 无"周末"上下文感知逻辑，"早上好，今天周末"不匹配任何意图 |
| 参数差异化 | 工作日(100%/22°C) vs 周末(50%/24°C) | LightingAgent/ClimateAgent 无工作日/周末参数分支 |
| 自动播放 | 周末自动播放轻音乐，无需用户确认 | MusicAgent 需要包含"播放"关键词才能触发 |
| 渐亮效果 | LightingAgent 60秒渐亮到50% | SimulatedLightingService.SetBrightnessAsync 为立即设置，无渐亮API |
| 所需改进 | 添加复合场景意图 "WeekendMorning"；LightingService 增加渐亮参数；建立工作日/周末参数配置表 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

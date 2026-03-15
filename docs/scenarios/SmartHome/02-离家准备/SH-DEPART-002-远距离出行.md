# SH-DEPART-002 远距离出行

---
metadata:
  case_id: SH-DEPART-002
  journey: 离家准备
  journey_order: 2
  case_type: variant
  domain: smarthome
  complexity: L3
  agents: [WeatherAgent, LightingAgent, ClimateAgent]
  capabilities: [weather-query, light-control, temperature-control, forecast]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 40
  requires_external_service: true
  requires_hardware: false
  depends_on: [SH-DEPART-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-DEPART-002
- **用户旅程**: 离家准备
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 40秒

## 🎯 业务目标

长时间外出（出差/旅行），需要完全关闭设备并查询目的地天气。

## 📝 执行流程

### Step 1: 触发

**用户输入**: "我要出差去上海，三天后回来"

**系统响应**:
1. 意图识别: 离家模式（长时间）
2. 实体提取: destination="上海", duration=3天

### Step 2: 深度关闭 + 目的地天气

```
🏠 长时间外出模式启动...

💡 所有灯光已关闭 ✅
🌡️ 空调已完全关闭（长时间外出节能）✅

📍 上海未来3天天气预报：
- 3月15日：多云，14~20°C
- 3月16日：小雨，12~18°C ☔ 记得带伞
- 3月17日：阴，13~19°C

👔 出差建议：带雨具，穿薄外套+一件防风衣

✅ 长时间外出模式已激活，祝出差顺利！
```

## ✅ 预期结果

- [x] 空调完全关闭（非节能模式）
- [x] 目的地多天天气预报正确
- [x] 携带物品建议合理

## 🧪 测试要点

- [ ] 区分短时外出和长时间外出
- [ ] 多天预报数据正确
- [ ] 空调关闭策略不同（节能 vs 完全关闭）

## 📚 相关文档

- **父用例**: [SH-DEPART-001](./SH-DEPART-001-标准离家模式.md)

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要出差去上海，三天后回来 |
| 1 | 🤖 系统 | _(意图未明确匹配，触发低置信度响应)_ 您好！请问您需要什么帮助？<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 帮我关掉所有设备，查一下上海天气 |
| 2 | 🤖 系统（理想） | 🏠 长时间外出模式启动...<br><br>💡 所有灯光已关闭 ✅<br>🌡️ 空调已完全关闭 ✅<br><br>📍 上海未来3天天气预报：<br>- 3月15日：多云，14\~20°C<br>- 3月16日：小雨，12\~18°C ☔ 记得带伞<br>- 3月17日：阴，13\~19°C<br><br>👔 出差建议：带雨具，穿薄外套+一件防风衣<br>✅ 长时间外出模式已激活，祝出差顺利！ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("我要出差去上海，三天后回来")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("我要出差去上海，三天后回来")
  │   ├─ 扫描所有意图关键词:
  │   │   ControlLight: → 0 匹配
  │   │   AdjustClimate: → 0 匹配
  │   │   PlayMusic: → 0 匹配
  │   │   SecurityControl: → 0 匹配（"出差"不含"门"/"锁"等）
  │   │   QueryWeather: → 0 匹配（"上海"不是天气关键词）
  │   │   GeneralQuery: → 0 匹配
  │   └─ 结果: PrimaryIntent="Unknown", Confidence=0.0
  │
  ├─ Step 2: Confidence 0.0 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │
  └─ 流程终止，未进入 TaskDecomposer/EntityExtractor
      注意: EntityExtractor 能提取 city="上海"，但因意图未识别而未被调用
```

**Turn 2 理想代码路径（需 TaskDecomposer 支持长外出场景分解）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("帮我关掉所有设备，查一下上海天气")
  │
  ├─ IntentRecognizer → 匹配结果:
  │   ├─ ControlLight: "关" 不在关键词列表中（需"关灯"）
  │   ├─ QueryWeather: "天气" → score=1/8=0.125
  │   ├─ GeneralQuery: "帮我" → score=1/5=0.2
  │   └─ 最高分: GeneralQuery 0.2 → < 0.3 → BuildLowConfidenceResponse
  │
  └─ 【理想路径】TaskDecomposer 分解为长外出模式:
      ├─ SubTask 1: LightingAgent → TurnOff(所有房间)
      ├─ SubTask 2: ClimateAgent → 完全关闭（非节能模式）
      ├─ SubTask 3: WeatherAgent → GetForecastAsync("上海", 3)
      │   └─ EntityExtractor 提取: city="上海", duration=3天
      └─ SubTask 4: SecurityAgent → EnableAwayMode() (可选)
```

**WeatherAgent.GetForecastAsync 调用路径（如果能到达）**:
```
WeatherAgent.ExecuteBusinessLogicAsync(request)
  │
  ├─ 参数: city="上海"（从 EntityExtractor 提取）
  ├─ 城市非空 → 不触发澄清
  │
  └─ SimulatedWeatherService.GetForecastAsync("上海", 3)
      ├─ Day 1: GetWeatherAsync("上海", 2026-03-15) → { Condition: 随机, Temp: 16±3 }
      ├─ Day 2: GetWeatherAsync("上海", 2026-03-16) → { Condition: 随机, Temp: 16±3 }
      └─ Day 3: GetWeatherAsync("上海", 2026-03-17) → { Condition: 随机, Temp: 16±3 }
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 出行意图 | 识别"出差""出行"等长外出语义 | 无 "LongAway" 意图，"出差"不匹配任何关键词 |
| 实体提取 | 提取 destination="上海", duration=3天 | EntityExtractor 可提取 city，但因意图未匹配未被调用 |
| 设备深度关闭 | 区分短时外出(节能) vs 长时外出(完全关闭) | LightingAgent/ClimateAgent 无长/短外出模式区分 |
| 多天预报 | WeatherAgent 调用 GetForecastAsync(city, days) | WeatherAgent.ExecuteBusinessLogicAsync 仅调用单日 GetWeatherAsync，未使用 GetForecastAsync |
| 携带建议 | 根据多天天气生成出行物品建议 | WeatherAgent.FormatWeatherResponse 只有单日穿衣建议 |
| 所需改进 | ① 增加 "LongAway" 意图关键词（出差/旅行/外出多天）<br>② WeatherAgent 增加多天预报响应路径<br>③ 增加设备完全关闭 vs 节能模式区分 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

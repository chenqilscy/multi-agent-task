# SH-MORNING-002 工作日晨起模式

---
metadata:
  case_id: SH-MORNING-002
  journey: 早晨唤醒
  journey_order: 2
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

- **用例ID**: SH-MORNING-002
- **用户旅程**: 早晨唤醒
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 60秒

## 🎯 业务目标

工作日早晨增强唤醒：标准功能 + 播放新闻/音乐，帮助用户快速清醒。

## 📝 执行流程

### Step 1: 标准唤醒

**用户输入**: "早上好，今天要上班"

**系统响应**: 识别工作日场景，执行标准天气+温度+灯光流程（同SH-MORNING-001），温度设为22°C。

### Step 2: 媒体选择

**系统提示**: "要为您播放新闻还是音乐呢？"

**用户输入**: "播放新闻吧"

**系统响应**:
1. 意图识别: PlayMusic（type=news）
2. Agent调用: MusicAgent.ExecuteAsync()
3. 返回: "📰 正在为您播放今日新闻摘要..."

### Step 3: 完成

```
✅ 工作日晨起模式已激活！
☀️ 天气已查询 | 🌡️ 温度22°C | 💡 灯光已调亮 | 📰 新闻播放中
祝您今天工作顺利！
```

## ✅ 预期结果

- [x] 包含SH-MORNING-001的所有功能
- [x] MusicAgent成功播放新闻
- [x] 4个Agent协作无冲突

## 🧪 测试要点

### 功能测试
- [ ] 继承SH-MORNING-001所有测试点
- [ ] MusicAgent成功播放指定类型媒体
- [ ] 4个Agent协作顺序正确

## 📚 相关文档

- **父用例**: [SH-MORNING-001](./SH-MORNING-001-标准晨起唤醒.md)
- **Agent实现**: `src/Demos/SmartHome/Agents/MusicAgent.cs`

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 早上好，今天要上班 |
| 1 | 🤖 系统 | _(意图未明确匹配，触发低置信度响应)_ 您好！请问您想做什么呢？<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 帮我打开灯，查下天气 |
| 2 | 🤖 系统 | _(此输入匹配: ControlLight("灯") + QueryWeather("天气") + GeneralQuery("帮我"))_<br>_(当前代码仅取最高分意图，不支持多意图并行)_<br>_(假设理想流程通过 TaskDecomposer 分解)_ |
| 2 | 🤖 系统（理想） | ☀️ 正在为您启动工作日晨起模式...<br>💡 客厅灯光已打开（亮度100%）<br>🌡️ 空调已调至22°C<br>📍 请问您想查询哪个城市的天气？ |
| 3 | 👤 用户 | 北京 |
| 3 | 🤖 系统 | 📍 今天北京：晴转多云，12\~22°C，东风2级<br>👔 建议穿薄外套 |
| 4 | 👤 用户 | 播放新闻吧 |
| 4 | 🤖 系统 | 📰 正在为您播放今日新闻摘要...<br><br>✅ 工作日晨起模式已激活！<br>☀️ 天气已查询 \| 🌡️ 温度22°C \| 💡 灯光已调亮 \| 📰 新闻播放中<br>祝您今天工作顺利！ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("早上好，今天要上班")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("早上好，今天要上班")
  │   ├─ 扫描所有意图关键词:
  │   │   ControlLight: ["灯","照明","亮","暗","开灯","关灯"] → 0 匹配
  │   │   AdjustClimate: ["温度","空调","冷","热","暖","制冷","制热"] → 0 匹配
  │   │   PlayMusic: ["音乐","播放","歌曲","歌","音频"] → 0 匹配
  │   │   SecurityControl: ["门","锁","安全","门锁","摄像头"] → 0 匹配
  │   │   QueryWeather: ["天气","气温","下雨","晴天","预报","穿什么","温度怎么样","气候"] → 0 匹配
  │   │   QueryTemperatureHistory: → 0 匹配
  │   │   GeneralQuery: ["查询","状态","怎么","什么","帮我"] → 0 匹配
  │   └─ 结果: PrimaryIntent="Unknown", Confidence=0.0
  │
  ├─ Step 2: Confidence 0.0 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │       → 返回4个候选选项（控制设备/查询天气/查看温度历史/播放音乐）
  │
  └─ 流程终止，未进入 TaskDecomposer
```

**Turn 2 理想代码路径（需 TaskDecomposer 支持多意图分解）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("帮我打开灯，查下天气")
  │
  ├─ IntentRecognizer → ControlLight(匹配"灯") score=1/6, GeneralQuery(匹配"帮我") score=1/5
  │   └─ 最高分: GeneralQuery 0.2 → 仍 < 0.3 → BuildLowConfidenceResponse
  │
  └─ 【理想路径】TaskDecomposer.DecomposeAsync() 分解为:
      ├─ SubTask 1: LightingAgent → TurnOn("客厅"), SetBrightness(100)
      ├─ SubTask 2: ClimateAgent → SetTemperature("客厅", 22)
      ├─ SubTask 3: WeatherAgent → 需要 city 实体 → 触发澄清
      └─ SubTask 4: MusicAgent → 等待用户选择
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 场景识别 | 识别"工作日晨起"组合场景 | 无"晨起模式"意图，"早上好，今天要上班"不匹配任何关键词 → Unknown |
| 多Agent协调 | TaskDecomposer 分解为4个并行子任务 | TaskDecomposer 依赖意图识别结果，Unknown 意图下不会分解 |
| 上下文记忆 | 记住"工作日"偏好用于后续调参 | DialogStateManager 可存储，但无工作日/周末区分逻辑 |
| 所需改进 | 添加复合场景意图（如 "MorningRoutine"），或降低 GeneralQuery 触发阈值，使 TaskDecomposer 可基于NLP进行多任务分解 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

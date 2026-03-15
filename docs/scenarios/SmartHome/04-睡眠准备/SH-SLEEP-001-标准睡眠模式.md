# SH-SLEEP-001 标准睡眠模式

---
metadata:
  case_id: SH-SLEEP-001
  journey: 睡眠准备
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, ClimateAgent]
  capabilities: [brightness-control, temperature-control]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 30
  requires_external_service: false
  requires_hardware: false
  depends_on: []
  enables: [SH-SLEEP-002, SH-SLEEP-003]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-SLEEP-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐
- **预估耗时**: 30秒

## 🎯 业务目标

营造助眠环境：逐渐调暗灯光、调低温度、关闭不必要设备。

## 📝 执行流程

### Step 1: 触发睡眠模式

**用户输入**: "我要睡觉了" 或 "晚安"

**系统响应**:
```
晚安！🌙 正在为您营造助眠环境...

💡 灯光处理：
  - 客厅灯 → 已关闭
  - 卧室灯 → 渐暗至5%（夜灯模式，60秒内）

🌡️ 空调已调至睡眠模式（22°C，低风速）

✅ 睡眠模式已激活，祝您好梦！💤
```

## ✅ 预期结果

- [x] 客厅灯关闭
- [x] 卧室灯渐暗至5%夜灯模式
- [x] 空调切换睡眠模式（22°C低风速）
- [x] 全流程 < 30秒

## 🧪 测试要点

- [ ] 灯光渐暗效果（60秒到5%）
- [ ] 空调睡眠模式参数正确
- [ ] "晚安""睡觉了"等多种触发词

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要睡觉了 |
| 1 | 🤖 系统 | _(无意图匹配，Unknown score=0.0 → 低置信度)_<br>请问您想做什么呢？<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 1 | 🤖 系统（理想） | 晚安！🌙 正在为您营造助眠环境...<br><br>💡 灯光处理：<br>  - 客厅灯 → 已关闭<br>  - 卧室灯 → 渐暗至5%（夜灯模式）<br>🌡️ 空调已调至睡眠模式（22°C，低风速）<br><br>✅ 睡眠模式已激活，祝您好梦！💤 |
| 2 | 👤 用户 | 帮我关灯，空调调到22度 |
| 2 | 🤖 系统 | _(ControlLight("灯","关灯") score=2/6=0.333 ≥ 0.3 ✅)_<br>💡 客厅的灯已关闭<br>_(注: 当前单意图路径只处理关灯，不会同时调温度)_ |
| 3 | 👤 用户 | 空调调到22度 |
| 3 | 🤖 系统 | _(AdjustClimate("空调") score=1/7=0.143 < 0.3 → 低置信度)_<br>_(若通过 SmartHomeControlService 路由: "空调"+"度" 匹配)_<br>🌡️ 客厅温度已设置为22度 ✅ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("我要睡觉了")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("我要睡觉了")
  │   ├─ 扫描所有意图关键词:
  │   │   ControlLight: ["灯","照明","亮","暗","开灯","关灯"] → 0 匹配
  │   │   AdjustClimate: ["温度","空调","冷","热","暖","制冷","制热"] → 0 匹配
  │   │   PlayMusic: ["音乐","播放","歌曲","歌","音频"] → 0 匹配
  │   │   SecurityControl: ["门","锁","安全","门锁","摄像头"] → 0 匹配
  │   │   QueryWeather: → 0 匹配
  │   │   QueryTemperatureHistory: → 0 匹配
  │   │   GeneralQuery: ["查询","状态","怎么","什么","帮我"] → 0 匹配
  │   └─ 结果: PrimaryIntent="Unknown", Confidence=0.0
  │
  ├─ Step 2: Confidence 0.0 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │
  └─ 流程终止，"睡觉""晚安"均无法匹配任何已定义意图
```

**Turn 2 实际代码路径（"帮我关灯，空调调到22度"）**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("帮我关灯，空调调到22度")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync(...)
  │   ├─ ControlLight: "灯"→1, "关灯"→1 → matchCount=2, score=2/6=0.333 ✅ (≥0.3)
  │   ├─ AdjustClimate: "空调"→1 → score=1/7=0.143
  │   ├─ GeneralQuery: "帮我"→1 → score=1/5=0.2
  │   └─ 最高分: ControlLight 0.333 ✅
  │
  ├─ Step 2: Confidence 0.333 ≥ 0.3 → 通过阈值 ✅
  │
  ├─ Step 3: EntityExtractor → { room: "客厅"(default) }
  │
  ├─ Step 4: CheckRequiredEntities("ControlLight", {...}) → null
  │
  ├─ Step 5: TaskDecomposer.DecomposeAsync() → SubTasks
  │   └─ 基于 ControlLight 意图分解，可能只生成灯光相关子任务
  │
  ├─ Step 6-7: AgentMatcher + TaskOrchestrator
  │   └─ LightingAgent.ExecuteBusinessLogicAsync()
  │       ├─ userInput.Contains("关灯") → true
  │       ├─ ILightingService.TurnOffAsync("客厅") → ✅
  │       └─ 返回: "客厅的灯已关闭"
  │
  └─ ⚠️ "空调调到22度" 部分被忽略（单意图识别只取最高分）
```

**Turn 3 通过 SmartHomeControlService 路径（"空调调到22度"）**:
```
SmartHomeControlService.ProcessCommandAsync("空调调到22度")
  │
  ├─ ContainsKeywords("空调调到22度", ["空调","温度","制热","制冷","度"])
  │   → "空调" 匹配 ✅, "度" 匹配 ✅ → 路由到 ClimateAgent
  │
  └─ ClimateAgent.ExecuteBusinessLogicAsync(request)
      ├─ room = "客厅"(default)
      ├─ Contains("冷") → false
      ├─ Contains("热") → false
      ├─ Contains("度") → true
      │   └─ ExtractTemperature("空调调到22度")
      │       → 扫描数字: "22" → int.TryParse → 22
      │       → 22 >= 16 && 22 <= 30 → 有效 ✅
      │       → IClimateService.SetTemperatureAsync("客厅", 22)
      └─ 返回: "客厅温度已设置为22度" ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 睡眠意图 | "我要睡觉了""晚安"触发睡眠模式 | 无 "SleepMode" 意图，两个触发词均不匹配任何关键词 → Unknown |
| 多设备协调 | 同时关灯+调温(睡眠模式) | 单意图路径只处理最高分意图，无法一次处理灯光+温度 |
| 灯光渐暗 | 卧室灯60秒渐暗至5%夜灯 | SimulatedLightingService.SetBrightnessAsync 立即设置，无渐暗参数 |
| 分区控制 | 客厅灯关闭 + 卧室灯渐暗 | LightingAgent 根据 room 参数操作单个房间，无多房间批量操作 |
| 睡眠模式温度 | 22°C + 低风速 | ClimateAgent 可设温度但无"低风速"API |
| 所需改进 | ① 添加 "SleepMode" 意图关键词（睡觉/晚安/睡眠/休息）<br>② TaskDecomposer 支持睡眠场景分解为多个子任务<br>③ LightingAgent 支持渐暗参数和多房间批量操作<br>④ ClimateAgent 增加睡眠模式（低风速）支持 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

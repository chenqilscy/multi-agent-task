# SH-GUEST-003 商务洽谈模式

---
metadata:
  case_id: SH-GUEST-003
  journey: 会客模式
  journey_order: 3
  case_type: variant
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, music-playback, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 25
  depends_on: [SH-GUEST-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-GUEST-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

商务洽谈场景，正式灯光、安静环境、适宜温度。

## 📝 执行流程

**用户输入**: "有客户要来谈业务，帮我调整一下"

**系统响应**:
```
💼 正在切换到商务洽谈模式...

💡 灯光调整：
  - 客厅主灯 → 正白光 90%
  - 书房灯 → 正白光 85%

🎵 音乐设置：
  - 已关闭背景音乐（保持安静环境）

🌡️ 温度调整：
  - 空调 → 24°C 静音模式

商务洽谈模式已激活 ✅ 祝会议顺利！
```

## ✅ 预期结果

- [x] 灯光调至明亮正式的白光
- [x] 音乐关闭避免干扰
- [x] 空调静音模式减少噪音

## 🧪 测试要点

- [ ] "商务/业务"意图准确识别
- [ ] MusicAgent正确执行关闭音乐操作
- [ ] 空调静音模式正确设置

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 有客户要来谈业务，帮我调整一下 |
| 1 | 🤖 系统 | _(IntentRecognizer: "帮我" → GeneralQuery, Confidence≈0.2 < 0.3)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 把客厅灯调亮，白光 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯"+"亮" → LightingAgent)_<br>💡 客厅灯已调至白光，亮度100% ✅ |
| 3 | 👤 用户 | 把音乐停了 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "音乐" → MusicAgent)_<br>🎵 音乐已停止播放 ✅ |
| 4 | 👤 用户 | 空调调到24度 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 空调已调至24°C ✅<br><br>🤝 商务洽谈环境已准备就绪！ |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("把客厅灯调亮，白光")
  ├─ ContainsKeywords("灯","亮") → true → LightingAgent
  ├─ ExtractRoomEntity("客厅") → "客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ 解析"调亮" → ILightingService.SetBrightnessAsync("客厅", 100) → ✅
      └─ 解析"白光" → ILightingService.SetColorAsync("客厅", "white") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 商务模式识别 | 一键进入商务洽谈模式 | 无"BusinessMode"意图，需分步操作 |
| 空调静音模式 | ClimateAgent支持静音模式 | SimulatedClimateService仅支持SetMode(cooling/heating/fan/auto)，无静音模式 |
| 音乐关闭 | 自动关闭避免干扰 | 需用户手动发"停止音乐"命令 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

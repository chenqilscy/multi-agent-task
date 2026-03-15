# SH-GUEST-001 标准会客模式

---
metadata:
  case_id: SH-GUEST-001
  journey: 会客模式
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, music-playback, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 30
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-GUEST-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

客人来访时，一键激活会客模式，灯光调至明亮温馨、播放轻柔背景音乐、温度调至舒适。

## 📝 执行流程

**用户输入**: "有客人要来，帮我准备一下"

**系统响应**:
```
🎉 正在切换到会客模式...

💡 灯光调整：
  - 客厅主灯 → 暖白光 80%
  - 走廊灯 → 暖白光 60%
  - 餐厅灯 → 暖白光 70%

🎵 音乐设置：
  - 播放列表：轻柔爵士
  - 音量：30%

🌡️ 温度调整：
  - 客厅空调 → 24°C 送风模式

会客模式已激活 ✅ 欢迎您的客人！
```

## ✅ 预期结果

- [x] 三个Agent协作完成调节
- [x] 灯光调至暖白光温馨氛围
- [x] 背景音乐选择适当曲风和音量
- [x] 温度调至舒适范围

## 🧪 测试要点

- [ ] 意图识别准确提取"会客"意图
- [ ] 多Agent并行调度
- [ ] 响应聚合展示完整信息

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 有客人要来，帮我准备一下 |
| 1 | 🤖 系统 | _(IntentRecognizer: "帮我" → GeneralQuery, Confidence≈0.2)_<br>_(Confidence 0.2 < 0.3 → BuildLowConfidenceResponse)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 打开客厅灯，调到暖色 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 客厅灯已打开，亮度80%，暖色调 ✅ |
| 3 | 👤 用户 | 播放轻柔音乐 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "音乐"+"播放" → MusicAgent)_<br>🎵 正在为您播放轻柔音乐... |
| 4 | 👤 用户 | 空调调到24度 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 客厅空调已设置为24°C<br><br>🎉 会客准备完毕！<br>💡 灯光暖色80% \| 🎵 轻音乐播放中 \| 🌡️ 24°C |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("打开客厅灯，调到暖色")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity("客厅") → "客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ 解析"打开" → ILightingService.TurnOnAsync("客厅") → ✅
      ├─ 解析"暖色" → ILightingService.SetColorAsync("客厅", "warm") → ✅
      └─ ILightingService.SetBrightnessAsync("客厅", 80) → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| "会客模式"识别 | 一句话激活整个会客场景 | 无"GuestMode"意图，"客人"不匹配关键词，需分步控制 |
| 多Agent并行 | 同时调用Lighting+Music+Climate | SmartHomeControlService串行if-else，一次仅调用一个Agent |
| 多房间灯光 | 客厅+走廊+餐厅同时调整 | 每次调用只处理一个房间 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

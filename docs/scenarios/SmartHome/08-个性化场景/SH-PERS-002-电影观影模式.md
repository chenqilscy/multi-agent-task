# SH-PERS-002 电影观影模式

---
metadata:
  case_id: SH-PERS-002
  journey: 个性化场景
  journey_order: 2
  case_type: primary
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, music-playback, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 25
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-PERS-002
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

电影观影场景，灯光调暗营造影院氛围、关闭背景音乐、温度舒适。

## 📝 执行流程

**用户输入**: "我要看电影了"

**系统响应**:
```
🎬 正在切换到观影模式...

💡 灯光调整：
  - 客厅主灯 → 关闭
  - 电视背景灯 → 暖黄色 10%
  - 走廊灯 → 5%（安全照明）

🎵 音乐设置：
  - 已关闭背景音乐
  - 音响模式 → 影院环绕声

🌡️ 温度调整：
  - 空调 → 24°C 静音模式

观影模式已激活 🍿 享受电影吧！
```

## ✅ 预期结果

- [x] 灯光调暗至影院氛围
- [x] 保留安全照明
- [x] 背景音乐关闭
- [x] 空调静音不干扰

## 🧪 测试要点

- [ ] "看电影/观影"意图识别
- [ ] 三个Agent协调工作
- [ ] 电视背景灯微亮效果

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要看电影了 |
| 1 | 🤖 系统 | _(IntentRecognizer: 无关键词匹配 → Unknown, Confidence=0.0)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 客厅灯调暗一点，暖色 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯"+"暗" → LightingAgent)_<br>💡 客厅灯已调至亮度20%，暖色调 ✅ |
| 3 | 👤 用户 | 空调调到24度 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 空调已调至24°C ✅ |
| 4 | 👤 用户 | 音乐停一下 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "音乐" → MusicAgent)_<br>🎵 音乐已暂停 ✅<br><br>🎬 观影环境已准备就绪！ |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("客厅灯调暗一点，暖色")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity("客厅") → "客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ 解析"调暗" → ILightingService.SetBrightnessAsync("客厅", 20) → ✅
      └─ 解析"暖色" → ILightingService.SetColorAsync("客厅", "warm") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 观影模式 | "看电影" 一键进入观影模式 | 无MovieMode意图，需分步操作 |
| 电视背景灯 | 微亮背景灯护眼 | LightingAgent可设低亮度，但无“背景灯”特殊模式 |
| 空调静音 | 调低风速避免干扰 | ClimateAgent无静音/低风速模式 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

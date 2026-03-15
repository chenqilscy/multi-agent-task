# SH-PERS-003 运动健身模式

---
metadata:
  case_id: SH-PERS-003
  journey: 个性化场景
  journey_order: 3
  case_type: primary
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, music-playback, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 20
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-PERS-003
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

运动健身场景，明亮灯光、动感音乐、温度适当降低。

## 📝 执行流程

**用户输入**: "开始运动"

**系统响应**:
```
💪 正在切换到健身模式...

💡 灯光调整：
  - 客厅灯 → 正白光 100%

🎵 音乐设置：
  - 播放列表：动感健身
  - 音量：50%

🌡️ 温度调整：
  - 空调 → 20°C 强力制冷

健身模式已激活 🏃 加油！
```

## ✅ 预期结果

- [x] 明亮白光照明
- [x] 动感音乐激励
- [x] 温度降低适应运动

## 🧪 测试要点

- [ ] "运动/健身"意图识别
- [ ] 温度设置偏低（20°C）
- [ ] 音乐风格匹配

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 开始运动 |
| 1 | 🤖 系统 | _(IntentRecognizer: 无关键词匹配 → Unknown, Confidence=0.0)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 播放动感音乐 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "音乐"+"播放" → MusicAgent)_<br>🎵 正在播放动感音乐 ✅ |
| 3 | 👤 用户 | 空调调到20度，制冷 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度"+"制冷" → ClimateAgent)_<br>🌡️ 空调已调至20°C 制冷模式 ✅ |
| 4 | 👤 用户 | 客厅灯调亮 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "灯"+"亮" → LightingAgent)_<br>💡 客厅灯已调至100%亮度 ✅<br><br>🏋️ 运动环境已准备就绪！加油！ |

### 代码执行追踪

**Turn 3 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("空调调到20度，制冷")
  ├─ ContainsKeywords("空调","度","制冷") → true → ClimateAgent
  └─ ClimateAgent.ExecuteBusinessLogicAsync(request)
      ├─ 提取温度: 20 (范围16-30 → 合法)
      ├─ IClimateService.SetTemperatureAsync("客厅", 20) → ✅
      └─ IClimateService.SetModeAsync("客厅", "cooling") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 运动模式 | 一键启动运动场景 | 无ExerciseMode意图，需分步操作 |
| 音乐风格 | 自动选择动感曲目 | MusicAgent仅支持播放/暂停，无风格选择 |
| 空气循环 | 开启新风系统 | ClimateAgent无新风系统控制 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

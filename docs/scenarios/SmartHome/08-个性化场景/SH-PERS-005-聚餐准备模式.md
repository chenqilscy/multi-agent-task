# SH-PERS-005 聚餐准备模式

---
metadata:
  case_id: SH-PERS-005
  journey: 个性化场景
  journey_order: 5
  case_type: multi-agent
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, music-playback, temperature-control, multi-room-control]
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

- **用例ID**: SH-PERS-005
- **用例类型**: 多Agent协作
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

家庭聚餐场景，餐厅为主的灯光布置、欢快音乐、多区域温度协调。

## 📝 执行流程

**用户输入**: "今晚要聚餐，帮我准备一下"

**系统响应**:
```
🍽️ 正在切换到聚餐模式...

💡 灯光调整：
  - 餐厅灯 → 暖黄光 85%（主用餐区域）
  - 客厅灯 → 暖白光 60%（休息区域）
  - 厨房灯 → 正白光 100%（备餐区域）

🎵 音乐设置：
  - 播放列表：欢快聚会
  - 音量：25%

🌡️ 温度调整：
  - 餐厅空调 → 23°C
  - 客厅空调 → 24°C
  - 厨房通风 → 已开启

聚餐模式已激活 🎉 祝用餐愉快！
```

## ✅ 预期结果

- [x] 餐厅区域灯光突出
- [x] 厨房明亮方便备餐
- [x] 厨房通风开启排油烟
- [x] 音乐欢快但不影响交谈

## 🧪 测试要点

- [ ] "聚餐"意图识别
- [ ] 多房间差异化设置
- [ ] 三个Agent协作完成

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 今晚要聚餐，帮我准备一下 |
| 1 | 🤖 系统 | _(IntentRecognizer: "帮我" → GeneralQuery, Confidence≈0.2 < 0.3)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 餐厅灯调亮，暖色 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯"+"亮" → LightingAgent)_<br>_(EntityExtractor: room="餐厅")_<br>💡 餐厅灯已调至亮度80%，暖色调 ✅ |
| 3 | 👤 用户 | 客厅灯也调亮 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "灯"+"亮" → LightingAgent)_<br>💡 客厅灯已调至亮度80% ✅ |
| 4 | 👤 用户 | 播放轻松音乐 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "音乐"+"播放" → MusicAgent)_<br>🎵 正在播放轻松音乐 ✅ |
| 5 | 👤 用户 | 空调调到24度 |
| 5 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 空调已调至24°C ✅<br><br>🍽️ 聚餐环境已准备就绪！祝您用餐愉快！ |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("餐厅灯调亮，暖色")
  ├─ ContainsKeywords("灯","亮") → true → LightingAgent
  ├─ ExtractRoomEntity("餐厅") → "餐厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ ILightingService.SetBrightnessAsync("餐厅", 80) → ✅
      └─ ILightingService.SetColorAsync("餐厅", "warm") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 聚餐模式 | 一键启动聚餐场景 | 无DinnerMode意图，需分步操作 |
| 多房间差异化 | 餐厅亮+客厅暖+厨房明 | ControlService每次只处理一个房间 |
| 多Agent并行 | 同时调用Lighting+Music+Climate | 串行if-else，一次仅调用一个Agent |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

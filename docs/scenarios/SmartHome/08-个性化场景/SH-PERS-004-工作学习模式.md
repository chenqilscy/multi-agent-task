# SH-PERS-004 工作学习模式

---
metadata:
  case_id: SH-PERS-004
  journey: 个性化场景
  journey_order: 4
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

- **用例ID**: SH-PERS-004
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

居家办公/学习场景，明亮护眼灯光、安静或轻音乐、舒适温度。

## 📝 执行流程

**用户输入**: "我要开始工作了"

**系统响应**:
```
💻 正在切换到工作模式...

💡 灯光调整：
  - 书房灯 → 正白光 85%（护眼色温 5000K）
  - 其他房间 → 关闭

🎵 音乐设置：
  - 播放轻柔钢琴曲（音量 10%）

🌡️ 温度调整：
  - 空调 → 23°C 静音模式

工作模式已激活 ✅ 专注高效！
```

## ✅ 预期结果

- [x] 护眼色温明亮照明
- [x] 低音量不干扰的背景音
- [x] 空调静音舒适

## 🧪 测试要点

- [ ] "工作/学习/办公"意图识别
- [ ] 仅书房灯开启
- [ ] 空调静音模式

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要开始工作了 |
| 1 | 🤖 系统 | _(IntentRecognizer: 无关键词匹配 → Unknown, Confidence=0.0)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 把书房灯打开，白光 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>_(EntityExtractor: room="书房")_<br>💡 书房灯已打开，白光100% ✅ |
| 3 | 👤 用户 | 其他房间的灯关掉 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "灯"+"关" → LightingAgent)_<br>💡 客厅灯已关闭 ✅<br>_(注: 每次只处理一个房间)_ |
| 4 | 👤 用户 | 空调调到23度 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 空调已调至23°C ✅<br><br>💻 工作学习环境已准备就绪！加油！ |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("把书房灯打开，白光")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity("书房") → "书房"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ 解析"打开" → ILightingService.TurnOnAsync("书房") → ✅
      └─ 解析"白光" → ILightingService.SetColorAsync("书房", "white") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 工作模式 | "开始工作"一键启动 | 无WorkMode意图，需分步操作 |
| 仅书房灯 | 关闭其他房间灯光 | ControlService不支持"其他房间"批量操作 |
| 空调静音 | 低风速减少噪音 | ClimateAgent无静音模式 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

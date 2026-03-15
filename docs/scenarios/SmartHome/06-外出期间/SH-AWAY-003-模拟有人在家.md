# SH-AWAY-003 模拟有人在家

---
metadata:
  case_id: SH-AWAY-003
  journey: 外出期间
  journey_order: 3
  case_type: variant
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, MusicAgent]
  capabilities: [brightness-control, music-playback, timer-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 25
  depends_on: [SH-AWAY-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-AWAY-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

长期外出时模拟有人在家，随机开关灯光和播放音乐，提升安全性。

## 📝 执行流程

**用户输入**: "帮我模拟有人在家的效果"

**系统响应**:
```
🏠 模拟有人在家模式已启动...

💡 灯光模拟计划：
  - 18:00-22:00 客厅灯随机开关（间隔30-90分钟）
  - 19:00-21:00 卧室灯随机开关
  - 22:00-23:00 卧室灯保持开启后关闭

🎵 音响模拟计划：
  - 19:00-21:00 客厅低音量播放广播（模拟日常）

⏰ 模拟周期：每日循环
🔄 随机偏移：±15分钟（避免规律性）

模拟模式已激活 ✅ 每日自动执行
```

## ✅ 预期结果

- [x] 灯光按随机时间表开关
- [x] 时间偏移避免规律性
- [x] 音乐低音量模拟日常活动

## 🧪 测试要点

- [ ] 定时计划正确生成
- [ ] 随机偏移在合理范围
- [ ] LightingAgent和MusicAgent协调工作

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 帮我模拟有人在家的效果 |
| 1 | 🤖 系统 | _(SmartHomeControlService: "模拟有人" → SecurityAgent)_<br>🏠 已开启模拟有人在家模式。<br>系统将随机控制灯光和窗帘模拟日常活动。 |
| 2 | 👤 用户 | 帮我定时开关客厅的灯 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 客厅灯已打开 ✅<br>_(注：当前不支持定时功能)_ |
| 3 | 👤 用户 | 播放一点音乐 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "音乐"+"播放" → MusicAgent)_<br>🎵 正在播放音乐...<br><br>✅ 已开启灯光和音乐，模拟家中有人 |

### 代码执行追踪

**Turn 1 代码路径（SecurityAgent已集成到路由）**:
```
SmartHomeControlService.ProcessCommandAsync("帮我模拟有人在家的效果")
  ├─ ContainsKeywords("天气"/...) → false
  ├─ ContainsKeywords("温度变化"/...) → false
  ├─ ContainsKeywords("门锁"/"锁门"/.../"模拟有人"/...) → true ("模拟有人")
  └─ SecurityAgent.ExecuteBusinessLogicAsync(request)
       ├─ userInput.Contains("模拟有人") → true
       └─ HandleEnablePresenceSimulationAsync()
           └─ ISecurityService.SimulatePresenceAsync() → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 模拟有人 | SecurityAgent.SimulatePresenceAsync | ✅ SecurityAgent已集成到ControlService路由 |
| 定时开关灯 | 随机时间表自动控制 | LightingAgent无定时功能，只支持即时控制 |
| 音乐音量 | 低音量模拟日常 | MusicAgent无音量控制API |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

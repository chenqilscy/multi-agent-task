# SH-AWAY-002 远程查询家中状态

---
metadata:
  case_id: SH-AWAY-002
  journey: 外出期间
  journey_order: 2
  case_type: single-agent
  domain: smarthome
  complexity: L1
  agents: [WeatherAgent, TemperatureHistoryAgent]
  capabilities: [status-query, temperature-history]
  coverage: { single-agent: true, multi-agent: false, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: medium
  estimated_duration_seconds: 15
  depends_on: [SH-AWAY-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-AWAY-002
- **用例类型**: 单Agent
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

外出期间远程查询家中温度、设备状态等信息。

## 📝 执行流程

**用户输入**: "家里现在温度多少？一切正常吗？"

**系统响应**:
```
🏠 家中状态报告：

🌡️ 室内温度：
  - 客厅：18°C
  - 卧室：17°C
  - 厨房：16°C

📊 温度趋势（近24h）：
  - 平均温度：17.5°C
  - 最低温度：15°C（凌晨03:00）
  - 防冻保护未触发

🔒 安防状态：正常
💡 灯光状态：外出模式运行中

一切正常 ✅
```

## ✅ 预期结果

- [x] 返回各房间当前温度
- [x] 提供温度趋势数据
- [x] 安防和设备状态汇总

## 🧪 测试要点

- [ ] WeatherAgent返回多房间温度
- [ ] TemperatureHistoryAgent提供趋势分析
- [ ] 状态信息准确完整

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 家里现在温度多少？一切正常吗？ |
| 1 | 🤖 系统 | _(SmartHomeControlService: "温度"+"多少" → ClimateAgent)_<br>🌡️ 当前室温状态：<br>客厅: 18°C<br>空调已关闭（外出模式） |
| 2 | 👤 用户 | 客厅最近温度变化怎么样 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "温度变化" → TemperatureHistoryAgent)_<br>📊 客厅温度趋势（近24小时）：<br>昨天 18:00: 22°C → 今天 06:00: 18°C<br>趋势: 缓慢下降（-4°C/12h） |
| 3 | 👤 用户 | 天气怎么样 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "天气" → WeatherAgent)_<br>☁️ 北京今天: 晴，8~18°C<br>室外较冷，建议远程开启空调预热 |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("客厅最近温度变化怎么样")
  ├─ ContainsKeywords("温度变化") → true → TemperatureHistoryAgent
  ├─ ExtractRoomEntity("客厅") → "客厅"
  └─ TemperatureHistoryAgent.ExecuteBusinessLogicAsync(request)
      └─ ITemperatureHistoryService.GetHistoryAsync("客厅", period) → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 多房间温度 | 一次返回所有房间温度 | ControlService每次只处理一个房间 |
| 安防状态 | 综合显示温度+安防+设备 | 安防状态未集成到查询流程 |
| 远程控制 | 支持远程开启空调预热 | 系统仅提供建议，无主动远程控制 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

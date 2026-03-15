# SH-SLEEP-004 灯光调暗失败

---
metadata:
  case_id: SH-SLEEP-004
  journey: 睡眠准备
  journey_order: 4
  case_type: exception
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, ClimateAgent]
  capabilities: [brightness-control, temperature-control, error-handling]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: [SH-SLEEP-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-SLEEP-004
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

灯光设备无法调暗时，提供替代方案并完成其他设备设置。

## 📝 执行流程

**用户输入**: "晚安"

**系统响应**（卧室灯渐暗功能异常）:
```
🌙 正在进入睡眠模式...

💡 灯光状态：
  - 客厅灯 ✅ 已关闭
  - 卧室灯 ⚠️ 渐暗功能不可用，已直接关闭

🌡️ 空调已调至睡眠模式 ✅

⚠️ 卧室灯渐暗功能异常，已使用直接关闭作为替代。
如需夜灯，请手动打开床头灯。

睡眠模式已激活 ✅
```

## ✅ 预期结果

- [x] 渐暗失败时回退到直接关闭
- [x] 提供替代建议（手动床头灯）
- [x] 其他设备正常

## 🧪 测试要点

- [ ] 渐暗超时后自动回退到直接关闭
- [ ] 回退方案友好提示
- [ ] 空调不受灯光故障影响

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 晚安 |
| 1 | 🤖 系统 | _(意图识别: "晚安"不匹配任何关键词 → Unknown, Confidence=0.0)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 帮我关灯，调暗卧室 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>_(故障注入: SimulatedLightingService.InjectFault("device_offline"))_<br>💡 灯光控制异常：卧室灯渐暗功能不可用<br>⚠️ 已采用替代方案：卧室灯直接关闭<br>客厅灯已关闭 ✅ |
| 3 | 👤 用户 | 空调调到睡眠模式 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "空调" → ClimateAgent)_<br>🌡️ 空调已调至22°C睡眠模式 ✅<br><br>睡眠模式已部分激活 ⚠️<br>💡 灯光：卧室渐暗异常，已直接关闭<br>🌡️ 空调：睡眠模式 22°C ✅ |

### 代码执行追踪

**Turn 2 代码路径（灯光故障场景）**:
```
SmartHomeControlService.ProcessCommandAsync("帮我关灯，调暗卧室")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity → "卧室"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ ILightingService.SetBrightnessAsync("卧室", 5)
      │   └─ ThrowIfFaultActive() → ❌ FaultActiveException("device_offline")
      ├─ 异常捕获 → 尝试 ILightingService.TurnOffAsync("卧室")
      │   └─ ThrowIfFaultActive() → ❌ 再次失败
      └─ 返回: { Success: false, Error: "灯光设备离线" }
```

**Turn 3 代码路径（空调正常）**:
```
SmartHomeControlService.ProcessCommandAsync("空调调到睡眠模式")
  ├─ ContainsKeywords("空调") → true → ClimateAgent
  └─ ClimateAgent.ExecuteBusinessLogicAsync(request)
      ├─ IClimateService.SetTemperatureAsync("客厅", 22) → ✅
      └─ IClimateService.SetModeAsync("客厅", "auto") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| "晚安"识别 | 直接触发睡眠模式 | 无"SleepMode"意图，"晚安"不匹配关键词 → BuildLowConfidenceResponse |
| 渐暗回退 | 渐暗失败自动切换直接关闭 | LightingAgent内部无自动回退，故障时直接抛异常 |
| 多Agent编排 | 同时调用LightingAgent+ClimateAgent | SmartHomeControlService为串行if-else，每次仅调用一个Agent |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

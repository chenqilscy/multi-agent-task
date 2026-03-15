# SH-SLEEP-002 工作日早睡模式

---
metadata:
  case_id: SH-SLEEP-002
  journey: 睡眠准备
  journey_order: 2
  case_type: variant
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, ClimateAgent]
  capabilities: [brightness-control, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 25
  depends_on: [SH-SLEEP-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-SLEEP-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

工作日早睡：快速进入睡眠，灯光直接关闭（非渐暗），设定闹钟提醒。

## 📝 执行流程

**用户输入**: "明天要早起，帮我快速关灯"

**系统响应**:
```
🌙 工作日早睡模式！
💡 所有灯光已直接关闭
🌡️ 空调已调至22°C睡眠模式
⏰ 建议设置明早闹钟

晚安，明天加油！💪
```

## ✅ 预期结果

- [x] 灯光直接关闭（无渐暗）
- [x] 响应更快速

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 明天要早起，帮我快速关灯 |
| 1 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>_(EntityExtractor: room=未指定→默认"客厅")_<br>💡 客厅灯已关闭 ✅ |
| 2 | 👤 用户 | 卧室灯也关掉，空调调低 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 卧室灯已关闭 ✅ |
| 3 | 👤 用户 | 空调调到22度 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 客厅空调已设置为22°C 睡眠模式<br><br>🌙 工作日早睡模式准备完毕！<br>💡 全屋灯光已关闭 \| 🌡️ 空调22°C<br>晚安，明天加油！💪 |

### 代码执行追踪

**Turn 1 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("明天要早起，帮我快速关灯")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity → 无匹配 → 默认"客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      └─ 解析"关灯" → ILightingService.TurnOffAsync("客厅") → ✅
```

**Turn 3 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("空调调到22度")
  ├─ ContainsKeywords("空调","度") → true → ClimateAgent
  ├─ ExtractRoomEntity → 默认"客厅"
  └─ ClimateAgent.ExecuteBusinessLogicAsync(request)
      ├─ 提取温度: 22
      ├─ IClimateService.SetTemperatureAsync("客厅", 22) → ✅
      └─ IClimateService.SetModeAsync("客厅", "auto") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 一句话多设备 | "帮我快速关灯"同时关所有灯+调空调 | SmartHomeControlService 为串行if-else，"灯"关键词命中后只调LightingAgent |
| 闹钟设置 | 提醒设置明早闹钟 | 无闹钟相关Agent或服务 |
| 全屋关灯 | 识别"快速关灯"=全屋所有灯 | 只能逐个房间控制，无全屋批量操作 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

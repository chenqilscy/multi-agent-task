# SH-DEPART-003 设备关闭失败

---
metadata:
  case_id: SH-DEPART-003
  journey: 离家准备
  journey_order: 3
  case_type: exception
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, ClimateAgent]
  capabilities: [light-control, temperature-control, error-handling]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: false
    error-handling: true
  demo_value: 3
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 20
  requires_external_service: false
  requires_hardware: false
  depends_on: [SH-DEPART-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-DEPART-003
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐
- **预估耗时**: 20秒

## 🎯 业务目标

验证设备无法关闭时的告警机制：明确告知用户哪些设备未关闭。

## 📝 执行流程

**用户输入**: "我要出门了"

**系统响应**（厨房灯关闭失败）:
```
🏠 离家模式启动中...

💡 灯光关闭状态：
  - 卧室灯 ✅ 已关闭
  - 客厅灯 ✅ 已关闭
  - 厨房灯 ❌ 关闭失败（设备无响应）

🌡️ 空调已切换为节能模式 ✅

⚠️ 注意：厨房灯未能关闭，请手动检查。
离家模式已部分激活。
```

## 🗣️ 详细对话交互示例

### Turn 1: 触发离家模式

```
用户: 我出门了
```

**内部处理链**:
```
1. SmartHomeMainAgent → 离家模式编排
2. LightingAgent 依次关闭各房间灯光
   → TurnOffAsync("卧室") → ✅
   → TurnOffAsync("客厅") → ✅
   → TurnOffAsync("厨房") → ❌ DeviceNotRespondingException
3. ClimateAgent → SetTemperatureAsync("客厅", 28) → ✅
```

**系统输出**:
```
🏠 离家模式启动中...

💡 灯光关闭状态：
  - 卧室灯 ✅ 已关闭
  - 客厅灯 ✅ 已关闭
  - 厨房灯 ❌ 关闭失败（设备无响应）

🌡️ 空调已切换为节能模式 ✅

⚠️ 注意：厨房灯未能关闭，请手动检查。
离家模式已部分激活。
```

### Turn 2: 用户重试

```
用户: 再试一次关厨房灯
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "ControlLight"（匹配"关""灯"）
2. EntityExtractor → { room: "厨房", action: "关闭" }
3. LightingAgent.ExecuteAsync({command: "关闭", room: "厨房"})
   → ILightingService.TurnOffAsync("厨房")（重试策略: 指数退避, 最多3次）
```

**结果A（重试成功）**:
```
💡 厨房灯已成功关闭 ✅
现在所有设备已安全处理，请放心出门！
```

**结果B（重试失败）**:
```
❌ 厨房灯仍然无响应。
建议出门前手动关闭厨房灯开关。
```

## ✅ 预期结果

- [x] 明确标注每个设备的关闭状态
- [x] 失败设备给出具体提示
- [x] 成功设备不受影响
- [x] 最终状态标注"部分激活"

## 🧪 测试要点

- [ ] 单个设备失败不阻塞其他设备
- [ ] 失败原因清晰（设备无响应/网络超时）
- [ ] 重试机制验证（自动重试1次后报错）

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

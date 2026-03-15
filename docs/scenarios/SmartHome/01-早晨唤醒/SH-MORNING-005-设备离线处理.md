# SH-MORNING-005 设备离线处理

---
metadata:
  case_id: SH-MORNING-005
  journey: 早晨唤醒
  journey_order: 5
  case_type: exception
  domain: smarthome
  complexity: L3
  agents: [WeatherAgent, ClimateAgent, LightingAgent]
  capabilities: [weather-query, temperature-control, brightness-control, error-handling]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: true
  demo_value: 3
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 30
  requires_external_service: true
  requires_hardware: false
  depends_on: [SH-MORNING-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-MORNING-005
- **用户旅程**: 早晨唤醒
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐
- **预估耗时**: 30秒

## 🎯 业务目标

验证家居设备（空调/灯光）离线时系统的容错能力：跳过离线设备，完成可用设备的操作。

## 📝 执行流程

### Step 1: 正常天气查询

**用户输入**: "早上好"

**系统响应**: WeatherAgent正常返回天气信息。

### Step 2: 设备控制（部分失败）

**系统自动执行**:
- ClimateAgent: 空调离线 → 返回设备不可用错误
- LightingAgent: 正常执行

**系统响应**:
```
☀️ 天气已查询完成。

⚠️ 客厅空调设备当前离线，无法调节温度。
请检查空调电源和网络连接，或稍后重试。

💡 灯光已正常调亮：
- 卧室灯光渐亮至80%
- 客厅灯光已打开（亮度50%）

晨起唤醒模式已部分激活 ⚠️
1个设备离线，其余设备正常工作。
```

## ✅ 预期结果

- [x] 设备离线不阻塞整个流程
- [x] 离线设备提供明确提示和建议
- [x] 在线设备正常工作
- [x] 最终状态明确标注"部分激活"

## 🗣️ 详细对话交互示例

### Turn 1: 用户触发晨起

```
用户: 早上好
```

**内部处理链**:
```
1. SmartHomeMainAgent.ExecuteBusinessLogicAsync("早上好")
2. IntentRecognizer → PrimaryIntent: "QueryWeather"
3. 编排决策: 并行调用 WeatherAgent + ClimateAgent + LightingAgent
```

**Agent 调用详情**:
```
[WeatherAgent] ExecuteAsync({city: "北京", date: "2026-03-15"})
  → IWeatherService.GetWeatherAsync("北京", DateOnly.Parse("2026-03-15"))
  → 返回: { Condition: "晴", Temperature: 15, MinTemp: 12, MaxTemp: 22 }
  → ✅ 成功

[ClimateAgent] ExecuteAsync({room: "客厅", temperature: 22})
  → IClimateService.SetTemperatureAsync("客厅", 22)
  → ❌ 异常: DeviceOfflineException("空调设备离线")
  → 返回: { Success: false, Error: "设备离线" }

[LightingAgent] ExecuteAsync({command: "调亮", room: "卧室"})
  → ILightingService.SetBrightnessAsync("卧室", 80)
  → ILightingService.TurnOnAsync("客厅")
  → ✅ 成功
```

**系统输出**:
```
☀️ 北京今天天气：晴，12~22°C
👔 建议穿薄外套

⚠️ 客厅空调设备当前离线，无法调节温度。
请检查空调电源和网络连接，或稍后重试。

💡 灯光已正常调亮：
- 卧室灯光渐亮至80%
- 客厅灯光已打开（亮度50%）

晨起唤醒模式已部分激活 ⚠️（1个设备离线）
```

### Turn 2: 用户手动处理

```
用户: 那帮我把卧室空调打开
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "AdjustClimate"（匹配"空调""打开"）
2. EntityExtractor → { room: "卧室", action: "打开" }
3. ClimateAgent.ExecuteAsync({room: "卧室"})
```

**可能结果A（卧室空调在线）**:
```
🌡️ 卧室空调已打开，当前设置为22°C制热模式。
```

**可能结果B（卧室空调也离线）**:
```
⚠️ 卧室空调也无法连接。建议检查家中网络或空调电源。
需要我记录一个故障报修工单吗？
```

## 🧪 测试要点

### 异常测试
- [ ] 空调离线时灯光仍正常
- [ ] 灯光离线时空调仍正常
- [ ] 所有设备离线时给出完整提示
- [ ] 设备恢复在线后可重新执行

## 📚 相关文档

- **容错设计**: `docs/specs/14-error-handling-guide.md`
- **父用例**: [SH-MORNING-001](./SH-MORNING-001-标准晨起唤醒.md)
- **同类用例**: [SH-MORNING-004 天气服务异常](./SH-MORNING-004-天气服务异常.md)

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

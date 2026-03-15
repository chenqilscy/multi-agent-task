# SH-MORNING-004 天气服务异常

---
metadata:
  case_id: SH-MORNING-004
  journey: 早晨唤醒
  journey_order: 4
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

- **用例ID**: SH-MORNING-004
- **用户旅程**: 早晨唤醒
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐
- **预估耗时**: 30秒

## 🎯 业务目标

验证天气API不可用时系统的容错能力：降级处理，不阻塞其他Agent工作。

## 📝 执行流程

### Step 1: 天气查询失败

**用户输入**: "早上好"

**系统响应**（WeatherAgent调用失败时）:
```
早上好！🌞

⚠️ 抱歉，天气服务暂时不可用，无法为您查询天气。
您可以稍后再试，或者查看手机天气App。

不过其他设备已为您准备好了：
🌡️ 客厅空调已设置为22°C
💡 灯光正在逐渐调亮...

晨起唤醒模式已部分激活 ✅
```

### Step 2: 降级行为

- WeatherAgent: 返回错误，系统记录日志并跳过
- ClimateAgent: 正常执行，使用默认温度22°C（无天气数据参考）
- LightingAgent: 正常执行

## ✅ 预期结果

- [x] 天气服务失败不阻塞整个流程
- [x] ClimateAgent和LightingAgent正常工作
- [x] 用户收到友好的错误提示
- [x] 系统使用默认温度（无天气数据时）
- [x] 错误已记录到日志

## 🧪 测试要点

### 异常测试
- [ ] WeatherAPI超时（3秒）时系统降级
- [ ] WeatherAPI返回错误码时系统降级
- [ ] 降级后其他Agent仍正常执行
- [ ] 错误信息对用户友好（非技术性语言）
- [ ] 日志记录完整（含错误详情）

## �️ 详细对话交互示例

### Turn 1: 用户触发晨起

```
用户: 早上好
```

**内部处理链**:
```
1. SmartHomeMainAgent.ExecuteBusinessLogicAsync("早上好")
2. IntentRecognizer → PrimaryIntent: "QueryWeather"（匹配关键词"早上"→天气问候）
3. EntityExtractor → { city: "北京", date: "今天" }
4. 编排决策: 并行调用 WeatherAgent + ClimateAgent + LightingAgent
```

**Agent 调用详情**:
```
[WeatherAgent] ExecuteAsync({city: "北京", date: "2026-03-15"})
  → IWeatherService.GetWeatherAsync("北京", DateOnly.Parse("2026-03-15"))
  → ❌ 异常: HttpRequestException / TimeoutException
  → 返回: { Success: false, Error: "天气服务暂时不可用" }

[ClimateAgent] ExecuteAsync({room: "客厅", temperature: 22})
  → IClimateService.SetTemperatureAsync("客厅", 22)
  → IClimateService.SetModeAsync("客厅", "heating")
  → 返回: { Success: true, Message: "客厅空调已设置为22°C" }

[LightingAgent] ExecuteAsync({command: "调亮", room: "卧室"})
  → ILightingService.SetBrightnessAsync("卧室", 80)
  → ILightingService.TurnOnAsync("客厅")
  → ILightingService.SetBrightnessAsync("客厅", 50)
  → 返回: { Success: true }
```

**系统输出**:
```
早上好！🌞

⚠️ 抱歉，天气服务暂时不可用，无法为您查询天气。
您可以稍后再试，或者查看手机天气App。

不过其他设备已为您准备好了：
🌡️ 客厅空调已设置为22°C（制热模式）
💡 卧室灯光渐亮至80% | 客厅灯光50%

晨起唤醒模式已部分激活 ⚠️
```

### Turn 2: 用户重试天气查询

```
用户: 那帮我查一下天气
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "QueryWeather"（匹配"查""天气"）
2. EntityExtractor → { city: "北京", date: "今天" }
3. WeatherAgent.ExecuteAsync()
4. 服务仍不可用 → 触发降级策略（Level 2: 缓存上次数据/返回默认建议）
```

**系统输出**:
```
⚠️ 天气服务仍在恢复中。

根据季节和历史数据的通用建议：
🌡️ 三月中旬北京气温约 5~18°C
👔 建议穿薄棉外套，早晚注意保暖

天气服务恢复后我会第一时间通知您。
```

## �📚 相关文档

- **容错设计**: `docs/specs/14-error-handling-guide.md`
- **父用例**: [SH-MORNING-001](./SH-MORNING-001-标准晨起唤醒.md)

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

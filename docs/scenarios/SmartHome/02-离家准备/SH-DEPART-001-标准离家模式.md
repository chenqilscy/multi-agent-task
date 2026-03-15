# SH-DEPART-001 标准离家模式

---
metadata:
  case_id: SH-DEPART-001
  journey: 离家准备
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [WeatherAgent, LightingAgent, ClimateAgent]
  capabilities: [weather-query, light-control, temperature-control]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 5
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 30
  requires_external_service: true
  requires_hardware: false
  depends_on: []
  enables: [SH-DEPART-002]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-DEPART-001
- **用户旅程**: 离家准备
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐
- **预估耗时**: 30秒

## 🎯 业务目标

用户出门时一键关闭所有设备，查询目的地天气，确保节能和安全。

## 📝 执行流程

### Step 1: 触发离家

**用户输入**: "我要出门了"

**系统响应**:
1. 意图识别: 离家模式
2. 编排多Agent并行执行

### Step 2: 设备关闭 + 天气查询

**系统自动执行**:
```
🏠 离家模式启动中...

💡 所有灯光已关闭
  - 卧室灯 ✅ 已关闭
  - 客厅灯 ✅ 已关闭
  - 厨房灯 ✅ 已关闭

🌡️ 空调已切换为节能模式（28°C）

📍 外出天气提醒：
今天北京：晴转多云，12~22°C
👔 建议带薄外套，中午可能会热

✅ 离家模式已激活！所有设备已安全处理。
出门注意安全，祝您一切顺利！🚗
```

## 🗣️ 详细对话交互示例

### Turn 1: 触发离家模式

```
用户: 我要出门了
```

**内部处理链**:
```
1. SmartHomeMainAgent.ExecuteBusinessLogicAsync("我要出门了")
2. IntentRecognizer → PrimaryIntent: "ControlLight"（匹配"出门"场景关键词）
3. SmartHomeControlService.ProcessCommandAsync("我要出门了")
4. 编排决策: 并行执行 LightingAgent(关闭) + ClimateAgent(节能) + WeatherAgent(出行天气)
```

**Agent 调用详情**:
```
[LightingAgent] 关闭所有房间灯光
  → ILightingService.TurnOffAsync("卧室") → ✅
  → ILightingService.TurnOffAsync("客厅") → ✅
  → ILightingService.TurnOffAsync("厨房") → ✅

[ClimateAgent] 切换节能模式
  → IClimateService.SetTemperatureAsync("客厅", 28)  // 节能温度
  → IClimateService.SetModeAsync("客厅", "auto")
  → ✅

[WeatherAgent] 查询出行天气
  → IWeatherService.GetWeatherAsync("北京", DateOnly.FromDateTime(DateTime.Today))
  → 返回: { Condition: "晴转多云", MinTemp: 12, MaxTemp: 22, WindLevel: 2 }
  → ✅
```

**系统输出**:
```
🏠 离家模式启动中...

💡 所有灯光已关闭
  - 卧室灯 ✅ 已关闭
  - 客厅灯 ✅ 已关闭
  - 厨房灯 ✅ 已关闭

🌡️ 空调已切换为节能模式（28°C）

📍 外出天气提醒：
今天北京：晴转多云，12~22°C，东风2级
👔 建议带薄外套，中午可能会热

✅ 离家模式已激活！所有设备已安全处理。
出门注意安全，祝您一切顺利！🚗
```

### Turn 2: 用户追加确认（可选）

```
用户: 空调不用关吗？
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "AdjustClimate"（匹配"空调""关"）
2. SmartHomeMainAgent 上下文感知 → 用户在离家模式中询问空调
3. ClimateAgent 响应当前状态
```

**系统输出**:
```
🌡️ 空调当前是节能模式（28°C），非常省电。
如果您要完全关闭空调，请说"关掉空调"。
如果出门时间较长（超过4小时），建议完全关闭。
```

## ✅ 预期结果

- [x] 所有灯光关闭
- [x] 空调切换为节能模式
- [x] 天气信息正确显示
- [x] 出行建议合理
- [x] 全流程 < 30秒

## 🧪 测试要点

### 功能测试
- [ ] LightingAgent关闭所有房间灯光
- [ ] ClimateAgent切换到节能模式
- [ ] WeatherAgent正常返回天气

### 集成测试
- [ ] 三Agent并行执行无冲突
- [ ] 全流程响应时间 < 3秒

## 📚 相关文档

- **Agent实现**: `src/Demos/SmartHome/Agents/`
- **相关用例**: [SH-DEPART-002 远距离出行](./SH-DEPART-002-远距离出行.md)

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

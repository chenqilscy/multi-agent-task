# SH-DEPART-004 网络不稳定

---
metadata:
  case_id: SH-DEPART-004
  journey: 离家准备
  journey_order: 4
  case_type: exception
  domain: smarthome
  complexity: L3
  agents: [WeatherAgent, LightingAgent, ClimateAgent]
  capabilities: [weather-query, light-control, temperature-control, error-handling]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: false
    error-handling: true
  demo_value: 3
  test_priority: P1
  doc_importance: medium
  estimated_duration_seconds: 25
  requires_external_service: true
  requires_hardware: false
  depends_on: [SH-DEPART-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-DEPART-004
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐
- **预估耗时**: 25秒

## 🎯 业务目标

验证网络不稳定时的降级策略：优先执行本地设备控制，延迟或跳过网络服务。

## 📝 执行流程

**用户输入**: "我要出门了"

**系统响应**（网络不稳定，天气查询超时）:
```
🏠 离家模式启动中...

💡 所有灯光已关闭 ✅（本地控制）
🌡️ 空调已切换为节能模式 ✅（本地控制）

⚠️ 网络不稳定，天气信息暂时无法获取。
建议出门前查看手机天气App。

✅ 离家模式已激活（设备控制正常，网络服务受限）
```

## ✅ 预期结果

- [x] 本地设备控制不受网络影响
- [x] 网络服务（天气）超时后优雅降级
- [x] 给出替代建议

## 🧪 测试要点

- [ ] 设备控制走本地通道不依赖外网
- [ ] 天气API超时3秒后降级
- [ ] 用户提示准确描述问题

---

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要出门了 |
| 1 | 🤖 系统 | _(意图匹配: SecurityControl("门") score=0.2, 低于0.3阈值)_ 请问您想做什么呢？<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 帮我关灯关空调，查下天气 |
| 2 | 🤖 系统（理想，网络正常） | 🏠 离家模式启动中...<br><br>💡 所有灯光已关闭 ✅（本地控制）<br>🌡️ 空调已切换为节能模式 ✅（本地控制）<br>📍 天气：晴转多云，12\~22°C<br><br>✅ 离家模式已激活！ |
| 2 | 🤖 系统（网络故障时） | 🏠 离家模式启动中...<br><br>💡 所有灯光已关闭 ✅（本地控制）<br>🌡️ 空调已切换为节能模式 ✅（本地控制）<br><br>⚠️ 网络不稳定，天气信息暂时无法获取。<br>建议出门前查看手机天气App。<br><br>✅ 离家模式已激活（设备控制正常，网络服务受限） |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("我要出门了")
  │
  ├─ Step 1: RuleBasedIntentRecognizer.RecognizeAsync("我要出门了")
  │   ├─ SecurityControl: "门" → matchCount=1, score=1/5=0.2
  │   ├─ 其余意图: 0 匹配
  │   └─ 结果: PrimaryIntent="SecurityControl", Confidence=0.2
  │
  ├─ Step 2: Confidence 0.2 < 0.3 阈值
  │   └─ 触发 BuildLowConfidenceResponse()
  │
  └─ 流程终止，未进入离家模式编排
```

**Turn 2 理想代码路径（故障注入演示）**:

```
// ====== 测试准备：注入故障 ======
var weatherService = serviceProvider.GetService<IWeatherService>();
((SimulatedWeatherService)weatherService).InjectFault("timeout");
// InjectFault → _faults["timeout"] = "Service timeout: timeout"

// ====== 正式执行 ======
SmartHomeMainAgent.ExecuteBusinessLogicAsync("帮我关灯关空调，查下天气")
  │
  ├─ IntentRecognizer:
  │   ├─ ControlLight: "灯" → score=1/6=0.167, "关灯" → score=2/6=0.333 ✅ (>0.3)
  │   ├─ AdjustClimate: "空调" → score=1/7=0.143
  │   ├─ QueryWeather: "天气" → score=1/8=0.125
  │   ├─ GeneralQuery: "帮我" → score=1/5=0.2
  │   └─ 最高分: ControlLight 0.333 ✅ → 进入完整流程
  │
  ├─ EntityExtractor → { room: "客厅"(default) }
  │
  ├─ TaskDecomposer.DecomposeAsync() → 分解为:
  │   ├─ SubTask 1: LightingAgent → TurnOff
  │   ├─ SubTask 2: ClimateAgent → 节能模式
  │   └─ SubTask 3: WeatherAgent → 查询天气
  │
  ├─ TaskOrchestrator.ExecutePlanAsync():
  │   ├─ [LightingAgent] ✅ 本地执行
  │   │   → SimulatedLightingService.TurnOffAsync("客厅")
  │   │   → ThrowIfFaultInjected("device_offline") → 无故障 → ✅
  │   │
  │   ├─ [ClimateAgent] ✅ 本地执行
  │   │   → SimulatedClimateService.SetModeAsync("客厅", "auto")
  │   │   → ThrowIfFaultInjected("device_offline") → 无故障 → ✅
  │   │
  │   └─ [WeatherAgent] ❌ 网络超时
  │       → SimulatedWeatherService.GetWeatherAsync("北京", today)
  │       → ThrowIfFaultInjected("timeout")
  │       → 抛出 TimeoutException("Service timeout: timeout")
  │       → WeatherAgent catch(Exception) → 返回降级响应:
  │         "抱歉，获取北京天气信息失败，请稍后重试"
  │
  └─ ResultAggregator.AggregateAsync()
      → 2个成功 + 1个失败 → 部分成功响应

// ====== 清理故障 ======
((SimulatedWeatherService)weatherService).ClearFaults();
```

### 故障注入机制说明

```csharp
// SimulatedWeatherService 继承自 FaultInjectableServiceBase
public class SimulatedWeatherService : FaultInjectableServiceBase, IWeatherService
{
    public Task<WeatherInfo> GetWeatherAsync(string city, DateOnly date, CancellationToken ct)
    {
        ThrowIfFaultInjected("service_unavailable");  // 检查服务不可用
        ThrowIfFaultInjected("timeout");               // 检查超时故障 ← 本场景触发
        // ... 正常逻辑
    }
}

// 测试代码中注入/清除故障:
service.InjectFault("timeout");     // 注入超时故障
service.InjectFault("timeout", "自定义消息");  // 带自定义消息
service.ClearFaults();              // 清除所有故障
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 离家意图 | "我要出门了"触发离家模式 | "门"匹配 SecurityControl(0.2) < 0.3 → 低置信度响应，不会进入离家编排 |
| 降级策略 | 本地设备优先执行，网络服务降级 | ResultAggregator 可聚合部分成功结果，但需先通过意图识别 |
| 故障注入 | SimulatedWeatherService.InjectFault("timeout") | ✅ 已实现，FaultInjectableServiceBase 支持 timeout/service_unavailable 故障类型 |
| 替代建议 | 天气失败时提示"查看手机天气App" | WeatherAgent catch 块返回通用错误消息，未提供替代建议 |
| 所需改进 | ① 增加 "DepartHome" 意图关键词（出门/外出/离开）<br>② WeatherAgent 错误响应增加替代建议文案<br>③ 分离本地设备控制和网络服务的执行优先级 | — |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

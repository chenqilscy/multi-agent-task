# SH-HOME-005 音乐服务故障

---
metadata:
  case_id: SH-HOME-005
  journey: 回家放松
  journey_order: 5
  case_type: exception
  domain: smarthome
  complexity: L3
  agents: [ClimateAgent, LightingAgent, MusicAgent]
  capabilities: [temperature-control, brightness-control, music-play, error-handling]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: false
    error-handling: true
  demo_value: 3
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 25
  requires_external_service: false
  requires_hardware: false
  depends_on: [SH-HOME-001]
  enables: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-HOME-005
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐
- **预估耗时**: 25秒

## 🎯 业务目标

音乐服务不可用时，其他设备正常工作，用户获得降级通知。

## 📝 执行流程

**用户输入**: "我回来了"

**系统响应**:
```
欢迎回家！🏠

💡 客厅灯光已打开，暖色调亮度70% ✅
🌡️ 空调已设置为24°C ✅
🎵 音乐服务暂时不可用 ⚠️
  已尝试重连，请稍后重试或手动播放。

回家模式已部分激活（2/3设备正常）
```

## ✅ 预期结果

- [x] 灯光和空调正常工作
- [x] 音乐失败不阻塞整体流程
- [x] 给出明确的降级提示
- [x] 标注设备完成率（2/3）

## 🗣️ 详细对话交互示例

### Turn 1: 触发回家模式

```
用户: 我回来了
```

**内部处理链**:
```
1. SmartHomeMainAgent → 回家模式编排
2. 并行调用 LightingAgent + ClimateAgent + MusicAgent
```

**Agent 调用详情**:
```
[LightingAgent] ExecuteAsync({command: "打开", room: "客厅"})
  → ILightingService.TurnOnAsync("客厅")
  → ILightingService.SetBrightnessAsync("客厅", 70)
  → ✅

[ClimateAgent] ExecuteAsync({room: "客厅", temperature: 24})
  → IClimateService.SetTemperatureAsync("客厅", 24)
  → ✅

[MusicAgent] ExecuteAsync({command: "播放"})
  → 模拟播放调用
  → ❌ 异常: ServiceUnavailableException("音乐服务不可用")
  → 自动重试(1次) → ❌ 仍失败
  → 返回: { Success: false, Error: "音乐服务暂时不可用" }
```

**系统输出**:
```
欢迎回家！🏠

💡 客厅灯光已打开，暖色调亮度70% ✅
🌡️ 空调已设置为24°C ✅
🎵 音乐服务暂时不可用 ⚠️
  已尝试重连，请稍后重试或手动播放。

回家模式已部分激活（2/3设备正常）
```

### Turn 2: 用户手动重试

```
用户: 放首音乐试试
```

**内部处理链**:
```
1. IntentRecognizer → PrimaryIntent: "PlayMusic"（匹配"放""音乐"）
2. MusicAgent.ExecuteAsync({command: "播放"})
   → 重试策略: 指数退避 1s→2s→4s
```

**结果A（恢复成功）**:
```
🎵 音乐服务已恢复！正在播放轻音乐～
```

**结果B（仍然失败）**:
```
⚠️ 音乐服务仍在恢复中。您可以使用手机蓝牙连接音箱播放。
```

## 🧪 测试要点

- [ ] MusicAgent故障隔离验证
- [ ] 其他Agent不受影响
- [ ] 重试机制（1次自动重试）

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

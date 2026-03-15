# SH-AWAY-004 异常入侵检测响应

---
metadata:
  case_id: SH-AWAY-004
  journey: 外出期间
  journey_order: 4
  case_type: exception
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, alarm, security-response]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 20
  depends_on: [SH-AWAY-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-AWAY-004
- **用例类型**: 异常流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

外出期间检测到疑似入侵，触发多设备联动安防响应。

## 📝 执行流程

**触发条件**: 门窗传感器检测到异常开启

**系统自动响应**:
```
🚨 安防警报！

📍 触发位置：客厅窗户异常开启
⏰ 触发时间：2026-03-15 02:30:15

🔔 应急响应已启动：
  1. 💡 全屋灯光 → 100% 闪烁模式
  2. 🔊 客厅音响 → 播放警报声
  3. 📱 已发送紧急推送通知
  4. 📸 已保存传感器时间戳记录

⚠️ 请确认是否为误报。如需报警请回复"报警"。
```

**用户远程回复**: "谢谢，是家人忘带钥匙了，可以解除"

**系统响应**:
```
✅ 安防警报已解除
  - 灯光恢复外出模式
  - 音响已停止
  - 事件已记录

外出模式已恢复 🏠
```

## 🗣️ 详细对话交互示例

### Event: 传感器触发

```
[事件] 客厅窗户传感器 → 异常开启检测
```

**内部处理链**:
```
1. SmartHomeMainAgent 接收传感器事件
2. 意图自动识别: "SecurityControl"（匹配"安全""入侵"事件）
3. 编排决策: 紧急模式 → 并行调用所有可用Agent
```

**Agent 调用详情**:
```
[LightingAgent] 紧急照明
  → ILightingService.TurnOnAsync("客厅") → ✅
  → ILightingService.SetBrightnessAsync("客厅", 100) → ✅
  → ILightingService.TurnOnAsync("卧室") → ✅
  → ILightingService.SetBrightnessAsync("卧室", 100) → ✅

[MusicAgent] 播放警报
  → ExecuteAsync({command: "播放", content: "警报音"})
  → ✅

[NotificationService] 推送通知
  → RealTimeNotificationService.NotifyAsync("安防警报触发")
  → ✅
```

**系统输出**:
```
🚨 安防警报！

📍 触发位置：客厅窗户异常开启
⏰ 触发时间：2026-03-15 02:30:15

🔔 应急响应已启动：
  1. 💡 全屋灯光 → 100% 闪烁模式
  2. 🔊 客厅音响 → 播放警报声
  3. 📱 已发送紧急推送通知
  4. 📸 已保存传感器时间戳记录

⚠️ 请确认是否为误报。如需报警请回复"报警"。
```

### Turn 1: 用户远程解除

```
用户: 谢谢，是家人忘带钥匙了，可以解除
```

**内部处理链**:
```
1. IntentRecognizer → "SecurityControl"（匹配"解除"）
2. SmartHomeMainAgent 检测到"解除"关键词 → 安防解除流程
3. LightingAgent → 恢复外出模式（关闭灯光或低亮度）
4. MusicAgent → 停止播放
```

**Agent 调用详情**:
```
[LightingAgent] 恢复外出模式
  → ILightingService.TurnOffAsync("客厅") → ✅
  → ILightingService.TurnOffAsync("卧室") → ✅

[MusicAgent] 停止警报
  → ExecuteAsync({command: "暂停"}) → ✅
```

**系统输出**:
```
✅ 安防警报已解除
  - 灯光恢复外出模式
  - 音响已停止
  - 事件已记录

外出模式已恢复 🏠
```

## ✅ 预期结果

- [x] 传感器异常触发自动响应
- [x] 多设备联动（灯光+音响+通知）
- [x] 支持远程确认解除
- [x] 事件完整记录

## 🧪 测试要点

- [ ] 异常事件触发速度（<3秒）
- [ ] 多Agent联动正确执行
- [ ] 解除后设备恢复外出模式
- [ ] 误报流程友好处理

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

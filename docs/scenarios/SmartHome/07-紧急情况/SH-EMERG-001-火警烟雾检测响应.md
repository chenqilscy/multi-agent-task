# SH-EMERG-001 火警烟雾检测响应

---
metadata:
  case_id: SH-EMERG-001
  journey: 紧急情况
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, ClimateAgent, MusicAgent]
  capabilities: [brightness-control, ventilation, alarm-playback, emergency-response]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 20
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-EMERG-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

烟雾传感器检测到烟雾后触发多设备联动应急，确保人员安全疏散。

## 📝 执行流程

**触发条件**: 烟雾传感器检测到烟雾浓度超标

**系统自动响应**:
```
🚨🔥 火警警报！

📍 检测位置：厨房烟雾传感器
⏰ 触发时间：2026-03-15 12:30:00
📊 烟雾浓度：超标

🔔 应急响应：
  1. 💡 全屋灯光 → 100% 最大亮度（疏散照明）
  2. 🔊 播放紧急疏散语音指引
  3. 🌬️ 关闭空调（防止烟雾扩散）
  4. 🔥 关闭燃气阀门
  5. 📱 已发送紧急通知至所有家庭成员
  6. 🚪 已解锁电子门锁（便于疏散）

⚠️ 请立即撤离！如为误报请回复"解除警报"
```

## 🗣️ 详细对话交互示例

### Event: 烟雾传感器触发

```
[事件] 厨房烟雾传感器 → 烟雾浓度超标
```

**内部处理链**:
```
1. SmartHomeMainAgent 接收传感器事件(priority: CRITICAL)
2. 场景自动识别: 火警应急响应
3. 编排决策: 并行触发所有Agent，优先级最高
```

**Agent 调用详情**:
```
[LightingAgent] 疏散照明（优先级1）
  → ILightingService.TurnOnAsync("客厅") → ✅
  → ILightingService.SetBrightnessAsync("客厅", 100) → ✅
  → ILightingService.TurnOnAsync("卧室") → ✅
  → ILightingService.SetBrightnessAsync("卧室", 100) → ✅
  → ILightingService.TurnOnAsync("走廊") → ✅
  → ILightingService.SetBrightnessAsync("走廊", 100) → ✅

[ClimateAgent] 关闭空调（防烟雾扩散）
  → IClimateService.SetModeAsync("客厅", "off") → ✅
  → IClimateService.SetModeAsync("卧室", "off") → ✅

[MusicAgent] 播放紧急疏散语音
  → ExecuteAsync({command: "播放", content: "紧急疏散语音指引"}) → ✅

[NotificationService] 推送紧急通知
  → RealTimeNotificationService.NotifyAsync("🔥 火警警报") → ✅
```

**系统输出**:
```
🚨🔥 火警警报！

📍 检测位置：厨房烟雾传感器
⏰ 触发时间：2026-03-15 12:30:00
📊 烟雾浓度：超标

🔔 应急响应：
  1. 💡 全屋灯光 → 100% 最大亮度（疏散照明）
  2. 🔊 播放紧急疏散语音指引
  3. 🌬️ 关闭空调（防止烟雾扩散）
  4. 🔥 关闭燃气阀门
  5. 📱 已发送紧急通知至所有家庭成员
  6. 🚪 已解锁电子门锁（便于疏散）

⚠️ 请立即撤离！如为误报请回复"解除警报"
```

### Turn 1: 误报解除

```
用户: 解除警报，是炒菜烟太大了
```

**内部处理链**:
```
1. IntentRecognizer → "SecurityControl"（匹配"解除""警报"）
2. SmartHomeMainAgent → 验证解除请求合法性
3. 恢复所有Agent到正常状态
```

**Agent 调用详情**:
```
[ClimateAgent] 恢复空调（开启排风）
  → IClimateService.SetModeAsync("客厅", "fan") → ✅

[MusicAgent] 停止警报音
  → ExecuteAsync({command: "暂停"}) → ✅

[LightingAgent] 恢复正常亮度
  → ILightingService.SetBrightnessAsync("客厅", 70) → ✅
  → ILightingService.SetBrightnessAsync("卧室", 50) → ✅
```

**系统输出**:
```
✅ 火警警报已解除（用户确认误报）

已执行恢复操作：
  - 灯光恢复正常亮度
  - 空调切换为通风模式（帮助排烟）
  - 警报音已停止
  - 事件已记录（误报-炒菜烟雾）

💡 建议：炒菜时开启抽油烟机，可避免烟雾传感器误报。
```

## ✅ 预期结果

- [x] 传感器触发后<3秒启动全部响应
- [x] 灯光最大亮度照明疏散路径
- [x] 空调关闭防止烟雾扩散
- [x] 燃气阀门自动关闭
- [x] 语音播报疏散指引
- [x] 推送紧急通知

## 🧪 测试要点

- [ ] 多Agent同时响应延迟<3秒
- [ ] 关键操作（燃气、门锁）优先执行
- [ ] 语音播报清晰持续
- [ ] 误报解除流程可用

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

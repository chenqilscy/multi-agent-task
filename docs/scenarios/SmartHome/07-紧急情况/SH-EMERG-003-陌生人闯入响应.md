# SH-EMERG-003 陌生人闯入响应

---
metadata:
  case_id: SH-EMERG-003
  journey: 紧急情况
  journey_order: 3
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, MusicAgent]
  capabilities: [brightness-control, alarm-playback, security-response]
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

- **用例ID**: SH-EMERG-003
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

检测到非授权人员进入，触发安防响应，灯光闪烁、警报声、通知用户。

## 📝 执行流程

**触发条件**: 门窗传感器+人体传感器异常触发（非授权时段）

**系统自动响应**:
```
🚨🚪 入侵警报！

📍 触发位置：前门 + 客厅人体感应
⏰ 触发时间：2026-03-15 03:15:00
👤 未识别到授权用户

🔔 安防响应：
  1. 💡 全屋灯光 → 100% 闪烁模式
  2. 🔊 播放高音量警报声
  3. 📱 已发送紧急通知（含时间戳）
  4. 🔒 其他区域门锁 → 已锁定

⚠️ 请确认情况。回复"报警"将拨打110。
```

## 🗣️ 详细对话交互示例

### Event: 入侵检测触发

```
[事件] 前门传感器 → 异常开启 + 客厅人体传感器 → 检测到移动
[时段] 03:15 (非授权时段: 00:00-06:00)
```

**内部处理链**:
```
1. SmartHomeMainAgent 接收多传感器联动事件
2. 判断逻辑: 非授权时段 + 门窗+人体双传感器 = 入侵风险
3. 编排决策: 紧急安防响应 → 并行调用威慑+通知
```

**Agent 调用详情**:
```
[LightingAgent] 威慑照明（闪烁模式）
  → ILightingService.TurnOnAsync("客厅") → ✅
  → ILightingService.SetBrightnessAsync("客厅", 100) → ✅
  → ILightingService.TurnOnAsync("走廊") → ✅
  → ILightingService.SetBrightnessAsync("走廊", 100) → ✅
  // 注: 闪烁模式通过快速切换亮度实现

[MusicAgent] 高音量警报
  → ExecuteAsync({command: "播放", content: "入侵警报声", volume: 100})
  → ✅

[NotificationService] 紧急推送
  → RealTimeNotificationService.NotifyAsync("🚪 入侵警报 - 前门+客厅")
  → ✅
```

**系统输出**:
```
🚨🚪 入侵警报！

📍 触发位置：前门 + 客厅人体感应
⏰ 触发时间：2026-03-15 03:15:00
👤 未识别到授权用户

🔔 安防响应：
  1. 💡 全屋灯光 → 100% 闪烁模式
  2. 🔊 播放高音量警报声
  3. 📱 已发送紧急通知（含时间戳）
  4. 🔒 其他区域门锁 → 已锁定

⚠️ 请确认情况。回复"报警"将拨打110。
```

### Turn 1: 用户远程确认

```
用户: 报警
```

**内部处理链**:
```
1. IntentRecognizer → "SecurityControl"（匹配"报警"）
2. SmartHomeMainAgent → 触发报警流程
3. NotificationService → 模拟拨打110
```

**系统输出**:
```
🚔 已为您发起报警请求
  - 报警时间: 2026-03-15 03:16:05
  - 地址信息已自动附带
  - 安防系统持续监控中
  - 灯光和警报保持开启

请保持手机畅通，等待警方联系。
```

## ✅ 预期结果

- [x] 灯光闪烁起到威慑作用
- [x] 高音量警报声持续播放
- [x] 紧急通知推送至用户
- [x] 支持远程确认和报警

## 🧪 测试要点

- [ ] 传感器联动判断准确
- [ ] 灯光闪烁+警报同时启动
- [ ] 远程"报警"指令处理
- [ ] 非授权时段判断逻辑

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

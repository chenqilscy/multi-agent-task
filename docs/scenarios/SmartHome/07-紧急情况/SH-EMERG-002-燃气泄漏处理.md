# SH-EMERG-002 燃气泄漏处理

---
metadata:
  case_id: SH-EMERG-002
  journey: 紧急情况
  journey_order: 2
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, ClimateAgent, MusicAgent]
  capabilities: [brightness-control, ventilation, valve-control, alarm-playback]
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

- **用例ID**: SH-EMERG-002
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

燃气传感器检测到泄漏，关闭燃气阀门、开窗通风、禁止触发明火设备。

## 📝 执行流程

**触发条件**: 燃气传感器检测到浓度异常

**系统自动响应**:
```
🚨⛽ 燃气泄漏警报！

📍 检测位置：厨房燃气传感器
📊 燃气浓度：超出安全阈值

🔔 应急响应：
  1. 🔥 燃气阀门 → 已紧急关闭
  2. 🌬️ 开启通风换气（排风扇最大功率）
  3. ⚡ 禁止触发任何电气开关（防明火）
  4. 🔊 播放燃气泄漏安全提示
  5. 📱 已发送紧急通知

⚠️ 请勿操作电气开关！请打开窗户通风并撤离！
```

## 🗣️ 详细对话交互示例

### Event: 燃气传感器触发

```
[事件] 厨房燃气传感器 → 浓度超出安全阈值
```

**内部处理链**:
```
1. SmartHomeMainAgent 接收传感器事件(priority: CRITICAL)
2. 场景自动识别: 燃气泄漏应急响应
3. 编排决策: 安全优先 → 先关阀门/通风，再通知
   ⚠️ 重要: 禁止触发 LightingAgent（电气开关可能产生火花）
```

**Agent 调用详情**:
```
[ClimateAgent] 通风换气（优先级1）
  → IClimateService.SetModeAsync("厨房", "fan")  // 排风模式
  → 注意: 不使用制冷/制热模式，仅送风
  → ✅

[MusicAgent] 播放安全提示（优先级2）
  → ExecuteAsync({command: "播放", content: "燃气泄漏安全提示"})
  → ✅

[LightingAgent] ❌ 本次不调用（防止电气开关产生火花）

[NotificationService] 推送紧急通知
  → RealTimeNotificationService.NotifyAsync("⛽ 燃气泄漏警报")
  → ✅
```

**系统输出**:
```
🚨⛽ 燃气泄漏警报！

📍 检测位置：厨房燃气传感器
📊 燃气浓度：超出安全阈值

🔔 应急响应：
  1. 🔥 燃气阀门 → 已紧急关闭
  2. 🌬️ 开启通风换气（排风扇最大功率）
  3. ⚡ 禁止触发任何电气开关（防明火）
  4. 🔊 播放燃气泄漏安全提示
  5. 📱 已发送紧急通知

⚠️ 请勿操作电气开关！请打开窗户通风并撤离！
```

### Turn 1: 用户确认安全后解除

```
用户: 已通风30分钟，可以恢复正常了
```

**内部处理链**:
```
1. SmartHomeMainAgent → 验证解除请求
2. 逐步恢复设备：先解锁电气 → 再恢复空调 → 最后恢复灯光
```

**系统输出**:
```
✅ 燃气警报已解除

恢复操作：
  - 电气开关已解锁
  - 排风扇继续运行10分钟后自动关闭
  - 灯光控制已恢复

💡 安全建议：建议定期检查燃气管道和阀门。
```

## ✅ 预期结果

- [x] 燃气阀门立即关闭
- [x] 排风扇启动通风
- [x] 电气设备锁定（防止明火）
- [x] 安全语音提示播放

## 🧪 测试要点

- [ ] 燃气阀门关闭最高优先级
- [ ] 电气设备安全锁定
- [ ] 通风系统正确启动
- [ ] 安全提示内容准确

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

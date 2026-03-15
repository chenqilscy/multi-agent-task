# SH-EMERG-004 紧急求助模式

---
metadata:
  case_id: SH-EMERG-004
  journey: 紧急情况
  journey_order: 4
  case_type: single-agent
  domain: smarthome
  complexity: L2
  agents: [MusicAgent]
  capabilities: [voice-broadcast, emergency-call]
  coverage: { single-agent: true, multi-agent: false, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 15
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-EMERG-004
- **用例类型**: 单Agent
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户通过语音触发紧急求助，系统提供快速求助通道。

## 📝 执行流程

**用户输入**: "紧急求助！"

**系统响应**:
```
🚨 紧急求助模式已激活

请选择求助类型：
  1️⃣ 医疗急救（拨打120）
  2️⃣ 火警（拨打119）
  3️⃣ 报警（拨打110）
  4️⃣ 通知紧急联系人

请说出序号或直接描述您的情况。
```

**用户输入**: "1"

**系统响应**:
```
📞 正在联系120急救中心...
📍 已准备好家庭地址信息
💡 已开启全屋照明（便于救援人员到达）
🔓 已解锁前门电子锁

请保持冷静，救援即将到达 🚑
```

## 🗣️ 详细对话交互示例

### Turn 1: 触发紧急求助

```
用户: 紧急求助！
```

**内部处理链**:
```
1. SmartHomeMainAgent.ExecuteBusinessLogicAsync("紧急求助！")
2. IntentRecognizer → PrimaryIntent: "SecurityControl"（匹配"紧急""求助"，高优先级）
3. SmartHomeMainAgent → 进入紧急求助模式
4. 返回求助类型选择菜单（不触发任何设备操作）
```

**系统输出**:
```
🚨 紧急求助模式已激活

请选择求助类型：
  1️⃣ 医疗急救（拨打120）
  2️⃣ 火警（拨打119）
  3️⃣ 报警（拨打110）
  4️⃣ 通知紧急联系人

请说出序号或直接描述您的情况。
```

### Turn 2: 用户选择求助类型

```
用户: 1
```

**内部处理链**:
```
1. SmartHomeMainAgent → 识别选择 = 医疗急救
2. 编排决策: 并行执行辅助操作
   - LightingAgent: 全屋照明
   - SecurityControl: 门锁解锁（模拟）
   - NotificationService: 模拟拨打120
```

**Agent 调用详情**:
```
[LightingAgent] 全屋照明（便于救援人员到达）
  → ILightingService.TurnOnAsync("客厅") → ✅
  → ILightingService.SetBrightnessAsync("客厅", 100) → ✅
  → ILightingService.TurnOnAsync("走廊") → ✅
  → ILightingService.SetBrightnessAsync("走廊", 100) → ✅

[NotificationService] 模拟拨打120
  → RealTimeNotificationService.NotifyAsync("🚑 医疗急救呼叫")
  → ✅
```

**系统输出**:
```
📞 正在联系120急救中心...
📍 已准备好家庭地址信息
💡 已开启全屋照明（便于救援人员到达）
🔓 已解锁前门电子锁

请保持冷静，救援即将到达 🚑
```

### Turn 3: 系统持续陪伴

```
[5秒后自动输出]
```

**系统输出**:
```
🏥 急救中心已收到呼叫
  - 您的地址已发送
  - 预计救护车 8~12 分钟到达

在等待期间：
  - 保持呼吸通畅
  - 如有出血请按压止血
  - 不要随意移动伤者

我会一直在这里，有其他需要请随时告诉我。
```

## ✅ 预期结果

- [x] 紧急求助快速触发
- [x] 提供分类选择界面
- [x] 自动准备地址信息
- [x] 照明和门锁自动配合

## 🧪 测试要点

- [ ] "紧急求助"高优先级识别
- [ ] 求助类型选择响应快速
- [ ] 辅助操作（照明、门锁）自动执行

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

# SH-GUEST-004 多房间灯光协调

---
metadata:
  case_id: SH-GUEST-004
  journey: 会客模式
  journey_order: 4
  case_type: multi-agent
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, ClimateAgent]
  capabilities: [brightness-control, multi-room-control, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: true, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 35
  depends_on: [SH-GUEST-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-GUEST-004
- **用例类型**: 多Agent协作
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

多个房间灯光统一协调，根据功能区域设置不同亮度和色温。

## 📝 执行流程

**用户输入**: "客人快到了，把所有房间的灯都调整一下"

**系统响应**:
```
🏠 正在协调多房间灯光...

💡 各房间灯光调整：
  - 玄关灯 → 暖白光 100%（迎接区域）
  - 客厅主灯 → 暖白光 80%（主要活动区域）
  - 餐厅灯 → 暖黄光 70%（用餐区域）
  - 走廊灯 → 暖白光 50%（引导区域）
  - 卫生间灯 → 正白光 80%

🌡️ 同步调整：
  - 客厅空调 → 24°C
  - 餐厅空调 → 24°C

多房间设置完成 ✅ 共调整 5 个区域灯光 + 2 个区域温度
```

**用户追问**: "餐厅再亮一点"

**系统响应**:
```
💡 餐厅灯已调至 85% 🔆
```

## ✅ 预期结果

- [x] 多房间灯光统一协调
- [x] 不同区域匹配不同灯光策略
- [x] 支持追问微调单个房间
- [x] 温度同步调整

## 🧪 测试要点

- [ ] LightingAgent处理多房间批量操作
- [ ] 不同区域自动匹配合适亮度
- [ ] 追问时只调整指定房间
- [ ] 多轮对话上下文保持

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 客人快到了，把所有房间的灯都调整一下 |
| 1 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>_(EntityExtractor: room=未指定→默认"客厅")_<br>💡 客厅灯已打开，亮度80% ✅ |
| 2 | 👤 用户 | 走廊灯也打开 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 走廊灯已打开，亮度60% ✅ |
| 3 | 👤 用户 | 餐厅灯调亮一些 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "灯"+"亮" → LightingAgent)_<br>_(EntityExtractor: room="餐厅")_<br>💡 餐厅灯已调至亮度70% ✅ |
| 4 | 👤 用户 | 客厅灯再亮一点 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "灯"+"亮" → LightingAgent)_<br>💡 客厅灯已调至亮度100% ✅ |
| 5 | 👤 用户 | 空调也开一下，24度 |
| 5 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 客厅空调已设置为24°C ✅<br><br>✅ 多房间灯光协调完毕！ |

### 代码执行追踪

**Turn 1 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("客人快到了，把所有房间的灯都调整一下")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity → 无匹配特定房间 → 默认"客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      └─ ILightingService.TurnOnAsync("客厅") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 全屋批量操作 | "所有房间的灯"一次调整全部 | ExtractRoomEntity每次只提取一个房间，需逐个控制 |
| 区域策略 | 不同区域自动匹配不同亮度 | LightingAgent使用固定默认亮度，无区域差异化逻辑 |
| 上下文记忆 | "再亮一点"基于当前亮度递增 | 无亮度状态记忆，每次设置绝对值 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

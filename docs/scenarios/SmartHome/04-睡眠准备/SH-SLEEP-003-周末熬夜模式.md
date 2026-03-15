# SH-SLEEP-003 周末熬夜模式

---
metadata:
  case_id: SH-SLEEP-003
  journey: 睡眠准备
  journey_order: 3
  case_type: variant
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, ClimateAgent]
  capabilities: [brightness-control, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: low
  estimated_duration_seconds: 25
  depends_on: [SH-SLEEP-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-SLEEP-003
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

周末熬夜后入睡：更长的渐暗时间，温度稍低辅助入睡。

## 📝 执行流程

**用户输入**: "好了不玩了，睡了"（凌晨1点）

**系统响应**:
```
🌙 夜深了，帮您准备入睡...
💡 卧室灯光渐暗中（2分钟缓慢过渡到夜灯）
🌡️ 空调已调至21°C（稍低助眠）

熬夜伤身，明天好好补觉哦～😴
```

## ✅ 预期结果

- [x] 渐暗时间更长（2分钟 vs 标准1分钟）
- [x] 温度稍低（21°C vs 标准22°C）
- [x] 贴心提醒注意身体

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 好了不玩了，睡了 |
| 1 | 🤖 系统 | _(意图识别: 无关键词匹配 → Unknown, Confidence=0.0)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史（如：客厅最近的温度变化）<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 把灯调暗一点 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯"+"暗" → LightingAgent)_<br>💡 客厅灯光已调暗至30% |
| 3 | 👤 用户 | 空调调到21度 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度" → ClimateAgent)_<br>🌡️ 空调已调至21°C<br><br>🌙 夜深了，注意休息哦～ |

### 代码执行追踪

**Turn 1 实际代码路径**:
```
SmartHomeMainAgent.ExecuteBusinessLogicAsync("好了不玩了，睡了")
  ├─ RuleBasedIntentRecognizer.RecognizeAsync()
  │   └─ 扫描所有意图关键词 → 0 匹配 → Unknown, Confidence=0.0
  ├─ Confidence 0.0 < 0.3
  └─ BuildLowConfidenceResponse() → 返回4个候选选项
```

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("把灯调暗一点")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity → 默认"客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      └─ 解析"调暗" → ILightingService.SetBrightnessAsync("客厅", 30) → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 睡觉意图 | 识别"睡了"="睡眠模式" | 无SleepMode意图，"睡了"不匹配关键词 |
| 渐暗时间 | 周末2分钟缓慢渐暗 vs 标准1分钟 | LightingAgent设置瞬时亮度，无渐暗时间控制 |
| 温度差异 | 周末21°C vs 标准22°C | ClimateAgent接受任意温度(16-30)，无场景区分 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

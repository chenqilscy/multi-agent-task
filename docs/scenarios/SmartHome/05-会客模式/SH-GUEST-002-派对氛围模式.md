# SH-GUEST-002 派对氛围模式

---
metadata:
  case_id: SH-GUEST-002
  journey: 会客模式
  journey_order: 2
  case_type: variant
  domain: smarthome
  complexity: L3
  agents: [LightingAgent, MusicAgent, ClimateAgent]
  capabilities: [brightness-control, color-control, music-playback, temperature-control]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P2
  doc_importance: medium
  estimated_duration_seconds: 30
  depends_on: [SH-GUEST-001]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-GUEST-002
- **用例类型**: 变体流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

派对场景模式，彩色灯光效果、动感音乐、稍低温度。

## 📝 执行流程

**用户输入**: "今晚要开派对，准备一下氛围"

**系统响应**:
```
🎉 正在切换到派对模式...

💡 灯光调整：
  - 客厅主灯 → 彩色循环模式 70%
  - 走廊灯 → 蓝紫交替 50%
  - 餐厅灯 → 暖黄色 60%

🎵 音乐设置：
  - 播放列表：派对热门
  - 音量：55%

🌡️ 温度调整：
  - 客厅空调 → 22°C 制冷模式

派对模式已激活 🎊 尽情享受吧！
```

## ✅ 预期结果

- [x] 灯光切换为彩色/动态效果
- [x] 音乐选择动感曲风并提高音量
- [x] 温度略低以适应活动场景

## 🧪 测试要点

- [ ] "派对"意图准确识别
- [ ] 灯光彩色模式正确设置
- [ ] 音量适合派对场景

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 今晚要开派对，准备一下氛围 |
| 1 | 🤖 系统 | _(IntentRecognizer: "帮" → 未匹配, 无关键词命中 → Unknown)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐（如：播放轻音乐） |
| 2 | 👤 用户 | 把灯调成彩色，亮一点 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 客厅灯已调至彩色模式，亮度70% ✅ |
| 3 | 👤 用户 | 播放派对音乐，声音大一点 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "音乐"+"播放" → MusicAgent)_<br>🎵 正在播放派对热门歌单，音量55% 🎊 |
| 4 | 👤 用户 | 空调调到22度制冷 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "空调"+"度"+"制冷" → ClimateAgent)_<br>🌡️ 空调已调至22°C 制冷模式 ✅<br><br>🎉 派对模式准备就绪！ |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("把灯调成彩色，亮一点")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity → 默认"客厅"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ 解析"彩色" → ILightingService.SetColorAsync("客厅", "rainbow") → ✅
      └─ 解析"亮" → ILightingService.SetBrightnessAsync("客厅", 70) → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| "派对"意图 | 一键切换到派对模式 | 无复合场景意图，需分步操作 |
| 彩色循环 | 灯光自动循环彩色 | SetColorAsync设置固定颜色，无循环动画 |
| 音量控制 | MusicAgent支持音量调节 | MusicAgent仅支持播放/暂停/下一首/停止，无音量API |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

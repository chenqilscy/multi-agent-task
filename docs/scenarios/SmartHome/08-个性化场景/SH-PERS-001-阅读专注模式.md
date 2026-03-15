# SH-PERS-001 阅读专注模式

---
metadata:
  case_id: SH-PERS-001
  journey: 个性化场景
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, MusicAgent]
  capabilities: [brightness-control, music-playback]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 3
  test_priority: P2
  doc_importance: medium
  estimated_duration_seconds: 20
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-PERS-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐

## 🎯 业务目标

阅读专注场景，柔和护眼灯光、轻柔白噪音或安静环境。

## 📝 执行流程

**用户输入**: "我要看书了"

**系统响应**:
```
📚 正在切换到阅读模式...

💡 灯光调整：
  - 书房台灯 → 暖白光 70%（护眼色温 4000K）
  - 其他灯光 → 调暗至 20%

🎵 音乐设置：
  - 播放轻柔白噪音（音量 15%）

阅读模式已激活 ✅ 专注阅读吧！
```

## ✅ 预期结果

- [x] 护眼色温灯光设置
- [x] 低干扰白噪音背景
- [x] 非阅读区域灯光调暗

## 🧪 测试要点

- [ ] "看书/阅读"意图识别
- [ ] 色温参数正确（4000K护眼）
- [ ] 白噪音音量低

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要看书了 |
| 1 | 🤖 系统 | _(IntentRecognizer: 无关键词匹配 → Unknown, Confidence=0.0)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 把书房灯调成护眼模式 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>_(EntityExtractor: room="书房")_<br>💡 书房灯已调整：亮度60%，暖白色 ✅ |
| 3 | 👤 用户 | 播放白噪音 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "播放" → MusicAgent)_<br>🎵 正在播放白噪音 ✅ |

### 代码执行追踪

**Turn 2 代码路径**:
```
SmartHomeControlService.ProcessCommandAsync("把书房灯调成护眼模式")
  ├─ ContainsKeywords("灯") → true → LightingAgent
  ├─ ExtractRoomEntity("书房") → "书房"
  └─ LightingAgent.ExecuteBusinessLogicAsync(request)
      ├─ ILightingService.SetBrightnessAsync("书房", 60) → ✅
      └─ ILightingService.SetColorAsync("书房", "warm") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 阅读模式 | "看书" 一键进入阅读模式 | 无ReadingMode意图，"看书"不匹配任何关键词 |
| 护眼色温 | 4000K护眼色温 | LightingAgent仅支持warm/cool/white，无K值精确控制 |
| 白噪音音量 | 低音量白噪音 | MusicAgent无音量控制，无白噪音曲目管理 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

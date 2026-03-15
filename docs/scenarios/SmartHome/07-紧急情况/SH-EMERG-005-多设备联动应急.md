# SH-EMERG-005 多设备联动应急

---
metadata:
  case_id: SH-EMERG-005
  journey: 紧急情况
  journey_order: 5
  case_type: multi-agent
  domain: smarthome
  complexity: L4
  agents: [LightingAgent, ClimateAgent, MusicAgent]
  capabilities: [brightness-control, ventilation, alarm-playback, device-coordination]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: true }
  demo_value: 5
  test_priority: P0
  doc_importance: critical
  estimated_duration_seconds: 30
  depends_on: [SH-EMERG-001, SH-EMERG-002, SH-EMERG-003]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-EMERG-005
- **用例类型**: 多Agent协作
- **演示价值**: ⭐⭐⭐⭐⭐

## 🎯 业务目标

多种紧急情况同时发生（如火灾+燃气），系统按优先级协调多设备联动。

## 📝 执行流程

**触发条件**: 烟雾传感器 + 燃气传感器同时触发

**系统自动响应**:
```
🚨🚨 复合紧急事件！

📊 检测到多重告警：
  ⚠️ 烟雾浓度超标（厨房）
  ⚠️ 燃气浓度异常（厨房）

🔔 综合应急响应（按优先级执行）：

  [最高优先级]
  1. 🔥 燃气阀门 → 已紧急关闭
  2. ⚡ 厨房电器 → 已全部断电

  [高优先级]
  3. 🌬️ 排风系统 → 最大功率通风
  4. 💡 全屋灯光 → 100% 疏散照明
  5. 🔓 前门/后门 → 已解锁

  [辅助响应]
  6. 🔊 播放紧急疏散语音
  7. 📱 已通知所有家庭成员
  8. 📞 建议拨打119

⚠️ 请立即撤离！
```

## ✅ 预期结果

- [x] 多重告警正确识别和合并
- [x] 按优先级顺序执行响应
- [x] 冲突操作智能协调（如不重复关阀门）
- [x] 三个Agent同时响应

## 🧪 测试要点

- [ ] 复合事件合并处理逻辑
- [ ] 优先级排序正确
- [ ] 冲突操作去重（不重复执行）
- [ ] 所有Agent<3秒内完成响应
- [ ] 辅助操作不阻塞关键操作

## 🗣️ 详细对话交互示例

### 对话流程

> **注意**: 此场景为传感器自动触发，非用户主动发起。

| 轮次 | 角色 | 内容 |
|------|------|------|
| 0 | ⚡ 传感器 | 烟雾传感器(厨房)+燃气传感器(厨房) 同时触发告警 |
| 0 | 🤖 系统 | _(事件处理器: 接收复合告警 → 按优先级分配任务)_ |
| 0 | 🤖 系统 | 🚨🚨 复合紧急事件！<br><br>📊 检测到多重告警：<br>  ⚠️ 烟雾浓度超标（厨房）<br>  ⚠️ 燃气浓度异常（厨房）<br><br>[最高优先级]<br>1. 🔥 燃气阀门 → 已紧急关闭<br>2. ⚡ 厨房电器 → 已全部断电<br><br>[高优先级]<br>3. 🌬️ 排风系统 → 最大功率通风<br>4. 💡 全屋灯光 → 100% 疏散照明<br>5. 🔓 前门/后门 → 已解锁<br><br>[辅助响应]<br>6. 🔊 播放紧急疏散语音<br>7. 📱 已通知所有家庭成员<br>8. 📞 建议拨打119<br><br>⚠️ 请立即撤离！ |
| 1 | 👤 用户 | 情况怎么样了 |
| 1 | 🤖 系统 | _(SmartHomeControlService: "怎么" → 无精确匹配)_<br>抱歉，我无法理解您的命令。请尝试使用照明、气候、音乐或天气查询相关的指令。 |

### 代码执行追踪

**传感器触发 理想代码路径**:
```
事件触发: {烟雾告警, 燃气告警} → 紧急事件处理器

[最高优先级 - 并行执行]
├─ ClimateAgent: IClimateService.CloseGasValveAsync() → ✅ 关闭燃气
├─ SecurityAgent: ISecurityService.UnlockDoorAsync("前门") → ✅ 解锁

[高优先级 - 并行执行]
├─ ClimateAgent: IClimateService.SetModeAsync("厨房", "fan") → ✅ 排风
├─ LightingAgent: ILightingService.SetBrightnessAsync("全屋", 100) → ✅ 疏散照明

[辅助 - 并行执行]
├─ MusicAgent: 播放紧急疏散语音
└─ 通知系统: 推送紧急通知
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 传感器事件 | 自动触发复合告警处理 | 无传感器事件监听机制，需用户主动触发 |
| 事件合并 | 多个告警智能合并处理 | 无事件合并/去重逻辑 |
| 优先级排序 | 按安全优先级执行 | ControlService为简单if-else，无优先级排序 |
| SecurityAgent | 自动解锁门+监控 | ✅ SecurityAgent已集成到ControlService路由 |
| 燃气阀门 | CloseGasValveAsync已实现 | IClimateService.CloseGasValveAsync()已在代码中 ✅ |
| 全屋联动 | 所有Agent并行执行 | 串行调用，一次一个Agent |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

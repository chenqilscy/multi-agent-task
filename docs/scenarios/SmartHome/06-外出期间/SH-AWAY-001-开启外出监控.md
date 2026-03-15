# SH-AWAY-001 开启外出监控

---
metadata:
  case_id: SH-AWAY-001
  journey: 外出期间
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L2
  agents: [LightingAgent, ClimateAgent]
  capabilities: [brightness-control, temperature-control, security-mode]
  coverage: { single-agent: false, multi-agent: true, multi-turn: false, error-handling: false }
  demo_value: 4
  test_priority: P1
  doc_importance: high
  estimated_duration_seconds: 25
  depends_on: []
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-AWAY-001
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐

## 🎯 业务目标

用户外出时一键开启外出模式，关闭不必要设备、设置节能参数。

## 📝 执行流程

**用户输入**: "我要出差几天，帮我设置外出模式"

**系统响应**:
```
🌴 正在切换到外出模式...

💡 灯光设置：
  - 所有房间灯光 → 已关闭
  - 玄关夜灯 → 保留（定时 18:00-06:00 自动开启）

🌡️ 空调设置：
  - 所有空调 → 已关闭
  - 防冻保护 → 已开启（低于5°C自动加热）

🔒 安防设置：
  - 门窗传感器 → 已激活
  - 异常检测 → 已开启

外出模式已激活 ✅ 出差愉快！
```

## ✅ 预期结果

- [x] 不必要设备全部关闭
- [x] 保留安防和防冻必要功能
- [x] 玄关夜灯定时模拟有人

## 🧪 测试要点

- [ ] "外出/出差"意图准确识别
- [ ] 灯光全关并设置夜灯定时
- [ ] 空调关闭但防冻保护开启

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 我要出差几天，帮我设置外出模式 |
| 1 | 🤖 系统 | _(IntentRecognizer: "帮我" → GeneralQuery, Confidence≈0.2 < 0.3)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备<br>2. 查询天气<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 关掉所有灯 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 客厅灯已关闭 ✅ |
| 3 | 👤 用户 | 空调也关了 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "空调" → ClimateAgent)_<br>🌡️ 客厅空调已关闭 ✅ |
| 4 | 👤 用户 | 帮我锁门 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "锁门" → SecurityAgent)_<br>🔒 大门已锁好。 |

### 代码执行追踪

**Turn 4 代码路径（SecurityAgent已集成到路由）**:
```
SmartHomeControlService.ProcessCommandAsync("帮我锁门")
  ├─ ContainsKeywords("天气"/"下雨"/...) → false
  ├─ ContainsKeywords("温度变化"/"温度历史"/...) → false
  ├─ ContainsKeywords("门锁"/"锁门"/"上锁"/"解锁"/"摄像头"/"监控"/...) → true ("锁门")
  └─ SecurityAgent.ExecuteBusinessLogicAsync(request)
       ├─ "锁门" → HandleLockDoorAsync
       ├─ ExtractLocation → "大门"(默认)
       └─ ISecurityService.LockDoorAsync("大门") → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| 外出模式 | 一键启动全屋外出模式 | 无AwayMode意图，需分步操作 |
| 安防控制 | SecurityAgent处理锁门/监控 | ✅ SecurityAgent已集成到ControlService路由 |
| 防冻保护 | 空调关闭但保留防冻 | ClimateAgent无防冻模式，关闭即完全停止 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

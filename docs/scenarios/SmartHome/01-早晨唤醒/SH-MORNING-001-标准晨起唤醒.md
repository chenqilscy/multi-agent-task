# SH-MORNING-001 标准晨起唤醒

---
metadata:
  case_id: SH-MORNING-001
  journey: 早晨唤醒
  journey_order: 1
  case_type: primary
  domain: smarthome
  complexity: L3
  agents: [WeatherAgent, ClimateAgent, LightingAgent]
  capabilities: [weather-query, temperature-control, brightness-control]
  coverage:
    single-agent: false
    multi-agent: true
    multi-turn: true
    error-handling: false
  demo_value: 5
  test_priority: P0
  doc_importance: high
  estimated_duration_seconds: 45
  requires_external_service: true
  requires_hardware: false
  depends_on: []
  enables: [SH-MORNING-002, SH-MORNING-003]
  status: designed
  last_updated: "2026-03-15"
---

## 📋 基本信息

- **用例ID**: SH-MORNING-001
- **用户旅程**: 早晨唤醒
- **用例类型**: 主流程
- **演示价值**: ⭐⭐⭐⭐⭐
- **预估耗时**: 45秒

## 🎯 业务目标

为用户提供舒适的起床体验，通过逐步唤醒设备、查询天气、调节环境，让用户从睡眠状态自然过渡到清醒状态。

## 👤 用户画像

- **场景**: 用户早上7点醒来，希望系统智能配合起床
- **用户期望**: 查询天气、穿衣建议、灯光逐渐调亮、温度舒适
- **痛点**: 早上刚醒来时不想操作复杂设备，希望一句话搞定

## 🔧 技术规格

### 涉及Agent

- **WeatherAgent**: 查询当天天气，提供穿衣和出行建议
- **ClimateAgent**: 调节室内温度到舒适范围
- **LightingAgent**: 逐步调亮卧室和客厅灯光

### 能力标签

- **多Agent协作**: true（3个Agent协同）
- **多轮对话**: 3轮
- **异常处理**: false

### 数据依赖

- **输入参数**: city（城市，可选，默认当前城市）
- **外部服务**: WeatherAPI
- **预期响应时间**: < 2秒（单次交互）

## 📝 执行流程

### Step 1: 问候和天气查询

**用户输入**: "早上好"

**系统响应**:
1. 意图识别: QueryWeather（由SmartHomeMainAgent编排）
2. 实体提取: city="北京", date="今天"
3. Agent调用: WeatherAgent.ExecuteAsync()
4. 返回结果:

```
早上好！美好的一天开始了～🌞

📍 北京今天天气：晴转多云
🌡️ 气温：12~22°C（当前 15°C）
💨 东风3级 | 💧 湿度 45% | 🌬️ 空气质量：良好（AQI 65）

👔 穿衣建议：温度适宜，建议穿薄外套或长袖衬衫。
✅ 出行建议：天气适宜出行，祝您出行愉快！
```

### Step 2: 温度调节

**系统提示**: "现在室温15度，需要帮您调高一些吗？"

**用户输入**: "调到22度吧"

**系统响应**:
1. 意图识别: AdjustClimate
2. 实体提取: room="客厅", temperature=22
3. Agent调用: ClimateAgent.ExecuteAsync()
4. 返回结果:

```
🌡️ 客厅空调已设置为22°C，制热模式中...
预计5分钟达到目标温度。
```

### Step 3: 灯光调节

**系统自动执行**:
1. Agent调用: LightingAgent.ExecuteAsync()（卧室渐亮）
2. Agent调用: LightingAgent.ExecuteAsync()（客厅灯）
3. 返回结果:

```
💡 正在为您逐渐调亮卧室灯光...
亮度将在30秒内从0%提升到80%。
客厅灯光已打开，亮度50%。

✅ 早晨唤醒模式已激活！
☀️ 天气已查询 | 🌡️ 温度已调节 | 💡 灯光已调亮
祝您今天有个好心情！
```

## ✅ 预期结果

- [x] 天气信息正确显示，包含温度、湿度、空气质量
- [x] 穿衣建议基于温度合理（15°C建议薄外套）
- [x] 客厅空调成功设置为22°C制热模式
- [x] 卧室灯光30秒渐亮到80%
- [x] 客厅灯光打开，亮度50%
- [x] 全流程耗时 < 45秒
- [x] 多Agent协作流畅无卡顿

## 🧪 测试要点

### 功能测试
- [ ] WeatherAgent返回正确天气数据
- [ ] ClimateAgent成功设置温度和模式
- [ ] LightingAgent成功实现渐亮效果
- [ ] 穿衣建议与温度匹配

### 集成测试
- [ ] 三个Agent按正确顺序调用
- [ ] Agent间数据传递正确
- [ ] 上下文保持一致（城市、房间等）
- [ ] 响应时间: Step 1 < 2s, Step 2 < 1s, Step 3 < 1s

## 💡 演示要点

### 展示亮点
1. **多Agent无缝协作**: 3个Agent协同工作，用户感知为统一服务
2. **自然语言交互**: 用日常语言控制系统
3. **智能预测**: 系统主动提供穿衣建议

### 演示脚本

**开场**: "今天演示智能家居早晨唤醒，展示多Agent协作。"
**Step 1**: 说"早上好" → 系统查询天气并播报
**Step 2**: 说"调到22度" → 系统调节温度
**Step 3**: 自动灯光渐亮 → 展示舒适唤醒体验
**总结**: "3个Agent无缝协作，用户只需自然对话。"

## 📚 相关文档

- **Agent实现**: `src/Demos/SmartHome/Agents/WeatherAgent.cs`
- **Agent实现**: `src/Demos/SmartHome/Agents/ClimateAgent.cs`
- **Agent实现**: `src/Demos/SmartHome/Agents/LightingAgent.cs`
- **相关用例**: [SH-MORNING-002 工作日晨起模式](./SH-MORNING-002-工作日晨起模式.md)

## 🗣️ 详细对话交互示例

### 对话流程

| 轮次 | 角色 | 内容 |
|------|------|------|
| 1 | 👤 用户 | 早上好 |
| 1 | 🤖 系统 | _(SmartHomeControlService: "天气"关键词未匹配,  但MainAgent可能触发天气流程)_<br>_(实际: IntentRecognizer无关键词匹配 → Unknown → BuildLowConfidenceResponse)_ |
| 1 | 🤖 系统 | 抱歉，我没有完全理解您的意思，您可能想要：<br>1. 控制设备（如：打开客厅的灯）<br>2. 查询天气（如：今天北京天气怎么样）<br>3. 查看温度历史<br>4. 播放音乐 |
| 2 | 👤 用户 | 今天北京天气怎么样 |
| 2 | 🤖 系统 | _(SmartHomeControlService: "天气" → WeatherAgent)_<br>_(EntityExtractor: city="北京", date="今天")_<br>📍 北京今天天气：晴转多云<br>🌡️ 气温：12~22°C<br>👔 穿衣建议：温度适宜，建议穿薄外套 |
| 3 | 👤 用户 | 调到22度吧 |
| 3 | 🤖 系统 | _(SmartHomeControlService: "度" → ClimateAgent)_<br>🌡️ 客厅空调已设置为22°C，制热模式中 ✅ |
| 4 | 👤 用户 | 打开客厅灯 |
| 4 | 🤖 系统 | _(SmartHomeControlService: "灯" → LightingAgent)_<br>💡 客厅灯已打开，亮度80% ✅<br><br>✅ 早晨唤醒准备完毕！<br>☀️ 天气已查询 \| 🌡️ 温度22°C \| 💡 灯光已调亮 |

### 代码执行追踪

**Turn 2 代码路径（天气查询）**:
```
SmartHomeControlService.ProcessCommandAsync("今天北京天气怎么样")
  ├─ ContainsKeywords("天气") → true → WeatherAgent
  ├─ ExtractCityEntity("北京") → "北京"
  ├─ ExtractDateEntity("今天") → "今天"
  └─ WeatherAgent.ExecuteBusinessLogicAsync(request)
      └─ IWeatherService.GetWeatherAsync("北京", DateOnly) → ✅
```

**Turn 3 代码路径（温度调节）**:
```
SmartHomeControlService.ProcessCommandAsync("调到22度吧")
  ├─ ContainsKeywords("度") → true → ClimateAgent
  ├─ ExtractRoomEntity → 默认"客厅"
  └─ ClimateAgent.ExecuteBusinessLogicAsync(request)
      ├─ 提取温度: 22 (范围16-30 → 合法)
      └─ IClimateService.SetTemperatureAsync("客厅", 22) → ✅
```

### ⚠️ 代码差距说明

| 项目 | 设计预期 | 当前代码实际行为 |
|------|---------|----------------|
| "早上好"触发 | 自动启动晨起模式(天气+温度+灯光) | "早上好"无关键词匹配 → BuildLowConfidenceResponse |
| 多Agent并行 | 3个Agent同时执行 | ControlService串行if-else，一次仅调用一个Agent |
| 灯光渐亮 | 30秒从0%渐亮到80% | LightingAgent设置瞬时亮度，无渐亮动画 |
| 自动灯光 | Step 3自动执行无需用户操作 | 每个操作都需用户主动发起命令 |

---

**文档版本**: v1.0 | **最后更新**: 2026-03-15

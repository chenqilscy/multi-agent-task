# 按Agent索引

本目录按照Agent组织用例，方便开发人员快速查找相关用例。

## 📁 Agent列表

### SmartHome Agents

| Agent | 说明 | 相关用例数 |
|-------|------|-----------|
| [WeatherAgent](#weatheragent) | 天气查询和建议 | 8 |
| [ClimateAgent](#climateagent) | 空调和温度控制 | 12 |
| [LightingAgent](#lightingagent) | 灯光控制和亮度调节 | 10 |
| [MusicAgent](#musicagent) | 音乐播放控制 | 6 |
| [TemperatureHistoryAgent](#temperaturehistoryagent) | 温度历史记录 | 4 |

### CustomerService Agents

| Agent | 说明 | 相关用例数 |
|-------|------|-----------|
| [CustomerServiceMainAgent](#customerservicemainagent) | 主控Agent，意图识别和路由 | 38 |
| [KnowledgeBaseAgent](#knowledgebaseagent) | 知识库查询 | 12 |
| [OrderAgent](#orderagent) | 订单处理 | 14 |
| [TicketAgent](#ticketagent) | 工单管理 | 12 |

---

## WeatherAgent

天气查询和建议Agent，查询实时天气，提供穿衣出行建议。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| SH-MORNING-001 | 标准晨起唤醒 | 早晨唤醒 |
| SH-MORNING-002 | 工作日晨起模式 | 早晨唤醒 |
| SH-MORNING-003 | 周末懒觉模式 | 早晨唤醒 |
| SH-MORNING-004 | 天气服务异常 | 早晨唤醒 |
| SH-DEPART-001 | 标准离家模式 | 离家准备 |
| SH-DEPART-002 | 远距离出行 | 离家准备 |
| SH-HOME-001 | 标准回家模式 | 回家放松 |
| SH-HOME-002 | 夏天回家 | 回家放松 |

## ClimateAgent

空调和温度控制Agent，管理制冷、制热、温度设置。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| SH-MORNING-001 | 标准晨起唤醒 | 早晨唤醒 |
| SH-MORNING-002 | 工作日晨起模式 | 早晨唤醒 |
| SH-MORNING-003 | 周末懒觉模式 | 早晨唤醒 |
| SH-DEPART-001 | 标准离家模式 | 离家准备 |
| SH-HOME-001 | 标准回家模式 | 回家放松 |
| SH-HOME-002 | 夏天回家 | 回家放松 |
| SH-HOME-003 | 冬天回家 | 回家放松 |
| SH-SLEEP-001 | 标准睡眠模式 | 睡眠准备 |
| SH-GUEST-001 | 标准会客模式 | 会客模式 |
| SH-AWAY-001 | 开启外出监控 | 外出期间 |
| SH-EMERG-001 | 火警烟雾检测响应 | 紧急情况 |
| SH-EMERG-005 | 多设备联动应急 | 紧急情况 |

## LightingAgent

灯光控制Agent，管理灯光开关、亮度和颜色。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| SH-MORNING-001 | 标准晨起唤醒 | 早晨唤醒 |
| SH-MORNING-005 | 设备离线处理 | 早晨唤醒 |
| SH-DEPART-001 | 标准离家模式 | 离家准备 |
| SH-HOME-001 | 标准回家模式 | 回家放松 |
| SH-SLEEP-001 | 标准睡眠模式 | 睡眠准备 |
| SH-SLEEP-004 | 灯光调暗失败 | 睡眠准备 |
| SH-GUEST-001 | 标准会客模式 | 会客模式 |
| SH-GUEST-004 | 多房间灯光协调 | 会客模式 |
| SH-EMERG-001 | 火警烟雾检测响应 | 紧急情况 |
| SH-PERS-002 | 电影观影模式 | 个性化场景 |

## MusicAgent

音乐播放控制Agent，管理播放、暂停、曲目切换。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| SH-MORNING-002 | 工作日晨起模式 | 早晨唤醒 |
| SH-HOME-001 | 标准回家模式 | 回家放松 |
| SH-HOME-005 | 音乐服务故障 | 回家放松 |
| SH-GUEST-001 | 标准会客模式 | 会客模式 |
| SH-GUEST-002 | 派对氛围模式 | 会客模式 |
| SH-PERS-005 | 聚餐准备模式 | 个性化场景 |

## TemperatureHistoryAgent

温度历史记录Agent，查询和分析温度趋势。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| SH-HOME-002 | 夏天回家 | 回家放松 |
| SH-HOME-003 | 冬天回家 | 回家放松 |
| SH-AWAY-002 | 远程查询家中状态 | 外出期间 |
| SH-PERS-001 | 阅读专注模式 | 个性化场景 |

## KnowledgeBaseAgent

知识库查询Agent，负责FAQ匹配和RAG问答。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| CS-INITIAL-001 | 标准咨询流程 | 初次咨询 |
| CS-INITIAL-002 | 问候后直接提问 | 初次咨询 |
| CS-INITIAL-003 | 多个连续问题 | 初次咨询 |
| CS-INITIAL-004 | 问题不明确 | 初次咨询 |
| CS-INITIAL-005 | 知识库无匹配答案 | 初次咨询 |
| CS-RETURN-003 | 超过退货期限 | 退换货处理 |
| CS-RETURN-005 | 特殊商品退货限制 | 退换货处理 |
| CS-PROACTIVE-002 | 促销活动智能推荐 | 主动服务 |

## OrderAgent

订单处理Agent，负责订单查询、取消、退款、物流追踪。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| CS-ORDER-001 | 标准订单查询 | 订单查询 |
| CS-ORDER-002 | 模糊查询 | 订单查询 |
| CS-ORDER-003 | 订单不存在 | 订单查询 |
| CS-ORDER-004 | 订单系统超时 | 订单查询 |
| CS-RETURN-001 | 标准退货申请 | 退换货处理 |
| CS-RETURN-002 | 换货申请 | 退换货处理 |
| CS-RETURN-004 | 缺少必要信息 | 退换货处理 |
| CS-PROACTIVE-001 | 发货延迟主动通知 | 主动服务 |
| CS-CLOSE-001 | 标准投诉处理闭环 | 投诉闭环 |

## TicketAgent

工单管理Agent，负责创建、查询、更新工单。

| 用例ID | 用例名称 | 旅程 |
|--------|----------|------|
| CS-COMPLAIN-001 | 标准投诉处理 | 投诉建议 |
| CS-COMPLAIN-002 | 产品缺陷投诉 | 投诉建议 |
| CS-COMPLAIN-003 | 物流延误投诉 | 投诉建议 |
| CS-COMPLAIN-004 | 情绪安抚和降级处理 | 投诉建议 |
| CS-TICKET-001 | 提交新工单 | 工单跟进 |
| CS-TICKET-002 | 查询工单状态 | 工单跟进 |
| CS-TICKET-003 | 补充工单信息 | 工单跟进 |
| CS-TICKET-004 | 工单分配和进度查询 | 工单跟进 |
| CS-TICKET-005 | 工单处理超时处理 | 工单跟进 |
| CS-CLOSE-002 | 多部门协调处理 | 投诉闭环 |
| CS-CLOSE-003 | 处理结果用户确认 | 投诉闭环 |
| CS-CLOSE-006 | 重复投诉预警 | 投诉闭环 |

---

**最后更新**: 2026-03-15

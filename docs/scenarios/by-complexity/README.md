# 按复杂度索引

本目录按照用例复杂度组织，提供循序渐进的学习路径。

## 📊 复杂度分级

### L1 - 单Agent基础功能

展示单个Agent的独立能力，适合初学者入门。

**特点**: 涉及1个Agent，单次交互，无多轮对话，响应时间 < 1秒

| 用例ID | 用例名称 | 场景 | 优先级 |
|--------|----------|------|--------|
| SH-AWAY-002 | 远程查询家中状态 | SmartHome | P2 |
| CS-TICKET-002 | 查询工单状态 | CustomerService | P1 |

---

### L2 - 多轮对话管理

展示多轮对话和澄清能力。

**特点**: 涉及1-2个Agent，2-3轮对话交互，包含意图澄清和参数确认

| 用例ID | 用例名称 | 场景 | 优先级 |
|--------|----------|------|--------|
| SH-SLEEP-001 | 标准睡眠模式 | SmartHome | P1 |
| SH-SLEEP-002 | 工作日早睡模式 | SmartHome | P2 |
| SH-SLEEP-003 | 周末熬夜模式 | SmartHome | P2 |
| SH-SLEEP-004 | 灯光调暗失败 | SmartHome | P1 |
| SH-AWAY-001 | 开启外出监控 | SmartHome | P1 |
| SH-AWAY-003 | 模拟有人在家 | SmartHome | P1 |
| CS-INITIAL-001 | 标准咨询流程 | CustomerService | P0 |
| CS-INITIAL-002 | 问候后直接提问 | CustomerService | P1 |
| CS-INITIAL-003 | 多个连续问题 | CustomerService | P1 |
| CS-INITIAL-004 | 问题不明确 | CustomerService | P0 |
| CS-INITIAL-005 | 知识库无匹配答案 | CustomerService | P0 |
| CS-ORDER-001 | 标准订单查询 | CustomerService | P0 |
| CS-ORDER-002 | 模糊查询 | CustomerService | P0 |
| CS-ORDER-003 | 订单不存在 | CustomerService | P1 |
| CS-ORDER-004 | 订单系统超时 | CustomerService | P0 |
| CS-TICKET-001 | 提交新工单 | CustomerService | P0 |
| CS-TICKET-003 | 补充工单信息 | CustomerService | P2 |
| CS-TICKET-005 | 工单处理超时处理 | CustomerService | P1 |

---

### L3 - 多Agent协作

展示多Agent协同完成任务的能力。

**特点**: 涉及2-4个Agent，复杂业务流程，Agent间数据共享

| 用例ID | 用例名称 | 场景 | 优先级 |
|--------|----------|------|--------|
| SH-MORNING-001 | 标准晨起唤醒 | SmartHome | P0 |
| SH-MORNING-002 | 工作日晨起模式 | SmartHome | P1 |
| SH-MORNING-003 | 周末懒觉模式 | SmartHome | P1 |
| SH-MORNING-004 | 天气服务异常 | SmartHome | P0 |
| SH-MORNING-005 | 设备离线处理 | SmartHome | P0 |
| SH-DEPART-001 | 标准离家模式 | SmartHome | P0 |
| SH-DEPART-002 | 远距离出行 | SmartHome | P1 |
| SH-DEPART-003 | 设备关闭失败 | SmartHome | P0 |
| SH-DEPART-004 | 网络不稳定 | SmartHome | P1 |
| SH-HOME-001 | 标准回家模式 | SmartHome | P0 |
| SH-HOME-002 | 夏天回家 | SmartHome | P1 |
| SH-HOME-003 | 冬天回家 | SmartHome | P1 |
| SH-HOME-004 | 澄清真请求 | SmartHome | P1 |
| SH-HOME-005 | 音乐服务故障 | SmartHome | P0 |
| SH-GUEST-001 | 标准会客模式 | SmartHome | P1 |
| SH-GUEST-002 | 派对氛围模式 | SmartHome | P2 |
| SH-GUEST-003 | 商务洽谈模式 | SmartHome | P2 |
| SH-GUEST-004 | 多房间灯光协调 | SmartHome | P1 |
| SH-AWAY-004 | 异常入侵检测响应 | SmartHome | P0 |
| SH-PERS-001 | 阅读专注模式 | SmartHome | P2 |
| SH-PERS-002 | 电影观影模式 | SmartHome | P1 |
| SH-PERS-003 | 运动健身模式 | SmartHome | P2 |
| SH-PERS-004 | 工作学习模式 | SmartHome | P2 |
| SH-PERS-005 | 聚餐准备模式 | SmartHome | P1 |
| CS-RETURN-001 | 标准退货申请 | CustomerService | P0 |
| CS-RETURN-002 | 换货申请 | CustomerService | P1 |
| CS-RETURN-003 | 超过退货期限 | CustomerService | P1 |
| CS-RETURN-004 | 缺少必要信息 | CustomerService | P0 |
| CS-RETURN-005 | 特殊商品退货限制 | CustomerService | P1 |
| CS-COMPLAIN-001 | 标准投诉处理 | CustomerService | P1 |
| CS-COMPLAIN-002 | 产品缺陷投诉 | CustomerService | P0 |
| CS-COMPLAIN-003 | 物流延误投诉 | CustomerService | P1 |
| CS-COMPLAIN-004 | 情绪安抚和降级处理 | CustomerService | P0 |
| CS-TICKET-004 | 工单分配和进度查询 | CustomerService | P1 |
| CS-ESCAL-001 | 自动升级到人工客服 | CustomerService | P0 |
| CS-ESCAL-002 | VIP优先通道 | CustomerService | P1 |
| CS-ESCAL-003 | 紧急问题快速响应 | CustomerService | P0 |
| CS-ESCAL-004 | 跨部门协作转接 | CustomerService | P1 |

---

### L4 - 复杂编排

展示复杂业务逻辑和编排能力。

**特点**: 涉及4+个Agent，长流程业务场景，包含决策分支和条件判断

| 用例ID | 用例名称 | 场景 | 优先级 |
|--------|----------|------|--------|
| SH-EMERG-001 | 火警烟雾检测响应 | SmartHome | P0 |
| SH-EMERG-002 | 燃气泄漏处理 | SmartHome | P0 |
| SH-EMERG-003 | 陌生人闯入响应 | SmartHome | P0 |
| SH-EMERG-004 | 紧急求助模式 | SmartHome | P0 |
| SH-EMERG-005 | 多设备联动应急 | SmartHome | P0 |
| CS-PROACTIVE-001 | 发货延迟主动通知 | CustomerService | P1 |
| CS-PROACTIVE-002 | 促销活动智能推荐 | CustomerService | P2 |
| CS-PROACTIVE-003 | 会员权益到期提醒 | CustomerService | P2 |
| CS-PROACTIVE-004 | 生日祝福和优惠券 | CustomerService | P1 |
| CS-PROACTIVE-005 | 异常交易主动核实 | CustomerService | P0 |
| CS-CLOSE-001 | 标准投诉处理闭环 | CustomerService | P0 |
| CS-CLOSE-002 | 多部门协调处理 | CustomerService | P0 |
| CS-CLOSE-003 | 处理结果用户确认 | CustomerService | P1 |
| CS-CLOSE-004 | 满意度回访 | CustomerService | P1 |
| CS-CLOSE-005 | 投诉数据分析和优化 | CustomerService | P2 |
| CS-CLOSE-006 | 重复投诉预警 | CustomerService | P0 |

---

## 🚀 推荐学习路径

1. **入门阶段**: 学习L1和L2用例，理解单Agent和多轮对话
2. **进阶阶段**: 学习L3用例，掌握多Agent协作模式
3. **高级阶段**: 研究L4用例，理解复杂业务编排

---

**最后更新**: 2026-03-15

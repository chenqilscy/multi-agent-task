# CKY.MAF 文档索引

> **最后更新**: 2026-03-22
> **框架版本**: v2.0
> **项目状态**: Phase 1 完成，Phase 2 加固中

本文档是 CKY.MAF 项目的统一文档导航入口。

---

## 快速导航

| 我想要... | 直接跳转 |
|-----------|----------|
| 了解架构 | [核心架构](design-docs/core-architecture.md) |
| 实现代码 | [实现指南](design-docs/implementation-guide.md) |
| 快速上手 LLM Agent | [LLM Agent 快速入门](guides/LLM_AGENT_QUICK_START.md) |
| 使用 MafAiAgent | [MafAiAgent 使用手册](guides/how-to-use-mafaiagent.md) |
| 部署到生产环境 | [部署指南](guides/deployment.md) |
| 查看测试策略 | [测试指南](guides/testing.md) |
| 查看项目计划 | [执行计划](exec-plans/) |
| 查看质量评分 | [质量评分](QUALITY_SCORE.md) |

---

## 目录结构

```
docs/
├── README.md                          ← 本文档（总导航）
├── ARCHITECTURE.md                    ← 顶层架构蓝图（精华提炼）
├── QUALITY_SCORE.md                   ← 各模块质量评分 + 技术债追踪
│
├── design-docs/                       ← 设计文档（权威参考）
│   ├── index.md                       ← 设计文档索引
│   ├── core-architecture.md           ← 5层DIP架构、核心设计模式
│   ├── implementation-guide.md        ← 项目结构、接口定义、实现模式
│   ├── architecture-diagrams.md       ← 架构流程图、数据流图
│   ├── task-scheduling.md             ← 任务优先级、依赖管理、DAG
│   ├── error-handling.md              ← 重试、熔断、5级降级
│   └── ms-af-integration.md           ← MS Agent Framework 集成
│
├── exec-plans/                        ← 执行计划（活跃 + 已完成）
│   ├── active/                        ← 当前活跃计划
│   │   └── phase2-consolidation.md    ← Phase 2 加固计划
│   ├── completed/                     ← 已完成计划
│   │   ├── phase1-initial-implementation.md
│   │   └── customer-service-production-plan.md
│   └── tech-debt-tracker.md           ← 技术债清单
│
├── guides/                            ← 操作指南（按主题索引）
│   ├── index.md                       ← 指南索引
│   ├── LLM_AGENT_QUICK_START.md       ← 5分钟上手
│   ├── deployment.md                  ← 部署指南
│   ├── testing.md                     ← 测试策略
│   └── [其他 how-to 指南]
│
├── examples/                          ← 使用示例
│   ├── dialog-management-usage.md
│   └── long-dialog-usage.md
│
├── scenarios/                         ← 场景用例（74个）
│   ├── SmartHome/                     ← 智能家居（36个）
│   ├── CustomerService/               ← 客服系统（38个）
│   └── coverage-report.md             ← 用例与代码覆盖映射
│
├── references/                        ← 外部参考资料
│
├── generated/                         ← 自动生成的文档
│
├── specs/                             ← [遗留] 原始规范文档
│
└── archives/                          ← 历史文档归档
    ├── decisions/                     ← 架构决策记录
    ├── plans/                         ← 开发计划历史
    └── reports/                       ← 分析报告
```

---

## 设计文档（design-docs/）

核心架构与设计决策的权威参考，详见 [设计文档索引](design-docs/index.md)。

| 文档 | 内容摘要 |
|------|----------|
| [核心架构](design-docs/core-architecture.md) | 5层DIP架构、LLM服务架构、核心设计模式 |
| [实现指南](design-docs/implementation-guide.md) | 项目结构、接口定义、Agent实现模式 |
| [任务调度](design-docs/task-scheduling.md) | 优先级系统、依赖管理、DAG拓扑排序 |
| [错误处理](design-docs/error-handling.md) | 重试、熔断、5级降级 |

---

## 执行计划（exec-plans/）

| 计划 | 状态 |
|------|------|
| [Phase 2 加固计划](exec-plans/active/phase2-consolidation.md) | 进行中 |
| [Phase 1 初始实现](exec-plans/completed/phase1-initial-implementation.md) | 已完成 |
| [技术债清单](exec-plans/tech-debt-tracker.md) | 持续更新 |

---

## 操作指南（guides/）

按主题组织的操作指南，详见 [指南索引](guides/index.md)。

---

## 场景用例（scenarios/）

74个场景用例，详见 [总索引](scenarios/TOTAL-INDEX.md)。

| 场景 | 用例数 |
|------|--------|
| [智能家居](scenarios/SmartHome/) | 36 |
| [客服系统](scenarios/CustomerService/) | 38 |
|------|------|
| [MS AF AI Context Provider](references/ms-agent-framework-ai-context-provider.md) | Microsoft Agent Framework 上下文提供者参考 |

## 📈 当前项目状态

| 模块 | 状态 | 说明 |
|------|------|------|
| Core 抽象层 | ✅ 完成 | 所有接口、模型定义完整 |
| Infrastructure/Caching | ✅ 完成 | Redis + Memory 缓存实现 |
| Infrastructure/Repository | ✅ 完成 | EF Core + SQLite/PostgreSQL |
| Infrastructure/Vectorization | ✅ 完成 | Qdrant + Memory 向量存储 |
| Services 层 | ✅ 完成 | 任务调度、NLP、对话管理 |
| LLM Agents（ZhipuAI、QwenAI）| ✅ 基本完成 | 流式支持待实现 |
| LLM Agents（其他提供商）| ⏳ 骨架实现 | API 调用待实现 |
| Demo/SmartHome | ✅ 基本可用 | 部分功能待完善 |
| Session Store 注册 | ✅ 已启用 | 支持 Database/Redis 两种实现 |
| Prometheus 监控 | ✅ 基本完成 | Redis 失败指标集成待完善 |
| 单元测试 | ✅ 覆盖率高 | 185 个测试，182 通过 |

---

> 📝 **文档维护说明**：
> - 架构规范文档（`specs/`）是权威参考，如有代码变更请同步更新对应规范文档
> - 操作指南（`guides/`）以实际代码为准，文档内容仅供参考
> - 历史文档（`archives/`）是开发过程的中间产物，不保证与当前代码一致
> - 场景用例（`scenarios/`）用于测试验证，反映当前功能状态

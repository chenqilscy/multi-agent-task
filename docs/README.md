# CKY.MAF 文档索引

> **最后更新**: 2026-03-16
> **框架版本**: v1.2
> **项目状态**: 开发阶段（约 75% 完成度）

本文档是 CKY.MAF 项目的统一文档导航入口，涵盖架构规范、操作指南和使用示例。

---

## 📌 快速导航

| 我想要... | 直接跳转 |
|-----------|----------|
| **快速参考** | [⚡ 快速参考卡片](SNAPSHOT.md) |
| **了解架构** | [🏗️ 核心架构文档](specs/00-CORE-ARCHITECTURE.md) |
| **实现代码** | [💻 实现指南](specs/01-IMPLEMENTATION-GUIDE.md) |
| 了解项目整体架构 | [架构规范 → 01-architecture-overview](specs/01-architecture-overview.md) |
| 快速上手 LLM Agent | [LLM Agent 快速入门](guides/LLM_AGENT_QUICK_START.md) |
| 集成 LLM 与 Agent 框架 | [集成指南](guides/how-to-integrate-llm-with-agent-framework.md) |
| 使用 MafAiAgent | [MafAiAgent 使用手册](guides/how-to-use-mafaiagent.md) |
| 配置 Redis / 缓存 | [Redis 深度解析](guides/deep-dive-redis-implementation.md) |
| 添加 Prometheus 监控 | [Prometheus 使用指南](guides/how-to-use-prometheus.md) |
| 部署到生产环境 | [部署指南](specs/08-deployment-guide.md) |
| 查看测试策略 | [测试指南](specs/10-testing-guide.md) |

---

## 📁 目录结构

```
docs/
├── README.md                          ← 本文档（总导航）
│
├── specs/                             ← 架构与规范文档（权威参考）
│   ├── README.md                      ← specs 目录导航
│   ├── 01-architecture-overview.md    ← ⭐ 架构概览（必读）
│   ├── 02-architecture-diagrams.md    ← 架构图表集
│   ├── 03-task-scheduling-design.md   ← 任务调度系统
│   ├── 04-langgraph-comparison.md     ← LangGraph 对比
│   ├── 05-industry-frameworks-comparison.md ← 业界框架对比
│   ├── 06-interface-design-spec.md    ← 接口设计规范
│   ├── 07-ui-design-spec.md           ← UI 设计规范
│   ├── 08-deployment-guide.md         ← 部署指南
│   ├── 09-implementation-guide.md     ← 实现指南
│   ├── 10-testing-guide.md            ← 测试指南
│   ├── 11-implementation-roadmap.md   ← 实施路线图
│   ├── 12-layered-architecture.md     ← ⭐ 5层分层架构（必读）
│   ├── 13-performance-benchmarks.md   ← 性能基准测试
│   ├── 14-error-handling-guide.md     ← 错误处理指南
│   └── MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md ← MS AF 集成
│
├── guides/                            ← 操作指南与快速入门
│   ├── LLM_AGENT_QUICK_START.md       ← LLM Agent 快速入门
│   ├── deep-dive-redis-implementation.md ← Redis 实现深度解析
│   ├── llm-agent-factory-architecture.md ← LLM Agent 工厂架构
│   ├── llm-provider-configuration-guide.md ← LLM 提供商配置指南
│   ├── how-to-integrate-llm-with-agent-framework.md
│   ├── how-to-load-llm-config.md
│   ├── how-to-use-llm-agent-factory.md
│   ├── how-to-use-llm-enhanced-entity-extraction.md
│   ├── how-to-use-mafaiagent.md
│   ├── how-to-use-prometheus.md
│   └── how-to-use-signalr.md
│
├── examples/                          ← 使用示例
│   ├── dialog-management-usage.md     ← 对话管理示例
│   └── long-dialog-usage.md           ← 长对话示例
│
├── scenarios/                         ← 场景用例文档
│   ├── CustomerService/               ← 客服场景
│   ├── SmartHome/                     ← 智能家居场景
│   └── [其他分类目录]
│
├── references/                        ← 外部参考资料
│   └── ms-agent-framework-ai-context-provider.md
│
└── archives/                          ← 历史文档归档（待清理）
    ├── plans/                         ← 开发计划（历史记录）
    ├── reports/                       ← 分析与执行报告
    └── decisions/                     ← 架构决策记录
```

---

## 📚 架构规范文档（specs/）

这是项目最权威的参考文档，按阅读优先级排序：

### 🏗️ 核心架构（必读）

| 文档 | 内容摘要 | 适合人群 |
|------|----------|----------|
| [01-架构概览](specs/01-architecture-overview.md) | 核心设计原则、分层架构、快速开始 | 所有人 |
| [12-分层依赖架构](specs/12-layered-architecture.md) ⭐ | 5层DIP架构、存储抽象、DI配置 | 架构师、开发 |
| [06-接口设计规范](specs/06-interface-design-spec.md) | 所有接口定义、数据模型 | 开发人员 |
| [03-任务调度系统](specs/03-task-scheduling-design.md) | 优先级系统、依赖管理、调度算法 | 架构师、核心开发 |

### 📋 开发与测试

| 文档 | 内容摘要 | 适合人群 |
|------|----------|----------|
| [09-实现指南](specs/09-implementation-guide.md) | 目录结构、代码模式、最佳实践 | 开发人员 |
| [10-测试指南](specs/10-testing-guide.md) | 70/25/5测试策略、44个单测场景 | 开发、测试 |
| [14-错误处理指南](specs/14-error-handling-guide.md) ⭐ | 重试、熔断、降级（5级）| 架构师、后端 |

### 🚀 运维与部署

| 文档 | 内容摘要 | 适合人群 |
|------|----------|----------|
| [08-部署指南](specs/08-deployment-guide.md) | Docker/K8s、配置管理、安全加固 | DevOps |
| [13-性能基准测试](specs/13-performance-benchmarks.md) | P50/P95目标、吞吐量、资源限制 | 运维、性能工程师 |
| [11-实施路线图](specs/11-implementation-roadmap.md) | 6阶段36天计划、里程碑 | PM、Tech Lead |

### 🔍 参考与对比

| 文档 | 内容摘要 |
|------|----------|
| [02-架构图表集](specs/02-architecture-diagrams.md) | 系统架构图、状态机图、序列图 |
| [04-LangGraph对比](specs/04-langgraph-comparison.md) | CKY.MAF vs LangGraph 10维度对比 |
| [05-业界框架对比](specs/05-industry-frameworks-comparison.md) | 6大框架对比矩阵 |
| [07-UI设计规范](specs/07-ui-design-spec.md) | Blazor UI、实时通信、响应式设计 |
| [MICROSOFT_AGENT_FRAMEWORK_INTEGRATION](specs/MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md) | MS AF 集成说明 |

---

## 🛠️ 操作指南（guides/）

这些文档专注于"如何做"，适合快速上手：

| 文档 | 适用场景 |
|------|----------|
| [LLM Agent 快速入门](guides/LLM_AGENT_QUICK_START.md) | 第一次使用 LLM Agent |
| [集成 LLM 与 Agent 框架](guides/how-to-integrate-llm-with-agent-framework.md) | 将 LLM 提供商接入框架 |
| [加载 LLM 配置](guides/how-to-load-llm-config.md) | 配置管理、多环境支持 |
| [使用 LLM Agent 工厂](guides/how-to-use-llm-agent-factory.md) | 通过工厂创建和管理 Agent |
| [使用 LLM 增强实体提取](guides/how-to-use-llm-enhanced-entity-extraction.md) | NLP 实体提取与 LLM 增强 |
| [使用 MafAiAgent](guides/how-to-use-mafaiagent.md) | MafAiAgent 核心功能使用 |
| [配置 Prometheus 监控](guides/how-to-use-prometheus.md) | 指标收集、仪表板 |
| [使用 SignalR 实时通信](guides/how-to-use-signalr.md) | 实时推送、Hub 配置 |
| [Redis 实现深度解析](guides/deep-dive-redis-implementation.md) | Redis 缓存层详解 |
| [LLM Agent 工厂架构](guides/llm-agent-factory-architecture.md) | 工厂模式设计与实现 |
| [LLM 提供商配置指南](guides/llm-provider-configuration-guide.md) | 多提供商配置最佳实践 |

---

## 💡 使用示例（examples/）

| 文档 | 内容 |
|------|------|
| [对话管理示例](examples/dialog-management-usage.md) | 多轮对话、槽位填充示例 |
| [长对话示例](examples/long-dialog-usage.md) | 长对话上下文管理、压缩示例 |

---

## 📦 场景用例（scenarios/）

详细的场景测试用例和业务流程文档：

| 场景类型 | 说明 |
|---------|------|
| [客服场景](scenarios/CustomerService/) | 8个客服场景，74个用例 |
| [智能家居](scenarios/SmartHome/) | 8个智能家居场景 |
| [按复杂度分类](scenarios/by-complexity/) | 简单/中等/复杂场景 |
| [按测试优先级](scenarios/by-test-priority/) | P0/P1/P2 优先级 |
| [场景模板](scenarios/templates/) | 用例文档模板 |

---

## 📚 历史文档归档（archives/）

开发过程中的历史文档，已归档待清理：

### 归档内容
- **plans/** - 历史开发计划（16个文档）
  - 单元测试设计与实现
  - LLM 增强实体提取
  - 自动服务注册
  - Demo 设计与实现
  - 内置存储架构调整
  - Demo 对话框架
  - Demo 场景用例
  - 长对话上下文优化

- **reports/** - 分析与执行报告（5个文档）
  - 长对话优化实现摘要
  - TODO 清理分析报告
  - TODO 清理执行摘要
  - 客服交叉验证报告
  - 启动前审查报告

- **decisions/** - 架构决策记录（5个文档）
  - 架构更新摘要
  - LLM 架构重构摘要
  - LLM 代码审查修复记录
  - MafAiAgent 架构更新
  - MafAiAgent 方法设计

> **注意**: 这些文档是开发过程的中间产物，仅供参考。最新架构和实现请以 `specs/` 和 `guides/` 目录中的文档为准。

---

## 🔗 外部参考（references/）

| 文档 | 内容 |
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

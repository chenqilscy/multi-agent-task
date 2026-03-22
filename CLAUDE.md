# CLAUDE.md

语言：使用简体中文

## 项目简介

**CKY.MAF** — 基于 Microsoft Agent Framework (Preview) 的企业级多 Agent 框架。  
.NET 10 | Blazor Server | EF Core | Redis | Qdrant | 7 大 LLM 提供商

## 架构 (5 层 DIP)

```
L5 Demo        Blazor Server (SmartHome / CustomerService)
L4 Services    调度 · 编排 · 意图 · 会话 · 降级
L3 Infra       EF Core · Redis · Qdrant
L2 Abstractions ISessionStorage · IMemoryManager
L1 Core        ICacheStore · IVectorStore · IRepository (零外部依赖)
```

**关键规则**: Core 层零外部依赖 (仅 MS AF + Microsoft.Extensions.*)。存储实现全部在 Infrastructure 层。

## 常用命令

```bash
dotnet build src/CKY.MAF.slnx          # 构建
dotnet test tests/UnitTests             # 单元测试
dotnet test tests/IntegrationTests      # 集成测试
dotnet run --project src/Demos/SmartHome # 运行 Demo
```

## 项目结构

```
src/
├─ Core/           核心抽象 (L1) — 接口、模型、Agent 基类
├─ Services/       业务服务 (L4) — 调度、编排、NLP、会话、降级
├─ Infrastructure/ 基础设施 (L3) — Repository(EF Core)、Caching(Redis)、Vectorization(Qdrant)
└─ Demos/          演示应用 (L5) — SmartHome (6 Agent)、CustomerService (RAG+工单)
tests/
├─ UnitTests/      370 通过 — xUnit + Moq + FluentAssertions
├─ IntegrationTests/ 102 通过 — Testcontainers
└─ E2ETests/
```

## 关键接口 → 实现

| 接口 | 实现 | 层 |
|------|------|----|
| ICacheStore | RedisCacheStore / MemoryCacheStore | L3 |
| IVectorStore | QdrantVectorStore / MemoryVectorStore | L3 |
| IRelationalDatabase | EfCoreRelationalDatabase | L3 |
| ITaskScheduler | MafTaskScheduler | L4 |
| ITaskOrchestrator | MafTaskOrchestrator (→ PersistentTaskOrchestrator) | L4 |
| IMafAiAgentRegistry | MafAiAgentRegistry | L4 |

## Agent 体系

- `MafAiAgent : AIAgent` — LLM 基类 (7 提供商: 智谱/通义/文心/讯飞/百川/MiniMax/Fallback)
- `MafBusinessAgentBase` — 纯业务基类
- 专用 Agent: Embedding · Dialogue · Intent · Code · Translation · Video · Summarization

## 开发规范

- ✅ Services 层只依赖抽象 — 禁止引用具体实现
- ✅ 所有 I/O 用 async/await — 禁止 `.Result` / `.Wait()`
- ✅ LLM 仅通过 MS AF 接入 — 禁止 SemanticKernel / 直接 SDK
- ✅ 测试: Unit(Moq) / Integration(Testcontainers) / E2E(真实 LLM)

## 文档导航

| 内容 | 路径 |
|------|------|
| 设计文档索引 | docs/design-docs/index.md |
| 开发指南索引 | docs/guides/index.md |
| Phase 2 执行计划 | docs/exec-plans/active/phase2-consolidation.md |
| Phase 1 记录 | docs/exec-plans/completed/phase1-initial-implementation.md |
| 质量评估 | docs/QUALITY_SCORE.md |
| 场景用例总索引 | docs/scenarios/TOTAL-INDEX.md |
| 历史报告存档 | docs/archives/ |

## 韧性策略

- 重试: 指数退避 + 抖动 (max 3 次)
- 熔断: LLM 10次/60s → 断开120s; Redis 20次/30s → 60s
- 5 级降级: 关闭推荐 → 关闭向量搜索 → 关闭L2缓存 → 简化模型 → 规则引擎

## DI 快速注册

```csharp
builder.Services.AddMafBuiltinServices(builder.Configuration);
```

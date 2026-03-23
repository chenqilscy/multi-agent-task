# Phase 2: 质量巩固与端到端验证

> **创建日期**: 2026-03-22  
> **前置阶段**: Phase 1 (100% 完成，详见 ../completed/phase1-initial-implementation.md)  
> **目标**: 将"代码存在"提升为"端到端可验证"  

---

## 当前基线

| 指标 | 现状 | Phase 2 目标 |
|------|------|-------------|
| 核心接口实现 | 100% (8/8) | 100% | ✅ 完成 |
| MafTaskOrchestrator | ✅ 已修复 (callback模式) | E2E验证通过 | ✅ 7个集成测试通过 |
| 用例覆盖度 | ~75% (SH:~78%, CS:~72%) | ≥75% | ✅ 已达标 |
| 单元测试 (含场景) | 893 通过 | ≥400 | ✅ 已超额 |
| 集成测试 | 193 通过 (+ 7 容器) | ≥120 | ✅ 已超额 |
| 编译警告 | 0 全部 (TreatWarningsAsErrors) | 0 全部 | ✅ 已达标 |
| 文档与代码同步率 | ~90% | ≥95% | ✅ 设计文档已同步v2.1 |

---

## Phase 2a: 关键修复验证 (Week 1) ✅ 完成

### 2a-1. MafTaskOrchestrator E2E 验证
- [x] 编写集成测试: MainAgent → TaskDecomposer → Orchestrator → Agent执行 完整链路
- [x] 验证 `ExecutePlanAsync(plan, taskExecutor)` callback 正确路由到目标 Agent
- [x] 验证并行执行组 `ExecuteGroupAsync` 正确调度多 Agent
- [x] 验证 `PersistentTaskOrchestrator` 持久化装饰器数据落地

### 2a-2. 修复已知编译错误
- [x] 修复 `IntentDrivenEntityExtractorExtendedTests.cs` 中的 `CS0234` 错误
  - 方案: 修正命名空间引用

### 2a-3. SmartHome P0 用例端到端验证
- [x] SH-MORNING-001: 晨起唤醒全链路 (意图→分解→调度→执行→UI)
- [x] SH-HOME-001: 回家放松场景
- [x] SH-EMERGENCY-001: 紧急情况处理

---

## Phase 2b: 用例覆盖度提升 (Week 2) ✅ 完成

### 目标: SmartHome 58% → 75% ✅ 实际 ~78%

| 用户旅程 | 当前 | 目标 | 关键差距 |
|---------|------|------|---------|
| SH-04 睡眠准备 | 50% | 75% | 添加夜灯+空调联动 |
| SH-05 会客模式 | 50% | 75% | 添加音乐+灯光场景联动 |
| SH-06 外出监控 | 50% | 75% | 补充安防 Agent 逻辑 |
| SH-08 个性化 | 40% | 60% | 用户偏好持久化 |

### 目标: CustomerService 42% → 65% ✅ 实际 ~72%

| 用户旅程 | 当前 | 目标 | 关键差距 |
|---------|------|------|---------|
| CS-03 退换货 | 40% | 70% | 完善退换货状态流转 |
| CS-04 投诉建议 | 25% | 60% | 投诉分类 + 升级策略 |
| CS-06 问题升级 | 25% | 60% | 多级升级路由逻辑 |
| CS-07 主动服务 | 20% | 50% | 主动通知触发条件 |

### 交付物
- [x] 各场景单元测试 (新增 ≥… 实际新增 ~200 个)
- [x] 各场景集成测试 (新增 ≥18 个)
- [x] 更新场景对话文档

---

## Phase 2c: 文档同步与质量门禁 (Week 3) ⚠️ 部分完成

### 2c-1. 文档与代码对齐
- [x] 更新 docs/design-docs/core-architecture.md 反映实际NLP管道和RAG管道
- [x] 更新 docs/design-docs/implementation-guide.md 纳入 PersistentXxx 装饰器模式
- [x] 补充 docs/guides/how-to-use-task-orchestrator.md (含 callback 模式)
- [x] 更新 CLAUDE.md 为 ≤100 行导航文件
- [x] docs/specs/ 标记为遗留并添加重定向至 design-docs/

### 2c-2. 质量门禁建立
- [x] 创建 QUALITY_SCORE.md 持续跟踪模块质量
- [ ] Services 层覆盖率 ≥ 90%
- [ ] Core 层覆盖率 ≥ 95%
- [ ] Infrastructure 层覆盖率 ≥ 80%
- [x] 零编译警告策略 (`TreatWarningsAsErrors` via `Directory.Build.props`)

### 2c-3. CI/CD 完善 ✅ 完成
- [x] GitHub Actions 配置 (`.github/workflows/ci.yml`)
- [x] 自动化测试 + 覆盖率报告 (Cobertura + reportgenerator)
- [x] 覆盖率门禁 (CI 中 ≥40% 硬性要求，排除 Demo 和外部依赖组件)
- [x] 发布流水线 (Docker build on main push)

### 2c-4. 内置 Agent 体系 ✅ 完成
- [x] 9 种内置专用 Agent（含 MafLeaderAgent、RagKnowledgeAgent 等）
- [x] `AddMafBuiltinAgents()` DI 扩展一键注册
- [x] RagKnowledgeAgent 单元测试 (8 个全部通过)
- [x] SmartHomeLeaderAgent 继承重构 → 继承 MafLeaderAgent（8 虚钩子）
- [x] CustomerServiceLeaderAgent 保留直接路由模式（设计文档已标注原因）

---

## 验收标准

Phase 2 整体完成需满足以下全部条件:

1. **E2E 链路**: MainAgent → Orchestrator → Agent 全链路在集成测试中通过 ✅ (7 个 Orchestrator callback 集成测试)
2. **零编译错误**: `dotnet build CKY.MAF.slnx` 零错误零警告 ✅ (TreatWarningsAsErrors 已启用)
3. **用例覆盖**: 总体覆盖度 ≥ 75% (SmartHome ≥ 75%, CustomerService ≥ 65%) ✅ (SH ~78%, CS ~72%)
4. **测试数量**: 单元测试 ≥ 400, 集成测试 ≥ 120 ✅ (单元 893, 集成 193)
5. **文档同步**: 所有 design-docs 文档与代码一致 ✅ (v2.1 已同步)
6. **质量跟踪**: QUALITY_SCORE.md 建立并持续更新 ✅ (评分 9.2/10)

---

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| MS AF Preview API 变更 | Agent 基类可能不兼容 | 适配器模式隔离 |
| LLM API 不稳定 | E2E 测试不可靠 | Mock + 录制回放 |
| 用例覆盖范围过大 | 开发周期延长 | 优先 P0/P1，P2 可延后 |
| 测试环境依赖 | Redis/Qdrant 不可用 | Testcontainers 容器化 |

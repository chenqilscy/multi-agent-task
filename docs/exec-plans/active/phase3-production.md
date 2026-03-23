# Phase 3: LLM 集成 + 生产运维

> **创建日期**: 2026-03-22  
> **前置阶段**: Phase 2 (质量巩固，已完成)  
> **目标**: 将框架从 Mock/测试状态推进至 LLM 真实集成 + 生产级运维能力  

---

## 当前基线

| 指标 | Phase 2 结果 | Phase 3 目标 |
|------|-------------|-------------|
| 单元测试 | 901 通过 | ≥950 |
| 集成测试 | 193 通过 (+7 容器) | ≥220 |
| 编译警告 | 0 (TreatWarningsAsErrors) | 0 |
| 代码覆盖率 | ~30% 整体 / ~52% Services | ≥50% 核心模块 |
| 内置 Agent | 9 种 (含 RagKnowledgeAgent) | 全部 LLM 真实调用验证 |
| Demo LeaderAgent | SmartHome 继承重构完成 | 两 Demo 场景端到端真实 LLM |
| CI/CD | GitHub Actions + 覆盖率门禁 | + 集成测试用 Docker Compose |

---

## Phase 3a: LLM Provider 真实集成 (Week 1-2)

### 3a-1. LLM Provider 配置标准化
- [ ] 设计统一 `LlmProviderConfig` 管理方案（环境变量 / Key Vault / 本地 secrets）
- [ ] 实现 `ILlmConfigurationProvider` 多源配置加载
- [ ] 支持热更新：运行时切换 Provider / Model 不重启
- [ ] 敏感信息加密存储（API Key 不落日志/不入 Git）

### 3a-2. OpenAI / Azure OpenAI 适配器
- [ ] 实现 `OpenAiLlmProvider : ILlmProvider`（Chat Completions API）
- [ ] 实现 `AzureOpenAiLlmProvider : ILlmProvider`（Azure 端点 + Managed Identity）
- [ ] Streaming 支持（SSE → `IAsyncEnumerable<string>`）
- [ ] Token 用量追踪（Prompt Tokens / Completion Tokens / Cost 估算）
- [ ] 集成测试：真实 API 调用桩（录制回放模式，避免 CI 消耗 Token）

### 3a-3. LLM 调用链路加固
- [ ] 重试策略：指数退避 + Jitter（429 / 500 / 503）
- [ ] 熔断器：`LlmCircuitBreaker` 已有框架，接入真实 Provider
- [ ] 超时控制：单次调用 ≤30s，Streaming 首 Token ≤5s
- [ ] Fallback：Primary → Secondary Provider 自动切换
- [ ] 限流：Token Bucket（RPM / TPM 限制适配各 Provider 配额）

### 交付物
- [ ] 至少 1 个 LLM Provider 端到端可用
- [ ] 录制回放测试 ≥10 条（覆盖 Chat / Embedding / Error 场景）
- [ ] `docs/guides/llm-provider-configuration-guide.md` 更新为可执行步骤

---

## Phase 3b: RAG 管道真实集成 (Week 2-3)

### 3b-1. Embedding 服务
- [ ] 接入 OpenAI Embedding API（text-embedding-3-small / large）
- [ ] 或 Azure OpenAI Embedding
- [ ] 批量 Embedding 接口（≤2048 条/批，并行执行）
- [ ] Embedding 缓存（Redis 存储，避免重复计算）

### 3b-2. 向量数据库
- [ ] Qdrant 集成验证（已有 `VectorizationService`，需 E2E 验证）
- [ ] Collection 管理：自动创建 / 索引优化 / 分片配置
- [ ] 混合检索：Dense + Sparse（BM25）+ Reranking
- [ ] Docker Compose 增加 Qdrant 服务

### 3b-3. RagKnowledgeAgent E2E
- [ ] 真实链路：文档 → 分块 → Embedding → Qdrant → 检索 → LLM 总结
- [ ] 知识库管理 API：上传 / 删除 / 更新文档
- [ ] 检索评测：Recall@10、MRR、Answer Relevance

### 交付物
- [ ] RAG 端到端 Demo 可运行
- [ ] Docker Compose 一键启动（API + Qdrant + Redis）
- [ ] 检索质量基线报告

---

## Phase 3c: 可观测性 & 生产运维 (Week 3-4)

### 3c-1. OpenTelemetry 完善
- [ ] `MafActivitySource` 已有埋点，验证 Trace 完整链路
- [ ] 补充 Metrics：请求量 / 延迟 P50/P95/P99 / 错误率 / LLM Token 用量
- [ ] 补充 Logs：结构化日志（Serilog → Seq / ELK）
- [ ] Activity 上下文传播：跨 Agent 调用链路可追踪

### 3c-2. Prometheus + Grafana
- [ ] 验证 `observability/` 目录已有 Dashboard 配置可用
- [ ] 新增 Dashboard：LLM 调用延迟 / Token 消耗 / 熔断状态
- [ ] 告警规则：错误率 >5%、P99 >10s、熔断器打开

### 3c-3. 健康检查 & 就绪探针
- [ ] `/health` 端点：Redis / Qdrant / LLM Provider 连通性
- [ ] `/ready` 端点：服务就绪（Agent 注册完成、模型加载完成）
- [ ] Kubernetes Liveness / Readiness Probe 配置

### 3c-4. 部署标准化
- [ ] Dockerfile 多阶段构建优化（base → build → publish → runtime）
- [ ] Helm Chart 验证（`deploy/helm/cky-maf/` 已有框架）
- [ ] 环境配置分离：dev / staging / production
- [ ] Secret 管理：Kubernetes Secrets / Azure Key Vault

### 交付物
- [ ] Grafana Dashboard 截图 + 告警配置
- [ ] Helm values-{env}.yaml 示例
- [ ] 生产部署 Runbook

---

## Phase 3d: 性能优化 & 压测 (Week 4)

### 3d-1. 性能基线建立
- [ ] 编排流水线延迟：意图识别 → 聚合全链路 P95 基线
- [ ] 并发能力：单实例 QPS 基线（Mock LLM / 真实 LLM）
- [ ] 内存分析：Agent 实例池、对话上下文内存占用

### 3d-2. 优化
- [ ] Agent 实例池化（避免每次请求创建新实例）
- [ ] 对话上下文 LRU 缓存（内存 + Redis 两级）
- [ ] LLM 响应缓存（相同 Prompt hash → 缓存结果）
- [ ] Batch 推理（短时间窗口内多请求合并调用 LLM）

### 3d-3. 压测
- [ ] K6 / NBomber 压测脚本（SmartHome / CustomerService 场景）
- [ ] 压测报告：QPS / P95 / 错误率 / 资源占用

### 交付物
- [ ] 性能基线报告（before/after）
- [ ] 压测脚本 + 执行报告

---

## 验收标准

Phase 3 整体完成需满足以下全部条件:

1. **LLM 集成**: 至少 1 个 Provider E2E 可用（录制回放测试通过）
2. **RAG E2E**: 文档 → Embedding → 检索 → 回答 全链路可运行
3. **可观测性**: Prometheus Metrics + Grafana Dashboard 可用
4. **部署**: Docker Compose 一键启动，Helm Chart 可部署
5. **性能**: 编排全链路 P95 ≤2s（Mock LLM），有压测基线
6. **测试**: 单元 ≥950、集成 ≥220、覆盖率核心模块 ≥50%

---

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| LLM API 成本 | CI 中每次运行消耗 Token | 录制回放模式，仅 nightly 真实调用 |
| Qdrant 版本兼容性 | 向量检索 API 变化 | 锁定版本 + 适配器隔离 |
| MS AF Preview 变更 | Agent 基类/协议变化 | MafBusinessAgentBase 适配层 |
| Provider 限流 | 高并发时 429 | Token Bucket + 多 Provider Fallback |
| Kubernetes 环境差异 | Helm 模板在不同集群不兼容 | CI 中 `helm template` 验证 |

---

## 依赖关系

```
Phase 3a (LLM Provider)
    ├── 3b 依赖 3a (Embedding 需要 LLM Provider)
    └── 3c 可与 3a 并行 (可观测性独立)
Phase 3b (RAG)
    └── 3d 依赖 3b (压测需要完整链路)
```

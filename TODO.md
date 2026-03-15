# CKY.MAF 实施补齐 TODO

> **生成日期**: 2026-03-13
> **最后更新**: 2026-03-15
> **当前完成度**: 97%
> **目标完成度**: 98%

---

## 🔥 P0 - 阻塞性任务（必须完成，Week 1）

### ✅ Task 1: 实现 Redis 缓存层
**工作量**: 1.5天 | **依赖**: 无

#### 子任务
- [ ] 创建项目 `src/Infrastructure/Caching/CKY.MAF.Infrastructure.Caching.csproj`
- [ ] 实现 `RedisCacheStore : ICacheStore` 类
  - [ ] `GetAsync<T>()` 方法
  - [ ] `SetAsync<T>()` 方法
  - [ ] `DeleteAsync()` 方法
  - [ ] `GetBatchAsync<T>()` 方法
  - [ ] `ExistsAsync()` 方法
- [ ] 实现 `MemoryCacheStore : ICacheStore` 测试用类
- [ ] 添加序列化/反序列化异常处理
- [ ] 添加日志记录
- [ ] 配置依赖注入 `Program.cs`
- [ ] 编写集成测试 `tests/IntegrationTests/Caching/RedisCacheStoreTests.cs`
- [ ] 性能测试：Get操作 < 10ms

#### 验收
- [ ] 所有接口方法实现完成
- [ ] 集成测试通过（使用 Testcontainers）
- [ ] 性能测试达标

---

### ✅ Task 2: 实现完整任务调度器
**工作量**: 2天 | **依赖**: Task 1

#### 子任务
- [ ] 创建 `MafTaskScheduler : ITaskScheduler` 主实现类
  - [ ] `ScheduleAsync()` 方法（任务编排）
  - [ ] `ExecuteTaskAsync()` 方法（任务执行）
- [ ] 实现 `PriorityCalculator : IPriorityCalculator` 类
  - [ ] 时间因子评分（40%）
  - [ ] 资源使用评分（30%）
  - [ ] 用户交互评分（20%）
  - [ ] 任务复杂度评分（10%）
- [ ] 实现 `TaskDependencyGraph` 依赖图类
  - [ ] `Validate()` 方法（循环检测）
  - [ ] `GenerateExecutionGroups()` 方法（拓扑排序）
- [ ] 创建 `ScheduleResult` 和 `ExecutionPlan` 模型类
- [ ] 添加任务状态管理（待执行、执行中、已完成、失败）
- [ ] 编写单元测试 `tests/UnitTests/Scheduling/MafTaskSchedulerTests.cs`
  - [ ] 优先级计算测试
  - [ ] 依赖图验证测试
  - [ ] 执行分组测试

#### 验收
- [ ] 依赖图构建和验证正确
- [ ] 支持并行/串行任务调度
- [ ] 单元测试覆盖率 > 90%

---

### ✅ Task 3: 统一 Agent 继承层次
**工作量**: 1.5天 | **依赖**: 无

#### 子任务
- [ ] 标记 `MafAgentBase` 为 `Obsolete`（保留用于向后兼容）
- [ ] 更新 Demo Agent 迁移到 `LlmAgent : AIAgent`
  - [ ] `LightingAgent` 迁移
  - [ ] `ClimateAgent` 迁移
  - [ ] `MusicAgent` 迁移
- [ ] 创建 `SmartHomeAgentFactory` 工厂类
- [ ] 更新依赖注入配置
- [ ] 更新单元测试适配新的继承层次
- [ ] 清理编译警告

#### 验收
- [ ] 所有 Agent 继承自 `LlmAgent : AIAgent`
- [ ] 编译无警告
- [ ] 所有单元测试通过

---

## 🚀 P1 - 重要任务（影响演示，Week 2）

### ✅ Task 4: 实现 Qdrant 向量存储
**工作量**: 2天 | **依赖**: 无

#### 子任务
- [ ] 创建项目 `src/Infrastructure/Vectorization/CKY.MAF.Infrastructure.Vectorization.csproj`
- [ ] 实现 `QdrantVectorStore : IVectorStore` 类
  - [ ] `CreateCollectionAsync()` 方法
  - [ ] `InsertAsync()` 方法
  - [ ] `SearchAsync()` 方法
  - [ ] `DeleteAsync()` 方法
  - [ ] `DeleteCollectionAsync()` 方法
- [ ] 实现 Payload（元数据）序列化/反序列化扩展
- [ ] 实现 `MemoryVectorStore : IVectorStore` 测试用类
- [ ] 配置依赖注入
- [ ] 编写集成测试 `tests/IntegrationTests/Vectorization/QdrantVectorStoreTests.cs`
- [ ] 向量检索精度测试

#### 验收
- [ ] 所有接口方法实现完成
- [ ] 集成测试通过（使用 Testcontainers Qdrant）
- [ ] 检索精度测试通过

---

### ✅ Task 5: 实现 A2A 通信机制
**工作量**: 1.5天 | **依赖**: Task 3

#### 子任务
- [ ] 定义 A2A 消息格式 `src/Core/Models/Agent/AgentMessage.cs`
- [ ] 创建 `IA2ACommunicationService` 接口
- [ ] 实现 `A2ACommunicationService` 服务类
  - [ ] `SendAsync()` 点对点通信
  - [ ] `BroadcastAsync()` 广播通信
- [ ] 扩展 `LlmAgent` 支持消息接收
  - [ ] `ReceiveMessageAsync()` 方法
  - [ ] `HandleRequestAsync()` 方法
  - [ ] `HandleNotificationAsync()` 方法
- [ ] 更新 `SmartHomeMainAgent` 实现 Agent 协调
- [ ] 编写单元测试 `tests/UnitTests/Communication/A2ACommunicationServiceTests.cs`
- [ ] 编写集成测试验证多 Agent 协作

#### 验收
- [ ] 支持点对点和广播通信
- [ ] Demo 中演示 Agent 协作场景
- [ ] 单元测试覆盖消息传递流程

---

### ✅ Task 6: 完成 Blazor UI 基础组件
**工作量**: 2天 | **依赖**: Task 5

#### 子任务
- [ ] 创建主页面 `src/Demos/SmartHome/Pages/Index.razor`
- [ ] 创建聊天界面 `src/Demos/SmartHome/Pages/Chat.razor`
- [ ] 创建设备控制页面 `src/Demos/SmartHome/Pages/DeviceControl.razor`
- [ ] 创建 Agent 状态页面 `src/Demos/SmartHome/Pages/AgentStatus.razor`
- [ ] 创建共享组件
  - [ ] `MainLayout.razor` 主布局
  - [ ] `ChatMessage.razor` 聊天消息组件
  - [ ] `DeviceCard.razor` 设备卡片组件
- [ ] 实现 `SmartHomeUIService` UI服务
- [ ] 实现 `ChatService` 聊天服务
- [ ] 添加响应式样式（支持移动端）

#### 验收
- [ ] 主页面布局完整
- [ ] 聊天界面可正常交互
- [ ] 设备控制功能可用
- [ ] UI 响应式设计

---

### ✅ Task 7: 补充集成测试
**工作量**: 1天 | **依赖**: Task 1-6

#### 子任务
- [ ] Redis 缓存集成测试 `tests/IntegrationTests/Caching/RedisCacheStoreTests.cs`
- [ ] Qdrant 向量存储集成测试 `tests/IntegrationTests/Vectorization/QdrantVectorStoreTests.cs`
- [ ] A2A 通信集成测试 `tests/IntegrationTests/Communication/A2ACommunicationTests.cs`
- [ ] 任务调度器集成测试 `tests/IntegrationTests/Scheduling/MafTaskSchedulerIntegrationTests.cs`
- [ ] 端到端场景测试 `tests/IntegrationTests/EndToEnd/SmartHomeScenarioTests.cs`
  - [ ] "晨间例程"场景测试
  - [ ] 多 Agent 协作测试
- [ ] 配置 CI/CD 自动化测试

#### 验收
- [ ] 所有 Infrastructure 组件有集成测试
- [ ] 端到端场景测试通过
- [ ] 测试覆盖率 > 60%（集成测试）

---

## 🔧 P2 - 优化任务（长期，Week 3）

### ✅ Task 8: 实现 SignalR 实时通信
**工作量**: 1.5天 | **依赖**: Task 6

#### 子任务
- [ ] 创建 `CKY.MAFHub` SignalR Hub
  - [ ] `JoinTaskGroup()` 方法
  - [ ] `LeaveTaskGroup()` 方法
  - [ ] `PushTaskUpdate()` 方法
- [ ] 在 Chat.razor 中添加 SignalR 连接
- [ ] 实现实时任务状态更新
- [ ] 实现实时 Agent 响应推送

#### 验收
- [ ] SignalR 连接正常
- [ ] 实时推送功能可用

---

### ✅ Task 9: 完善 Prometheus 监控
**工作量**: 1天 | **依赖**: 无

#### 子任务
- [ ] 实现 `PrometheusMetricsCollector : IMetricsCollector`
  - [ ] 请求计数器 (`maf_requests_total`)
  - [ ] 响应时间直方图 (`maf_response_time_seconds`)
  - [ ] 活跃任务数 Gauge (`maf_active_tasks`)
- [ ] 在关键路径添加指标收集
- [ ] 配置 Prometheus 端点 `/metrics`
- [ ] 创建 Grafana 仪表板（可选）

#### 验收
- [ ] Prometheus 指标正常上报
- [ ] 关键指标可查询

---

### ✅ Task 10: 更新架构文档
**工作量**: 0.5天 | **依赖**: 所有任务

#### 子任务
- [ ] 更新 `docs/specs/12-layered-architecture.md`
  - [ ] 添加 Repository 层说明
  - [ ] 更新依赖关系图
- [ ] 更新 `docs/specs/11-implementation-roadmap.md`
  - [ ] 标记已完成任务
  - [ ] 更新进度百分比
- [ ] 创建 `docs/ARCHITECTURE_CHANGES.md`
  - [ ] 记录 Repository 层引入
  - [ ] 记录 Agent 继承统一
  - [ ] 记录其他架构变更

#### 验收
- [ ] 文档与代码同步
- [ ] 架构图准确反映当前实现

---

## 📋 执行清单

### 每日检查（Daily Checklist）
- [ ] 检查单元测试是否全部通过
- [ ] 检查代码覆盖率是否达标
- [ ] 提交代码前进行 Code Review
- [ ] 更新 TODO.md（勾选已完成项）
- [ ] 推送代码到远程仓库

### Week 1 检查点（Day 5）
- [ ] Task 1-3 全部完成
- [ ] P0 阻塞性任务清零
- [ ] 基础设施层（Redis）可用
- [ ] 任务调度器可用
- [ ] Agent 继承层次统一

### Week 2 检查点（Day 10）
- [ ] Task 4-7 全部完成
- [ ] P1 重要任务清零
- [ ] 向量存储可用
- [ ] A2A 通信可用
- [ ] Blazor UI 可演示
- [ ] 集成测试覆盖率 > 60%

### Week 3 检查点（Day 14）
- [ ] Task 8-10 全部完成
- [ ] P2 优化任务清零
- [ ] SignalR 实时推送可用
- [ ] Prometheus 监控正常
- [ ] 文档与代码同步
- [ ] 项目整体完成度 > 95%

---

## 🚨 阻塞和风险

### 当前阻塞
- 无

### 识别的风险
- **MS AF API 变化**: 使用适配器模式缓解
- **Qdrant 性能问题**: 预留优化时间
- **UI 开发延期**: 简化 UI 需求
- **测试覆盖不足**: 强制覆盖率要求

### 应急预案
- 如果 Task 2（任务调度器）延期，先实现简化版
- 如果 Task 4（Qdrant）延期，暂时使用 `MemoryVectorStore`
- 如果 Task 6（Blazor UI）延期，先提供命令行 Demo

---

## 📊 进度跟踪

| 任务 | 负责人 | 状态 | 完成度 | 预计完成 |
|------|--------|------|--------|----------|
| Task 1: Redis缓存 | - | ✅ 完成 | 100% | Day 2 |
| Task 2: 任务调度器 | - | ✅ 完成 | 100% | Day 4 |
| Task 3: Agent继承 | - | ✅ 完成 | 100% | Day 5 |
| Task 4: Qdrant向量存储 | - | ✅ 完成 | 100% | Day 7 |
| Task 5: A2A通信 | - | ✅ 完成 | 100% | Day 8 |
| Task 6: Blazor UI | - | ✅ 完成 | 100% | Day 10 |
| Task 7: 集成测试 | - | ✅ 完成 | 100% | Day 10 |
| Task 8: SignalR | - | ✅ 完成 | 100% | Day 12 |
| Task 9: Prometheus | - | ✅ 完成 | 100% | Day 13 |
| Task 10: 文档更新 | - | ✅ 完成 | 100% | Day 14 |

### 新增任务（2026-03-15）

| 任务 | 状态 | 说明 |
|------|------|------|
| 5 大 LLM 提供商 Agent 实现 | ✅ 完成 | 智谱AI、通义千问、文心一言、讯飞星火、MiniMax |
| EF Core Repository 模式 | ✅ 完成 | MainTask/SubTask CRUD + UnitOfWork |
| SecurityAgent 路由集成 | ✅ 完成 | 集成到 SmartHomeControlService |
| 分布式追踪 (ActivitySource) | ✅ 完成 | Agent/Task/LLM 三层追踪，OTLP 导出 |
| Grafana+Jaeger Docker Compose | ✅ 完成 | docker-compose.observability.yml |
| SmartHome 对话示例 (P0+P1+P2) | ✅ 完成 | 18 份对话文档 |
| CustomerService 对话示例 (P0+P1+P2) | ✅ 完成 | 25 份对话文档 |
| 单元测试 | ✅ 348 通过 | 覆盖 Core/Services/Infrastructure/Resilience |
| 集成测试 | ✅ 38 通过 | MemoryVectorStore/EfCore/MemoryCache/DI 注册 |
| Docker 部署配置 | ✅ 完成 | 多阶段 Dockerfile + docker-compose.yml（含应用+基础设施） |
| Grafana 预配置仪表盘 | ✅ 完成 | Agent/Task/LLM 三层仪表盘 JSON + 自动供给 |
| Demo Agent 追踪埋点 | ✅ 完成 | SmartHomeControlService + CustomerServiceMainAgent Activity |
| LLM 降级策略 (Level 5) | ✅ 完成 | DegradationManager + IRuleEngine + SmartHome/CS RuleEngine |

---

## 🔗 相关文档

- [详细实施计划](./IMPLEMENTATION_ROADMAP_DETAILED.md)
- [架构设计规范](./docs/specs/12-layered-architecture.md)
- [接口设计规范](./docs/specs/06-interface-design-spec.md)
- [测试指南](./docs/specs/10-testing-guide.md)

---

**最后更新**: 2026-03-15
**下次审查**: 每日站会时更新

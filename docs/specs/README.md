# CKY.MAF 框架设计文档

> **⚠️ 遗留文档**: 此目录为旧版规范文档 (v1.3)，**已不再维护**。  
> 权威设计文档已迁移至 [`docs/design-docs/`](../design-docs/index.md) (v2.1)。  
> 如需查阅最新架构，请参阅 [核心架构](../design-docs/core-architecture.md) 和 [实现指南](../design-docs/implementation-guide.md)。

> **最后更新**: 2026-03-20
> **框架版本**: v1.3 (**已过时 — 当前 v2.1**)
> **状态**: 🔒 已归档
> **核心依赖**: Microsoft Agent Framework (Preview)

---

## 📌 核心概念

### CKY.MAF定位

**CKY.MAF = 基于Microsoft Agent Framework的企业级增强层**

```
┌─────────────────────────────────────┐
│  应用层              │  智能家居、设备控制  │
├─────────────────────────────────────┤
│  CKY.MAF增强层      │  调度、存储、监控  │
├─────────────────────────────────────┤
│  MS Agent Framework  │  AIAgent、A2A     │
└─────────────────────────────────────┘
```

**关键点**:
- ✅ 所有Agent继承自MS AF的`AIAgent`
- ✅ Agent间通信使用MS AF的A2A机制
- ✅ CKY.MAF提供MS AF缺失的企业级特性

---

## 🔧 技术栈

**核心框架**:
- Microsoft Agent Framework (Preview) - **硬性依赖**
- .NET 10
- 原生ASP.NET Core（无ABP依赖）

**CKY.MAF增强**:
- 任务调度：优先级系统、依赖管理、弹性调度
- 三层存储：L1内存、L2 Redis、L3 PostgreSQL
- 监控告警：Prometheus、分布式追踪

**LLM提供商**:
- 首选：智谱AI (GLM-4/GLM-4-Plus)
- 备选：通义千问、文心一言、讯飞星火

---

## 📚 文档结构

```
docs/specs/
├── 📖 README.md                                # 本文档
│
├── 📐 核心文档（2个）⭐ 必读
│   ├── 00-CORE-ARCHITECTURE.md                 # 核心架构（整合版）
│   └── 01-IMPLEMENTATION-GUIDE.md               # 实现指南（整合版）
│
├── 📊 专题文档（8个）
│   ├── 02-architecture-diagrams.md             # 架构图表集
│   ├── 03-task-scheduling-design.md            # 任务调度系统
│   ├── 07-ui-design-spec.md                    # UI设计规范
│   ├── 08-deployment-guide.md                  # 部署指南
│   ├── 10-testing-guide.md                     # 测试指南
│   ├── 13-performance-benchmarks.md           # 性能基准测试
│   ├── 14-error-handling-guide.md              # 错误处理指南
│   └── MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md # MS AF集成
```

**总计**: 10个文档（精简后）

---

## 🎯 快速导航（按角色）

### 🏗️ 架构师/技术负责人
1. [00-核心架构文档](./00-CORE-ARCHITECTURE.md) ⭐ **必读**
2. [02-架构图表集](./02-architecture-diagrams.md)
3. [03-任务调度系统](./03-task-scheduling-design.md)
4. [13-性能基准测试](./13-performance-benchmarks.md)
5. [14-错误处理指南](./14-error-handling-guide.md) ⭐ **重点**

### 💻 开发人员
1. [00-核心架构文档](./00-CORE-ARCHITECTURE.md) ⭐ **必读**
2. [01-实现指南](./01-IMPLEMENTATION-GUIDE.md) ⭐ **重点**
3. [02-架构图表集](./02-architecture-diagrams.md)
4. [10-测试指南](./10-testing-guide.md)
5. [14-错误处理指南](./14-error-handling-guide.md)

### 🎨 前端开发人员
1. [00-核心架构文档](./00-CORE-ARCHITECTURE.md)
2. [07-UI设计规范](./07-ui-design-spec.md) ⭐ **重点**
3. [02-架构图表集](./02-architecture-diagrams.md)

### 🔧 运维人员
1. [00-核心架构文档](./00-CORE-ARCHITECTURE.md)
2. [08-部署指南](./08-deployment-guide.md) ⭐ **重点**
3. [13-性能基准测试](./13-performance-benchmarks.md) ⭐ **重点**
4. [14-错误处理指南](./14-error-handling-guide.md) ⭐ **重点**
5. [02-架构图表集](./02-architecture-diagrams.md)

---

## 📖 核心文档详细说明

### 1. 核心架构文档 ⭐ 必读
**文件**: `00-CORE-ARCHITECTURE.md` | **大小**: 24KB

**内容**：
- 整合后的核心架构设计
- 基于Microsoft Agent Framework的定位
- 5层分层依赖架构详解
- LLM服务架构设计
- Main-Agent + Sub-Agent模式
- 弹性与容错策略
- 监控与可观测性

**适合人群**: 所有人 ⭐ **优先阅读**

---

### 2. 实现指南 ⭐ 必读
**文件**: `01-IMPLEMENTATION-GUIDE.md` | **大小**: 38KB

**内容**：
- 项目结构规范
- 核心接口定义（已验证与代码一致）
- Agent实现模式
- 依赖注入配置
- 错误处理最佳实践
- 测试策略

**适合人群**: 开发人员 ⭐ **优先阅读**

---

### 3. 架构图表集
**文件**: `02-architecture-diagrams.md` | **大小**: 21KB

**内容**：
- 系统架构图（Mermaid）
- 接口类图
- 状态机图
- 序列图
- 部署架构图

**适合人群**: 所有人

---

### 4. 任务调度系统
**文件**: `03-task-scheduling-design.md` | **大小**: 59KB

**内容**：
- 任务优先级系统
- 依赖关系建模
- 任务调度算法
- 执行策略
- 异常处理

**适合人群**: 架构师、核心开发人员

---

### 5. UI设计规范
**文件**: `07-ui-design-spec.md` | **大小**: 20KB

**内容**：
- 前端架构设计
- Blazor Server实现
- 实时通信设计（SignalR）
- 对话式UI设计
- 设备控制UI

**适合人群**: 前端开发人员、UI/UX设计师

---

### 6. 部署指南
**文件**: `08-deployment-guide.md` | **大小**: 20KB

**内容**：
- 环境要求
- Docker部署
- Kubernetes部署
- 配置管理
- 监控和日志
- 安全加固

**适合人群**: 运维人员、DevOps工程师

---

### 7. 测试指南
**文件**: `10-testing-guide.md` | **大小**: 31KB

**内容**：
- 测试金字塔策略（70/25/5）
- 单元测试场景
- 集成测试策略
- 测试工具链

**适合人群**: 开发人员、测试工程师

---

### 8. 性能基准测试
**文件**: `13-performance-benchmarks.md` | **大小**: 17KB

**内容**：
- 响应时间指标（P50/P95/P99）
- 吞吐量指标
- 资源占用限制
- 性能优化策略

**适合人群**: 架构师、运维人员、性能工程师

---

### 9. 错误处理指南 ⭐ 重点
**文件**: `14-error-handling-guide.md` | **大小**: 36KB

**内容**：
- 错误分类体系
- 重试策略（指数退避+抖动）
- 熔断器模式
- 服务降级策略（5级）
- 日志与监控

**适合人群**: 架构师、后端开发人员、运维人员 ⭐ **重点**

---

### 10. MS Agent Framework集成
**文件**: `MICROSOFT_AGENT_FRAMEWORK_INTEGRATION.md` | **大小**: 187行

**内容**：
- MS AF集成说明
- AIAgent基类使用
- A2A通信机制
- 集成最佳实践

**适合人群**: 架构师、开发人员

---

## 🔄 文档更新计划

### 已完成 ✅
- [x] 01-架构设计概览
- [x] 02-架构图表集
- [x] 03-任务调度系统
- [x] 04-LangGraph对比分析
- [x] 05-业界框架对比
- [x] 06-接口设计规范（含存储抽象接口）
- [x] 07-UI设计规范
- [x] 08-部署指南
- [x] 09-实现指南
- [x] 10-测试指南
- [x] 11-实施路线图
- [x] 12-分层依赖架构 ⭐ NEW
- [x] 13-性能基准测试
- [x] 14-错误处理指南 ⭐ NEW

### 进行中 🔄
- [ ] API文档

---

## 📝 文档更新日志

### 2026-03-13 重大更新（错误处理指南新增）

**文档新增**:
- ✅ [14-错误处理指南](./14-error-handling-guide.md) - 36KB ⭐ NEW
  - 错误分类体系（异常类型层次、统一错误码）
  - 重试策略（指数退避+抖动，各服务推荐配置）
  - 熔断器模式（状态机、各组件参数、完整实现）
  - 服务降级策略（5个级别，渐进式降级）
  - Prometheus指标和告警规则
  - 完整单元测试和集成测试示例

- ✅ [13-性能基准测试](./13-performance-benchmarks.md) - 17KB
  - 响应时间、吞吐量、资源占用指标

**文档更新**:
- ✅ [README.md](./README.md) - 补全文档目录，更新导航

### 2026-03-13 重大更新（依赖倒置重构）

**架构重构 - 遵循依赖倒置原则（DIP）**:
- ✅ 将具体实现从框架核心完全剥离
- ✅ Core 层只定义抽象接口，零外部依赖（除 MS AF）
- ✅ Infrastructure 层包含所有具体实现（Redis、PostgreSQL、Qdrant）
- ✅ 新增三个核心存储抽象接口：
  - `ICacheStore` - 缓存存储接口
  - `IVectorStore` - 向量存储接口
  - `IRelationalDatabase` - 关系数据库接口

**文档新增**:
- ✅ 12-分层依赖架构（已整合至 [00-CORE-ARCHITECTURE.md](./00-CORE-ARCHITECTURE.md)）
  - 5层架构设计详解
  - 依赖规则和设计原则
  - 完整代码示例
  - 测试策略和扩展性指南

**文档更新**:
- ✅ 06-接口设计规范（已整合至 [01-IMPLEMENTATION-GUIDE.md](./01-IMPLEMENTATION-GUIDE.md)）- 新增6.1存储抽象接口章节
- ✅ 11-实施路线图（已整合至 [01-IMPLEMENTATION-GUIDE.md](./01-IMPLEMENTATION-GUIDE.md)）- 重构依赖结构
  - 具体实现包全部移至 Infrastructure 层
  - 添加分层依赖架构图
  - 明确 Core 层零外部依赖原则

**架构优势**:
- ✅ 框架核心零外部依赖
- ✅ 所有存储实现可替换
- ✅ 单元测试无需外部服务
- ✅ 支持多种实现方案（Redis/NCache、PostgreSQL/MySQL、Qdrant/Pinecone）

### 2026-03-13 重大更新（目录结构优化）

**目录结构优化**:
- ✅ 所有文档移至单层目录，移除子目录
- ✅ 文档添加序号前缀（01-09）
- ✅ 删除临时脚本文件
- ✅ 整合 INDEX.md 和 README.md 为单一导航文档
- ✅ 更新所有文档路径引用

### 2026-03-12 重大更新

**架构重新定位**:
- ✅ 明确基于Microsoft Agent Framework构建
- ✅ 更新所有架构图，反映MS AF依赖
- ✅ 调整项目结构：Core框架 vs Demo应用

**重命名**:
- ✅ 所有文档：CKY.MAF框架 → CKY.MAF框架
- ✅ 添加类图中文注释（架构图表集）

**新增文档**:
- ✅ 业界框架对比分析（6大主流框架）
- ✅ LangGraph详细对比

---

## 📝 文档维护规范

### 命名规范
```
序号-文档主题.md

例如：
- 00-CORE-ARCHITECTURE.md
- 01-IMPLEMENTATION-GUIDE.md
- 03-task-scheduling-design.md
```

### 版本控制
- 主版本号：架构重大变更
- 次版本号：新增功能或章节
- 修订号：bug修复、内容补充

### 文档评审
- 设计文档需要架构师评审
- 实现指南需要Tech Lead评审
- 代码变更需要Code Review

---

## 🔗 相关资源

- [项目README](../../README.md)
- [Microsoft Agent Framework文档](https://learn.microsoft.com/ai-framework/)
- 
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)

---

**文档维护**: CKY.MAF架构团队
**最后更新**: 2026-03-13
**联系方式**: 见项目README

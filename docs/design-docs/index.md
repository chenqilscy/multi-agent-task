# 设计文档索引

> CKY.MAF 核心架构与设计决策的权威参考。
> **最后同步**: 2026-03-22（与源码对齐）

| 文档 | 说明 | 状态 |
|------|------|------|
| [core-architecture.md](core-architecture.md) | 5层DIP架构、LLM服务架构、核心设计模式 | v2.1 已同步 |
| [implementation-guide.md](implementation-guide.md) | 项目结构、接口定义、Agent实现模式、DI配置 | v2.1 已同步 |
| [architecture-diagrams.md](architecture-diagrams.md) | 架构流程图、数据流图、消息序列图 | 已完成 |
| [task-scheduling.md](task-scheduling.md) | 任务优先级系统、依赖关系管理、DAG拓扑排序 | 已完成 |
| [error-handling.md](error-handling.md) | 重试策略、熔断器、5级降级机制 | 已完成 |
| [ms-af-integration.md](ms-af-integration.md) | Microsoft Agent Framework 集成报告 | 已完成 |
| [builtin-agents-design.md](builtin-agents-design.md) | 内置通用 Agent 设计讨论（差距分析+实施路径） | 讨论稿 |

## 阅读顺序建议

1. **新手**：core-architecture → implementation-guide
2. **开发者**：core-architecture → task-scheduling → error-handling
3. **集成**：ms-af-integration → implementation-guide

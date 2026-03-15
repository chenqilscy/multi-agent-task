# TODO 清理分析报告

生成时间：2026-03-15

## 概述

本报告分析了 CKY.MAF 代码库中的所有 TODO 注释，并提供了清理建议。

**统计**：
- 总共发现 **65 个 TODO/FIXME/XXX** 标记
- 其中 **31 个**是实际需要处理的 TODO
- **20 个**是 LLM Agent 流式支持的占位符
- **9 个**是误报或已实现的方法
- **5 个**是示例文本中的占位符

---

## 分类详情

### 1. ✅ 可以立即删除的 TODO（已实现或误报）

#### 1.1 已实现的方法签名

| 文件 | 行号 | TODO 内容 | 状态 |
|------|------|-----------|------|
| `src/Core/Models/Persisted/LlmProviderConfigEntity.cs` | 70 | `public LlmProviderConfig ToDomainModel()` | ✅ **已完整实现**，删除注释 |
| `src/Infrastructure/Repository/Repositories/LlmProviderConfigRepository.cs` | 31, 45, 58, 82, 117, 125 | `.ToDomainModel()` 调用 | ✅ **正常工作**，不是 TODO |

**操作建议**：这些是误报，方法已经完整实现。Grep 匹配到了方法签名，但不是真正的 TODO。

#### 1.2 示例文本中的占位符

| 文件 | 行号 | TODO 内容 | 状态 |
|------|------|-----------|------|
| `src/Services/Orchestration/MafTaskDecomposer.cs` | 164-168 | `"XXX"` 示例文本 | ✅ **仅示例**，保持原样 |

**操作建议**：这些是提示词模板中的示例，不是 TODO 标记。

---

### 2. 🗑️ 可以删除的占位符文件

#### 2.1 集成测试占位符

| 文件 | 行号 | TODO 内容 | 建议 |
|------|------|-----------|------|
| `src/tests/IntegrationTests/IntegrationTestsPlaceholder.cs` | 1 | `// TODO: 集成测试骨架` | **删除整个文件**或实现集成测试 |

**理由**：这是一个占位符文件，内容已经在 `docs/specs/10-testing-guide.md` 中文档化。

#### 2.2 已注释的测试文件

| 文件 | 行号 | TODO 内容 | 建议 |
|------|------|-----------|------|
| `src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs` | 1 | `// TODO: Uncomment when Demos project is created` | **删除整个文件**或取消注释 |

**理由**：Demos/SmartHome 项目已经存在，测试应该启用或删除。

---

### 3. 📋 需要创建 GitHub Issue 的 TODO

#### 3.1 高优先级（核心功能）

| # | 文件 | 行号 | TODO 内容 | 建议操作 |
|---|------|------|-----------|----------|
| 1 | `src/Infrastructure/DependencyInjection/MafServiceRegistrationExtensions.cs` | 96 | Task 11 - Implement session store registration | **创建 Issue** |
| 2 | `src/Demos/SmartHome/Program.cs` | 78 | 实现 MafAgentStartupService | **创建 Issue** |
| 3 | `src/Infrastructure/Context/ContextCompressionProvider.cs` | 245 | 实现统计信息收集 | **创建 Issue** |

#### 3.2 中优先级（功能增强）

| # | 文件 | 行号 | TODO 内容 | 建议操作 |
|---|------|------|-----------|----------|
| 4 | `src/Infrastructure/Caching/Redis/RedisCacheStore.cs` | 259 | 集成 Prometheus | **创建 Issue** |
| 5 | `src/Demos/SmartHome/Services/SmartHomeControlService.cs` | 169 | 从各个Agent获取设备状态 | **创建 Issue** |
| 6 | `src/Services/Dialog/SlotManager.cs` | 360 | 使用 System.Text.Json 解析 | **创建 Issue** |
| 7 | `src/Services/Dialog/DialogStateManager.cs` | 269-270 | 解析用户响应，填充槽位 | **创建 Issue** |

#### 3.3 低优先级（会话存储集成）

| # | 文件 | 行号 | TODO 内容 | 建议操作 |
|---|------|------|-----------|----------|
| 8 | `src/Services/Dialog/ContextCompressor.cs` | 41 | 从 session store 获取消息 | **依赖 Issue #1** |
| 9 | `src/Services/Dialog/ContextCompressor.cs` | 81 | 存储压缩数据到 L2/L3 | **依赖 Issue #1** |
| 10 | `src/Services/Session/MafAiSessionManager.cs` | 447 | 扩展 L1CacheManager 支持按用户查询 | **依赖 Issue #1** |

---

### 4. 🤖 LLM Agent 流式支持 TODO（批量创建 Issue）

这些是类似的 TODO，可以批量处理：

| # | Provider | 文件 | 行号 | TODO 内容 | 状态 |
|---|----------|------|------|-----------|------|
| 11 | ZhipuAI | `src/Core/Agents/AiAgents/ZhipuAIAgent.cs` | 123 | Implement streaming support | **创建 Issue** |
| 12 | QwenAI | `src/Core/Agents/AiAgents/QwenAIAgent.cs` | 122 | Implement streaming support | **创建 Issue** |
| 13 | Fallback | `src/Core/Agents/AiAgents/FallbackLlmAgent.cs` | 188 | Implement streaming support | **创建 Issue** |
| 14 | Xunfei | `src/Core/Agents/AiAgents/XunfeiLlmAgent.cs` | 67, 78 | 实现 API 调用和流式输出 | **创建 Issue** |
| 15 | Wenxin | `src/Core/Agents/AiAgents/WenxinLlmAgent.cs` | 68, 79 | 实现 API 调用和流式输出 | **创建 Issue** |
| 16 | Tongyi | `src/Core/Agents/AiAgents/TongyiLlmAgent.cs` | 67, 78 | 实现 API 调用和流式输出 | **创建 Issue** |
| 17 | MiniMax | `src/Core/Agents/AiAgents/MiniMaxLlmAgent.cs` | 67, 78 | 实现 API 调用和流式输出 | **创建 Issue** |
| 18 | Baichuan | `src/Core/Agents/AiAgents/BaichuanLlmAgent.cs` | 66, 77 | 实现 API 调用和流式输出 | **创建 Issue** |

**建议**：这些是骨架实现类，用于说明如何集成不同的 LLM 提供商。可以：
- 保留作为示例
- 或删除未使用的提供商
- 或创建一个跟踪 Issue

---

### 5. 🔄 需要更新的 TODO

| # | 文件 | 行号 | TODO 内容 | 建议操作 |
|---|------|------|-----------|----------|
| 19 | `src/Services/Orchestration/LlmAgentRegistry.cs` | 158 | 实现从数据库加载配置 | **已部分实现**，更新注释 |
| 20 | `src/Services/Registry/LlmAgentRegistry.cs` | 134-135 | 从数据库重新加载配置 | **已部分实现**，更新注释 |
| 21 | `src/Services/Factory/LlmAgentFactory.cs` | 294 | 在工厂中添加 HttpClient 支持 | **验证是否需要** |

---

## 推荐清理步骤

### 第一阶段：立即清理（5分钟）

1. ✅ **删除误报**：
   - 移除 `LlmProviderConfigEntity.cs:70` 的 TODO 注释（如果存在）

2. 🗑️ **删除占位符文件**：
   ```bash
   # 删除集成测试占位符
   rm src/tests/IntegrationTests/IntegrationTestsPlaceholder.cs

   # 删除或启用 SmartHome 测试
   rm src/tests/UnitTests/NLP/SmartHomeEntityPatternProviderTests.cs
   ```

### 第二阶段：创建 GitHub Issues（15分钟）

为高优先级 TODO 创建 Issues：

- [ ] Issue #1: 实现 Session Store 注册和集成
- [ ] Issue #2: 实现 MafAgentStartupService
- [ ] Issue #3: 添加 Prometheus 监控指标
- [ ] Issue #4: 实现上下文压缩统计信息收集

### 第三阶段：代码清理（30分钟）

1. **更新已实现的功能**：
   - 更新 `LlmAgentRegistry.cs` 中的 TODO 注释
   - 移除或更新 `LlmAgentFactory.cs` 中的 HttpClient TODO

2. **将 TODO 转换为 Issue 引用**：
   ```csharp
   // TODO: 实现从数据库加载配置
   // tracked in: https://github.com/your-org/CKY.MAF/issues/1
   ```

### 第四阶段：LLM Agent 清理（可选）

**选项 A**：保留作为示例
- 在文件头部添加注释说明这是骨架实现

**选项 B**：删除未使用的提供商
- 删除 XunfeiLlmAgent, WenxinLlmAgent, TongyiLlmAgent, MiniMaxLlmAgent, BaichuanLlmAgent

**选项 C**：创建一个跟踪 Issue
- "实现所有 LLM 提供商的流式支持"

---

## 清理后的预期结果

- **代码库中的 TODO 数量**：从 65 个减少到约 10 个
- **所有 TODO 都关联到 GitHub Issues**，便于跟踪
- **移除占位符文件**，代码库更整洁
- **文档化的技术债务**，便于后续处理

---

## 建议

1. **创建 GitHub Issue 模板**：
   ```markdown
   ### TODO 清理

   **来源**：`src/path/to/file.cs:123`
   **描述**：[TODO 内容]
   **优先级**：高/中/低
   **依赖**：[其他 Issues]
   ```

2. **定期审查**：每月审查一次 TODO 状态

3. **禁止新的 TODO**：要求所有新 TODO 必须关联到 Issue

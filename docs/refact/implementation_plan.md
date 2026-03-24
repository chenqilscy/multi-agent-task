# CKY.MAF 架构重构方案 (SDK-First 模式)

## 核心重构目标

将 CKY.MAF 的定位彻底回归并在架构上锁定为 **"开发框架/SDK"**，而不是一个中心化的可执行网关。第三方开发者通过包引用（NuGet 或项目引用）将 MAF 嵌入自己的系统，用原生 C# 代码扩展 Agent 能力。

同时，为了提供极佳的可视化运维和调试体验，我们将引入一种类似 **Hangfire 或 .NET Aspire Dashboard 的嵌入式中间件架构**。

1. **废弃旧包袱**：原有的 `src/Demos/SmartHome` 和 `src/Demos/CustomerService` **全部废弃删除，不再保留**。
2. **纯粹的核心与 SDK**：保留 `Core`、`Services` 和 `Infrastructure` 层，优化其作为 Class Library 的依赖注入体验。
3. **独立的中间件可视化探针 (MAF Dashboard)**：
   * 将 React 前端（用于呈现 DAG 树、Agent 聊天沙盒）与其依附的监控 API 组合成一个全新的类库 `CKY.MAF.Dashboard`。
   * 第三方宿主只需在自己的 `Program.cs` 写入 `app.UseMafDashboard("/maf-ui")`，即可在宿主内部启动这套现代化控制台。
4. **统一演练场**：使用单一的 `CKY.MAF.ExampleHost` 替代原先复杂的 Demo。

---

## 1. 整体系统架构与集成关系 (Integration Architecture)

重构后的系统以 SDK 的逻辑嵌入任何业务宿主中：

```
┌───────────────────────────────────────────────────────────────┐
│        外部第三方宿主进程 (Third-Party ASP.NET Core Project)     │
│                                                               │
│  ┌────────────────────────┐    ┌───────────────────────────┐  │
│  │ 第三方自定的业务 Controller │    │ 业务 Agent (继承自 MafAgent) │  │
│  └──────────────────┬─────┘    └──────────────┬────────────┘  │
│                     │                         │               │
│                     ▼                         ▼               │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │               CKY.MAF Core & Services 引擎基座            │  │
│  └┬───────────────────────────────────────────────────────┬┘  │
│   │ (DI 容器层提供标准接口与编排器)                             │   │
│   ▼                                                       ▼   │
│  ┌────────────────────────┐    ┌───────────────────────────┐  │
│  │     Infrastructure     │    │ CKY.MAF.Dashboard (中间件)  │  │
│  │ (PostgreSQL, Redis 等) │    │ - 嵌入式 React SPA         │  │
│  │                        │    │ - 内部 SignalR 状态钩子      │  │
│  └────────────────────────┘    └──────────────┬────────────┘  │
└───────────────────────────────────────────────┼───────────────┘
                                                │ 
                                                ▼ HTTP / WebSocket
                                 ┌───────────────────────────┐ 
                                 │ 浏览器 (研发/运维人员)     │
                                 │ - 开箱即用的运维控制台      │
                                 └───────────────────────────┘ 
```

### 第三方系统集成的标准流程：
1. 第三方新建自己的 `.NET 10` Web API 项目，引用 `CKY.MAF.Core` 等包。
2. 内部研发 `public class OrderProcessingAgent : MafAgent` 放置自己的业务逻辑。
3. 在 `Program.cs` 中注册：
   ```csharp
   // 1. 注册 MAF 核心及其存储
   builder.Services.AddMafCore()
                   .UseSqliteStorage("...")
                   .UseZhipuAi("...");
                   
   // 2. 注册自己的第三方业务 Agents               
   builder.Services.AddMafAgent<OrderProcessingAgent>();
   
   // 3. 注册可视化 Dashboard
   builder.Services.AddMafDashboard();
   
   var app = builder.Build();
   
   // 4. 挂载中间件，本地打开 /maf-ui 即可访问控制台
   app.UseMafDashboard(options => {
       options.RoutePrefix = "/maf-ui";
   });
   ```

> ⚠️ 通过这种 SDK 模式，第三方系统既保持了最高性能的进程内 C# 对象调用（不需要跨进程 Webhook），又白嫖了 MAF 提供的极品可视化界面。

---

## 2. React Dashboard 关键功能与体验要求 (UI/UX)

Dashboard 前端基于 **React 18 + Vite + TypeScript + shadcn/ui** 开发。编译后的静态文件将压缩至 `CKY.MAF.Dashboard.dll` 中内嵌分发。

必须包含以下两个极其重要的页面进行框架能力的验证和调试：

### UX 重点一：任务执行树可视化视图 (Task Execution Visualizer)
由于 MAF 的核心是子任务的 DAG（有向无环图）编排，前端必须直观展示：
*   **DAG 图形化展示**：使用 `React Flow`，当一个根任务被分解出多个子任务时，画出树状或网图谱。
*   **实时节点高亮**：通过内部的 SignalR，当子任务由 `Pending` 变 `Running` 再到 `Success`，树节点实时变色。
*   **侧边内窥镜**：点击节点展开右侧面板，实时展示该 Agent 的思维链 (Chain of Thought)、工具调用日志。

### UX 重点二：Agent Chat 调试沙盒 (Agent Chat View)
为验证内置的槽位对话、历史记忆等特性，内置类似 ChatGPT 的对话调试框。
*   **多轮交互**：标准的左右气泡聊天框。
*   **内部状态透视 (Slot & Memory)**：在对话边缘悬浮一个 🔍 图标，点击可平移出面板展示：“这轮识别了哪些 Slots”、“从上下文本补全了哪些 Slots”、“新增了什么短期记忆”。
*   **澄清组件渲染**：当触发 `NeedsClarification` 时，不再抛出纯文本错误，而是渲染优美的操作按钮供手工点击补充实体。

---

## 3. 测试策略要求

*   **开发数据库**：SQLite 仍然是开发与测试的优先选择（作为 SDK 的默认存储提供程序最轻量）。
*   **测试覆盖率目标**：**≥ 90% Line / ≥ 80% Branch**（针对 Core + Services 层）。
*   **测试隔离**：移除旧 Demos 后，单元测试与集成测试只需依赖内部的假数据（`MockAgent`）或纯引擎默认实现，测试断言将极其稳定。

---

## 4. 分阶段执行计划

### Phase A: 废弃旧应用与重塑 SDK 骨架
1. 从物理磁盘和 `.slnx` 彻底删除 `src/Demos/SmartHome` 和 `CustomerService`。
2. 新增 `src/UI/CKY.MAF.Dashboard` (Class Library)，配置嵌入式路由挂载逻辑与静态资源清单。
3. 新增 `src/Examples/CKY.MAF.ExampleHost` (ASP.NET Core Web API)，用作唯一的演示和调试宿主，在其中 `builder.Services.AddMafDashboard()` 跑起来。

### Phase B: React 基础环境搭建与打包通联
1. 在 `frontend/maf-console` 初始化 Vite + React + shadcn/ui。
2. 建立自动化 MSBuild Target：当编译 `CKY.MAF.Dashboard.csproj` 时，自动执行 `npm run build` 并将 Dist 拷贝进 DLL 的纯内嵌资源中。
3. 在 Dashboard 类库中编写内部使用的隐藏 API 与 SignalR Hub，为前端提供数据接口。

### Phase C: Dashboard 复杂业务流研发 (UX 核心)
1. 引入 React Flow 开发任务树状态渲染器（DAG 可视化）。
2. 开发 Agent Chat 多轮聊天框及“核心状态透视”面板。

### Phase D: 测试抢修与覆盖率护航
1. 修复之前因删除 Demo 而跑错的 UnitTests（将其改造为基于 MockAgent 的测试）。
2. 引入 Coverlet 收集，确立 CI 流水线的门禁机制。

---

## ✅ 架构定稿确认

1. **彻底回归 SDK 集成本位**（仿效 Semantic Kernel / Hangfire）。
2. **零侵入的 React 独立监控画板**（打包进中间件，第三方只需 `UseMafDashboard()`）。
3. **移除庞杂业务 Demo，代以极简 ExampleHost**。
4. **开发数据库坚守 SQLite**，降低接入者心智负担。
5. **覆盖率 ≥ 90% Line / ≥ 80% Branch** 必须达标。

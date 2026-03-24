# CKY.MAF 重构任务清单 (SDK-First)

## Phase A: 废弃旧应用与重塑 SDK 骨架

### A1. 清理旧 Demo 代码
- [ ] 物理删除 `src/Demos/SmartHome` 文件夹
- [ ] 物理删除 `src/Demos/CustomerService` 文件夹
- [ ] 更新 `src/CKY.MAF.slnx` 移除 Demo 项目 and Layer 5 Folder

### A2. 构建 ExampleHost 与 Dashboard 类库
- [ ] 创建类库项目 `src/UI/CKY.MAF.Dashboard/CKY.MAF.Dashboard.csproj`
- [ ] 创建 WebAPI 项目 `src/Examples/CKY.MAF.ExampleHost/CKY.MAF.ExampleHost.csproj`
- [ ] 更新 `src/CKY.MAF.slnx` 添加新的 UI 和 ExampleHost 项目
- [ ] 在 `ExampleHost` 中配置基础的 `AddMafCore` 启动逻辑和使用 SQLite 存储

---

## Phase B: Dashboard React 环境与嵌入式构建栈

### B1. React 基础环境 (maf-console)
- [ ] 运行 `npx create-vite@latest frontend/maf-console --template react-ts`
- [ ] 集成 TailwindCSS 和 shadcn/ui 组件库
- [ ] 配置 Vite 基础路由体系

### B2. 嵌入式路由联调
- [ ] 调整 Vite build 配置，将编译输出定向到 `CKY.MAF.Dashboard/wwwroot` 或对应的嵌入式资源目录
- [ ] 在 `CKY.MAF.Dashboard` 中编写中间件 `UseMafDashboard(RoutePrefix)` 处理静态文件的路由转发拦截
- [ ] 测试能在 `ExampleHost` 启动时通过 `http://localhost:xxxx/maf-ui` 访问到 React 空白页

---

## Phase C: 控制台后端服务与 UX 研发 (核心难点)

### C1. 内部通信机制 (API + SignalR)
- [ ] `CKY.MAF.Dashboard` 添加内嵌在内部路由规则上的 Controller API (任务查询、Agent列表查询等)
- [ ] `CKY.MAF.Dashboard` 添加 `MafDashboardHub` (SignalR) 用于推送任务生命周期状态

### C2. 核心大屏页面 (Visualizer & Chat)
- [ ] 引入 `React Flow`，实现任务执行历史的 DAG 可视化界面并接通数据
- [ ] 研发 Agent Chat 沙盒调试页面，实现 左右聊天气泡 + 隐藏 Slots 分析版 + 实时流式输出
- [ ] 对接澄清卡片组件，拦截并渲染 `NeedsClarification` 信息

---

## Phase D: 测试抢修与 CI 覆盖率门禁

### D1. 修复原有的单元与集成测试
- [ ] 清理 `UnitTests` 和 `IntegrationTests` 测试库中对已删除 Demo 的引用
- [ ] 将失败的包含真实 Agent 业务的测试替换为 `MockAgent`，跑通所有基础 Core 功能
- [ ] CKY.MAF.Tests.csproj 导入 `coverlet.collector` 与门禁脚本

### D2. CI/CD 流水线
- [ ] 修改 `.github/workflows/` 中的脚本，覆盖率 < 90% 时产生报错
- [ ] 添加发布 `.nupkg` 逻辑以支持第三方真实 NuGet 引用

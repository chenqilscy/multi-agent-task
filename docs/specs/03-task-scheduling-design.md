# Main-Agent/Sub-Agent 任务调度架构设计

> **文档版本**: v1.2
> **创建日期**: 2026-03-12
> **主题**: 任务优先级、依赖关系、执行调度完整方案

---

## 📋 目录

1. [任务模型设计](#一任务模型设计)
2. [优先级系统](#二优先级系统)
3. [依赖关系建模](#三依赖关系建模)
4. [任务调度算法](#四任务调度算法)
5. [执行策略](#五执行策略)
6. [异常处理](#六异常处理)
7. [代码实现](#七代码实现)

---

## 一、任务模型设计

### 1.1 任务层次结构

```
UserInput (用户输入)
    │
    ▼
MainTask (主任务)
    │
    ├──► DecomposedTask[] (分解任务)
    │     │
    │     ├──► Priority (优先级)
    │     ├──► Dependencies[] (依赖关系)
    │     ├──► ExecutionStrategy (执行策略)
    │     └──► ResourceRequirements (资源需求)
    │
    ▼
ExecutionPlan (执行计划)
    │
    ├──► SerialTasks[] (串行任务组)
    └──► ParallelTaskGroups[] (并行任务组)
```

### 1.2 核心数据模型

```csharp
namespace MultiAgentFramework.Core.Models
{
    /// <summary>
    /// 分解后的任务
    /// </summary>
    public class DecomposedTask
    {
        // ===== 基本信息 =====
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string Intent { get; set; }
        public string Description { get; set; }

        // ===== 优先级相关 =====
        public TaskPriority Priority { get; set; }
        public int PriorityScore { get; set; }  // 0-100，数值越高优先级越高
        public PriorityReason PriorityReason { get; set; }

        // ===== 依赖关系 =====
        public List<TaskDependency> Dependencies { get; set; } = new();
        public bool IsBlocked => Dependencies.Any(d => !d.IsSatisfied);

        // ===== 执行策略 =====
        public ExecutionStrategy ExecutionStrategy { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public TimeSpan MaxWaitTime { get; set; }

        // ===== 资源需求 =====
        public string RequiredCapability { get; set; }
        public string TargetAgentId { get; set; }
        public ResourceRequirements ResourceRequirements { get; set; }

        // ===== 上下文数据 =====
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();

        // ===== 状态跟踪 =====
        public TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TaskExecutionResult Result { get; set; }
    }

    /// <summary>
    /// 任务优先级枚举
    /// </summary>
    public enum TaskPriority
    {
        Critical = 5,   // 关键任务（安全相关、用户强制中断）
        High = 4,       // 高优先级（用户明确要求）
        Normal = 3,     // 普通优先级（常规任务）
        Low = 2,        // 低优先级（后台任务）
        Background = 1  // 后台任务（日志、统计等）
    }

    /// <summary>
    /// 优先级原因
    /// </summary>
    public enum PriorityReason
    {
        UserExplicit,          // 用户明确指定
        SafetyCritical,        // 安全关键
        UserInteraction,       // 用户交互相关
        SystemDefault,         // 系统默认
        BackgroundTask,        // 后台任务
        DependentOnHighPriority // 依赖高优先级任务
    }

    /// <summary>
    /// 任务依赖关系
    /// </summary>
    public class TaskDependency
    {
        public string DependsOnTaskId { get; set; }
        public DependencyType Type { get; set; }
        public bool IsSatisfied { get; set; }
        public string Condition { get; set; }  // 可选条件表达式

        /// <summary>
        /// 检查依赖是否满足
        /// </summary>
        public bool CheckSatisfied(DecomposedTask targetTask)
        {
            if (targetTask == null) return false;

            return Type switch
            {
                DependencyType.MustComplete => targetTask.Status == TaskStatus.Completed,
                DependencyType.MustSucceed => targetTask.Status == TaskStatus.Completed
                                      && targetTask.Result?.Success == true,
                DependencyType.MustStart => targetTask.Status == TaskStatus.Running
                                      || targetTask.Status == TaskStatus.Completed,
                DependencyType.DataDependency => targetTask.Status == TaskStatus.Completed
                                           && targetTask.Result?.Data != null,
                _ => false
            };
        }
    }

    /// <summary>
    /// 依赖类型
    /// </summary>
    public enum DependencyType
    {
        MustComplete,    // 必须完成（无论成功失败）
        MustSucceed,     // 必须成功
        MustStart,       // 必须已开始
        DataDependency,  // 数据依赖（需要输出数据）
        SoftDependency   // 软依赖（可选，优先级继承）
    }

    /// <summary>
    /// 执行策略
    /// </summary>
    public enum ExecutionStrategy
    {
        Immediate,       // 立即执行
        Parallel,        // 并行执行
        Serial,          // 串行执行
        Delayed,         // 延迟执行
        Conditional      // 条件执行
    }

    /// <summary>
    /// 资源需求
    /// </summary>
    public class ResourceRequirements
    {
        public int RequiredAgentSlots { get; set; } = 1;
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(5);
        public bool RequiresExclusiveAccess { get; set; } = false;
        public List<string> RequiredCapabilities { get; set; } = new();
    }

    /// <summary>
    /// 任务状态
    /// </summary>
    public enum TaskStatus
    {
        Pending,     // 等待执行
        Ready,       // 准备就绪（依赖已满足）
        Scheduled,   // 已调度
        Running,     // 执行中
        Completed,   // 已完成
        Failed,      // 失败
        Cancelled,   // 已取消
        Timeout      // 超时
    }
}
```

---

## 二、优先级系统

### 2.1 优先级计算模型

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 任务优先级计算器
    /// </summary>
    public interface ITaskPriorityCalculator
    {
        /// <summary>
        /// 计算任务优先级分数
        /// </summary>
        int CalculatePriorityScore(DecomposedTask task, TaskContext context);

        /// <summary>
        /// 比较两个任务的优先级
        /// </summary>
        int CompareTasks(DecomposedTask task1, DecomposedTask task2);
    }

    public class MafTaskPriorityCalculator : ITaskPriorityCalculator
    {
        private readonly IPriorityRuleEngine _ruleEngine;

        public MafTaskPriorityCalculator(IPriorityRuleEngine ruleEngine)
        {
            _ruleEngine = ruleEngine;
        }

        public int CalculatePriorityScore(DecomposedTask task, TaskContext context)
        {
            var score = 0;

            // 1. 基础优先级（0-40分）
            score += GetBasePriorityScore(task.Priority);

            // 2. 用户交互因素（0-30分）
            score += GetUserInteractionScore(task, context);

            // 3. 时间因素（0-15分）
            score += GetTimeFactorScore(task, context);

            // 4. 资源利用率（0-10分）
            score += GetResourceUtilizationScore(task, context);

            // 5. 依赖传播（0-5分）
            score += GetDependencyPropagationScore(task);

            return Math.Clamp(score, 0, 100);
        }

        private int GetBasePriorityScore(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Critical => 40,
                TaskPriority.High => 30,
                TaskPriority.Normal => 20,
                TaskPriority.Low => 10,
                TaskPriority.Background => 0,
                _ => 20
            };
        }

        private int GetUserInteractionScore(DecomposedTask task, TaskContext context)
        {
            var score = 0;

            // 用户明确要求
            if (task.PriorityReason == PriorityReason.UserExplicit)
                score += 20;

            // 用户交互相关
            if (task.PriorityReason == PriorityReason.UserInteraction)
                score += 15;

            // 用户等待中
            if (context.UserWaitingForResponse)
                score += 10;

            // 对话中的任务（比后台任务优先）
            if (context.IsInConversation)
                score += 5;

            return score;
        }

        private int GetTimeFactorScore(DecomposedTask task, TaskContext context)
        {
            var score = 0;

            // 任务等待时间
            var waitTime = DateTime.UtcNow - task.CreatedAt;
            if (waitTime > TimeSpan.FromMinutes(1))
                score += 5;
            if (waitTime > TimeSpan.FromMinutes(5))
                score += 10;

            // 截止时间压力
            if (task.MaxWaitTime != default)
            {
                var remainingTime = task.MaxWaitTime - waitTime;
                if (remainingTime < TimeSpan.FromSeconds(30))
                    score += 5;
            }

            return score;
        }

        private int GetResourceUtilizationScore(DecomposedTask task, TaskContext context)
        {
            // 资源利用率高时，优先执行资源占用少的任务
            var utilization = context.ResourceUtilization;

            if (utilization > 0.9 && task.ResourceRequirements.RequiredAgentSlots == 1)
                return 10;

            if (utilization > 0.8 && task.ResourceRequirements.RequiredAgentSlots == 1)
                return 5;

            return 0;
        }

        private int GetDependencyPropagationScore(DecomposedTask task)
        {
            // 如果依赖的任务优先级高，提升当前任务优先级
            var maxDependentPriority = task.Dependencies
                .Where(d => d.Type == DependencyType.MustSucceed)
                .Max(d => d.SourceTask?.PriorityScore ?? 0);

            return (int)(maxDependentPriority * 0.1);  // 继承10%
        }

        public int CompareTasks(DecomposedTask task1, DecomposedTask task2)
        {
            // 1. 首先比较优先级分数
            var score1 = task1.PriorityScore;
            var score2 = task2.PriorityScore;

            if (Math.Abs(score1 - score2) > 5)  // 分数差大于5才认为有显著差异
                return score2.CompareTo(score1);

            // 2. 分数相近时，比较创建时间（FIFO）
            return task1.CreatedAt.CompareTo(task2.CreatedAt);
        }
    }
}
```

### 2.2 优先级规则引擎

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 优先级规则引擎
    /// </summary>
    public interface IPriorityRuleEngine
    {
        /// <summary>
        /// 应用规则提升优先级
        /// </summary>
        void ApplyRules(DecomposedTask task, TaskContext context);
    }

    public class MafPriorityRuleEngine : IPriorityRuleEngine
    {
        public void ApplyRules(DecomposedTask task, TaskContext context)
        {
            // 规则1: 安全相关任务始终最高优先级
            if (IsSafetyCritical(task))
            {
                task.Priority = TaskPriority.Critical;
                task.PriorityReason = PriorityReason.SafetyCritical;
                return;
            }

            // 规则2: 用户取消/停止操作优先级最高
            if (IsUserCancellation(task))
            {
                task.Priority = TaskPriority.Critical;
                task.PriorityReason = PriorityReason.UserExplicit;
                return;
            }

            // 规则3: 用户等待响应的任务提升优先级
            if (context.UserWaitingForResponse && IsResponseTask(task))
            {
                task.Priority = TaskPriority.High;
                task.PriorityReason = PriorityReason.UserInteraction;
            }

            // 规则4: 紧急命令提升优先级
            if (HasUrgencyKeyword(task.Description))
            {
                if (task.Priority < TaskPriority.High)
                {
                    task.Priority = TaskPriority.High;
                    task.PriorityReason = PriorityReason.UserExplicit;
                }
            }

            // 规则5: 后台任务保持低优先级
            if (IsBackgroundTask(task))
            {
                if (task.Priority < TaskPriority.Normal)
                {
                    task.Priority = TaskPriority.Background;
                    task.PriorityReason = PriorityReason.BackgroundTask;
                }
            }
        }

        private bool IsSafetyCritical(DecomposedTask task)
        {
            var safetyKeywords = new[] { "emergency", "stop", "danger", "alert", "紧急", "停止" };
            return safetyKeywords.Any(kw =>
                task.Intent.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                task.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsUserCancellation(DecomposedTask task)
        {
            var cancelKeywords = new[] { "cancel", "abort", "取消", "停止" };
            return cancelKeywords.Any(kw =>
                task.Intent.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsResponseTask(DecomposedTask task)
        {
            return task.Intent.Contains("response", StringComparison.OrdinalIgnoreCase) ||
                   task.Intent.Contains("notify", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasUrgencyKeyword(string description)
        {
            var urgencyKeywords = new[] { "immediately", "asap", "urgent", "立即", "马上", "紧急" };
            return urgencyKeywords.Any(kw =>
                description?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true);
        }

        private bool IsBackgroundTask(DecomposedTask task)
        {
            var backgroundIntents = new[] { "log", "report", "statistics", "monitor" };
            return backgroundIntents.Any(intent =>
                task.Intent.Contains(intent, StringComparison.OrdinalIgnoreCase));
        }
    }
}
```

---

## 三、依赖关系建模

### 3.1 依赖关系分析

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 任务依赖分析器
    /// </summary>
    public interface ITaskDependencyAnalyzer
    {
        /// <summary>
        /// 分析任务依赖关系
        /// </summary>
        Task AnalyzeDependenciesAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default);

        /// <summary>
        /// 检测循环依赖
        /// </summary>
        bool HasCircularDependency(List<DecomposedTask> tasks);

        /// <summary>
        /// 构建依赖图
        /// </summary>
        TaskDependencyGraph BuildDependencyGraph(List<DecomposedTask> tasks);
    }

    public class MafTaskDependencyAnalyzer : ITaskDependencyAnalyzer
    {
        public async Task AnalyzeDependenciesAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct = default)
        {
            // 1. 自动识别隐式依赖
            await IdentifyImplicitDependenciesAsync(tasks, ct);

            // 2. 验证依赖有效性
            ValidateDependencies(tasks);

            // 3. 检测循环依赖
            if (HasCircularDependency(tasks))
            {
                throw new MafTaskException("检测到循环依赖，无法构建执行计划");
            }

            // 4. 计算任务深度（用于拓扑排序）
            CalculateTaskDepth(tasks);
        }

        private async Task IdentifyImplicitDependenciesAsync(
            List<DecomposedTask> tasks,
            CancellationToken ct)
        {
            // 规则1: 同一房间的设备操作可能有序依赖
            var roomGroups = tasks
                .Where(t => t.Parameters.ContainsKey("room"))
                .GroupBy(t => t.Parameters["room"].ToString());

            foreach (var group in roomGroups)
            {
                var tasksInRoom = group.ToList();
                for (int i = 1; i < tasksInRoom.Count; i++)
                {
                    // 如果后面的任务依赖前面的任务结果
                    if (ShouldAddSequentialDependency(tasksInRoom[i-1], tasksInRoom[i]))
                    {
                        tasksInRoom[i].Dependencies.Add(new TaskDependency
                        {
                            DependsOnTaskId = tasksInRoom[i-1].TaskId,
                            Type = DependencyType.MustSucceed
                        });
                    }
                }
            }

            // 规则2: 读取设备状态后再控制
            var readTasks = tasks.Where(t => t.Intent.Contains("get_state") ||
                                          t.Intent.Contains("query"));
            var controlTasks = tasks.Where(t => t.Intent.Contains("control") ||
                                            t.Intent.Contains("set"));

            foreach (var readTask in readTasks)
            {
                var dependentControls = controlTasks
                    .Where(ct => ct.Parameters.ContainsKey("deviceId") &&
                                readTask.Parameters.ContainsKey("deviceId") &&
                                ct.Parameters["deviceId"].Equals(readTask.Parameters["deviceId"]));

                foreach (var controlTask in dependentControls)
                {
                    if (!controlTask.Dependencies.Any(d => d.DependsOnTaskId == readTask.TaskId))
                    {
                        controlTask.Dependencies.Add(new TaskDependency
                        {
                            DependsOnTaskId = readTask.TaskId,
                            Type = DependencyType.DataDependency
                        });
                    }
                }
            }

            await Task.CompletedTask;
        }

        private bool ShouldAddSequentialDependency(DecomposedTask first, DecomposedTask second)
        {
            // 如果第二个任务需要第一个任务的输出数据
            return second.Context.ContainsKey("requires_input_from") &&
                   second.Context["requires_input_from"].ToString() == first.TaskId;
        }

        private void ValidateDependencies(List<DecomposedTask> tasks)
        {
            var taskIds = tasks.Select(t => t.TaskId).ToHashSet();

            foreach (var task in tasks)
            {
                foreach (var dep in task.Dependencies)
                {
                    if (!taskIds.Contains(dep.DependsOnTaskId))
                    {
                        throw new MafTaskException(
                            $"任务 {task.TaskId} 依赖的任务 {dep.DependsOnTaskId} 不存在");
                    }
                }
            }
        }

        public bool HasCircularDependency(List<DecomposedTask> tasks)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var task in tasks)
            {
                if (HasCycleDFS(task, tasks, visited, recursionStack))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCycleDFS(
            DecomposedTask current,
            List<DecomposedTask> allTasks,
            HashSet<string> visited,
            HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(current.TaskId))
                return true;  // 检测到环

            if (visited.Contains(current.TaskId))
                return false;  // 已访问过

            visited.Add(current.TaskId);
            recursionStack.Add(current.TaskId);

            foreach (var dep in current.Dependencies)
            {
                var dependentTask = allTasks.FirstOrDefault(t => t.TaskId == dep.DependsOnTaskId);
                if (dependentTask != null)
                {
                    if (HasCycleDFS(dependentTask, allTasks, visited, recursionStack))
                        return true;
                }
            }

            recursionStack.Remove(current.TaskId);
            return false;
        }

        public TaskDependencyGraph BuildDependencyGraph(List<DecomposedTask> tasks)
        {
            var graph = new TaskDependencyGraph();

            foreach (var task in tasks)
            {
                graph.AddNode(task);

                foreach (var dep in task.Dependencies)
                {
                    var dependentTask = tasks.FirstOrDefault(t => t.TaskId == dep.DependsOnTaskId);
                    if (dependentTask != null)
                    {
                        graph.AddEdge(dependentTask, task, dep.Type);
                    }
                }
            }

            return graph;
        }

        private void CalculateTaskDepth(List<DecomposedTask> tasks)
        {
            var depthMap = new Dictionary<string, int>();

            foreach (var task in tasks)
            {
                CalculateTaskDepthDFS(task, tasks, depthMap);
            }

            foreach (var task in tasks)
            {
                task.Context["Depth"] = depthMap.GetValueOrDefault(task.TaskId, 0);
            }
        }

        private int CalculateTaskDepthDFS(
            DecomposedTask task,
            List<DecomposedTask> allTasks,
            Dictionary<string, int> depthMap)
        {
            if (depthMap.ContainsKey(task.TaskId))
                return depthMap[task.TaskId];

            if (!task.Dependencies.Any())
            {
                depthMap[task.TaskId] = 0;
                return 0;
            }

            var maxDependentDepth = 0;
            foreach (var dep in task.Dependencies)
            {
                var dependentTask = allTasks.FirstOrDefault(t => t.TaskId == dep.DependsOnTaskId);
                if (dependentTask != null)
                {
                    var depDepth = CalculateTaskDepthDFS(dependentTask, allTasks, depthMap);
                    maxDependentDepth = Math.Max(maxDependentDepth, depDepth);
                }
            }

            var depth = maxDependentDepth + 1;
            depthMap[task.TaskId] = depth;
            return depth;
        }
    }
}
```

### 3.2 依赖图数据结构

```csharp
namespace MultiAgentFramework.Core.Models
{
    /// <summary>
    /// 任务依赖图
    /// </summary>
    public class TaskDependencyGraph
    {
        private readonly Dictionary<string, DecomposedTask> _nodes = new();
        private readonly Dictionary<string, List<DependencyEdge>> _adjacencyList = new();

        public void AddNode(DecomposedTask task)
        {
            _nodes[task.TaskId] = task;
            if (!_adjacencyList.ContainsKey(task.TaskId))
            {
                _adjacencyList[task.TaskId] = new List<DependencyEdge>();
            }
        }

        public void AddEdge(DecomposedTask from, DecomposedTask to, DependencyType type)
        {
            _adjacencyList[from.TaskId].Add(new DependencyEdge
            {
                From = from,
                To = to,
                Type = type
            });
        }

        /// <summary>
        /// 获取拓扑排序（满足依赖关系的执行顺序）
        /// </summary>
        public List<DecomposedTask> TopologicalSort()
        {
            var inDegree = new Dictionary<string, int>();
            var result = new List<DecomposedTask>();
            var queue = new Queue<DecomposedTask>();

            // 计算入度
            foreach (var node in _nodes.Values)
            {
                inDegree[node.TaskId] = 0;
            }

            foreach (var edges in _adjacencyList.Values)
            {
                foreach (var edge in edges)
                {
                    inDegree[edge.To.TaskId]++;
                }
            }

            // 入度为0的节点入队
            foreach (var node in _nodes.Values)
            {
                if (inDegree[node.TaskId] == 0)
                {
                    queue.Enqueue(node);
                }
            }

            // Kahn算法
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                foreach (var edge in _adjacencyList[current.TaskId])
                {
                    inDegree[edge.To.TaskId]--;
                    if (inDegree[edge.To.TaskId] == 0)
                    {
                        queue.Enqueue(edge.To);
                    }
                }
            }

            // 检查是否所有节点都被访问（是否存在环）
            if (result.Count != _nodes.Count)
            {
                throw new MafTaskException("依赖图中存在循环依赖");
            }

            return result;
        }

        /// <summary>
        /// 获取可以并行执行的任务组
        /// </summary>
        public List<List<DecomposedTask>> GetParallelGroups()
        {
            var sorted = TopologicalSort();
            var groups = new List<List<DecomposedTask>>();
            var depthMap = new Dictionary<string, int>();

            // 计算每个任务的深度
            foreach (var task in sorted)
            {
                var depth = 0;
                foreach (var edge in _adjacencyList.Values.SelectMany(e => e))
                {
                    if (edge.To == task)
                    {
                        var fromDepth = depthMap.GetValueOrDefault(edge.From.TaskId, 0);
                        depth = Math.Max(depth, fromDepth + 1);
                    }
                }
                depthMap[task.TaskId] = depth;
            }

            // 按深度分组
            var maxDepth = depthMap.Values.Max();
            for (int i = 0; i <= maxDepth; i++)
            {
                var group = sorted.Where(t => depthMap[t.TaskId] == i).ToList();
                if (group.Any())
                {
                    groups.Add(group);
                }
            }

            return groups;
        }
    }

    public class DependencyEdge
    {
        public DecomposedTask From { get; set; }
        public DecomposedTask To { get; set; }
        public DependencyType Type { get; set; }
    }
}
```

---

## 四、任务调度算法

### 4.1 调度器接口

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 任务调度器
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// 创建执行计划
        /// </summary>
        Task<ExecutionPlan> CreatePlanAsync(
            List<DecomposedTask> tasks,
            SchedulingConstraints constraints,
            CancellationToken ct = default);

        /// <summary>
        /// 调度下一个待执行任务
        /// </summary>
        Task<DecomposedTask> ScheduleNextAsync(
            ExecutionPlan plan,
            CancellationToken ct = default);
    }

    /// <summary>
    /// 调度约束
    /// </summary>
    public class SchedulingConstraints
    {
        public int MaxParallelTasks { get; set; } = 5;
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(10);
        public bool AllowPartialExecution { get; set; } = true;
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// 执行计划
    /// </summary>
    public class ExecutionPlan
    {
        public string PlanId { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<TaskGroup> SerialGroups { get; set; } = new();
        public List<TaskGroup> ParallelGroups { get; set; } = new();

        public List<DecomposedTask> AllTasks =>
            SerialGroups.SelectMany(g => g.Tasks)
                       .Concat(ParallelGroups.SelectMany(g => g.Tasks))
                       .ToList();

        public TaskGroup CurrentGroup { get; set; }
        public int CurrentGroupIndex { get; set; }
    }

    /// <summary>
    /// 任务组
    /// </summary>
    public class TaskGroup
    {
        public string GroupId { get; set; }
        public GroupExecutionMode Mode { get; set; }
        public List<DecomposedTask> Tasks { get; set; } = new();
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public GroupStatus Status { get; set; }
    }

    public enum GroupExecutionMode
    {
        Serial,     // 串行执行
        Parallel    // 并行执行
    }

    public enum GroupStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
```

### 4.2 调度算法实现

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 智能任务调度器
    /// </summary>
    public class MafTaskScheduler : ITaskScheduler
    {
        private readonly ITaskPriorityCalculator _priorityCalculator;
        private readonly ITaskDependencyAnalyzer _dependencyAnalyzer;
        private readonly ILogger<MafTaskScheduler> _logger;

        public MafTaskScheduler(
            ITaskPriorityCalculator priorityCalculator,
            ITaskDependencyAnalyzer dependencyAnalyzer,
            ILogger<MafTaskScheduler> logger)
        {
            _priorityCalculator = priorityCalculator;
            _dependencyAnalyzer = dependencyAnalyzer;
            _logger = logger;
        }

        public async Task<ExecutionPlan> CreatePlanAsync(
            List<DecomposedTask> tasks,
            SchedulingConstraints constraints,
            CancellationToken ct = default)
        {
            _logger.LogInformation("开始创建执行计划，任务数：{TaskCount}", tasks.Count);

            // 1. 分析依赖关系
            await _dependencyAnalyzer.AnalyzeDependenciesAsync(tasks, ct);

            // 2. 计算优先级
            var context = new TaskContext();
            foreach (var task in tasks)
            {
                task.PriorityScore = _priorityCalculator.CalculatePriorityScore(task, context);
            }

            // 3. 构建依赖图
            var graph = _dependencyAnalyzer.BuildDependencyGraph(tasks);

            // 4. 拓扑排序
            var sortedTasks = graph.TopologicalSort();

            // 5. 获取并行组
            var parallelGroups = graph.GetParallelGroups();

            // 6. 优化调度（考虑资源约束）
            var optimizedPlan = await OptimizeScheduleAsync(
                sortedTasks,
                parallelGroups,
                constraints,
                ct);

            _logger.LogInformation(
                "执行计划创建完成：{SerialGroups}个串行组，{ParallelGroups}个并行组",
                optimizedPlan.SerialGroups.Count,
                optimizedPlan.ParallelGroups.Count);

            return optimizedPlan;
        }

        private async Task<ExecutionPlan> OptimizeScheduleAsync(
            List<DecomposedTask> sortedTasks,
            List<List<DecomposedTask>> parallelGroups,
            SchedulingConstraints constraints,
            CancellationToken ct)
        {
            var plan = new ExecutionPlan
            {
                PlanId = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };

            // 策略：根据组内任务数量决定执行模式
            foreach (var group in parallelGroups)
            {
                var taskGroup = new TaskGroup
                {
                    GroupId = Guid.NewGuid().ToString("N"),
                    Mode = GroupExecutionMode.Parallel,
                    Tasks = group.ToList(),
                    Status = GroupStatus.Pending
                };

                // 如果组内任务少于2个，或者有独占资源需求的任务，改为串行
                if (group.Count < 2 || group.Any(t => t.ResourceRequirements.RequiresExclusiveAccess)))
                {
                    taskGroup.Mode = GroupExecutionMode.Serial;
                }
                // 如果超过最大并行数限制，拆分子组
                else if (group.Count > constraints.MaxParallelTasks)
                {
                    var subGroups = SplitIntoSubGroups(group, constraints.MaxParallelTasks);
                    foreach (var subGroup in subGroups)
                    {
                        plan.ParallelGroups.Add(new TaskGroup
                        {
                            GroupId = Guid.NewGuid().ToString("N"),
                            Mode = GroupExecutionMode.Parallel,
                            Tasks = subGroup,
                            Status = GroupStatus.Pending
                        });
                    }
                    continue;
                }

                plan.ParallelGroups.Add(taskGroup);
            }

            // 按优先级对并行组排序（高优先级的组先执行）
            plan.ParallelGroups = plan.ParallelGroups
                .OrderByDescending(g => g.Tasks.Average(t => t.PriorityScore))
                .ToList();

            // 将并行组转换为串行依赖
            for (int i = 1; i < plan.ParallelGroups.Count; i++)
            {
                var prevGroup = plan.ParallelGroups[i - 1];
                var currentGroup = plan.ParallelGroups[i];

                // 添加跨组依赖
                foreach (var task in currentGroup.Tasks)
                {
                    var representativeTask = prevGroup.Tasks
                        .OrderByDescending(t => t.PriorityScore)
                        .First();

                    task.Dependencies.Add(new TaskDependency
                    {
                        DependsOnTaskId = representativeTask.TaskId,
                        Type = DependencyType.MustComplete
                    });
                }
            }

            return plan;
        }

        private List<List<DecomposedTask>> SplitIntoSubGroups(
            List<DecomposedTask> tasks,
            int maxSize)
        {
            var groups = new List<List<DecomposedTask>>();

            // 按优先级排序后分组
            var sorted = tasks.OrderByDescending(t => t.PriorityScore).ToList();

            for (int i = 0; i < sorted.Count; i += maxSize)
            {
                var groupSize = Math.Min(maxSize, sorted.Count - i);
                groups.Add(sorted.Skip(i).Take(groupSize).ToList());
            }

            return groups;
        }

        public async Task<DecomposedTask> ScheduleNextAsync(
            ExecutionPlan plan,
            CancellationToken ct)
        {
            // 1. 查找当前执行组
            var currentGroup = plan.CurrentGroup ??
                             plan.ParallelGroups.FirstOrDefault(g => g.Status == GroupStatus.Pending) ??
                             plan.SerialGroups.FirstOrDefault(g => g.Status == GroupStatus.Pending);

            if (currentGroup == null)
            {
                return null;  // 所有任务已完成
            }

            // 2. 从当前组中选择下一个任务
            var nextTask = currentGroup.Tasks
                .Where(t => t.Status == TaskStatus.Pending || t.Status == TaskStatus.Ready)
                .OrderByDescending(t => t.PriorityScore)
                .FirstOrDefault();

            if (nextTask == null)
            {
                // 当前组已完成或失败，移动到下一组
                currentGroup.Status = currentGroup.Tasks.All(t => t.Status == TaskStatus.Completed)
                    ? GroupStatus.Completed
                    : GroupStatus.Failed;

                // 递归查找下一组
                return await ScheduleNextAsync(plan, ct);
            }

            // 3. 检查依赖是否满足
            if (nextTask.IsBlocked)
            {
                _logger.LogWarning(
                    "任务 {TaskId} 被阻塞，等待依赖完成",
                    nextTask.TaskId);

                // 返回组内其他可执行任务
                var alternativeTask = currentGroup.Tasks
                    .Where(t => t.TaskId != nextTask.TaskId)
                    .Where(t => !t.IsBlocked)
                    .OrderByDescending(t => t.PriorityScore)
                    .FirstOrDefault();

                return alternativeTask ?? await ScheduleNextAsync(plan, ct);
            }

            plan.CurrentGroup = currentGroup;
            return await Task.FromResult(nextTask);
        }
    }
}
```

---

## 五、执行策略

### 5.1 执行引擎

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 任务执行引擎
    /// </summary>
    public interface ITaskExecutionEngine
    {
        /// <summary>
        /// 执行计划
        /// </summary>
        Task<ExecutionResult> ExecutePlanAsync(
            ExecutionPlan plan,
            CancellationToken ct = default);

        /// <summary>
        /// 执行单个任务组
        /// </summary>
        Task<GroupExecutionResult> ExecuteGroupAsync(
            TaskGroup group,
            CancellationToken ct = default);
    }

    public class MafTaskExecutionEngine : ITaskExecutionEngine
    {
        private readonly IAgentOrchestrator _agentOrchestrator;
        private readonly ILogger<MafTaskExecutionEngine> _logger;

        public async Task<ExecutionResult> ExecutePlanAsync(
            ExecutionPlan plan,
            CancellationToken ct = default)
        {
            _logger.LogInformation("开始执行计划：{PlanId}", plan.PlanId);

            var overallResult = new ExecutionResult
            {
                PlanId = plan.PlanId,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                foreach (var group in plan.ParallelGroups)
                {
                    var groupResult = await ExecuteGroupAsync(group, ct);

                    if (!groupResult.Success && !plan.AllowPartialExecution)
                    {
                        // 不允许部分执行，终止整个计划
                        overallResult.Success = false;
                        overallResult.Error = $"任务组 {group.GroupId} 执行失败";
                        return overallResult;
                    }
                }

                overallResult.Success = true;
                overallResult.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行计划失败");
                overallResult.Success = false;
                overallResult.Error = ex.Message;
            }

            return overallResult;
        }

        public async Task<GroupExecutionResult> ExecuteGroupAsync(
            TaskGroup group,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "执行任务组：{GroupId}，模式：{Mode}，任务数：{Count}",
                group.GroupId,
                group.Mode,
                group.Tasks.Count);

            group.Status = GroupStatus.Running;
            group.StartTime = DateTime.UtcNow;

            var result = new GroupExecutionResult
            {
                GroupId = group.GroupId,
                ExecutionMode = group.Mode
            };

            try
            {
                if (group.Mode == GroupExecutionMode.Serial)
                {
                    result = await ExecuteSerialGroupAsync(group, ct);
                }
                else
                {
                    result = await ExecuteParallelGroupAsync(group, ct);
                }

                group.Status = result.Success ? GroupStatus.Completed : GroupStatus.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务组执行异常");
                group.Status = GroupStatus.Failed;
                result.Success = false;
                result.Error = ex.Message;
            }
            finally
            {
                group.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        private async Task<GroupExecutionResult> ExecuteSerialGroupAsync(
            TaskGroup group,
            CancellationToken ct)
        {
            var result = new GroupExecutionResult
            {
                GroupId = group.GroupId,
                ExecutionMode = GroupExecutionMode.Serial
            };

            var orderedTasks = group.Tasks
                .OrderByDescending(t => t.PriorityScore)
                .ToList();

            foreach (var task in orderedTasks)
            {
                if (ct.IsCancellationRequested)
                {
                    task.Status = TaskStatus.Cancelled;
                    break;
                }

                var taskResult = await ExecuteSingleTaskAsync(task, ct);
                result.TaskResults.Add(taskResult);

                if (!taskResult.Success && task.ExecutionStrategy == ExecutionStrategy.Serial)
                {
                    // 串行任务失败，停止后续任务
                    result.Success = false;
                    result.Error = $"任务 {task.TaskId} 失败，终止串行组";
                    return result;
                }
            }

            result.Success = result.TaskResults.All(tr => tr.Success);
            return result;
        }

        private async Task<GroupExecutionResult> ExecuteParallelGroupAsync(
            TaskGroup group,
            CancellationToken ct)
        {
            var result = new GroupExecutionResult
            {
                GroupId = group.GroupId,
                ExecutionMode = GroupExecutionMode.Parallel
            };

            // 并行执行所有任务
            var tasks = group.Tasks.Select(async task =>
            {
                if (ct.IsCancellationRequested)
                {
                    task.Status = TaskStatus.Cancelled;
                    return null;
                }

                return await ExecuteSingleTaskAsync(task, ct);
            }).ToList();

            var taskResults = await Task.WhenAll(tasks);

            foreach (var taskResult in taskResults.Where(tr => tr != null))
            {
                result.TaskResults.Add(taskResult);
            }

            // 并行组允许部分失败
            result.Success = result.TaskResults.Any(tr => tr.Success);
            return result;
        }

        private async Task<TaskExecutionResult> ExecuteSingleTaskAsync(
            DecomposedTask task,
            CancellationToken ct)
        {
            _logger.LogInformation("执行任务：{TaskId} - {TaskName}", task.TaskId, task.TaskName);

            task.Status = TaskStatus.Running;
            task.StartedAt = DateTime.UtcNow;

            var result = new TaskExecutionResult
            {
                TaskId = task.TaskId,
                StartedAt = task.StartedAt.Value
            };

            try
            {
                // 超时控制
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(task.ResourceRequirements.MaxExecutionTime);

                // 委托给Agent执行器
                var agentResult = await _agentOrchestrator.ExecuteAgentTaskAsync(task, cts.Token);

                result.Success = agentResult.Success;
                result.Message = agentResult.Message;
                result.Data = agentResult.Data;
                result.CompletedAt = DateTime.UtcNow;

                task.Status = TaskStatus.Completed;
                task.CompletedAt = result.CompletedAt;
                task.Result = result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("任务 {TaskId} 执行超时", task.TaskId);
                task.Status = TaskStatus.Timeout;
                result.Success = false;
                result.Error = "任务执行超时";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务 {TaskId} 执行失败", task.TaskId);
                task.Status = TaskStatus.Failed;
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }
    }
}
```

### 5.2 实际案例分析

```csharp
// 案例1：智能家居晨间例程
public class MorningRoutineExample
{
    public async Task Example_MorningRoutine()
    {
        var tasks = new List<DecomposedTask>
        {
            // 任务1：打开客厅灯（高优先级，用户立即看到效果）
            new DecomposedTask
            {
                TaskId = "task_001",
                Intent = "lighting.control.turn_on",
                Description = "打开客厅灯",
                Priority = TaskPriority.High,
                PriorityReason = PriorityReason.UserInteraction,
                ExecutionStrategy = ExecutionStrategy.Immediate,
                Parameters = new Dictionary<string, object>
                {
                    ["room"] = "living_room",
                    ["device"] = "ceiling_light"
                }
            },

            // 任务2：设置空调温度（中优先级，舒适度相关）
            new DecomposedTask
            {
                TaskId = "task_002",
                Intent = "climate.control.set_temperature",
                Description = "设置空调温度为26度",
                Priority = TaskPriority.Normal,
                ExecutionStrategy = ExecutionStrategy.Parallel,
                Parameters = new Dictionary<string, object>
                {
                    ["room"] = "living_room",
                    ["temperature"] = 26
                }
            },

            // 任务3：播放音乐（中优先级，增强体验）
            new DecomposedTask
            {
                TaskId = "task_003",
                Intent = "music.play",
                Description = "播放轻音乐",
                Priority = TaskPriority.Normal,
                ExecutionStrategy = ExecutionStrategy.Parallel,
                Dependencies = new List<TaskDependency>
                {
                    // 依赖任务1完成（用户体验：先开灯再播放音乐）
                    new TaskDependency
                    {
                        DependsOnTaskId = "task_001",
                        Type = DependencyType.MustComplete
                    }
                }
            },

            // 任务4：打开窗帘（低优先级，后台执行）
            new DecomposedTask
            {
                TaskId = "task_004",
                Intent = "curtain.open",
                Description = "打开卧室窗帘",
                Priority = TaskPriority.Low,
                ExecutionStrategy = ExecutionStrategy.Delayed,
                Parameters = new Dictionary<string, object>
                {
                    ["room"] = "bedroom"
                }
            }
        };

        // 调度器分析
        var scheduler = new MafTaskScheduler(/* ... */);
        var plan = await scheduler.CreatePlanAsync(tasks, new SchedulingConstraints
        {
            MaxParallelTasks = 3
        });

        // 执行计划：
        // 第一组（并行）：task_001（开灯）
        // 第二组（并行）：task_002（空调）、task_003（音乐，依赖task_001）
        // 第三组（并行）：task_004（窗帘）
    }
}

// 案例2：紧急停止（安全关键）
public class EmergencyStopExample
{
    public void Example_EmergencyStop()
    {
        var emergencyTask = new DecomposedTask
        {
            TaskId = "emergency_001",
            Intent = "emergency.stop_all",
            Description = "紧急停止所有设备",
            Priority = TaskPriority.Critical,
            PriorityReason = PriorityReason.SafetyCritical,
            ExecutionStrategy = ExecutionStrategy.Immediate,
            ResourceRequirements = new ResourceRequirements
            {
                RequiresExclusiveAccess = true,  // 独占资源
                MaxExecutionTime = TimeSpan.FromSeconds(5)  // 快速执行
            }
        };

        // 关键任务会：
        // 1. 立即中断当前正在执行的任务
        // 2. 优先级最高，跳过队列
        // 3. 独占资源，确保执行
        // 4. 超时时间短（5秒）
    }
}
```

---

## 六、异常处理

### 6.1 任务失败处理策略

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 任务失败处理器
    /// </summary>
    public interface ITaskFailureHandler
    {
        /// <summary>
        /// 处理任务失败
        /// </summary>
        Task<FailureHandlingResult> HandleFailureAsync(
            DecomposedTask failedTask,
            Exception exception,
            ExecutionPlan plan,
            CancellationToken ct = default);
    }

    public class MafTaskFailureHandler : ITaskFailureHandler
    {
        private readonly ILogger<MafTaskFailureHandler> _logger;

        public async Task<FailureHandlingResult> HandleFailureAsync(
            DecomposedTask failedTask,
            Exception exception,
            ExecutionPlan plan,
            CancellationToken ct = default)
        {
            _logger.LogError(exception, "任务失败：{TaskId}", failedTask.TaskId);

            var result = new FailureHandlingResult
            {
                TaskId = failedTask.TaskId,
                Action = FailureAction.None
            };

            // 策略1：关键任务失败，终止整个计划
            if (failedTask.Priority == TaskPriority.Critical)
            {
                result.Action = FailureAction.AbortPlan;
                result.Reason = "关键任务失败，终止执行计划";
                return result;
            }

            // 策略2：检查是否有重试机会
            if (failedTask.ResourceRequirements != null &&
                failedTask.Result?.RetryCount < 3)
            {
                result.Action = FailureAction.Retry;
                result.Reason = "任务将重试";
                result.RetryDelay = TimeSpan.FromSeconds(5);
                return result;
            }

            // 策略3：检查是否有降级方案
            var fallbackTask = FindFallbackTask(failedTask);
            if (fallbackTask != null)
            {
                result.Action = FailureAction.ExecuteFallback;
                result.FallbackTask = fallbackTask;
                result.Reason = "执行降级方案";
                return result;
            }

            // 策略4：检查是否允许部分失败
            if (plan.AllowPartialExecution)
            {
                result.Action = FailureAction.Continue;
                result.Reason = "允许部分执行，继续执行后续任务";
                return result;
            }

            // 策略5：默认终止
            result.Action = FailureAction.AbortPlan;
            result.Reason = "任务失败且无法恢复";
            return result;
        }

        private DecomposedTask FindFallbackTask(DecomposedTask failedTask)
        {
            // 示例：灯光控制失败，降级为手动控制提示
            if (failedTask.Intent.Contains("lighting"))
            {
                return new DecomposedTask
                {
                    TaskId = $"{failedTask.TaskId}_fallback",
                    Intent = "notification.manual_control_required",
                    Description = "请手动控制灯光",
                    Priority = TaskPriority.Low,
                    ExecutionStrategy = ExecutionStrategy.Immediate
                };
            }

            return null;
        }
    }

    public enum FailureAction
    {
        None,            // 不处理
        Retry,           // 重试
        ExecuteFallback, // 执行降级方案
        Continue,        // 继续执行
        AbortPlan        // 终止计划
    }

    public class FailureHandlingResult
    {
        public string TaskId { get; set; }
        public FailureAction Action { get; set; }
        public string Reason { get; set; }
        public DecomposedTask FallbackTask { get; set; }
        public TimeSpan? RetryDelay { get; set; }
    }
}
```

### 6.2 任务超时处理

```csharp
namespace MultiAgentFramework.Core.Services
{
    /// <summary>
    /// 任务超时处理器
    /// </summary>
    public class TaskTimeoutHandler
    {
        public async Task HandleTimeoutAsync(DecomposedTask task, ExecutionPlan plan)
        {
            // 1. 标记任务超时
            task.Status = TaskStatus.Timeout;
            task.CompletedAt = DateTime.UtcNow;

            // 2. 检查是否有关联任务需要取消
            var dependentTasks = plan.AllTasks
                .Where(t => t.Dependencies.Any(d =>
                    d.DependsOnTaskId == task.TaskId &&
                    d.Type == DependencyType.MustSucceed))
                .ToList();

            foreach (var dependentTask in dependentTasks)
            {
                dependentTask.Status = TaskStatus.Cancelled;
            }

            // 3. 记录超时事件
            _logger.LogWarning(
                "任务 {TaskId} 执行超时，影响 {Count} 个依赖任务",
                task.TaskId,
                dependentTasks.Count);
        }
    }
}
```

---

## 七、代码实现

### 7.1 完整使用示例

```csharp
// 主任务分解
var mainTask = await mainAgent.DecomposeTaskAsync("我起床了", ct);

// 分析任务优先级和依赖关系
var priorityCalculator = new MafTaskPriorityCalculator(/* ... */);
var dependencyAnalyzer = new MafTaskDependencyAnalyzer();

// 计算每个任务的优先级分数
foreach (var task in mainTask.SubTasks)
{
    task.PriorityScore = priorityCalculator.CalculatePriorityScore(
        task,
        new TaskContext { IsInConversation = true, UserWaitingForResponse = true });
}

// 分析依赖关系
await dependencyAnalyzer.AnalyzeDependenciesAsync(mainTask.SubTasks, ct);

// 创建执行计划
var scheduler = new MafTaskScheduler(/* ... */);
var executionPlan = await scheduler.CreatePlanAsync(
    mainTask.SubTasks,
    new SchedulingConstraints
    {
        MaxParallelTasks = 5,
        MaxExecutionTime = TimeSpan.FromMinutes(5),
        AllowPartialExecution = true
    },
    ct);

// 执行计划
var executionEngine = new MafTaskExecutionEngine(/* ... */);
var result = await executionEngine.ExecutePlanAsync(executionPlan, ct);

// 处理结果
if (result.Success)
{
    await NotifySuccessAsync(result);
}
else
{
    await NotifyFailureAsync(result);
}
```

### 7.2 监控和追踪

```csharp
// 任务执行监控
public class TaskExecutionMonitor
{
    public void SubscribeToTaskEvents(ExecutionPlan plan)
    {
        foreach (var task in plan.AllTasks)
        {
            task.StatusChanged += (sender, args) =>
            {
                _logger.LogInformation(
                    "任务状态变更：{TaskId} - {OldStatus} → {NewStatus}",
                    task.TaskId,
                    args.OldStatus,
                    args.NewStatus);

                // 实时推送SignalR通知
                _signalRHub.SendTaskUpdateAsync(task.TaskId, args.NewStatus);
            };
        }
    }

    public TaskMetrics CalculateMetrics(ExecutionPlan plan)
    {
        var completedTasks = plan.AllTasks.Where(t => t.Status == TaskStatus.Completed);
        var failedTasks = plan.AllTasks.Where(t => t.Status == TaskStatus.Failed);

        return new TaskMetrics
        {
            TotalTasks = plan.AllTasks.Count,
            CompletedTasks = completedTasks.Count(),
            FailedTasks = failedTasks.Count(),
            AverageExecutionTime = completedTasks.Average(t =>
                (t.CompletedAt - t.StartedAt)?.TotalSeconds ?? 0),
            SuccessRate = (double)completedTasks.Count() / plan.AllTasks.Count * 100
        };
    }
}
```

---

## 📊 总结

### 核心设计原则

1. **优先级多维评分**
   - 基础优先级 + 用户交互 + 时间因素 + 资源利用率 + 依赖传播
   - 分数范围：0-100

2. **依赖关系管理**
   - 5种依赖类型：MustComplete、MustSucceed、MustStart、DataDependency、SoftDependency
   - 自动识别隐式依赖
   - 循环依赖检测

3. **智能调度算法**
   - 拓扑排序满足依赖关系
   - 并行组识别和优化
   - 资源约束考虑

4. **灵活执行策略**
   - 串行/并行/延迟/条件执行
   - 部分失败容错
   - 超时控制和重试机制

### 关键优势

- ✅ **用户体验优先**：用户交互任务优先级自动提升
- ✅ **安全性保障**：关键任务独占资源，快速执行
- ✅ **高效并行**：自动识别可并行任务组
- ✅ **容错机制**：部分失败不影响整体执行
- ✅ **可扩展性**：支持自定义优先级规则和调度策略

---

**文档版本**: v1.2
**最后更新**: 2026-03-13

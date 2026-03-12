using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Core.Agents
{
    /// <summary>
    /// CKY.MAF Agent增强基类
    /// 提供会话存储、优先级计算和监控功能
    /// 注意：实际应用中此类应继承自MS Agent Framework的AIAgent
    /// </summary>
    public abstract class MafAgentBase
    {
        /// <summary>会话存储（CKY.MAF增强）</summary>
        protected readonly IMafSessionStorage SessionStorage;

        /// <summary>优先级计算器（CKY.MAF增强）</summary>
        protected readonly IPriorityCalculator PriorityCalculator;

        /// <summary>监控指标收集器（CKY.MAF增强）</summary>
        protected readonly IMetricsCollector MetricsCollector;

        /// <summary>日志记录器</summary>
        protected readonly ILogger Logger;

        /// <summary>Agent当前状态</summary>
        public MafAgentStatus Status { get; private set; } = MafAgentStatus.Initializing;

        /// <summary>最后健康检查时间</summary>
        public DateTime? LastHealthCheck { get; private set; }

        /// <summary>Agent统计信息</summary>
        public AgentStatistics Statistics { get; } = new();

        /// <summary>Agent唯一标识</summary>
        public abstract string AgentId { get; }

        /// <summary>Agent名称</summary>
        public abstract string Name { get; }

        /// <summary>Agent描述</summary>
        public abstract string Description { get; }

        /// <summary>Agent版本</summary>
        public virtual string Version => "1.0.0";

        /// <summary>Agent能力列表</summary>
        public abstract IReadOnlyList<string> Capabilities { get; }

        protected MafAgentBase(
            IMafSessionStorage sessionStorage,
            IPriorityCalculator priorityCalculator,
            IMetricsCollector metricsCollector,
            ILogger logger)
        {
            SessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
            PriorityCalculator = priorityCalculator ?? throw new ArgumentNullException(nameof(priorityCalculator));
            MetricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行任务（模板方法）
        /// </summary>
        public async Task<MafTaskResponse> ExecuteAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
        {
            Status = MafAgentStatus.Busy;
            var startTime = DateTime.UtcNow;

            try
            {
                Logger.LogInformation("Agent {AgentName} starting task {TaskId}", Name, request.TaskId);

                // 1. 前置处理（子类可重写）
                await OnBeforeExecuteAsync(request, ct);

                // 2. 加载会话上下文（CKY.MAF增强）
                var session = await SessionStorage.LoadSessionAsync(request.ConversationId, ct);

                // 3. 执行业务逻辑（子类必须实现）
                var result = await ExecuteBusinessLogicAsync(request, session, ct);

                // 4. 保存会话上下文（CKY.MAF增强）
                await SessionStorage.SaveSessionAsync(session, ct);

                // 5. 后置处理（子类可重写）
                await OnAfterExecuteAsync(request, result, ct);

                // 6. 更新统计
                Statistics.TotalExecutions++;
                Statistics.SuccessfulExecutions++;
                Statistics.LastExecutionTime = DateTime.UtcNow;

                // 7. 记录指标（CKY.MAF增强）
                await MetricsCollector.RecordExecutionAsync(Name, startTime, result.Success, ct);

                Logger.LogInformation("Agent {AgentName} completed task {TaskId}", Name, request.TaskId);

                return result;
            }
            catch (Exception ex)
            {
                Statistics.TotalExecutions++;
                Statistics.FailedExecutions++;

                Logger.LogError(ex, "Agent {AgentName} failed task {TaskId}", Name, request.TaskId);
                await MetricsCollector.RecordErrorAsync(Name, ex, ct);

                return await HandleExceptionAsync(request, ex, ct);
            }
            finally
            {
                Status = MafAgentStatus.Idle;
            }
        }

        /// <summary>
        /// 子类实现具体的业务逻辑
        /// </summary>
        protected abstract Task<MafTaskResponse> ExecuteBusinessLogicAsync(
            MafTaskRequest request,
            IAgentSession session,
            CancellationToken ct = default);

        /// <summary>
        /// 执行前置处理（子类可重写）
        /// </summary>
        protected virtual Task OnBeforeExecuteAsync(
            MafTaskRequest request,
            CancellationToken ct = default)
            => Task.CompletedTask;

        /// <summary>
        /// 执行后置处理（子类可重写）
        /// </summary>
        protected virtual Task OnAfterExecuteAsync(
            MafTaskRequest request,
            MafTaskResponse result,
            CancellationToken ct = default)
            => Task.CompletedTask;

        /// <summary>
        /// 异常处理（子类可重写）
        /// </summary>
        protected virtual Task<MafTaskResponse> HandleExceptionAsync(
            MafTaskRequest request,
            Exception exception,
            CancellationToken ct = default)
        {
            return Task.FromResult(new MafTaskResponse
            {
                TaskId = request.TaskId,
                Success = false,
                Error = exception.Message,
                Result = $"处理您的请求时遇到问题：{exception.Message}"
            });
        }

        /// <summary>
        /// 初始化Agent
        /// </summary>
        public virtual Task InitializeAsync(CancellationToken ct = default)
        {
            Status = MafAgentStatus.Idle;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 关闭Agent
        /// </summary>
        public virtual Task ShutdownAsync(CancellationToken ct = default)
        {
            Status = MafAgentStatus.Shutdown;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        public virtual Task<AgentHealthReport> CheckHealthAsync(CancellationToken ct = default)
        {
            LastHealthCheck = DateTime.UtcNow;
            return Task.FromResult(new AgentHealthReport
            {
                AgentId = AgentId,
                Status = Status == MafAgentStatus.Error ? MafHealthStatus.Unhealthy : MafHealthStatus.Healthy,
                CheckedAt = LastHealthCheck.Value,
                Description = $"Agent {Name} is {Status}"
            });
        }
    }
}

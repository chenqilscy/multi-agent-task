using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Orchestration;
using CKY.MultiAgentFramework.Services.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services
{
    /// <summary>
    /// 持久化任务服务
    /// 整合 TaskScheduler、TaskOrchestrator 和持久化能力
    /// </summary>
    public class PersistentTaskService
    {
        private readonly ITaskScheduler _scheduler;
        private readonly ITaskOrchestrator _orchestrator;
        private readonly IMainTaskRepository _mainTaskRepository;
        private readonly ISubTaskRepository _subTaskRepository;
        private readonly ILogger<PersistentTaskService> _logger;

        public PersistentTaskService(
            ITaskScheduler scheduler,
            ITaskOrchestrator orchestrator,
            IMainTaskRepository mainTaskRepository,
            ISubTaskRepository subTaskRepository,
            ILogger<PersistentTaskService> logger)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _mainTaskRepository = mainTaskRepository ?? throw new ArgumentNullException(nameof(mainTaskRepository));
            _subTaskRepository = subTaskRepository ?? throw new ArgumentNullException(nameof(subTaskRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 提交并执行任务
        /// 整合调度、编排和持久化
        /// </summary>
        public async Task<MafTaskResponse> SubmitAndExecuteAsync(
            MafTaskRequest request,
            TaskDecomposition decomposition,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Submitting task {TaskId} with persistence", request.TaskId);

            try
            {
                // 1. 保存主任务和子任务到数据库
                var (mainTask, subTasks) = Services.Mapping.TaskMapper.CreateFromDecomposition(decomposition);
                mainTask = await _mainTaskRepository.AddAsync(mainTask, ct);
                _logger.LogInformation("Main task saved with ID {TaskId}", mainTask.Id);

                // 更新子任务的外键并保存
                foreach (var subTask in subTasks)
                {
                    subTask.MainTaskId = mainTask.Id;
                }
                await _subTaskRepository.AddRangeAsync(subTasks, ct);
                _logger.LogInformation("SubTasks saved for main task {TaskId}", mainTask.Id);

                // 2. 调度任务
                var scheduleResult = await _scheduler.ScheduleAsync(decomposition.SubTasks, ct);
                _logger.LogInformation("Tasks scheduled for plan {PlanId}", scheduleResult.ExecutionPlan.PlanId);

                // 3. 创建执行计划
                var executionPlan = await _orchestrator.CreatePlanAsync(decomposition.SubTasks, ct);
                _logger.LogInformation("Execution plan created: {PlanId}", executionPlan.PlanId);

                // 4. 执行任务
                var executionResults = await _orchestrator.ExecutePlanAsync(executionPlan, ct);
                _logger.LogInformation("Task execution completed. Total: {Total}, Success: {Success}",
                    executionResults.Count, executionResults.Count(r => r.Success));

                // 5. 更新主任务状态
                var allSuccess = executionResults.All(r => r.Success);
                mainTask.Status = allSuccess ? MafTaskStatus.Completed : MafTaskStatus.Failed;
                mainTask.UpdatedAt = DateTime.UtcNow;
                await _mainTaskRepository.UpdateAsync(mainTask, ct);

                // 6. 构建响应
                var response = new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = allSuccess,
                    Result = allSuccess ? "任务执行成功" : "任务部分失败",
                    SubTaskResults = executionResults.Select(r => new SubTaskResult
                    {
                        TaskId = r.TaskId,
                        Success = r.Success,
                        Message = r.Message,
                        Error = r.Error
                    }).ToList()
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute task {TaskId}", request.TaskId);
                return new MafTaskResponse
                {
                    TaskId = request.TaskId,
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 查询任务状态
        /// </summary>
        public async Task<(MainTask? MainTask, List<SubTask> SubTasks)> GetTaskStatusAsync(
            int mainTaskId,
            CancellationToken ct = default)
        {
            var mainTask = await _mainTaskRepository.GetByIdAsync(mainTaskId, ct);
            if (mainTask == null)
            {
                return (null, new List<SubTask>());
            }

            var subTasks = await _subTaskRepository.GetByMainTaskIdAsync(mainTaskId, ct);
            return (mainTask, subTasks);
        }

        /// <summary>
        /// 获取任务列表
        /// </summary>
        public async Task<List<MainTask>> GetTasksAsync(
            MafTaskStatus? status = null,
            CancellationToken ct = default)
        {
            if (status.HasValue)
            {
                return await _mainTaskRepository.GetByStatusAsync(status.Value, ct);
            }

            return await _mainTaskRepository.GetAllAsync(ct);
        }
    }

    /// <summary>
    /// 依赖注入扩展
    /// </summary>
    public static class PersistentTaskServiceExtensions
    {
        /// <summary>
        /// 注册持久化任务服务
        /// </summary>
        public static IServiceCollection AddPersistentTaskServices(
            this IServiceCollection services)
        {
            // 核心服务（无持久化）
            services.AddSingleton<IPriorityCalculator, MafPriorityCalculator>();

            // 基础 TaskScheduler 和 TaskOrchestrator
            services.AddSingleton<ITaskScheduler, MafTaskScheduler>();
            services.AddSingleton<ITaskOrchestrator, MafTaskOrchestrator>();

            // 如果需要持久化，使用 PersistentTaskScheduler 和 PersistentTaskOrchestrator 包装
            // services.Decorate<ITaskScheduler, PersistentTaskScheduler>();
            // services.Decorate<ITaskOrchestrator, PersistentTaskOrchestrator>();

            // 持久化任务服务
            services.AddSingleton<PersistentTaskService>();

            return services;
        }

        /// <summary>
        /// 注册带持久化的任务服务
        /// </summary>
        public static IServiceCollection AddPersistentTaskServicesWithPersistence(
            this IServiceCollection services)
        {
            // 核心服务
            services.AddSingleton<IPriorityCalculator, MafPriorityCalculator>();

            // 基础实现
            services.AddSingleton<ITaskScheduler, MafTaskScheduler>();
            services.AddSingleton<ITaskOrchestrator, MafTaskOrchestrator>();

            // 持久化包装器
            services.AddSingleton<PersistentTaskScheduler>();
            services.AddSingleton<PersistentTaskOrchestrator>();

            // 重新注册接口，指向持久化实现
            services.AddSingleton<ITaskScheduler>(sp =>
            {
                var inner = sp.GetRequiredService<MafTaskScheduler>();
                var planRepo = sp.GetRequiredService<ISchedulePlanRepository>();
                var logger = sp.GetRequiredService<ILogger<PersistentTaskScheduler>>();
                return new PersistentTaskScheduler(inner, planRepo, logger);
            });

            services.AddSingleton<ITaskOrchestrator>(sp =>
            {
                var inner = sp.GetRequiredService<MafTaskOrchestrator>();
                var planRepo = sp.GetRequiredService<IExecutionPlanRepository>();
                var resultRepo = sp.GetRequiredService<ITaskExecutionResultRepository>();
                var logger = sp.GetRequiredService<ILogger<PersistentTaskOrchestrator>>();
                return new PersistentTaskOrchestrator(inner, planRepo, resultRepo, logger);
            });

            // 持久化任务服务
            services.AddSingleton<PersistentTaskService>();

            return services;
        }
    }
}

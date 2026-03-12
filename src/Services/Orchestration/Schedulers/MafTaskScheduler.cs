using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration.Schedulers
{
    /// <summary>
    /// 优先级计算器实现
    /// </summary>
    public class TaskPriorityCalculator : IPriorityCalculator
    {
        /// <inheritdoc />
        public int CalculateScore(DecomposedTask task)
        {
            // 基础分数（由优先级枚举决定）
            var baseScore = (int)task.Priority * 20;

            // 依赖紧迫性加分
            var dependencyBonus = task.Dependencies.Any() ? 5 : 0;

            // 已等待时间加分（每等待10秒加1分，最多加20分）
            var waitBonus = Math.Min(20, (int)(DateTime.UtcNow - task.CreatedAt).TotalSeconds / 10);

            return Math.Min(100, baseScore + dependencyBonus + waitBonus);
        }

        /// <inheritdoc />
        public int Compare(DecomposedTask task1, DecomposedTask task2)
        {
            var score1 = CalculateScore(task1);
            var score2 = CalculateScore(task2);
            return score2.CompareTo(score1); // 降序排列（高分在前）
        }
    }

    /// <summary>
    /// 任务调度器实现
    /// </summary>
    public class MafTaskScheduler : ITaskScheduler
    {
        private readonly ICacheStore _cacheStore;
        private readonly IRelationalDatabase _database;
        private readonly IPriorityCalculator _priorityCalculator;
        private readonly ILogger<MafTaskScheduler> _logger;

        private const string PendingTasksKey = "maf:pending_tasks";

        public MafTaskScheduler(
            ICacheStore cacheStore,
            IRelationalDatabase database,
            IPriorityCalculator priorityCalculator,
            ILogger<MafTaskScheduler> logger)
        {
            _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _priorityCalculator = priorityCalculator ?? throw new ArgumentNullException(nameof(priorityCalculator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ScheduleAsync(DecomposedTask task, CancellationToken ct = default)
        {
            _logger.LogInformation("Scheduling task {TaskId} with priority {Priority}", task.TaskId, task.Priority);

            task.PriorityScore = _priorityCalculator.CalculateScore(task);
            task.Status = MafTaskStatus.Scheduled;

            // 保存到缓存（快速访问）
            await _cacheStore.SetAsync($"task:{task.TaskId}", task, TimeSpan.FromHours(1), ct);

            // 持久化到数据库
            await _database.InsertAsync(task, ct);

            _logger.LogDebug("Task {TaskId} scheduled with score {Score}", task.TaskId, task.PriorityScore);
        }

        /// <inheritdoc />
        public async Task CancelAsync(string taskId, CancellationToken ct = default)
        {
            _logger.LogInformation("Cancelling task {TaskId}", taskId);

            var task = await _cacheStore.GetAsync<DecomposedTask>($"task:{taskId}", ct);
            if (task != null)
            {
                task.Status = MafTaskStatus.Cancelled;
                await _cacheStore.SetAsync($"task:{taskId}", task, TimeSpan.FromHours(1), ct);
                await _database.UpdateAsync(task, ct);
            }
        }

        /// <inheritdoc />
        public async Task<List<DecomposedTask>> GetPendingTasksAsync(CancellationToken ct = default)
        {
            var tasks = await _database.GetListAsync<DecomposedTask>(
                t => t.Status == MafTaskStatus.Pending || t.Status == MafTaskStatus.Scheduled,
                ct);

            return tasks.OrderBy(t => t, Comparer<DecomposedTask>.Create(_priorityCalculator.Compare)).ToList();
        }
    }
}

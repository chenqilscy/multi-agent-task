using CKY.MultiAgentFramework.Core.Constants;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Services.Mapping
{
    /// <summary>
    /// 任务实体映射器
    /// 负责在领域模型（DecomposedTask）和持久化模型（MainTask/SubTask）之间转换
    /// </summary>
    public static class TaskMapper
    {
        /// <summary>
        /// 将 DecomposedTask 转换为主任务实体（用于持久化）
        /// </summary>
        public static MainTask ToMainTaskEntity(DecomposedTask task)
        {
            return new MainTask
            {
                Id = 0, // 数据库生成
                Title = task.TaskName,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.StartedAt
            };
        }

        /// <summary>
        /// 将主任务实体转换为 DecomposedTask（从持久化恢复）
        /// </summary>
        public static DecomposedTask FromMainTaskEntity(MainTask entity)
        {
            return new DecomposedTask
            {
                TaskId = entity.Id.ToString(),
                TaskName = entity.Title,
                Description = entity.Description ?? string.Empty,
                Intent = string.Empty, // 需要从其他地方恢复
                Priority = entity.Priority,
                PriorityScore = PersistenceConstants.Defaults.DefaultPriorityScore, // 默认值
                PriorityReason = PriorityReason.SystemDefault,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                StartedAt = null, // 没有单独字段
                CompletedAt = null // 没有单独字段
            };
        }

        /// <summary>
        /// 将 DecomposedTask 转换为子任务实体（用于持久化）
        /// </summary>
        /// <param name="task">分解后的任务</param>
        /// <param name="mainTaskId">主任务ID</param>
        /// <param name="executionOrder">执行顺序</param>
        public static SubTask ToSubTaskEntity(DecomposedTask task, int mainTaskId, int executionOrder)
        {
            return new SubTask
            {
                Id = 0, // 数据库生成
                MainTaskId = mainTaskId,
                Title = task.TaskName,
                Description = task.Description,
                Status = task.Status,
                ExecutionOrder = executionOrder
            };
        }

        /// <summary>
        /// 将子任务实体转换为 DecomposedTask（从持久化恢复）
        /// </summary>
        public static DecomposedTask FromSubTaskEntity(SubTask entity)
        {
            return new DecomposedTask
            {
                TaskId = entity.Id.ToString(),
                TaskName = entity.Title,
                Description = entity.Description ?? string.Empty,
                Intent = string.Empty,
                Priority = TaskPriority.Normal,
                PriorityScore = 50,
                PriorityReason = PriorityReason.SystemDefault,
                Status = entity.Status,
                CreatedAt = DateTime.UtcNow // 子任务没有创建时间字段
            };
        }

        /// <summary>
        /// 更新主任务实体的状态
        /// </summary>
        public static void UpdateMainTaskStatus(MainTask entity, DecomposedTask task)
        {
            entity.Status = task.Status;
            entity.UpdatedAt = task.StartedAt ?? task.CompletedAt;

            // 如果任务完成，更新完成时间（存储在 UpdatedAt 字段）
            if (task.Status == MafTaskStatus.Completed || task.Status == MafTaskStatus.Failed)
            {
                entity.UpdatedAt = task.CompletedAt;
            }
        }

        /// <summary>
        /// 更新子任务实体的状态
        /// </summary>
        public static void UpdateSubTaskStatus(SubTask entity, DecomposedTask task)
        {
            entity.Status = task.Status;
        }

        /// <summary>
        /// 批量转换：从任务分解结果创建主任务和子任务列表
        /// </summary>
        /// <param name="decomposition">任务分解结果</param>
        /// <returns>主任务和子任务列表</returns>
        public static (MainTask MainTask, List<SubTask> SubTasks) CreateFromDecomposition(
            TaskDecomposition decomposition)
        {
            var mainTask = new MainTask
            {
                Id = 0,
                Title = $"任务: {decomposition.OriginalUserInput[..Math.Min(50, decomposition.OriginalUserInput.Length)]}",
                Description = decomposition.OriginalUserInput,
                Priority = TaskPriority.Normal,
                Status = MafTaskStatus.Pending,
                CreatedAt = decomposition.Metadata.DecomposedAt
            };

            var subTasks = new List<SubTask>();
            for (int i = 0; i < decomposition.SubTasks.Count; i++)
            {
                var subTask = ToSubTaskEntity(decomposition.SubTasks[i], 0, i + 1);
                subTasks.Add(subTask);
            }

            return (mainTask, subTasks);
        }

        /// <summary>
        /// 批量转换：从主任务和子任务列表恢复任务分解结果
        /// </summary>
        public static TaskDecomposition RestoreToDecomposition(
            MainTask mainTask,
            List<SubTask> subTasks)
        {
            var decomposition = new TaskDecomposition
            {
                OriginalUserInput = mainTask.Description ?? string.Empty,
                SubTasks = new List<DecomposedTask>(),
                Metadata = new DecompositionMetadata
                {
                    DecomposedAt = mainTask.CreatedAt,
                    ElapsedMs = 0,
                    Strategy = PersistenceConstants.Strategies.RestoreFromPersistence
                }
            };

            foreach (var subTask in subTasks.OrderBy(st => st.ExecutionOrder))
            {
                var decomposedTask = FromSubTaskEntity(subTask);
                decomposition.SubTasks.Add(decomposedTask);
            }

            return decomposition;
        }
    }
}

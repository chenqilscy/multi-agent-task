using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 任务分解器接口
    /// 将用户输入的复杂任务分解为多个可独立执行的子任务
    /// </summary>
    public interface ITaskDecomposer
    {
        /// <summary>
        /// 分解任务
        /// </summary>
        Task<TaskDecomposition> DecomposeAsync(
            string userInput,
            IntentRecognitionResult intent,
            CancellationToken ct = default);
    }
}

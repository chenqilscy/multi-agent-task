namespace CKY.MultiAgentFramework.Core.Models.Task
{
    /// <summary>
    /// 任务分解结果
    /// </summary>
    public class TaskDecomposition
    {
        /// <summary>分解ID</summary>
        public string DecompositionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>原始用户输入</summary>
        public string OriginalUserInput { get; set; } = string.Empty;

        /// <summary>意图识别结果</summary>
        /// <remarks>
        /// 使用 <see cref="CKY.MultiAgentFramework.Core.Abstractions.IntentRecognitionResult"/> 类型，
        /// 声明为 object? 以避免 Core/Models 层对 Abstractions 层的循环依赖。
        /// </remarks>
        public object? Intent { get; set; }

        /// <summary>子任务列表</summary>
        public List<DecomposedTask> SubTasks { get; set; } = new();

        /// <summary>分解元数据</summary>
        public DecompositionMetadata Metadata { get; set; } = new();
    }
}

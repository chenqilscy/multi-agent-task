using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 结果聚合结果
    /// </summary>
    public class AggregatedResult
    {
        /// <summary>整体是否成功</summary>
        public bool Success { get; set; }

        /// <summary>各子任务的执行结果</summary>
        public List<TaskExecutionResult> IndividualResults { get; set; } = new();

        /// <summary>聚合数据</summary>
        public Dictionary<string, object> AggregatedData { get; set; } = new();

        /// <summary>摘要信息</summary>
        public string? Summary { get; set; }
    }

    /// <summary>
    /// 结果聚合器接口
    /// 将多个子任务的执行结果聚合成统一的用户响应
    /// </summary>
    public interface IResultAggregator
    {
        /// <summary>
        /// 聚合多个子任务结果
        /// </summary>
        Task<AggregatedResult> AggregateAsync(
            List<TaskExecutionResult> results,
            string originalUserInput,
            CancellationToken ct = default);

        /// <summary>
        /// 生成自然语言响应
        /// </summary>
        Task<string> GenerateResponseAsync(
            AggregatedResult aggregatedResult,
            CancellationToken ct = default);
    }
}

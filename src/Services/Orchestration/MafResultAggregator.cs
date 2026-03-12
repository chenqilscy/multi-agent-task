using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Task;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Orchestration
{
    /// <summary>
    /// 结果聚合服务
    /// 将多个子任务的执行结果聚合成统一的用户响应
    /// </summary>
    public class MafResultAggregator : IResultAggregator
    {
        private readonly ILogger<MafResultAggregator> _logger;

        public MafResultAggregator(ILogger<MafResultAggregator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<AggregatedResult> AggregateAsync(
            List<TaskExecutionResult> results,
            string originalUserInput,
            CancellationToken ct = default)
        {
            _logger.LogDebug("Aggregating {Count} task results", results.Count);

            var aggregated = new AggregatedResult
            {
                IndividualResults = results,
                Success = results.All(r => r.Success)
            };

            // 汇总数据
            foreach (var result in results)
            {
                if (result.Data != null)
                {
                    aggregated.AggregatedData[result.TaskId] = result.Data;
                }
            }

            var successCount = results.Count(r => r.Success);
            var totalCount = results.Count;

            aggregated.Summary = totalCount == 0
                ? "没有任务需要执行"
                : successCount == totalCount
                    ? $"所有 {totalCount} 个任务已成功完成"
                    : $"{successCount}/{totalCount} 个任务成功完成";

            return Task.FromResult(aggregated);
        }

        /// <inheritdoc />
        public Task<string> GenerateResponseAsync(
            AggregatedResult aggregatedResult,
            CancellationToken ct = default)
        {
            if (!aggregatedResult.IndividualResults.Any())
            {
                return Task.FromResult("已收到您的请求，但没有找到可执行的任务。");
            }

            var successTasks = aggregatedResult.IndividualResults.Where(r => r.Success).ToList();
            var failedTasks = aggregatedResult.IndividualResults.Where(r => !r.Success).ToList();

            var responseBuilder = new System.Text.StringBuilder();

            if (successTasks.Any())
            {
                responseBuilder.Append("好的！");
                foreach (var task in successTasks)
                {
                    if (!string.IsNullOrEmpty(task.Message))
                    {
                        responseBuilder.Append(task.Message);
                        responseBuilder.Append("；");
                    }
                }
            }

            if (failedTasks.Any())
            {
                responseBuilder.Append($"但以下任务失败：{string.Join("、", failedTasks.Select(t => t.Error ?? t.TaskId))}");
            }

            var response = responseBuilder.ToString().TrimEnd('；');
            return Task.FromResult(string.IsNullOrEmpty(response) ? aggregatedResult.Summary ?? "操作完成" : response);
        }
    }
}

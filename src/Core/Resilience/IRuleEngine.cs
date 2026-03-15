using CKY.MultiAgentFramework.Core.Models.Task;

namespace CKY.MultiAgentFramework.Core.Resilience
{
    /// <summary>
    /// 规则引擎接口（Level 5 降级时替代 LLM）
    /// </summary>
    public interface IRuleEngine
    {
        /// <summary>是否能处理给定输入</summary>
        bool CanHandle(string userInput);

        /// <summary>使用规则引擎处理请求（无 LLM 调用）</summary>
        Task<MafTaskResponse> ProcessAsync(MafTaskRequest request, CancellationToken ct = default);
    }
}

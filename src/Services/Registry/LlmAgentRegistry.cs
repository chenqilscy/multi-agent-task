using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Registry
{
    /// <summary>
    /// LLM Agent 注册表（转发到 Orchestration 层的统一实现）
    /// </summary>
    /// <remarks>
    /// 此类保留是为了向后兼容。实际实现在 <see cref="Orchestration.MafAiAgentRegistry"/>。
    /// 新代码应直接使用 <c>CKY.MultiAgentFramework.Services.Orchestration.MafAiAgentRegistry</c>。
    /// </remarks>
    public class MafAiAgentRegistry : Orchestration.MafAiAgentRegistry
    {
        public MafAiAgentRegistry(ILogger<MafAiAgentRegistry> logger)
            : base(logger)
        {
        }
    }
}

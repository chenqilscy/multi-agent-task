using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Agent;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// Agent注册信息
    /// </summary>
    public class AgentRegistration
    {
        /// <summary>Agent ID</summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>Agent名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Agent描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>版本</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>能力列表</summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>当前状态</summary>
        public MafAgentStatus Status { get; set; } = MafAgentStatus.Idle;

        /// <summary>注册时间</summary>
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>最后心跳时间</summary>
        public DateTime? LastHeartbeat { get; set; }

        /// <summary>元数据</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Agent注册表接口
    /// 管理系统中的所有Agent注册信息
    /// </summary>
    public interface IAgentRegistry
    {
        /// <summary>
        /// 注册Agent
        /// </summary>
        Task RegisterAsync(
            AgentRegistration registration,
            CancellationToken ct = default);

        /// <summary>
        /// 注销Agent
        /// </summary>
        Task UnregisterAsync(
            string agentId,
            CancellationToken ct = default);

        /// <summary>
        /// 根据能力查找Agent注册信息
        /// </summary>
        Task<AgentRegistration?> FindByCapabilityAsync(
            string capability,
            CancellationToken ct = default);

        /// <summary>
        /// 根据ID查找Agent注册信息
        /// </summary>
        Task<AgentRegistration?> FindByIdAsync(
            string agentId,
            CancellationToken ct = default);

        /// <summary>
        /// 获取所有Agent注册信息
        /// </summary>
        Task<List<AgentRegistration>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// 更新Agent状态
        /// </summary>
        Task UpdateStatusAsync(
            string agentId,
            MafAgentStatus status,
            CancellationToken ct = default);
    }
}

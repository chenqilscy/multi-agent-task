using System.Diagnostics;

namespace CKY.MultiAgentFramework.Core.Diagnostics
{
    /// <summary>
    /// CKY.MAF 分布式追踪 ActivitySource 定义
    /// 用于链路追踪：Agent 调用、任务执行、LLM 调用等关键路径
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// <code>
    /// using var activity = MafActivitySource.Agent.StartActivity("agent.execute");
    /// activity?.SetTag("agent.id", agentId);
    /// </code>
    ///
    /// 在 Demo 应用中配置 OpenTelemetry 导出：
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(tracing => tracing
    ///         .AddSource(MafActivitySource.AgentSourceName)
    ///         .AddSource(MafActivitySource.TaskSourceName)
    ///         .AddSource(MafActivitySource.LlmSourceName)
    ///         .AddOtlpExporter());
    /// </code>
    /// </remarks>
    public static class MafActivitySource
    {
        /// <summary>Agent 执行追踪 Source 名称</summary>
        public const string AgentSourceName = "CKY.MAF.Agent";

        /// <summary>任务调度/编排追踪 Source 名称</summary>
        public const string TaskSourceName = "CKY.MAF.Task";

        /// <summary>LLM API 调用追踪 Source 名称</summary>
        public const string LlmSourceName = "CKY.MAF.LLM";

        /// <summary>Agent 执行链路追踪</summary>
        public static readonly ActivitySource Agent = new(AgentSourceName, "1.0.0");

        /// <summary>任务调度/编排链路追踪</summary>
        public static readonly ActivitySource Task = new(TaskSourceName, "1.0.0");

        /// <summary>LLM API 调用链路追踪</summary>
        public static readonly ActivitySource Llm = new(LlmSourceName, "1.0.0");

        /// <summary>
        /// 获取所有 Source 名称（用于 OpenTelemetry 注册）
        /// </summary>
        public static readonly string[] AllSourceNames =
        [
            AgentSourceName,
            TaskSourceName,
            LlmSourceName
        ];
    }
}

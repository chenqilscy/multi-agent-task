namespace CKY.MultiAgentFramework.Services.Monitoring
{
    /// <summary>
    /// Prometheus 指标收集器接口
    /// 基于 System.Diagnostics.Metrics API 的同步接口
    /// </summary>
    public interface IPrometheusMetricsCollector
    {
        /// <summary>
        /// 记录计数器指标
        /// </summary>
        void IncrementCounter(string name, double value = 1, string[]? labels = null);

        /// <summary>
        /// 记录直方图指标
        /// </summary>
        void RecordHistogram(string name, double value, string[]? labels = null);

        /// <summary>
        /// 记录仪表指标
        /// </summary>
        void RecordGauge(string name, double value, string[]? labels = null);

        /// <summary>
        /// 记录摘要指标
        /// </summary>
        void RecordSummary(string name, double value, string[]? labels = null);
    }

    /// <summary>
    /// MAF 框架标准指标名称
    /// </summary>
    public static class MafMetrics
    {
        // 请求相关
        public const string HttpRequestTotal = "maf_http_requests_total";
        public const string HttpRequestDuration = "maf_http_request_duration_seconds";
        public const string HttpRequestInProgress = "maf_http_requests_in_progress";

        // Agent 相关
        public const string AgentExecutionTotal = "maf_agent_executions_total";
        public const string AgentExecutionDuration = "maf_agent_execution_duration_seconds";
        public const string AgentExecutionErrors = "maf_agent_execution_errors_total";

        // 任务相关
        public const string TaskCreatedTotal = "maf_task_created_total";
        public const string TaskCompletedTotal = "maf_task_completed_total";
        public const string TaskFailedTotal = "maf_task_failed_total";
        public const string TaskDuration = "maf_task_duration_seconds";

        // 缓存相关
        public const string CacheHitsTotal = "maf_cache_hits_total";
        public const string CacheMissesTotal = "maf_cache_misses_total";
        public const string CacheDuration = "maf_cache_duration_seconds";

        // LLM 调用相关
        public const string LlmRequestsTotal = "maf_llm_requests_total";
        public const string LlmCallsTotal = "maf_llm_calls_total";
        public const string LlmRequestDuration = "maf_llm_request_duration_seconds";
        public const string LlmLatencySeconds = "maf_llm_latency_seconds";
        public const string LlmRequestErrors = "maf_llm_request_errors_total";
        public const string LlmTokensUsed = "maf_llm_tokens_used_total";
        public const string LlmPromptTokensTotal = "maf_llm_prompt_tokens_total";
        public const string LlmResponseTokensTotal = "maf_llm_response_tokens_total";

        // 降级相关
        public const string DegradationLevel = "maf_degradation_level";

        // 任务并发相关
        public const string TaskConcurrentExecutions = "maf_task_concurrent_executions";

        // 系统资源相关
        public const string MemoryUsage = "maf_memory_usage_bytes";
        public const string CpuUsage = "maf_cpu_usage_percent";
        public const string GcCount = "maf_gc_count_total";
        public const string GcDuration = "maf_gc_duration_seconds";

        // SignalR 连接相关
        public const string SignalRConnections = "maf_signalr_connections_total";
        public const string SignalRMessagesSent = "maf_signalr_messages_sent_total";
        public const string SignalRMessagesReceived = "maf_signalr_messages_received_total";
    }
}

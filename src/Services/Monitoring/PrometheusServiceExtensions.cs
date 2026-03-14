using Microsoft.Extensions.DependencyInjection;

namespace CKY.MultiAgentFramework.Services.Monitoring
{
    /// <summary>
    /// Prometheus 监控服务扩展方法
    /// 注意：此扩展仅注册服务，不包含 ASP.NET Core 特定配置
    /// ASP.NET Core 应用（如 Demo 项目）需要单独配置 OpenTelemetry 和 Prometheus 端点
    /// </summary>
    public static class PrometheusServiceExtensions
    {
        /// <summary>
        /// 添加 MAF Prometheus 监控服务
        /// 在 ASP.NET Core 应用中，还需额外配置 OpenTelemetry Metrics 导出到 Prometheus
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddMafPrometheus(
            this IServiceCollection services,
            Action<PrometheusOptions>? configureOptions = null)
        {
            // 配置选项
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // 注册 MetricsCollector
            services.AddSingleton<IPrometheusMetricsCollector, PrometheusMetricsCollector>();

            // 注册 SystemMetricsCollector（用于后台系统指标收集）
            services.AddSingleton<SystemMetricsCollector>();

            return services;
        }
    }

    /// <summary>
    /// Prometheus 配置选项
    /// </summary>
    public class PrometheusOptions
    {
        /// <summary>
        /// 是否启用 Prometheus 监控
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 指标端点路径（需在 ASP.NET Core 应用中配置）
        /// </summary>
        public string MetricsEndpoint { get; set; } = "/metrics";

        /// <summary>
        /// 是否启用系统指标收集
        /// </summary>
        public bool EnableSystemMetricsCollection { get; set; } = true;

        /// <summary>
        /// 系统指标收集间隔（毫秒）
        /// </summary>
        public int SystemMetricsCollectionInterval { get; set; } = 5000;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;
    }
}

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Monitoring
{
    /// <summary>
    /// 系统指标收集器
    /// 定期收集CPU、内存、GC等系统资源指标
    /// </summary>
    public class SystemMetricsCollector : IDisposable
    {
        private readonly ILogger<SystemMetricsCollector> _logger;
        private readonly IPrometheusMetricsCollector _metrics;
        private readonly Timer _collectionTimer;

        private readonly Process _currentProcess;

        public SystemMetricsCollector(
            IPrometheusMetricsCollector metrics,
            ILogger<SystemMetricsCollector> logger)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentProcess = Process.GetCurrentProcess();

            // 每5秒收集一次系统指标
            _collectionTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private void CollectMetrics(object? state)
        {
            try
            {
                // 收集内存使用情况
                var memoryUsage = _currentProcess.WorkingSet64;
                _metrics.RecordGauge(MafMetrics.MemoryUsage, memoryUsage);

                // 收集GC信息
                var gen0Count = GC.CollectionCount(0);
                var gen1Count = GC.CollectionCount(1);
                var gen2Count = GC.CollectionCount(2);

                _metrics.IncrementCounter(MafMetrics.GcCount, gen0Count, new[] { "0" });
                _metrics.IncrementCounter(MafMetrics.GcCount, gen1Count, new[] { "1" });
                _metrics.IncrementCounter(MafMetrics.GcCount, gen2Count, new[] { "2" });

                // CPU使用率（简化计算）
                var cpuUsage = CalculateCpuUsage();
                if (cpuUsage.HasValue)
                {
                    _metrics.RecordGauge(MafMetrics.CpuUsage, cpuUsage.Value);
                }

                _logger.LogDebug("系统指标已收集: Memory={MemoryMB}MB, CPU={CPU}%",
                    memoryUsage / 1024 / 1024,
                    cpuUsage?.ToString("F2") ?? "N/A");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收集系统指标失败");
            }
        }

        private double? CalculateCpuUsage()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = _currentProcess.TotalProcessorTime;

                // 短暂等待以计算CPU使用率
                Thread.Sleep(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;

                if (totalMsPassed > 0)
                {
                    var cpuUsage = (cpuUsedMs / totalMsPassed) * 100;
                    return Math.Min(cpuUsage, 100); // 限制在100%
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _collectionTimer?.Dispose();
        }
    }
}

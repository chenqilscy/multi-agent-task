# Prometheus 监控使用指南

## 概述

CKY.MAF 框架集成了 Prometheus 监控系统，提供全方位的指标收集、导出和可视化能力。基于 .NET 的 `System.Diagnostics.Metrics` API 实现。

## 架构设计

### 核心组件

```
Services/Monitoring/
├── IMetricsCollector.cs              # 指标收集器接口
├── PrometheusMetricsCollector.cs     # Prometheus 指标实现
├── SystemMetricsCollector.cs         # 系统指标自动收集
└── PrometheusServiceExtensions.cs    # DI 注册扩展
```

### 指标分类

#### 1. HTTP 指标
- `maf_http_requests_total` - HTTP 请求总数
- `maf_http_request_duration_ms` - HTTP 请求耗时
- `maf_http_requests_in_flight` - 进行中的请求数

**标签**: `method`, `status_code`, `endpoint`

#### 2. Agent 指标
- `maf_agent_tasks_total` - Agent 任务总数
- `maf_agent_task_duration_ms` - Agent 任务耗时
- `maf_agent_errors_total` - Agent 错误总数
- `maf_agents_active` - 活跃 Agent 数量

**标签**: `agent_type`, `agent_name`, `status`

#### 3. 任务调度指标
- `maf_tasks_scheduled_total` - 已调度任务总数
- `maf_tasks_executing_total` - 执行中任务数
- `maf_tasks_completed_total` - 已完成任务总数
- `maf_tasks_failed_total` - 失败任务总数
- `maf_task_queue_depth` - 任务队列深度

**标签**: `task_type`, `priority_level`

#### 4. 缓存指标
- `maf_cache_hits_total` - 缓存命中总数
- `maf_cache_misses_total` - 缓存未命中总数
- `maf_cache_hit_rate` - 缓存命中率
- `maf_cache_size_bytes` - 缓存大小

**标签**: `cache_level`, `cache_type`

#### 5. LLM 指标
- `maf_llm_requests_total` - LLM 请求总数
- `maf_llm_request_duration_ms` - LLM 请求耗时
- `maf_llm_tokens_used_total` - Token 使用总数
- `maf_llm_errors_total` - LLM 错误总数

**标签**: `model_id`, `scenario`, `provider`

#### 6. 系统指标
- `maf_system_memory_usage_bytes` - 内存使用量
- `maf_system_cpu_usage_percent` - CPU 使用率
- `maf_system_gc_count` - GC 回收次数

**标签**: `gc_generation`

#### 7. SignalR 指标
- `maf_signalr_connections_total` - SignalR 连接总数
- `maf_signalr_messages_sent_total` - 发送消息总数
- `maf_signalr_messages_received_total` - 接收消息总数

**标签**: `hub_name`

## 快速开始

### 1. 注册 Prometheus 服务

在 `Program.cs` 中添加：

```csharp
using CKY.MultiAgentFramework.Services.Monitoring;

// 添加 Prometheus 监控
builder.Services.AddMafPrometheus();

// 配置 Prometheus 端点（可选，默认 /metrics）
app.MapPrometheusScrapingEndpoint();
```

### 2. 配置 OpenTelemetry

确保已安装 NuGet 包：
- `OpenTelemetry.Exporter.Prometheus.HttpListener` (或 Prometheus.AspNetCore)
- `OpenTelemetry.Extensions.Hosting`

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("CKY.MAF")  // 添加 MAF 框架的 Meter
            .AddPrometheusExporter(options =>
            {
                options.ScrapeResponseCacheDurationMilliseconds = 1000;
            });
    });
```

### 3. 使用 IMetricsCollector

在服务中注入并使用：

```csharp
public class MyService
{
    private readonly IMetricsCollector _metrics;

    public MyService(IMetricsCollector metrics)
    {
        _metrics = metrics;
    }

    public async Task ProcessAsync(string input)
    {
        // 记录开始时间
        var stopwatch = ValueStopwatch.StartNew();

        try
        {
            // 业务逻辑
            await DoWorkAsync(input);

            // 记录成功
            _metrics.IncrementCounter(MafMetrics.AgentTasks,
                new[] { "my_agent", "success" });
        }
        catch (Exception ex)
        {
            // 记录错误
            _metrics.IncrementCounter(MafMetrics.AgentErrors,
                new[] { "my_agent", ex.GetType().Name });
            throw;
        }
        finally
        {
            // 记录耗时
            _metrics.RecordHistogram(MafMetrics.AgentTaskDuration,
                stopwatch.ElapsedMilliseconds,
                new[] { "my_agent" });
        }
    }
}
```

## 高级用法

### 自定义指标

```csharp
// 创建自定义 Counter
var myCounter = _meter.CreateCounter<int>(
    "my_custom_metric",
    description: "My custom counter");

// 创建自定义 Gauge
var myGauge = _meter.CreateGauge<double>(
    "my_custom_gauge",
    description: "My custom gauge");

// 创建自定义 Histogram
var myHistogram = _meter.CreateHistogram<double>(
    "my_custom_histogram",
    description: "My custom histogram");

// 使用自定义指标
myCounter.Add(1, new[] { "label1", "value1" });
myGauge.Record(42.0);
myHistogram.Record(123.45);
```

### 系统指标自动收集

`SystemMetricsCollector` 会自动在后台收集系统指标：

```csharp
// 已在 AddMafPrometheus() 中自动注册
// 无需手动启动
// 收集间隔: 每 5 秒
```

指标包括：
- 进程内存使用量（WorkingSet64）
- GC 回收次数（Gen0, Gen1, Gen2）
- CPU 使用率（采样计算）

### 中间件集成

使用 `PrometheusMetricsMiddleware` 自动跟踪 HTTP 请求：

```csharp
app.UseMiddleware<PrometheusMetricsMiddleware>();
```

这将自动记录：
- 每个 HTTP 请求的计数
- 请求耗时直方图
- 进行中的请求数量

## Grafana 仪表板

### 导入预配置仪表板

在 Grafana 中导入 `grafana/maf-dashboard.json`，包含：
- 系统概览面板（CPU、内存、GC）
- Agent 任务监控
- 缓存性能分析
- LLM 调用统计
- 任务调度队列

### 关键查询示例

**CPU 使用率趋势**:
```promql
rate(maf_system_cpu_usage_percent[5m])
```

**Agent 任务错误率**:
```promql
sum(rate(maf_agent_errors_total[5m])) by (agent_type)
/
sum(rate(maf_agent_tasks_total[5m])) by (agent_type)
```

**缓存命中率**:
```promql
maf_cache_hits_total
/
(maf_cache_hits_total + maf_cache_misses_total)
```

**LLM 请求 P95 耗时**:
```promql
histogram_quantile(0.95,
  sum(rate(maf_llm_request_duration_ms_bucket[5m])) by (le, model_id))
```

## 性能优化

### 1. 降低采样率

对于高频指标，可以采样记录：

```csharp
private int _sampleCounter = 0;

public void RecordMetric()
{
    _sampleCounter++;
    if (_sampleCounter % 10 == 0)  // 每 10 次记录一次
    {
        _metrics.IncrementCounter(MafMetrics.MyMetric);
    }
}
```

### 2. 批量导出

配置 Prometheus 批量导出间隔：

```csharp
.AddPrometheusExporter(options =>
{
    options.ScrapeResponseCacheDurationMilliseconds = 5000;  // 5 秒
});
```

### 3. 过滤标签

减少标签基数（cardinality），避免指标爆炸：

```csharp
// 不推荐：每个用户一个标签
metrics.IncrementCounter("requests", new[] { userId });  // X

// 推荐：使用标签分组
metrics.IncrementCounter("requests", new[] { userTier });  // ✓
```

## 故障排查

### 指标未显示

1. 检查 `/metrics` 端点是否可访问
2. 验证 Meter 名称是否为 "CKY.MAF"
3. 确认 Prometheus 正在抓取该端点

### 内存泄漏

如果发现指标收集导致内存问题：

1. 检查是否有过多的标签组合
2. 确保 Histogram 分桶设置合理
3. 考虑使用 ExponentialHistogram 自动分桶

### 性能影响

如果指标收集影响性能：

1. 降低 SystemMetricsCollector 的收集频率
2. 禁用高基数指标
3. 使用异步导出

## 最佳实践

### DO ✅
- 使用描述性的指标名称（带 `maf_` 前缀）
- 为标签使用一致的命名规范
- 在关键路径添加指标记录
- 定期审查和清理无用指标
- 使用 Histogram 测量耗时分布

### DON'T ❌
- 不要在指标中存储高基数数据（如用户 ID）
- 不要在热路径中进行复杂的标签计算
- 不要忽略单位（bytes, milliseconds, percent）
- 不要混用 Counter 和 Gauge

## 相关文档

- [架构设计规范](../design-docs/core-architecture.md)
- [性能基准测试](./specs/13-performance-benchmarks.md)
- [错误处理指南](../design-docs/error-handling.md)

## API 参考

详见源码：
- [IMetricsCollector.cs](../src/Services/Monitoring/IMetricsCollector.cs)
- [PrometheusMetricsCollector.cs](../src/Services/Monitoring/PrometheusMetricsCollector.cs)
- [MafMetrics 常量](../src/Services/Monitoring/IMetricsCollector.cs)

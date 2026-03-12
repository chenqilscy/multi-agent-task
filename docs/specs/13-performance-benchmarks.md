# CKY.MAF框架性能基准测试指南

> **文档版本**: v1.0
> **创建日期**: 2026-03-13
> **用途**: 定义性能指标、基准测试方法和优化策略

---

## 📋 目录

1. [性能指标定义](#一性能指标定义)
2. [基准测试方法](#二基准测试方法)
3. [性能优化策略](#三性能优化策略)
4. [监控指标](#四监控指标)
5. [性能测试场景](#五性能测试场景)

---

## 一、性能指标定义

### 1.1 响应时间指标

| 指标 | P50 | P95 | P99 | 目标 | 测试方法 |
|------|-----|-----|-----|------|---------|
| **简单任务** | 500ms | 1s | 2s | P95 < 1s | 意图识别+单Agent |
| **复杂任务** | 2s | 5s | 10s | P95 < 5s | 任务分解+多Agent |
| **长对话** | 1s | 3s | 5s | P95 < 3s | 多轮对话上下文 |
| **LLM调用** | 1s | 3s | 5s | P95 < 3s | 单次LLM API调用 |

**定义**：
- **P50** (中位数): 50%的请求在此时间内完成
- **P95**: 95%的请求在此时间内完成
- **P99**: 99%的请求在此时间内完成

---

### 1.2 吞吐量指标

| 指标 | 目标值 | 测试方法 | 备注 |
|------|--------|---------|------|
| **简单任务QPS** | > 100 req/s | 负载测试 | 单Agent任务 |
| **复杂任务QPS** | > 50 req/s | 负载测试 | 多Agent协作 |
| **并发用户数** | > 100 concurrent | 并发测试 | 同时在线用户 |
| **任务处理能力** | > 1000 tasks/min | 压力测试 | 任务队列处理速率 |

---

### 1.3 资源占用指标

| 指标 | 空闲 | 正常负载 | 高负载 | 告警阈值 |
|------|------|---------|--------|---------|
| **CPU使用率** | < 10% | < 50% | < 80% | > 80% |
| **内存占用** | < 200MB | < 500MB | < 1GB | > 1GB |
| **GC暂停** | < 10ms | < 50ms | < 100ms | > 100ms |
| **线程数** | < 50 | < 200 | < 500 | > 500 |

---

### 1.4 缓存性能指标

| 指标 | 目标值 | 测试方法 | 优化建议 |
|------|--------|---------|---------|
| **L1缓存命中率** | > 80% | 监控指标 | 优化热点数据 |
| **L2缓存命中率** | > 85% | 监控指标 | 调整TTL策略 |
| **L3数据库查询** | < 10ms | 性能测试 | 添加索引 |
| **缓存回源率** | < 15% | 监控指标 | 预热缓存 |

---

### 1.5 LLM调用性能

| 指标 | 目标值 | 测试方法 | 备注 |
|------|--------|---------|------|
| **Token生成速度** | > 50 tokens/s | 实时监控 | 智谱AI/通义千问 |
| **API响应时间** | P95 < 3s | 性能测试 | 包含网络延迟 |
| **Token使用量** | < 1000 tokens/任务 | 统计分析 | 优化Prompt |
| **API失败率** | < 1% | 监控指标 | 实现降级机制 |

---

## 二、基准测试方法

### 2.1 测试工具选择

**推荐工具**：

| 工具 | 用途 | 链接 |
|------|------|------|
| **BenchmarkDotNet** | 微基准测试 | https://benchmarkdotnet.org/ |
| **NBomber** | 负载测试 | https://github.com/AdrienTorris/nbomb |
| **Locust** | 分布式负载测试 | https://locust.io/ |
| **k6** | 现代化性能测试 | https://k6.io/ |
| **wrk2** | HTTP基准测试 | https://github.com/gpt-writer/wrk2 |

---

### 2.2 单元性能测试（BenchmarkDotNet）

**安装**：
```bash
dotnet add package BenchmarkDotNet
```

**示例**：
```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CKY.MAF.Benchmarks
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    public class IntentRecognizerBenchmarks
    {
        private MafIntentRecognizer _recognizer;
        private string _simpleInput = "打开客厅灯";
        private string _complexInput = "我起床了，帮我打开客厅的灯，设置空调到26度，播放轻音乐";

        [GlobalSetup]
        public void Setup()
        {
            _recognizer = new MafIntentRecognizer();
        }

        [Benchmark]
        [Arguments("打开客厅灯", "把温度调到26度", "我起床了")]
        public async Task<IntentRecognitionResult> RecognizeIntent(string input)
        {
            return await _recognizer.RecognizeAsync(input);
        }
    }
}
```

**运行**：
```bash
dotnet run -c Release -p MafBenchmarks
```

---

### 2.3 负载测试（NBomber）

**示例配置**：
```csharp
using NBomber.Contracts.Catalog;
using NBomber.Catelog;
using NBomber;
using NBomber.Contracts;

namespace CKY.MAF.LoadTests
{
    public class SimpleTaskLoadTest
    {
        private const string BaseUrl = "http://localhost:5000";
        private HttpClient _client;

        [GlobalSetup]
        public void Setup()
        {
            _client = new HttpClient();
        }

        [Benchmark]
        public async Task HandleSimpleTask()
        {
            var request = new MafTaskRequest
            {
                UserInput = "打开客厅灯",
                ConversationId = Guid.NewGuid().ToString()
            };

            var response = await _client.PostAsJsonAsync(
                $"{BaseUrl}/api/task/execute",
                request);

            response.EnsureSuccessStatusCode();
        }
    }
}
```

**运行**：
```bash
dotnet run -c Release -p MafLoadTests -- --job SimpleLoad --run-duration 60s
```

---

### 2.4 压力测试（Locust）

**示例**：
```python
from locust import HttpUser, task, between

class MAFUser(HttpUser):
    wait_time = between(1, 3)

    @task
    def simple_task(self):
        self.client.post("/api/task/execute", json={
            "userInput": "打开客厅灯",
            "conversationId": "test-conv-001"
        })

    @task(3)
    def complex_task(self):
        self.client.post("/api/task/execute", json={
            "userInput": "我起床了",
            "conversationId": "test-conv-002"
        })

# 启动命令: locust -f locustfile.py --host=http://localhost:5000 --users=100 --spawn-rate-10
```

---

## 三、性能优化策略

### 3.1 任务调度优化

**优化策略1：并行组优化**
```csharp
// 优化前：串行执行
foreach (var task in tasks)
{
    await ExecuteTaskAsync(task);
}

// 优化后：并行执行
var parallelGroups = IdentifyParallelGroup(tasks);
foreach (var group in parallelGroups)
{
    await Task.WhenAll(group.Select(t => ExecuteTaskAsync(t)));
}
```

**性能提升**：2-5x（取决于任务数量）

---

**优化策略2：任务预计算**
```csharp
// 预计算优先级分数（批量）
public async Task<Dictionary<string, int>> BatchCalculatePriorityAsync(
    List<DecomposedTask> tasks)
{
    var results = new Dictionary<string, int>();

    // 批量计算，减少I/O
    var priorities = await _priorityCalculator.CalculateBatchAsync(tasks);

    foreach (var task in tasks)
    {
        results[task.TaskId] = priorities[task.TaskId];
    }

    return results;
}
```

**性能提升**：1.5-2x（批量操作）

---

### 3.2 缓存优化

**优化策略1：智能缓存预热**
```csharp
public class CacheWarmupService
{
    public async Task WarmupAsync()
    {
        // 预热常用场景数据
        var scenarios = new[]
        {
            "morning_routine",
            "away_mode",
            "cinema_mode"
        };

        foreach (var scenario in scenarios)
        {
            var data = await _database.GetScenarioDataAsync(scenario);
            await _cache.SetAsync($"scenario:{scenario}", data, TimeSpan.FromHours(24));
        }
    }
}
```

---

**优化策略2：缓存分层策略**
```csharp
public async Task<T> GetFromCacheAsync<T>(string key, Func<Task<T>> factory)
{
    // L1: 内存缓存（最快）
    var value = await _memoryCache.GetAsync<T>(key);
    if (value != null)
        return value;

    // L2: Redis缓存（快）
    value = await _redisCache.GetAsync<T>(key);
    if (value != null)
    {
        await _memoryCache.SetAsync(key, value);
        return value;
    }

    // L3: 数据库（慢）
    value = await factory();
    await _redisCache.SetAsync(key, value, TimeSpan.FromHours(1));
    await _memoryCache.SetAsync(key, value, TimeSpan.FromMinutes(30));

    return value;
}
```

**性能提升**：10-100x（取决于数据大小）

---

### 3.3 LLM调用优化

**优化策略1：Prompt缓存**
```csharp
public class PromptCacheDecorator : ILLMService
{
    private readonly ICacheStore _cache;
    private readonly ILLMService _service;

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        // 计算Prompt Hash
        var promptHash = ComputeHash(prompt);

        // 检查缓存
        var cached = await _cache.GetAsync<string>($"llm:{promptHash}", ct);
        if (cached != null)
            return cached;

        // 调用LLM
        var result = await _service.CompleteAsync(prompt, ct);

        // 缓存结果（TTL: 7天）
        await _cache.SetAsync($"llm:{promptHash}", result, TimeSpan.FromDays(7), ct);

        return result;
    }
}
```

**性能提升**：5-10x（对于重复Prompt）

---

**优化策略2：批量请求**
```csharp
// 优化前：逐个调用
foreach (var task in tasks)
{
    var result = await _llmService.CompleteAsync(task.Prompt);
}

// 优化后：批量调用
var prompts = tasks.Select(t => t.Prompt).ToList();
var results = await _llmService.CompleteBatchAsync(prompts);
```

**性能提升**：2-3x（减少网络往返）

---

### 3.4 数据库优化

**优化策略1：批量操作**
```csharp
// 优化前：逐条插入
foreach (var task in tasks)
{
    await _database.InsertAsync(task);
}

// 优化后：批量插入
await _database.BulkInsertAsync(tasks);
```

---

**优化策略2：连接池配置**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=maf;Username=maf;Password=***;Maximum Pool Size=100;Minimum Pool Size=10;Connection Lifetime=300;"
  }
}
```

---

## 四、监控指标

### 4.1 Prometheus指标定义

```csharp
public class MafMetrics
{
    private readonly Counter _requestCounter;
    private readonly Histogram _responseTimeHistogram;
    private readonly Gauge _activeAgentsGauge;
    private readonly Counter _llmTokenCounter;
    private readonly Gauge _cacheHitRateGauge;

    public Mafetrics(IMetricFactory metrics)
    {
        _requestCounter = metrics.CreateCounter(
            "maf_requests_total",
            "Total requests processed",
            new[] { "agent_type", "intent" });

        _responseTimeHistogram = metrics.CreateHistogram(
            "maf_request_duration_seconds",
            "Request duration in seconds",
            new[] { "agent_type" });

        _activeAgentsGauge = metrics.CreateGauge(
            "maf_active_agents",
            "Number of active agents");

        _llmTokenCounter = metrics.CreateCounter(
            "maf_llm_tokens_total",
            "Total LLM tokens consumed",
            new[] { "model", "provider" });

        _cacheHitRateGauge = metrics.CreateGauge(
            "maf_cache_hit_rate",
            "Cache hit rate percentage");
    }
}
```

---

### 4.2 Grafana仪表板配置

**推荐面板**：

1. **请求速率面板**
   - 图表：Requests per second
   - 指标：`rate(maf_requests_total[1m])`

2. **响应时间面板**
   - 图表：Request duration percentiles
   - 指标：`histogram_quantile(0.95, maf_request_duration_seconds_bucket)`

3. **LLM调用面板**
   - 图表：LLM tokens per second
   - 指标：`rate(maf_llm_tokens_total[1m])`

4. **缓存命中率面板**
   - 图表：Cache hit rate
   - 指标：`maf_cache_hit_rate`

---

### 4.3 告警规则

| 告警条件 | 严重级别 | 触发动作 |
|---------|---------|---------|
| P95响应时间 > 5s | Warning | 通知开发团队 |
| P99响应时间 > 10s | Critical | 立即处理 |
| 错误率 > 5% | Warning | 查看日志 |
| LLM API失败率 > 10% | Critical | 启用降级 |
| CPU使用率 > 85% | Warning | 准备扩容 |
| 内存使用率 > 90% | Critical | 立即扩容 |

---

## 五、性能测试场景

### 5.1 场景1：简单任务（单Agent）

**场景描述**：
- 用户输入："打开客厅灯"
- 意图识别 + 单Agent执行

**性能目标**：
- P95响应时间 < 1s
- QPS > 100 req/s

**测试方法**：
```bash
# NBomber
dotnet run -c Release -p MafBenchmarks -- --job SimpleTask --run-duration 60s

# Locust
locust -f locustfile.py --host=http://localhost:5000 --users=100 --spawn-rate-10 --run-time 60s
```

---

### 5.2 场景2：复杂任务（多Agent协作）

**场景描述**：
- 用户输入："我起床了"
- 任务分解 + 4个Agent并行执行

**性能目标**：
- P95响应时间 < 5s
- QPS > 50 req/s

**测试方法**：
```bash
# NBomber
dotnet run -c Release -p MafBenchmarks -- --job ComplexTask --run-duration 60s
```

---

### 5.3 场景3：长对话（多轮交互）

**场景描述**：
- 第1轮："打开客厅的灯"
- 第2轮："把它调暗一点"
- 第3轮："关闭它"

**性能目标**：
- P95响应时间 < 3s
- 上下文保持率 100%

**测试方法**：
```bash
# 使用Scenario测试
dotnet test --filter "FullyQualifiedName~ConversationTests"
```

---

### 5.4 场景4：高并发（压力测试）

**场景描述**：
- 1000个并发用户
- 混合简单/复杂任务

**性能目标**：
- 错误率 < 1%
- P95响应时间 < 3s

**测试方法**：
```bash
# Locust
locust -f locustfile.py --host=http://localhost:5000 --users=1000 --spawn-rate=50 --run-time 120s
```

---

### 5.5 场景5：缓存性能

**场景描述**：
- 10000次请求，重复任务
- 测试L1/L2缓存命中率

**性能目标**：
- L1命中率 > 80%
- L2命中率 > 85%
- P95响应时间 < 500ms

**测试方法**：
```csharp
[Benchmark]
public async Task CacheHitRateTest()
{
    // 预热缓存
    await _taskExecutor.ExecuteAsync("打开客厅灯");

    // 测试缓存命中
    for (int i = 0; i < 10000; i++)
    {
        await _taskExecutor.ExecuteAsync("打开客厅灯");
    }
}
```

---

## 六、性能优化检查清单

### 6.1 开发阶段

- [ ] 使用BenchmarkDotNet进行微基准测试
- [ ] 关键算法复杂度 < O(n²)
- [ ] 避免N+1查询问题
- [ ] 实现批量操作接口
- [ ] 使用异步I/O（async/await）

### 6.2 测试阶段

- [ ] 单元测试包含性能基准
- [ ] 集成测试包含性能指标
- [ ] 使用Testcontainers进行真实环境测试
- [ ] 性能测试纳入CI/CD流水线

### 6.3 生产环境

- [ ] 配置Prometheus指标收集
- [ ] 配置Grafana仪表板
- [ ] 配置告警规则
- [ ] 定期执行性能基准测试
- [ ] 监控缓存命中率并优化

---

## 七、常见性能问题

### 7.1 问题：响应时间慢

**诊断步骤**：
1. 检查CPU/内存使用率
2. 查看数据库慢查询日志
3. 检查LLM API响应时间
4. 查看缓存命中率

**常见原因**：
- 数据库查询未优化（缺少索引）
- LLM API调用未缓存
- 同步I/O阻塞线程
- 缓存命中率低

**解决方案**：
- 添加数据库索引
- 实现Prompt缓存
- 使用异步I/O
- 预热缓存

---

### 7.2 问题：内存占用高

**诊断步骤**：
1. 使用dotnet-trace分析内存分配
2. 检查是否有内存泄漏
3. 查看GC统计信息

**常见原因**：
- 事件订阅未取消
- 缓存未设置TTL
- 大对象未释放

**解决方案**：
- 使用IDisposable模式
- 设置合理的缓存TTL
- 定期清理过期数据

---

### 7.3 问题：吞吐量低

**诊断步骤**：
1. 检查线程池配置
2. 查看任务队列长度
3. 检查是否存在锁竞争

**常见原因**：
- 线程池耗尽
- 任务调度算法效率低
- 锁竞争严重

**解决方案**：
- 优化线程池配置
- 优化任务调度算法
- 减少锁粒度

---

## 八、性能测试最佳实践

### 8.1 测试环境

**要求**：
- 测试环境应与生产环境配置一致
- 使用真实数据规模（避免测试数据过少）
- 网络延迟模拟真实环境

**建议**：
- 使用Docker Compose搭建测试环境
- 使用Testcontainers管理测试容器
- 使用Mock服务模拟外部依赖

---

### 8.2 测试数据

**原则**：
- 测试数据应具有代表性
- 避免使用固定数据（如"test1", "test2"）
- 测试数据量应接近生产规模

**示例**：
```csharp
// 使用Bogus生成测试数据
var faker = new Faker();
var userInput = faker.Lorem.Sentence();
var userName = faker.Person.FullName;
var deviceName = faker.Hacker.Noun();
```

---

### 8.3 持续性能监控

**生产环境监控**：
- 使用Application Insights或Prometheus
- 配置实时告警
- 定期生成性能报告

**建议**：
```yaml
# Prometheus配置（每15秒采集一次）
scrape_configs:
  - job_name: 'maf-application'
    scrape_interval: 15s
    metrics_path: '/metrics'
```

---

## 九、性能优化路线图

### Phase 1：基础优化（Week 1）
- [ ] 实现Prompt缓存
- [ ] 优化数据库查询（添加索引）
- [ ] 实现批量操作接口
- [ ] 配置线程池参数

### Phase 2：高级优化（Week 2）
- [ ] 优化任务调度算法
- [ ] 实现智能缓存预热
- [ ] 优化LLM批量调用
- [ ] 实现连接池优化

### Phase 3：生产优化（Week 3）
- [ ] 配置CDN加速
- [ ] 实现读写分离
- [ ] 配置自动扩缩容
- [ ] 建立性能监控体系

---

## 🔗 相关文档

- [接口设计规范](./06-interface-design-spec.md)
- [实现指南](./09-implementation-guide.md)
- [部署指南](./08-deployment-guide.md)
- [测试指南](./10-testing-guide.md)

---

**文档版本**: v1.0
**创建日期**: 2026-03-13
**维护团队**: CKY.MAF架构团队

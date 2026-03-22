using BenchmarkDotNet.Attributes;
using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Enums;
using CKY.MultiAgentFramework.Core.Models.Task;
using CKY.MultiAgentFramework.Services.Scheduling;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MAF.Benchmarks;

/// <summary>
/// 优先级计算器性能基准测试
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class PriorityCalculatorBenchmarks
{
    private MafPriorityCalculator _calculator = null!;
    private PriorityCalculationRequest _simpleRequest = null!;
    private PriorityCalculationRequest _complexRequest = null!;
    private List<PriorityCalculationRequest> _batchRequests = null!;

    [GlobalSetup]
    public void Setup()
    {
        _calculator = new MafPriorityCalculator(NullLogger<MafPriorityCalculator>.Instance);

        _simpleRequest = new PriorityCalculationRequest
        {
            TaskId = "task-1",
            BasePriority = TaskPriority.Normal,
            UserInteraction = UserInteractionType.Automatic,
            TimeFactor = TimeFactor.Normal,
            ResourceUsage = ResourceUsage.Low
        };

        _complexRequest = new PriorityCalculationRequest
        {
            TaskId = "task-2",
            BasePriority = TaskPriority.Critical,
            UserInteraction = UserInteractionType.Active,
            TimeFactor = TimeFactor.Immediate,
            ResourceUsage = ResourceUsage.High,
            IsOverdue = true,
            DependencyTask = new DecomposedTask { PriorityScore = 80 }
        };

        _batchRequests = Enumerable.Range(0, 1000).Select(i => new PriorityCalculationRequest
        {
            TaskId = $"task-{i}",
            BasePriority = (TaskPriority)(i % 5),
            UserInteraction = (UserInteractionType)(i % 3),
            TimeFactor = (TimeFactor)(i % 4),
            ResourceUsage = (ResourceUsage)(i % 3),
            IsOverdue = i % 10 == 0
        }).ToList();
    }

    [Benchmark(Description = "简单优先级计算")]
    public int CalculateSimplePriority()
    {
        return _calculator.CalculatePriority(_simpleRequest);
    }

    [Benchmark(Description = "复杂优先级计算（含依赖+超期）")]
    public int CalculateComplexPriority()
    {
        return _calculator.CalculatePriority(_complexRequest);
    }

    [Benchmark(Description = "批量优先级计算 (1000个)")]
    public int CalculateBatchPriority()
    {
        int total = 0;
        foreach (var req in _batchRequests)
        {
            total += _calculator.CalculatePriority(req);
        }
        return total;
    }
}

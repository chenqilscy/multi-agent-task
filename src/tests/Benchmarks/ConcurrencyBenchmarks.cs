using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace CKY.MAF.Benchmarks;

/// <summary>
/// 并发场景性能基准测试
/// 模拟多用户并发请求的吞吐量
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ConcurrencyBenchmarks
{
    private ConcurrentDictionary<string, string> _cacheStore = null!;

    [Params(10, 50, 100)]
    public int ConcurrentUsers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _cacheStore = new ConcurrentDictionary<string, string>();
        // 预填充缓存
        for (int i = 0; i < 10000; i++)
        {
            _cacheStore[$"key-{i}"] = $"value-{i}";
        }
    }

    [Benchmark(Description = "并发缓存读取")]
    public async Task ConcurrentCacheReads()
    {
        var tasks = Enumerable.Range(0, ConcurrentUsers).Select(i => Task.Run(() =>
        {
            for (int j = 0; j < 100; j++)
            {
                _cacheStore.TryGetValue($"key-{(i * 100 + j) % 10000}", out _);
            }
        }));
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "并发缓存读写混合 (80%读/20%写)")]
    public async Task ConcurrentCacheReadWrite()
    {
        var tasks = Enumerable.Range(0, ConcurrentUsers).Select(i => Task.Run(() =>
        {
            for (int j = 0; j < 100; j++)
            {
                var key = $"key-{(i * 100 + j) % 10000}";
                if (j % 5 == 0) // 20% 写
                    _cacheStore[key] = $"updated-{i}-{j}";
                else // 80% 读
                    _cacheStore.TryGetValue(key, out _);
            }
        }));
        await Task.WhenAll(tasks);
    }

    [Benchmark(Description = "并发任务调度模拟")]
    public async Task ConcurrentTaskScheduling()
    {
        var semaphore = new SemaphoreSlim(10); // 模拟最大10并发执行
        var tasks = Enumerable.Range(0, ConcurrentUsers).Select(async i =>
        {
            await semaphore.WaitAsync();
            try
            {
                // 模拟轻量级任务处理
                await Task.Yield();
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }
}

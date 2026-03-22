using CKY.MultiAgentFramework.Core.Models.Resilience;
using CKY.MultiAgentFramework.Core.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Core.Resilience;

/// <summary>
/// LlmCircuitBreaker 扩展测试 — 覆盖半开→关闭、半开→开 转换、Reset、GetStatus
/// </summary>
public class LlmCircuitBreakerExtendedTests
{
    private readonly Mock<ILogger> _loggerMock = new();

    private LlmCircuitBreaker CreateBreaker(int threshold = 3, TimeSpan? breakDuration = null)
    {
        var options = new LlmCircuitBreakerOptions
        {
            FailureThreshold = threshold,
            BreakDuration = breakDuration ?? TimeSpan.FromSeconds(1)
        };
        return new LlmCircuitBreaker(_loggerMock.Object, options);
    }

    [Fact]
    public async Task ExecuteAsync_HalfOpen_SuccessTransitionsBackToClosed()
    {
        var breaker = CreateBreaker(threshold: 2, breakDuration: TimeSpan.FromMilliseconds(50));

        // 触发 Open
        for (int i = 0; i < 2; i++)
        {
            try { await breaker.ExecuteAsync<string>("a", _ => throw new Exception("fail")); }
            catch { }
        }
        breaker.State.Should().Be(LlmCircuitState.Open);

        // 等待 breakDuration 过期
        await Task.Delay(100);

        // 下次请求应 HalfOpen → 成功后 Closed
        var result = await breaker.ExecuteAsync("a", _ => Task.FromResult("ok"));
        result.Should().Be("ok");
        breaker.State.Should().Be(LlmCircuitState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_HalfOpen_FailureGoesBackToOpen()
    {
        var breaker = CreateBreaker(threshold: 2, breakDuration: TimeSpan.FromMilliseconds(50));

        // 触发 Open
        for (int i = 0; i < 2; i++)
        {
            try { await breaker.ExecuteAsync<string>("a", _ => throw new Exception("fail")); }
            catch { }
        }
        breaker.State.Should().Be(LlmCircuitState.Open);

        // 等待 breakDuration 过期
        await Task.Delay(100);

        // 下次请求进入 HalfOpen，失败后回到 Open
        var act = () => breaker.ExecuteAsync<string>("a", _ => throw new Exception("still failing"));
        await act.Should().ThrowAsync<Exception>();
        breaker.State.Should().Be(LlmCircuitState.Open);
    }

    [Fact]
    public void Reset_FromOpen_ShouldReturnToClosed()
    {
        var breaker = CreateBreaker(threshold: 1);

        // 触发 Open
        try { breaker.ExecuteAsync<string>("a", _ => throw new Exception("fail")).Wait(); }
        catch { }
        breaker.State.Should().Be(LlmCircuitState.Open);

        breaker.Reset("a");
        breaker.State.Should().Be(LlmCircuitState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    [Fact]
    public void GetStatus_ReflectsCurrentState()
    {
        var breaker = CreateBreaker(threshold: 5);

        var status = breaker.GetStatus();
        status.State.Should().Be(LlmCircuitState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
        status.FailureThreshold.Should().Be(5);
        status.LastStateChangeTime.Should().NotBeNull();
        status.LastFailureTime.Should().BeNull();
    }

    [Fact]
    public async Task GetStatus_AfterFailures_ReflectsFailureInfo()
    {
        var breaker = CreateBreaker(threshold: 5);

        try { await breaker.ExecuteAsync<string>("a", _ => throw new Exception("err")); }
        catch { }

        var status = breaker.GetStatus();
        status.FailureCount.Should().Be(1);
        status.LastFailureTime.Should().NotBeNull();
        status.State.Should().Be(LlmCircuitState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_Success_ResetsFailureCountInClosed()
    {
        var breaker = CreateBreaker(threshold: 5);

        // 2 failures
        for (int i = 0; i < 2; i++)
        {
            try { await breaker.ExecuteAsync<string>("a", _ => throw new Exception()); }
            catch { }
        }
        breaker.FailureCount.Should().Be(2);

        // 1 success resets
        await breaker.ExecuteAsync("a", _ => Task.FromResult(42));
        breaker.FailureCount.Should().Be(0);
    }

    [Fact]
    public void LastStateChangeTime_SetOnConstruction()
    {
        var breaker = CreateBreaker();
        breaker.LastStateChangeTime.Should().NotBeNull();
        breaker.LastStateChangeTime!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

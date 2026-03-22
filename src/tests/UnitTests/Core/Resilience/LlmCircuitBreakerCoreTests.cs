using CKY.MultiAgentFramework.Core.Models.Resilience;
using CKY.MultiAgentFramework.Core.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Resilience
{
    public class LlmCircuitBreakerTests
    {
        private readonly Mock<ILogger> _mockLogger = new();

        private LlmCircuitBreaker CreateBreaker(int failureThreshold = 3, int breakDurationSeconds = 1)
        {
            var options = new LlmCircuitBreakerOptions
            {
                FailureThreshold = failureThreshold,
                BreakDuration = TimeSpan.FromSeconds(breakDurationSeconds)
            };
            return new LlmCircuitBreaker(_mockLogger.Object, options);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new LlmCircuitBreaker(null!, null);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_DefaultState_ShouldBeClosed()
        {
            var breaker = CreateBreaker();

            breaker.State.Should().Be(LlmCircuitState.Closed);
            breaker.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_SuccessfulOperation_ShouldReturnResult()
        {
            var breaker = CreateBreaker();

            var result = await breaker.ExecuteAsync("agent-1",
                ct => Task.FromResult("success"));

            result.Should().Be("success");
            breaker.State.Should().Be(LlmCircuitState.Closed);
        }

        [Fact]
        public async Task ExecuteAsync_FailuresBelowThreshold_ShouldStayClosed()
        {
            var breaker = CreateBreaker(failureThreshold: 3);

            // 2 failures (below threshold of 3)
            for (int i = 0; i < 2; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("agent-1",
                        ct => throw new InvalidOperationException("fail")));
            }

            breaker.State.Should().Be(LlmCircuitState.Closed);
            breaker.FailureCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_FailuresReachThreshold_ShouldOpenCircuit()
        {
            var breaker = CreateBreaker(failureThreshold: 3);

            // 3 failures (reaches threshold)
            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("agent-1",
                        ct => throw new InvalidOperationException("fail")));
            }

            breaker.State.Should().Be(LlmCircuitState.Open);
        }

        [Fact]
        public async Task ExecuteAsync_WhenOpen_ShouldRejectRequests()
        {
            var breaker = CreateBreaker(failureThreshold: 1, breakDurationSeconds: 60);

            // Trigger open
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await breaker.ExecuteAsync<string>("agent-1",
                    ct => throw new InvalidOperationException("fail")));

            breaker.State.Should().Be(LlmCircuitState.Open);

            // Next request should be rejected
            await Assert.ThrowsAsync<LlmCircuitBreakerOpenException>(async () =>
                await breaker.ExecuteAsync<string>("agent-1",
                    ct => Task.FromResult("should not execute")));
        }

        [Fact]
        public async Task ExecuteAsync_SuccessInClosed_ShouldResetFailureCount()
        {
            var breaker = CreateBreaker(failureThreshold: 3);

            // 2 failures
            for (int i = 0; i < 2; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("agent-1",
                        ct => throw new InvalidOperationException("fail")));
            }

            // 1 success should reset
            await breaker.ExecuteAsync("agent-1", ct => Task.FromResult("ok"));

            breaker.FailureCount.Should().Be(0);
        }

        [Fact]
        public void Reset_ShouldRestoreClosedState()
        {
            var breaker = CreateBreaker();

            breaker.Reset("agent-1");

            breaker.State.Should().Be(LlmCircuitState.Closed);
            breaker.FailureCount.Should().Be(0);
        }

        [Fact]
        public void GetStatus_ShouldReturnCurrentState()
        {
            var breaker = CreateBreaker(failureThreshold: 5);

            var status = breaker.GetStatus();

            status.State.Should().Be(LlmCircuitState.Closed);
            status.FailureCount.Should().Be(0);
            status.FailureThreshold.Should().Be(5);
            status.LastStateChangeTime.Should().NotBeNull();
        }

        [Fact]
        public void GetStatus_ShouldReturnCorrectToString()
        {
            var breaker = CreateBreaker();
            var status = breaker.GetStatus();

            status.ToString().Should().Contain("Closed");
        }

        [Fact]
        public void LlmCircuitBreakerStatus_DefaultProperties()
        {
            var status = new LlmCircuitBreakerStatus();

            status.FailureCount.Should().Be(0);
            status.SuccessCount.Should().Be(0);
        }
    }
}

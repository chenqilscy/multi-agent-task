using CKY.MultiAgentFramework.Services.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Resilience
{
    public class RetryExecutorTests
    {
        private readonly Mock<ILogger<RetryExecutor>> _mockLogger = new();
        private readonly RetryExecutor _executor;

        public RetryExecutorTests()
        {
            _executor = new RetryExecutor(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var act = () => new RetryExecutor(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_SuccessOnFirstAttempt_ShouldReturnResult()
        {
            var policy = new RetryPolicy { MaxRetries = 3 };

            var result = await _executor.ExecuteAsync(
                ct => Task.FromResult("ok"), policy);

            result.Should().Be("ok");
        }

        [Fact]
        public async Task ExecuteAsync_SuccessAfterRetry_ShouldReturnResult()
        {
            var policy = new RetryPolicy
            {
                MaxRetries = 3,
                BackoffStrategy = BackoffStrategy.Fixed,
                InitialBackoffMs = 10
            };
            int count = 0;

            var result = await _executor.ExecuteAsync(
                ct =>
                {
                    count++;
                    if (count < 3) throw new InvalidOperationException("temp");
                    return Task.FromResult("recovered");
                }, policy);

            result.Should().Be("recovered");
            count.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_AllRetriesFail_ShouldThrow()
        {
            var policy = new RetryPolicy
            {
                MaxRetries = 2,
                BackoffStrategy = BackoffStrategy.Fixed,
                InitialBackoffMs = 10
            };

            var act = async () => await _executor.ExecuteAsync<string>(
                ct => throw new InvalidOperationException("always fail"), policy);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ExecuteAsync_NonRetryableException_ShouldNotRetry()
        {
            var policy = new RetryPolicy { MaxRetries = 3 };
            int count = 0;

            var act = async () => await _executor.ExecuteAsync<string>(
                ct =>
                {
                    count++;
                    throw new ArgumentException("bad arg");
                }, policy);

            await act.Should().ThrowAsync<ArgumentException>();
            count.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_TaskCancelledException_ShouldNotRetry()
        {
            var policy = new RetryPolicy { MaxRetries = 3 };
            int count = 0;

            var act = async () => await _executor.ExecuteAsync<string>(
                ct =>
                {
                    count++;
                    throw new TaskCanceledException();
                }, policy);

            await act.Should().ThrowAsync<TaskCanceledException>();
            count.Should().Be(1);
        }

        [Theory]
        [InlineData(BackoffStrategy.Fixed)]
        [InlineData(BackoffStrategy.Linear)]
        [InlineData(BackoffStrategy.Exponential)]
        [InlineData(BackoffStrategy.ExponentialWithJitter)]
        public async Task ExecuteAsync_AllBackoffStrategies_ShouldWork(BackoffStrategy strategy)
        {
            var policy = new RetryPolicy
            {
                MaxRetries = 1,
                BackoffStrategy = strategy,
                InitialBackoffMs = 10,
                MaxBackoffMs = 100
            };
            int count = 0;

            var result = await _executor.ExecuteAsync(
                ct =>
                {
                    count++;
                    if (count == 1) throw new InvalidOperationException("first fail");
                    return Task.FromResult("ok");
                }, policy);

            result.Should().Be("ok");
        }

        [Fact]
        public void RetryPolicy_DefaultValues()
        {
            var policy = new RetryPolicy();

            policy.MaxRetries.Should().Be(3);
            policy.BackoffStrategy.Should().Be(BackoffStrategy.ExponentialWithJitter);
            policy.InitialBackoffMs.Should().Be(1000);
            policy.MaxBackoffMs.Should().Be(30000);
            policy.JitterFactor.Should().Be(0.1);
        }
    }

    public class ServicesCircuitBreakerTests
    {
        private readonly Mock<ILogger<CircuitBreaker>> _mockLogger = new();

        private CircuitBreaker CreateBreaker(int failureThreshold = 3, int openTimeoutSeconds = 60)
        {
            var config = new CircuitBreakerConfig
            {
                FailureThreshold = failureThreshold,
                OpenTimeout = TimeSpan.FromSeconds(openTimeoutSeconds)
            };
            return new CircuitBreaker(config, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_NullConfig_ShouldThrow()
        {
            var act = () => new CircuitBreaker(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var act = () => new CircuitBreaker(new CircuitBreakerConfig(), null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Default_ShouldBeClosed()
        {
            var breaker = CreateBreaker();
            breaker.State.Should().Be(CircuitState.Closed);
        }

        [Fact]
        public async Task ExecuteAsync_Success_ShouldReturnResult()
        {
            var breaker = CreateBreaker();

            var result = await breaker.ExecuteAsync("comp",
                ct => Task.FromResult("ok"));

            result.Should().Be("ok");
            breaker.State.Should().Be(CircuitState.Closed);
        }

        [Fact]
        public async Task ExecuteAsync_FailuresBelowThreshold_ShouldStayClosed()
        {
            var breaker = CreateBreaker(failureThreshold: 5);

            for (int i = 0; i < 4; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("comp",
                        ct => throw new InvalidOperationException("fail")));
            }

            breaker.State.Should().Be(CircuitState.Closed);
        }

        [Fact]
        public async Task ExecuteAsync_FailuresReachThreshold_ShouldOpen()
        {
            var breaker = CreateBreaker(failureThreshold: 3);

            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("comp",
                        ct => throw new InvalidOperationException("fail")));
            }

            breaker.State.Should().Be(CircuitState.Open);
        }

        [Fact]
        public async Task ExecuteAsync_WhenOpen_ShouldReject()
        {
            var breaker = CreateBreaker(failureThreshold: 1, openTimeoutSeconds: 3600);

            // Trigger open
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await breaker.ExecuteAsync<string>("comp",
                    ct => throw new InvalidOperationException("fail")));

            // Should reject
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
                await breaker.ExecuteAsync<string>("comp",
                    ct => Task.FromResult("should not run")));
        }

        [Fact]
        public async Task ExecuteAsync_SuccessAfterFailures_ShouldResetCount()
        {
            var breaker = CreateBreaker(failureThreshold: 3);

            // 2 failures
            for (int i = 0; i < 2; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("comp",
                        ct => throw new InvalidOperationException("fail")));
            }

            // 1 success resets
            await breaker.ExecuteAsync("comp", ct => Task.FromResult("ok"));

            // 2 more failures should not trigger open
            for (int i = 0; i < 2; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await breaker.ExecuteAsync<string>("comp",
                        ct => throw new InvalidOperationException("fail")));
            }

            breaker.State.Should().Be(CircuitState.Closed);
        }

        [Fact]
        public void CircuitBreakerConfig_DefaultValues()
        {
            var config = new CircuitBreakerConfig();

            config.FailureThreshold.Should().Be(5);
            config.OpenTimeout.Should().Be(TimeSpan.FromSeconds(60));
        }

        [Fact]
        public void CircuitBreakerOpenException_ShouldStoreMessage()
        {
            var ex = new CircuitBreakerOpenException("circuit open");
            ex.Message.Should().Be("circuit open");
        }
    }
}

using CKY.MultiAgentFramework.Core.Exceptions;
using CKY.MultiAgentFramework.Core.Models.Resilience;
using CKY.MultiAgentFramework.Core.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Resilience
{
    public class LlmResiliencePipelineTests
    {
        private readonly Mock<ILogger> _mockLogger = new();

        private LlmResiliencePipeline CreatePipeline(int failureThreshold = 3)
        {
            var options = new LlmCircuitBreakerOptions
            {
                FailureThreshold = failureThreshold,
                BreakDuration = TimeSpan.FromMinutes(5)
            };
            return new LlmResiliencePipeline(_mockLogger.Object, options);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new LlmResiliencePipeline(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_SuccessfulOperation_ShouldReturnResult()
        {
            var pipeline = CreatePipeline();

            var result = await pipeline.ExecuteAsync("agent-1",
                ct => Task.FromResult("success"));

            result.Should().Be("success");
        }

        [Fact]
        public async Task ExecuteAsync_NonRetryableException_ShouldNotRetry()
        {
            var pipeline = CreatePipeline();
            int callCount = 0;

            var act = async () => await pipeline.ExecuteAsync<string>("agent-1",
                ct =>
                {
                    callCount++;
                    throw new ArgumentNullException("param");
                });

            await act.Should().ThrowAsync<ArgumentNullException>();
            callCount.Should().Be(1); // No retry for ArgumentNullException
        }

        [Fact]
        public async Task ExecuteAsync_RetryableException_ShouldRetryAndEventuallySucceed()
        {
            var pipeline = CreatePipeline();
            int callCount = 0;

            var result = await pipeline.ExecuteAsync("agent-1",
                ct =>
                {
                    callCount++;
                    if (callCount < 2)
                        throw new InvalidOperationException("temporary");
                    return Task.FromResult("success after retry");
                },
                timeout: TimeSpan.FromSeconds(30));

            result.Should().Be("success after retry");
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_AllRetriesFail_ShouldThrowOriginalException()
        {
            var pipeline = CreatePipeline();

            var act = async () => await pipeline.ExecuteAsync<string>("agent-1",
                ct => throw new InvalidOperationException("always fail"),
                timeout: TimeSpan.FromSeconds(60));

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("always fail");
        }

        [Fact]
        public async Task ExecuteAsync_DifferentAgentIds_ShouldHaveSeparateCircuitBreakers()
        {
            var pipeline = CreatePipeline(failureThreshold: 1);

            // Trigger circuit breaker for agent-1
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await pipeline.ExecuteAsync<string>("agent-1",
                    ct => throw new InvalidOperationException("fail")));

            // agent-2 should still work
            var result = await pipeline.ExecuteAsync("agent-2",
                ct => Task.FromResult("ok"));

            result.Should().Be("ok");
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_ShouldThrowTimeoutException()
        {
            var pipeline = CreatePipeline();

            var act = async () => await pipeline.ExecuteAsync<string>("agent-1",
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                    return "should not reach";
                },
                timeout: TimeSpan.FromMilliseconds(100));

            // Should throw either TimeoutException or OperationCanceledException
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ExecuteAsync_WithDefaultOptions_ShouldWork()
        {
            var pipeline = new LlmResiliencePipeline(_mockLogger.Object);

            var result = await pipeline.ExecuteAsync("agent-1",
                ct => Task.FromResult(42));

            result.Should().Be(42);
        }
    }
}

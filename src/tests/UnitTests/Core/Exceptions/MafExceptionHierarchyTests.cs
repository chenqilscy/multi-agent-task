using CKY.MultiAgentFramework.Core.Exceptions;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Exceptions
{
    public class MafExceptionHierarchyTests
    {
        // ========== LlmServiceException ==========

        [Fact]
        public void LlmServiceException_ShouldSetDefaultProperties()
        {
            var ex = new LlmServiceException("API调用失败");

            ex.Message.Should().Be("API调用失败");
            ex.ErrorCode.Should().Be(MafErrorCode.LlmServiceError);
            ex.Component.Should().Be("LlmService");
            ex.IsRetryable.Should().BeTrue();
            ex.StatusCode.Should().BeNull();
            ex.IsRateLimited.Should().BeFalse();
        }

        [Fact]
        public void LlmServiceException_WithStatusCode_ShouldSetStatusCode()
        {
            var ex = new LlmServiceException("服务器错误", statusCode: 500);

            ex.StatusCode.Should().Be(500);
            ex.IsRateLimited.Should().BeFalse();
        }

        [Fact]
        public void LlmServiceException_WithRateLimited_ShouldSetFlag()
        {
            var ex = new LlmServiceException("限流", statusCode: 429, isRateLimited: true);

            ex.StatusCode.Should().Be(429);
            ex.IsRateLimited.Should().BeTrue();
        }

        // ========== CacheServiceException ==========

        [Fact]
        public void CacheServiceException_ShouldSetDefaultProperties()
        {
            var ex = new CacheServiceException("缓存失败");

            ex.Message.Should().Be("缓存失败");
            ex.ErrorCode.Should().Be(MafErrorCode.CacheServiceError);
            ex.Component.Should().Be("CacheStore");
            ex.IsRetryable.Should().BeTrue();
        }

        // ========== DatabaseException ==========

        [Fact]
        public void DatabaseException_NonTransient_ShouldNotBeRetryable()
        {
            var ex = new DatabaseException("主键冲突", isTransient: false);

            ex.Message.Should().Be("主键冲突");
            ex.ErrorCode.Should().Be(MafErrorCode.DatabaseError);
            ex.Component.Should().Be("RelationalDatabase");
            ex.IsTransient.Should().BeFalse();
            ex.IsRetryable.Should().BeFalse();
        }

        [Fact]
        public void DatabaseException_Transient_ShouldBeRetryable()
        {
            var ex = new DatabaseException("连接超时", isTransient: true);

            ex.IsTransient.Should().BeTrue();
            ex.IsRetryable.Should().BeTrue();
        }

        [Fact]
        public void DatabaseException_DefaultIsTransient_ShouldBeFalse()
        {
            var ex = new DatabaseException("错误");

            ex.IsTransient.Should().BeFalse();
            ex.IsRetryable.Should().BeFalse();
        }

        // ========== VectorStoreException ==========

        [Fact]
        public void VectorStoreException_ShouldSetDefaultProperties()
        {
            var ex = new VectorStoreException("搜索超时");

            ex.Message.Should().Be("搜索超时");
            ex.ErrorCode.Should().Be(MafErrorCode.VectorStoreError);
            ex.Component.Should().Be("VectorStore");
            ex.IsRetryable.Should().BeTrue();
        }

        // ========== TaskSchedulingException ==========

        [Fact]
        public void TaskSchedulingException_ShouldSetTaskId()
        {
            var ex = new TaskSchedulingException("task-123", "调度失败");

            ex.Message.Should().Be("调度失败");
            ex.TaskId.Should().Be("task-123");
            ex.ErrorCode.Should().Be(MafErrorCode.TaskSchedulingError);
            ex.Component.Should().Be("TaskScheduler");
            ex.IsRetryable.Should().BeFalse();
        }

        [Fact]
        public void TaskSchedulingException_NullTaskId_ShouldBeAllowed()
        {
            var ex = new TaskSchedulingException(null, "通用调度错误");

            ex.TaskId.Should().BeNull();
            ex.Message.Should().Be("通用调度错误");
        }

        // ========== LlmResilienceException ==========

        [Fact]
        public void LlmResilienceException_ShouldNotBeRetryable()
        {
            var ex = new LlmResilienceException("重试耗尽");

            ex.Message.Should().Be("重试耗尽");
            ex.ErrorCode.Should().Be(MafErrorCode.LlmServiceError);
            ex.Component.Should().Be("LlmResiliencePipeline");
            ex.IsRetryable.Should().BeFalse();
            ex.InnerException.Should().BeNull();
        }

        [Fact]
        public void LlmResilienceException_WithInnerException_ShouldPreserveChain()
        {
            var inner = new InvalidOperationException("原始错误");
            var ex = new LlmResilienceException("管道失败", inner);

            ex.InnerException.Should().Be(inner);
            ex.InnerException!.Message.Should().Be("原始错误");
        }

        // ========== MafErrorCode Enum ==========

        [Theory]
        [InlineData(MafErrorCode.Unknown, 1000)]
        [InlineData(MafErrorCode.LlmServiceError, 2000)]
        [InlineData(MafErrorCode.LlmRateLimited, 2001)]
        [InlineData(MafErrorCode.CacheServiceError, 3000)]
        [InlineData(MafErrorCode.DatabaseError, 4000)]
        [InlineData(MafErrorCode.VectorStoreError, 5000)]
        [InlineData(MafErrorCode.TaskSchedulingError, 6000)]
        public void MafErrorCode_ShouldHaveExpectedValues(MafErrorCode code, int expected)
        {
            ((int)code).Should().Be(expected);
        }

        // ========== Inheritance ==========

        [Fact]
        public void AllExceptions_ShouldInheritFromException()
        {
            new LlmServiceException("test").Should().BeAssignableTo<Exception>();
            new CacheServiceException("test").Should().BeAssignableTo<Exception>();
            new DatabaseException("test").Should().BeAssignableTo<Exception>();
            new VectorStoreException("test").Should().BeAssignableTo<Exception>();
            new TaskSchedulingException(null, "test").Should().BeAssignableTo<Exception>();
            new LlmResilienceException("test").Should().BeAssignableTo<Exception>();
        }
    }
}

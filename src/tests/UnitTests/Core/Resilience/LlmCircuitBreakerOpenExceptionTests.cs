using CKY.MultiAgentFramework.Core.Resilience;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Resilience;

public class LlmCircuitBreakerOpenExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        var ex = new LlmCircuitBreakerOpenException("breaker open");
        ex.Message.Should().Be("breaker open");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInner_ShouldSetBoth()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new LlmCircuitBreakerOpenException("breaker open", inner);

        ex.Message.Should().Be("breaker open");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ShouldBeExceptionSubclass()
    {
        var ex = new LlmCircuitBreakerOpenException("test");
        ex.Should().BeAssignableTo<Exception>();
    }
}

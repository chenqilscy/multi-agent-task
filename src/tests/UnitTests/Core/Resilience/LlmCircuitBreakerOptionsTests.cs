using CKY.MultiAgentFramework.Core.Models.Resilience;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Resilience
{
    public class LlmCircuitBreakerOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeSet()
        {
            // Arrange & Act
            var options = new LlmCircuitBreakerOptions();

            // Assert
            Assert.Equal(3, options.FailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(5), options.BreakDuration);
            Assert.Equal(TimeSpan.FromSeconds(30), options.HalfOpenTimeout);
        }

        [Fact]
        public void CanSetCustomValues()
        {
            // Arrange
            var options = new LlmCircuitBreakerOptions
            {
                FailureThreshold = 5,
                BreakDuration = TimeSpan.FromMinutes(10),
                HalfOpenTimeout = TimeSpan.FromSeconds(60)
            };

            // Act & Assert
            Assert.Equal(5, options.FailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(10), options.BreakDuration);
            Assert.Equal(TimeSpan.FromSeconds(60), options.HalfOpenTimeout);
        }

        [Fact]
        public void FailureThreshold_ShouldAllowPositiveValues()
        {
            // Arrange
            var options = new LlmCircuitBreakerOptions();

            // Act
            options.FailureThreshold = 1;

            // Assert
            Assert.Equal(1, options.FailureThreshold);
        }
    }
}

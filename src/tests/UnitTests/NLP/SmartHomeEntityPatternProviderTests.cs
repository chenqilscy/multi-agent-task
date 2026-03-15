using CKY.MultiAgentFramework.Demos.SmartHome;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    public class SmartHomeEntityPatternProviderTests
    {
        [Fact]
        public void GetFewShotExamples_ShouldReturnNonEmptyString()
        {
            // Arrange
            var provider = new SmartHomeEntityPatternProvider();

            // Act
            var examples = provider.GetFewShotExamples();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(examples));
            Assert.Contains("输入", examples);
            Assert.Contains("输出", examples);
        }

        [Fact]
        public void GetPatterns_WithValidEntityType_ReturnsPatterns()
        {
            // Arrange
            var provider = new SmartHomeEntityPatternProvider();

            // Act
            var patterns = provider.GetPatterns("Room");

            // Assert
            Assert.NotNull(patterns);
            Assert.Contains("客厅", patterns);
        }

        [Fact]
        public void GetPatterns_WithInvalidEntityType_ReturnsNull()
        {
            // Arrange
            var provider = new SmartHomeEntityPatternProvider();

            // Act
            var patterns = provider.GetPatterns("InvalidType");

            // Assert
            Assert.Null(patterns);
        }

        [Fact]
        public void GetSupportedEntityTypes_ReturnsExpectedTypes()
        {
            // Arrange
            var provider = new SmartHomeEntityPatternProvider();

            // Act
            var types = provider.GetSupportedEntityTypes();

            // Assert
            Assert.Contains("Room", types);
            Assert.Contains("Device", types);
            Assert.Contains("Action", types);
        }
    }
}

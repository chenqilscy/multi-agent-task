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
    }
}

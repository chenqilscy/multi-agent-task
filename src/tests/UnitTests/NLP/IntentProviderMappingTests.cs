using CKY.MultiAgentFramework.Core.Abstractions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    // Mock providers for testing
    public class LightControlEntityPatternProvider : IEntityPatternProvider
    {
        public string?[]? GetPatterns(string entityType)
        {
            if (entityType == "device")
                return new[] { "灯", "light" };
            return null;
        }

        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return new[] { "device" };
        }

        public string GetFewShotExamples()
        {
            return "Example few-shot content for light control";
        }
    }

    public class ACControlEntityPatternProvider : IEntityPatternProvider
    {
        public string?[]? GetPatterns(string entityType)
        {
            if (entityType == "device")
                return new[] { "空调", "AC" };
            return null;
        }

        public IEnumerable<string> GetSupportedEntityTypes()
        {
            return new[] { "device" };
        }

        public string GetFewShotExamples()
        {
            return "Example few-shot content for AC control";
        }
    }

    public class IntentProviderMappingTests
    {
        [Fact]
        public void Register_WithValidParameters_ShouldStoreMapping()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            var intent = "ControlLight";
            var providerType = typeof(LightControlEntityPatternProvider);

            // Act
            mapping.Register(intent, providerType);

            // Assert
            var result = mapping.GetProviderType(intent);
            Assert.Equal(providerType, result);
        }

        [Fact]
        public void Register_WithNullIntent_ShouldThrow()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                mapping.Register(null!, typeof(LightControlEntityPatternProvider)));
        }

        [Fact]
        public void Register_WithNonProviderType_ShouldThrow()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                mapping.Register("TestIntent", typeof(string)));
        }

        [Fact]
        public void GetProviderType_WithUnregisteredIntent_ShouldReturnNull()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act
            var result = mapping.GetProviderType("NonExistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRegisteredIntents_ShouldReturnAllRegistered()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            mapping.Register("Intent1", typeof(LightControlEntityPatternProvider));
            mapping.Register("Intent2", typeof(ACControlEntityPatternProvider));

            // Act
            var intents = mapping.GetRegisteredIntents();

            // Assert
            Assert.Contains("Intent1", intents);
            Assert.Contains("Intent2", intents);
        }

        [Fact]
        public void Register_WithEmptyIntent_ShouldThrow()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                mapping.Register("   ", typeof(LightControlEntityPatternProvider)));
        }

        [Fact]
        public void Register_WithNullProviderType_ShouldThrow()
        {
            // Arrange
            var mapping = new IntentProviderMapping();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                mapping.Register("TestIntent", null!));
        }

        [Fact]
        public void GetProviderType_WithNullIntent_ShouldReturnNull()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            mapping.Register("TestIntent", typeof(LightControlEntityPatternProvider));

            // Act
            var result = mapping.GetProviderType(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetProviderType_WithWhitespaceIntent_ShouldReturnNull()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            mapping.Register("TestIntent", typeof(LightControlEntityPatternProvider));

            // Act
            var result = mapping.GetProviderType("   ");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Register_WithCaseInsensitiveIntent_ShouldOverwrite()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            var intent1 = "ControlLight";
            var intent2 = "controllight"; // Different case
            var providerType1 = typeof(LightControlEntityPatternProvider);
            var providerType2 = typeof(ACControlEntityPatternProvider);

            // Act
            mapping.Register(intent1, providerType1);
            mapping.Register(intent2, providerType2);

            // Assert
            var result = mapping.GetProviderType(intent1);
            Assert.Equal(providerType2, result); // Should be overwritten
        }

        [Fact]
        public void GetProviderType_WithCaseInsensitiveLookup_ShouldSucceed()
        {
            // Arrange
            var mapping = new IntentProviderMapping();
            var intent = "ControlLight";
            var providerType = typeof(LightControlEntityPatternProvider);

            // Act
            mapping.Register(intent, providerType);
            var result1 = mapping.GetProviderType("controllight");
            var result2 = mapping.GetProviderType("CONTROLLIGHT");

            // Assert
            Assert.Equal(providerType, result1);
            Assert.Equal(providerType, result2);
        }
    }
}

using CKY.MultiAgentFramework.Core.Abstractions;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Abstractions;

// A test implementation of IEntityPatternProvider for testing IntentProviderMapping
file class TestEntityPatternProvider : IEntityPatternProvider
{
    public string?[]? GetPatterns(string entityType) => new string?[] { "test" };
    public IEnumerable<string> GetSupportedEntityTypes() => new[] { "test_type" };
    public string GetFewShotExamples() => "example";
}

file class NonProvider { }

public class IntentProviderMappingTests
{
    [Fact]
    public void Register_ValidIntentAndType_ShouldSucceed()
    {
        var mapping = new IntentProviderMapping();
        mapping.Register("control_light", typeof(TestEntityPatternProvider));

        mapping.GetProviderType("control_light").Should().Be(typeof(TestEntityPatternProvider));
    }

    [Fact]
    public void Register_NullIntent_ShouldThrow()
    {
        var mapping = new IntentProviderMapping();
        Assert.Throws<ArgumentException>(() => mapping.Register(null!, typeof(TestEntityPatternProvider)));
    }

    [Fact]
    public void Register_EmptyIntent_ShouldThrow()
    {
        var mapping = new IntentProviderMapping();
        Assert.Throws<ArgumentException>(() => mapping.Register("", typeof(TestEntityPatternProvider)));
    }

    [Fact]
    public void Register_WhitespaceIntent_ShouldThrow()
    {
        var mapping = new IntentProviderMapping();
        Assert.Throws<ArgumentException>(() => mapping.Register("   ", typeof(TestEntityPatternProvider)));
    }

    [Fact]
    public void Register_NullType_ShouldThrow()
    {
        var mapping = new IntentProviderMapping();
        Assert.Throws<ArgumentNullException>(() => mapping.Register("test", null!));
    }

    [Fact]
    public void Register_NonProviderType_ShouldThrow()
    {
        var mapping = new IntentProviderMapping();
        Assert.Throws<ArgumentException>(() => mapping.Register("test", typeof(NonProvider)));
    }

    [Fact]
    public void Register_CaseInsensitive_ShouldOverwrite()
    {
        var mapping = new IntentProviderMapping();
        mapping.Register("Control_LIGHT", typeof(TestEntityPatternProvider));

        mapping.GetProviderType("control_light").Should().Be(typeof(TestEntityPatternProvider));
        mapping.GetProviderType("CONTROL_LIGHT").Should().Be(typeof(TestEntityPatternProvider));
    }

    [Fact]
    public void GetProviderType_Registered_ShouldReturnType()
    {
        var mapping = new IntentProviderMapping();
        mapping.Register("query_weather", typeof(TestEntityPatternProvider));

        mapping.GetProviderType("query_weather").Should().NotBeNull();
    }

    [Fact]
    public void GetProviderType_NotRegistered_ShouldReturnNull()
    {
        var mapping = new IntentProviderMapping();
        mapping.GetProviderType("unknown_intent").Should().BeNull();
    }

    [Fact]
    public void GetProviderType_NullInput_ShouldReturnNull()
    {
        var mapping = new IntentProviderMapping();
        mapping.GetProviderType(null!).Should().BeNull();
    }

    [Fact]
    public void GetProviderType_EmptyInput_ShouldReturnNull()
    {
        var mapping = new IntentProviderMapping();
        mapping.GetProviderType("").Should().BeNull();
    }

    [Fact]
    public void GetRegisteredIntents_ShouldReturnAll()
    {
        var mapping = new IntentProviderMapping();
        mapping.Register("intent_a", typeof(TestEntityPatternProvider));
        mapping.Register("intent_b", typeof(TestEntityPatternProvider));

        var intents = mapping.GetRegisteredIntents().ToList();
        intents.Should().HaveCount(2);
        intents.Should().Contain("intent_a");
        intents.Should().Contain("intent_b");
    }

    [Fact]
    public void GetRegisteredIntents_Empty_ShouldReturnEmpty()
    {
        var mapping = new IntentProviderMapping();
        mapping.GetRegisteredIntents().Should().BeEmpty();
    }
}

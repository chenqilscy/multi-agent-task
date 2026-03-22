using CKY.MultiAgentFramework.Services.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Services.Serialization;

public class JsonSerializerHelperTests
{
    private readonly Mock<ILogger> _logger = new();

    #region Serialize

    [Fact]
    public void Serialize_ValidObject_ShouldReturnJson()
    {
        var obj = new TestDto { Name = "Alice", Age = 30 };
        var json = JsonSerializerHelper.Serialize(obj);

        json.Should().Contain("\"Name\"");
        json.Should().Contain("Alice");
        json.Should().Contain("30");
    }

    [Fact]
    public void Serialize_NullObject_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JsonSerializerHelper.Serialize<TestDto>(null!));
    }

    [Fact]
    public void Serialize_NullProperties_ShouldOmitNullFields()
    {
        var obj = new TestDto { Name = "Bob", NullableField = null };
        var json = JsonSerializerHelper.Serialize(obj);

        json.Should().NotContain("NullableField");
    }

    #endregion

    #region Deserialize

    [Fact]
    public void Deserialize_ValidJson_ShouldReturnObject()
    {
        var json = "{\"Name\":\"Alice\",\"Age\":30}";
        var result = JsonSerializerHelper.Deserialize<TestDto>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_EmptyString_ShouldReturnDefault()
    {
        var result = JsonSerializerHelper.Deserialize<TestDto>("", logger: _logger.Object);
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WhitespaceString_ShouldReturnDefault()
    {
        var result = JsonSerializerHelper.Deserialize<TestDto>("   ", logger: _logger.Object);
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_NullString_ShouldReturnDefault()
    {
        var result = JsonSerializerHelper.Deserialize<TestDto>(null!, logger: _logger.Object);
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_InvalidJson_ShouldThrowInvalidOperation()
    {
        var action = () => JsonSerializerHelper.Deserialize<TestDto>("not json", logger: _logger.Object);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_WithContext_ShouldIncludeContextInError()
    {
        var action = () => JsonSerializerHelper.Deserialize<TestDto>("{invalid", context: "test-context", logger: _logger.Object);
        action.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region TryDeserialize

    [Fact]
    public void TryDeserialize_ValidJson_ShouldReturnObject()
    {
        var json = "{\"Name\":\"Test\"}";
        var result = JsonSerializerHelper.TryDeserialize<TestDto>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ShouldReturnDefault()
    {
        var result = JsonSerializerHelper.TryDeserialize<TestDto>("broken json", logger: _logger.Object);
        result.Should().BeNull();
    }

    [Fact]
    public void TryDeserialize_EmptyString_ShouldReturnDefault()
    {
        var result = JsonSerializerHelper.TryDeserialize<TestDto>("");
        result.Should().BeNull();
    }

    #endregion

    #region SerializeWithVersion / DeserializeWithVersion

    [Fact]
    public void SerializeWithVersion_ShouldWrapData()
    {
        var obj = new TestDto { Name = "Versioned" };
        var json = JsonSerializerHelper.SerializeWithVersion(obj);

        json.Should().Contain("Version");
        json.Should().Contain("Data");
        json.Should().Contain("1.0");
    }

    [Fact]
    public void DeserializeWithVersion_ValidJson_ShouldReturnObject()
    {
        var obj = new TestDto { Name = "Roundtrip", Age = 25 };
        var json = JsonSerializerHelper.SerializeWithVersion(obj);
        var result = JsonSerializerHelper.DeserializeWithVersion<TestDto>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Roundtrip");
        result.Age.Should().Be(25);
    }

    [Fact]
    public void DeserializeWithVersion_EmptyString_ShouldReturnDefault()
    {
        var result = JsonSerializerHelper.DeserializeWithVersion<TestDto>("", logger: _logger.Object);
        result.Should().BeNull();
    }

    [Fact]
    public void DeserializeWithVersion_InvalidJson_ShouldThrow()
    {
        var action = () => JsonSerializerHelper.DeserializeWithVersion<TestDto>("broken", logger: _logger.Object);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeWithVersion_VersionMismatch_ShouldStillDeserialize()
    {
        var obj = new TestDto { Name = "Old" };
        var json = JsonSerializerHelper.SerializeWithVersion(obj, "0.9");
        var result = JsonSerializerHelper.DeserializeWithVersion<TestDto>(json, "1.0", _logger.Object);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Old");
    }

    #endregion

    #region Roundtrip

    [Fact]
    public void Roundtrip_Serialize_Deserialize_ShouldPreserveData()
    {
        var original = new TestDto { Name = "Round", Age = 42 };
        var json = JsonSerializerHelper.Serialize(original);
        var restored = JsonSerializerHelper.Deserialize<TestDto>(json);

        restored.Should().NotBeNull();
        restored!.Name.Should().Be("Round");
        restored.Age.Should().Be(42);
    }

    #endregion

    private class TestDto
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? NullableField { get; set; }
    }
}

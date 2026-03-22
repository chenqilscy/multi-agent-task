using CKY.MultiAgentFramework.Core.Abstractions;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Abstractions;

public class DocumentParseResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new DocumentParseResult();
        result.Text.Should().BeEmpty();
        result.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_ShouldWork()
    {
        var result = new DocumentParseResult
        {
            Text = "Extracted document text content",
            Metadata = new Dictionary<string, object>
            {
                ["title"] = "Test Document",
                ["pageCount"] = 5
            }
        };

        result.Text.Should().Be("Extracted document text content");
        result.Metadata.Should().HaveCount(2);
        result.Metadata["title"].Should().Be("Test Document");
    }
}

public class MetricsContextTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var ctx = new MetricsContext();
        ctx.OperationName.Should().BeEmpty();
        ctx.StartTime.Should().Be(default);
        ctx.Success.Should().BeFalse();
        ctx.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_ShouldWork()
    {
        var now = DateTime.UtcNow;
        var ctx = new MetricsContext
        {
            OperationName = "llm.chat",
            StartTime = now,
            Success = true,
            Tags = new Dictionary<string, string>
            {
                ["provider"] = "zhipuai",
                ["model"] = "glm-4"
            }
        };

        ctx.OperationName.Should().Be("llm.chat");
        ctx.StartTime.Should().Be(now);
        ctx.Success.Should().BeTrue();
        ctx.Tags.Should().HaveCount(2);
    }
}

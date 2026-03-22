using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models;

public class MemoryAndDialogModelTests
{
    #region EpisodicMemory

    [Fact]
    public void EpisodicMemory_DefaultValues()
    {
        var memory = new EpisodicMemory();
        memory.MemoryId.Should().NotBeNullOrEmpty();
        memory.Summary.Should().BeEmpty();
        memory.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        memory.Tags.Should().BeEmpty();
    }

    [Fact]
    public void EpisodicMemory_SetProperties()
    {
        var memory = new EpisodicMemory
        {
            MemoryId = "mem-1",
            Summary = "User asked about weather",
            Tags = new List<string> { "weather", "query" }
        };

        memory.MemoryId.Should().Be("mem-1");
        memory.Summary.Should().Be("User asked about weather");
        memory.Tags.Should().HaveCount(2);
    }

    #endregion

    #region SemanticMemory

    [Fact]
    public void SemanticMemory_DefaultValues()
    {
        var memory = new SemanticMemory();
        memory.Key.Should().BeEmpty();
        memory.Value.Should().BeEmpty();
        memory.Embedding.Should().BeNull();
        memory.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SemanticMemory_SetProperties()
    {
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var memory = new SemanticMemory
        {
            Key = "user_preference",
            Value = "likes warm weather",
            Embedding = embedding,
            Tags = new List<string> { "preference" }
        };

        memory.Key.Should().Be("user_preference");
        memory.Value.Should().Be("likes warm weather");
        memory.Embedding.Should().BeEquivalentTo(embedding);
        memory.Tags.Should().ContainSingle("preference");
    }

    #endregion

    #region MemoryContext

    [Fact]
    public void MemoryContext_DefaultValues()
    {
        var context = new MemoryContext();
        context.EpisodicMemories.Should().BeEmpty();
        context.SemanticMemories.Should().BeEmpty();
        context.WorkingMemory.Should().BeEmpty();
    }

    [Fact]
    public void MemoryContext_CanAddMemories()
    {
        var context = new MemoryContext
        {
            EpisodicMemories = new List<EpisodicMemory>
            {
                new() { Summary = "ep1" }
            },
            SemanticMemories = new List<SemanticMemory>
            {
                new() { Key = "key1", Value = "val1" }
            },
            WorkingMemory = new Dictionary<string, object>
            {
                ["temp"] = 25
            }
        };

        context.EpisodicMemories.Should().HaveCount(1);
        context.SemanticMemories.Should().HaveCount(1);
        context.WorkingMemory.Should().ContainKey("temp");
    }

    #endregion

    #region ForgettingCandidate / ForgettingDecision

    [Fact]
    public void ForgettingCandidate_DefaultValues()
    {
        var candidate = new ForgettingCandidate();
        candidate.MemoryId.Should().BeEmpty();
        candidate.Decision.Should().Be(ForgettingDecision.Keep);
        candidate.Reason.Should().BeEmpty();
    }

    [Fact]
    public void ForgettingCandidate_SetProperties()
    {
        var candidate = new ForgettingCandidate
        {
            MemoryId = "mem-old",
            Decision = ForgettingDecision.Delete,
            Reason = "Expired and irrelevant"
        };

        candidate.MemoryId.Should().Be("mem-old");
        candidate.Decision.Should().Be(ForgettingDecision.Delete);
        candidate.Reason.Should().Be("Expired and irrelevant");
    }

    [Theory]
    [InlineData(ForgettingDecision.Keep)]
    [InlineData(ForgettingDecision.Downgrade)]
    [InlineData(ForgettingDecision.MarkForCleanup)]
    [InlineData(ForgettingDecision.Delete)]
    public void ForgettingDecision_AllValues(ForgettingDecision decision)
    {
        var candidate = new ForgettingCandidate { Decision = decision };
        candidate.Decision.Should().Be(decision);
    }

    #endregion

    #region MemoryClassificationResult

    [Fact]
    public void MemoryClassificationResult_DefaultValues()
    {
        var result = new MemoryClassificationResult();
        result.ShortTermMemories.Should().BeEmpty();
        result.LongTermMemories.Should().BeEmpty();
        result.ForgettingCandidates.Should().BeEmpty();
    }

    #endregion

    #region ShortTermMemory

    [Fact]
    public void ShortTermMemory_DefaultValues()
    {
        var stm = new ShortTermMemory();
        stm.Key.Should().BeEmpty();
        stm.Expiry.Should().Be(TimeSpan.FromHours(24));
        stm.Reason.Should().BeEmpty();
    }

    #endregion

    #region LongTermMemory

    [Fact]
    public void LongTermMemory_DefaultValues()
    {
        var ltm = new LongTermMemory();
        ltm.Key.Should().BeEmpty();
        ltm.Value.Should().BeEmpty();
        ltm.ImportanceScore.Should().Be(0);
        ltm.Tags.Should().BeEmpty();
        ltm.Reason.Should().BeEmpty();
    }

    [Fact]
    public void LongTermMemory_SetProperties()
    {
        var ltm = new LongTermMemory
        {
            Key = "user.name",
            Value = "Alice",
            ImportanceScore = 0.9,
            Tags = new List<string> { "user-profile" },
            Reason = "User identification"
        };

        ltm.ImportanceScore.Should().Be(0.9);
        ltm.Tags.Should().ContainSingle("user-profile");
    }

    #endregion
}

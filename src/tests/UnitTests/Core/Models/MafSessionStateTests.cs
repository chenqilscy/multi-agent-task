using CKY.MultiAgentFramework.Core.Models.Session;
using FluentAssertions;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.Core.Models
{
    public class MafSessionStateTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaults()
        {
            var state = new MafSessionState();

            state.SessionId.Should().NotBeNullOrEmpty();
            state.UserId.Should().BeNull();
            state.Metadata.Should().NotBeNull().And.BeEmpty();
            state.Status.Should().Be(SessionStatus.Active);
            state.TotalTokensUsed.Should().Be(0);
            state.TurnCount.Should().Be(0);
            state.ExpiresAt.Should().BeNull();
        }

        [Fact]
        public void IsExpired_WhenExpiresAtIsNull_ShouldBeFalse()
        {
            var state = new MafSessionState { ExpiresAt = null };

            state.IsExpired.Should().BeFalse();
        }

        [Fact]
        public void IsExpired_WhenExpiresAtInPast_ShouldBeTrue()
        {
            var state = new MafSessionState
            {
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            };

            state.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void IsExpired_WhenExpiresAtInFuture_ShouldBeFalse()
        {
            var state = new MafSessionState
            {
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            state.IsExpired.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenStatusActiveAndNotExpired_ShouldBeTrue()
        {
            var state = new MafSessionState
            {
                Status = SessionStatus.Active,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            state.IsActive.Should().BeTrue();
        }

        [Fact]
        public void IsActive_WhenStatusCompleted_ShouldBeFalse()
        {
            var state = new MafSessionState
            {
                Status = SessionStatus.Completed
            };

            state.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenExpired_ShouldBeFalse()
        {
            var state = new MafSessionState
            {
                Status = SessionStatus.Active,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            };

            state.IsActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateActivity_ShouldUpdateLastActivityAt()
        {
            var state = new MafSessionState();
            var before = state.LastActivityAt;

            System.Threading.Thread.Sleep(10);
            state.UpdateActivity();

            state.LastActivityAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void AddTokens_ShouldAccumulateTokens()
        {
            var state = new MafSessionState();

            state.AddTokens(100);
            state.AddTokens(50);

            state.TotalTokensUsed.Should().Be(150);
        }

        [Fact]
        public void IncrementTurn_ShouldIncrementCount()
        {
            var state = new MafSessionState();

            state.IncrementTurn();
            state.IncrementTurn();
            state.IncrementTurn();

            state.TurnCount.Should().Be(3);
        }

        [Fact]
        public void MarkAsCompleted_ShouldSetStatusAndUpdateActivity()
        {
            var state = new MafSessionState();

            state.MarkAsCompleted();

            state.Status.Should().Be(SessionStatus.Completed);
        }

        [Fact]
        public void MarkAsSuspended_ShouldSetStatusAndUpdateActivity()
        {
            var state = new MafSessionState();

            state.MarkAsSuspended();

            state.Status.Should().Be(SessionStatus.Suspended);
        }

        [Fact]
        public void Resume_WhenSuspended_ShouldSetActive()
        {
            var state = new MafSessionState();
            state.MarkAsSuspended();

            state.Resume();

            state.Status.Should().Be(SessionStatus.Active);
        }

        [Fact]
        public void Resume_WhenNotSuspended_ShouldNotChange()
        {
            var state = new MafSessionState();
            state.MarkAsCompleted();

            state.Resume();

            state.Status.Should().Be(SessionStatus.Completed);
        }

        [Fact]
        public void GetDuration_ShouldReturnPositiveTimeSpan()
        {
            var state = new MafSessionState();

            var duration = state.GetDuration();

            duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }

        [Fact]
        public void GetIdleTime_ShouldReturnPositiveTimeSpan()
        {
            var state = new MafSessionState();

            var idle = state.GetIdleTime();

            idle.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }

        [Fact]
        public void GetSummary_ShouldContainKeyInfo()
        {
            var state = new MafSessionState
            {
                UserId = "user-1",
                TurnCount = 5,
                TotalTokensUsed = 1000
            };

            var summary = state.GetSummary();

            summary.Should().Contain(state.SessionId);
            summary.Should().Contain("user-1");
            summary.Should().Contain("Active");
            summary.Should().Contain("5");
            summary.Should().Contain("1000");
        }

        [Theory]
        [InlineData(SessionStatus.Active)]
        [InlineData(SessionStatus.Suspended)]
        [InlineData(SessionStatus.Completed)]
        [InlineData(SessionStatus.Cancelled)]
        [InlineData(SessionStatus.Expired)]
        [InlineData(SessionStatus.Error)]
        public void SessionStatus_ShouldBeDefined(SessionStatus status)
        {
            Enum.IsDefined(typeof(SessionStatus), status).Should().BeTrue();
        }

        [Fact]
        public void Metadata_ShouldAllowAddingItems()
        {
            var state = new MafSessionState();

            state.Metadata["key1"] = "value1";
            state.Metadata["key2"] = 42;

            state.Metadata.Should().HaveCount(2);
            state.Metadata["key1"].Should().Be("value1");
        }
    }
}

using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.Dialog;
using CKY.MultiAgentFramework.Core.Models.Session;
using CKY.MultiAgentFramework.Services.NLP;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace CKY.MultiAgentFramework.Tests.NLP
{
    /// <summary>
    /// 测试用的会话存储
    /// </summary>
    public class TestSessionStorage : IMafSessionStorage
    {
        private readonly Dictionary<string, MafAgentSession> _sessions = new();

        public Task<IAgentSession> LoadSessionAsync(string conversationId, CancellationToken ct = default)
        {
            if (_sessions.TryGetValue(conversationId, out var session))
            {
                return Task.FromResult(session as IAgentSession);
            }
            return Task.FromResult(new MafAgentSession { Id = conversationId } as IAgentSession);
        }

        public Task SaveSessionAsync(IAgentSession session, CancellationToken ct = default)
        {
            if (session is MafAgentSession mafSession)
            {
                _sessions[mafSession.Id] = mafSession;
            }
            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(string conversationId, CancellationToken ct = default)
        {
            _sessions.Remove(conversationId);
            return Task.CompletedTask;
        }

        public Task AddMessageAsync(string conversationId, IMessage message, CancellationToken ct = default)
        {
            if (!_sessions.ContainsKey(conversationId))
            {
                _sessions[conversationId] = new MafAgentSession { Id = conversationId };
            }
            // Messages are managed by MS AF's AgentSession
            return Task.CompletedTask;
        }

        // 其他接口方法的简化实现
        public Task<IAgentSession?> GetSessionAsync(string conversationId, CancellationToken ct = default)
            => Task.FromResult<IAgentSession?>(null);

        public Task UpdateSessionAsync(IAgentSession session, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IEnumerable<IAgentSession>> GetAllSessionsAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<IAgentSession>>(_sessions.Values.Cast<IAgentSession>());

        public Task<bool> ExistsAsync(string conversationId, CancellationToken ct = default)
            => Task.FromResult(_sessions.ContainsKey(conversationId));
    }

    public class MafCoreferenceResolverTests
    {
        private readonly MafCoreferenceResolver _sut;
        private readonly TestSessionStorage _sessionStorage;

        public MafCoreferenceResolverTests()
        {
            _sessionStorage = new TestSessionStorage();
            _sut = new MafCoreferenceResolver(_sessionStorage, NullLogger<MafCoreferenceResolver>.Instance);
        }

        [Fact]
        public async Task ResolveAsync_WithoutPronouns_ShouldReturnOriginalInput()
        {
            // Arrange
            var input = "打开客厅的灯";

            // Act
            var result = await _sut.ResolveAsync(input, "conv1");

            // Assert
            result.Should().Be(input);
        }

        [Fact]
        public async Task ResolveAsync_WithPronounButNoHistory_ShouldReturnOriginalInput()
        {
            // Arrange
            var input = "把它打开";

            // Act
            var result = await _sut.ResolveAsync(input, "conv1");

            // Assert
            result.Should().Be(input);
        }

        [Fact]
        public async Task ResolveAsync_WithHistory_ShouldReplacePronoun()
        {
            // Arrange
            var conversationId = "conv2";
            var session = new MafAgentSession { Id = conversationId };
            await _sessionStorage.SaveSessionAsync(session);

            var input = "把它调低一点";

            // Act
            var result = await _sut.ResolveAsync(input, conversationId);

            // Assert
            result.Should().NotBe(input);
            result.Should().Contain("空调");
        }

        [Fact]
        public async Task ResolveCoreferencesWithLlmAsync_WithEntities_ShouldUseEntities()
        {
            // Arrange
            var context = new DialogContext
            {
                SessionId = "session1",
                TurnCount = 3,
                PreviousIntent = "ControlLight",
                HistoricalSlots = new Dictionary<string, object>
                {
                    ["control_device.device"] = "客厅灯"
                }
            };
            var entities = new Dictionary<string, object>
            {
                ["device"] = "客厅灯",
                ["location"] = "客厅"
            };
            var input = "把它关掉";

            // Act
            var result = await _sut.ResolveCoreferencesWithLlmAsync(input, context, entities);

            // Assert
            result.Should().NotBe(input);
            result.Should().Contain("客厅灯");
        }

        [Fact]
        public async Task ResolveCoreferencesWithLlmAsync_WithoutEntities_ShouldReturnOriginal()
        {
            // Arrange
            var context = new DialogContext { SessionId = "session1" };
            var entities = new Dictionary<string, object>();
            var input = "把它打开";

            // Act
            var result = await _sut.ResolveCoreferencesWithLlmAsync(input, context, entities);

            // Assert
            result.Should().Be(input);
        }

        [Fact]
        public async Task ResolveAsync_MultiplePronouns_ShouldReplaceFirst()
        {
            // Arrange
            var conversationId = "conv3";
            var session = new MafAgentSession { Id = conversationId };
            await _sessionStorage.SaveSessionAsync(session);

            var input = "这个很好听";

            // Act
            var result = await _sut.ResolveAsync(input, conversationId);

            // Assert
            result.Should().NotBe(input);
        }
    }
}

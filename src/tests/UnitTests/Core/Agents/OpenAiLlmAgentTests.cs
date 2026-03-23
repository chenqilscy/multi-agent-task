using CKY.MultiAgentFramework.Core.Agents.Providers;
using CKY.MultiAgentFramework.Core.Models.LLM;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Tests.Core.Agents;

/// <summary>
/// OpenAiLlmAgent 单元测试
/// 使用 MockHttpMessageHandler 模拟 OpenAI API 响应
/// </summary>
public class OpenAiLlmAgentTests
{
    private static LlmProviderConfig CreateConfig(string modelId = "gpt-4o") => new()
    {
        ProviderName = "openai",
        ProviderDisplayName = "OpenAI",
        ApiKey = "sk-test-key-12345678901234567890",
        ApiBaseUrl = "https://api.openai.com/v1/",
        ModelId = modelId,
        SupportedScenarios = [LlmScenario.Chat],
        MaxTokens = 1000,
        Temperature = 0.7,
    };

    private static string BuildChatCompletionJson(string content) => JsonSerializer.Serialize(new
    {
        id = "chatcmpl-123",
        @object = "chat.completion",
        choices = new[]
        {
            new
            {
                index = 0,
                message = new { role = "assistant", content },
                finish_reason = "stop"
            }
        },
        usage = new { prompt_tokens = 10, completion_tokens = 20, total_tokens = 30 }
    });

    private static string BuildStreamChunk(string content, bool done = false)
    {
        if (done) return "data: [DONE]\n\n";
        var json = JsonSerializer.Serialize(new
        {
            choices = new[] { new { delta = new { content }, index = 0 } }
        });
        return $"data: {json}\n\n";
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_SuccessfulResponse_ReturnsContent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("Hello, world!"),
            HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(
            CreateConfig(),
            Mock.Of<ILogger>(),
            httpClient);

        // Act
        var result = await agent.ExecuteAsync("gpt-4o", "Hello", "You are helpful");

        // Assert
        result.Should().Be("Hello, world!");
    }

    [Fact]
    public async Task ExecuteAsync_SendsCorrectRequestBody()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("response"),
            HttpStatusCode.OK,
            onRequest: req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().Result;
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        await agent.ExecuteAsync("gpt-4o", "test prompt", "system prompt");

        // Assert
        capturedBody.Should().NotBeNull();
        using var doc = JsonDocument.Parse(capturedBody!);
        var root = doc.RootElement;
        root.GetProperty("model").GetString().Should().Be("gpt-4o");
        root.GetProperty("temperature").GetDouble().Should().Be(0.7);
        root.GetProperty("max_tokens").GetInt32().Should().Be(1000);

        var messages = root.GetProperty("messages");
        messages.GetArrayLength().Should().Be(2);
        messages[0].GetProperty("role").GetString().Should().Be("system");
        messages[0].GetProperty("content").GetString().Should().Be("system prompt");
        messages[1].GetProperty("role").GetString().Should().Be("user");
        messages[1].GetProperty("content").GetString().Should().Be("test prompt");
    }

    [Fact]
    public async Task ExecuteAsync_NoSystemPrompt_SendsOnlyUserMessage()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("response"),
            HttpStatusCode.OK,
            onRequest: req => capturedBody = req.Content!.ReadAsStringAsync().Result);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        await agent.ExecuteAsync("gpt-4o", "hello");

        // Assert
        using var doc = JsonDocument.Parse(capturedBody!);
        doc.RootElement.GetProperty("messages").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => agent.ExecuteAsync("gpt-4o", "hello"));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyChoices_ThrowsInvalidOperation()
    {
        // Arrange
        var emptyResponse = JsonSerializer.Serialize(new { choices = Array.Empty<object>() });
        var handler = new MockHttpMessageHandler(emptyResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.ExecuteAsync("gpt-4o", "hello"));
    }

    [Fact]
    public async Task ExecuteAsync_PostsToChatCompletionsPath()
    {
        // Arrange
        Uri? capturedUri = null;
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("ok"),
            HttpStatusCode.OK,
            onRequest: req => capturedUri = req.RequestUri);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        await agent.ExecuteAsync("gpt-4o", "hello");

        // Assert
        capturedUri!.PathAndQuery.Should().Contain("chat/completions");
    }

    #endregion

    #region ExecuteStreamingAsync Tests

    [Fact]
    public async Task ExecuteStreamingAsync_ReturnsChunks()
    {
        // Arrange
        var streamBody = BuildStreamChunk("Hello") +
                         BuildStreamChunk(", world") +
                         BuildStreamChunk("!", done: false) +
                         BuildStreamChunk("", done: true);

        var handler = new MockHttpMessageHandler(streamBody, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in agent.ExecuteStreamingAsync("gpt-4o", "hello"))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().HaveCount(3);
        string.Join("", chunks).Should().Be("Hello, world!");
    }

    [Fact]
    public async Task ExecuteStreamingAsync_EmptyStream_ReturnsNoChunks()
    {
        // Arrange
        var streamBody = "data: [DONE]\n\n";
        var handler = new MockHttpMessageHandler(streamBody, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in agent.ExecuteStreamingAsync("gpt-4o", "hello"))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().BeEmpty();
    }

    #endregion

    #region Constructor & Agent Properties Tests

    [Fact]
    public void AgentId_IncludesProviderAndModel()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig("gpt-4o"), Mock.Of<ILogger>(), httpClient);

        agent.AgentId.Should().Be("openai-gpt-4o");
    }

    [Fact]
    public void SupportsScenario_ConfiguredScenario_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var agent = new OpenAiLlmAgent(CreateConfig(), Mock.Of<ILogger>(), httpClient);

        agent.SupportsScenario(LlmScenario.Chat).Should().BeTrue();
        agent.SupportsScenario(LlmScenario.Image).Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OpenAiLlmAgent(null!, Mock.Of<ILogger>()));
    }

    #endregion

    #region Resilience Pipeline Tests

    [Fact]
    public async Task ExecuteAsync_WithResiliencePipeline_DelegatesToPipeline()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(BuildChatCompletionJson("ok"), HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer sk-test-key-12345678901234567890");

        var resilienceMock = new Mock<CKY.MultiAgentFramework.Core.Resilience.ILlmResiliencePipeline>();
        resilienceMock
            .Setup(r => r.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<string>>, TimeSpan, CancellationToken>(
                (_, func, _, ct) => func(ct));

        var agent = new OpenAiLlmAgent(
            CreateConfig(),
            Mock.Of<ILogger>(),
            httpClient,
            resilienceMock.Object);

        // Act
        var result = await agent.ExecuteAsync("gpt-4o", "hello");

        // Assert
        result.Should().Be("ok");
        resilienceMock.Verify(r => r.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    /// <summary>
    /// 可复用的 MockHttpMessageHandler 用于模拟 HTTP 响应
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;
        private readonly Action<HttpRequestMessage>? _onRequest;

        public MockHttpMessageHandler(
            string responseBody,
            HttpStatusCode statusCode,
            Action<HttpRequestMessage>? onRequest = null)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
            _onRequest = onRequest;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onRequest?.Invoke(request);

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}

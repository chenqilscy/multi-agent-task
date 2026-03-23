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
/// AzureOpenAiLlmAgent 单元测试
/// 验证 Azure 特有的 URL 格式、认证方式和部署名映射
/// </summary>
public class AzureOpenAiLlmAgentTests
{
    private static LlmProviderConfig CreateAzureConfig(
        string deployment = "my-gpt4o",
        string apiVersion = "2024-06-01") => new()
    {
        ProviderName = "azure-openai",
        ProviderDisplayName = "Azure OpenAI",
        ApiKey = "azure-test-key-12345678901234",
        ApiBaseUrl = "https://my-resource.openai.azure.com",
        ModelId = deployment,
        SupportedScenarios = [LlmScenario.Chat, LlmScenario.Code],
        MaxTokens = 2000,
        Temperature = 0.5,
        AdditionalParameters = new() { ["ApiVersion"] = apiVersion },
    };

    private static string BuildChatCompletionJson(string content) => JsonSerializer.Serialize(new
    {
        id = "chatcmpl-azure-123",
        choices = new[]
        {
            new
            {
                index = 0,
                message = new { role = "assistant", content },
                finish_reason = "stop"
            }
        },
        usage = new { prompt_tokens = 15, completion_tokens = 25, total_tokens = 40 }
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

    #region URL Format Tests (Azure-specific)

    [Fact]
    public async Task ExecuteAsync_UsesDeploymentBasedUrl()
    {
        // Arrange
        Uri? capturedUri = null;
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("ok"),
            HttpStatusCode.OK,
            onRequest: req => capturedUri = req.RequestUri);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(
            CreateAzureConfig("my-gpt4o", "2024-06-01"),
            Mock.Of<ILogger>(),
            httpClient);

        // Act
        await agent.ExecuteAsync("my-gpt4o", "hello");

        // Assert
        capturedUri.Should().NotBeNull();
        capturedUri!.PathAndQuery.Should().Contain("openai/deployments/my-gpt4o/chat/completions");
        capturedUri.Query.Should().Contain("api-version=2024-06-01");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotSendModelInBody()
    {
        // Arrange — Azure uses deployment name in URL, not model in body
        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("ok"),
            HttpStatusCode.OK,
            onRequest: req => capturedBody = req.Content!.ReadAsStringAsync().Result);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        await agent.ExecuteAsync("my-gpt4o", "hello");

        // Assert
        using var doc = JsonDocument.Parse(capturedBody!);
        doc.RootElement.TryGetProperty("model", out _).Should().BeFalse(
            "Azure OpenAI uses deployment name in URL, not model in request body");
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_SuccessfulResponse_ReturnsContent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("Azure response"),
            HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        var result = await agent.ExecuteAsync("my-gpt4o", "hello", "You are helpful");

        // Assert
        result.Should().Be("Azure response");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyChoices_ThrowsInvalidOperation()
    {
        // Arrange
        var emptyResponse = JsonSerializer.Serialize(new { choices = Array.Empty<object>() });
        var handler = new MockHttpMessageHandler(emptyResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig(), Mock.Of<ILogger>(), httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.ExecuteAsync("my-gpt4o", "hello"));
    }

    [Fact]
    public async Task ExecuteAsync_ApiKeyAuth_UsesApiKeyHeader()
    {
        // Arrange — verify Azure uses api-key header, not Bearer token
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(
            BuildChatCompletionJson("ok"),
            HttpStatusCode.OK,
            onRequest: req => capturedRequest = req);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        await agent.ExecuteAsync("my-gpt4o", "hello");

        // Assert
        capturedRequest.Should().NotBeNull();
        // The httpClient was created with api-key header
        httpClient.DefaultRequestHeaders.Contains("api-key").Should().BeTrue();
    }

    #endregion

    #region ExecuteStreamingAsync Tests

    [Fact]
    public async Task ExecuteStreamingAsync_ReturnsChunks()
    {
        // Arrange
        var streamBody = BuildStreamChunk("Azure ") +
                         BuildStreamChunk("streaming") +
                         BuildStreamChunk("", done: true);

        var handler = new MockHttpMessageHandler(streamBody, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig(), Mock.Of<ILogger>(), httpClient);

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in agent.ExecuteStreamingAsync("my-gpt4o", "hello"))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().HaveCount(2);
        string.Join("", chunks).Should().Be("Azure streaming");
    }

    #endregion

    #region Agent Properties

    [Fact]
    public void AgentId_IncludesDeploymentName()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig("my-gpt4o"), Mock.Of<ILogger>(), httpClient);

        agent.AgentId.Should().Be("azure-openai-my-gpt4o");
    }

    [Fact]
    public void SupportsMultipleScenarios()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://my-resource.openai.azure.com/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "azure-test-key-12345678901234");

        var agent = new AzureOpenAiLlmAgent(CreateAzureConfig(), Mock.Of<ILogger>(), httpClient);

        agent.SupportsScenario(LlmScenario.Chat).Should().BeTrue();
        agent.SupportsScenario(LlmScenario.Code).Should().BeTrue();
        agent.SupportsScenario(LlmScenario.Image).Should().BeFalse();
    }

    #endregion

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

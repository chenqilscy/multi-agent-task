using System.Net;
using System.Text;
using System.Text.Json;
using CKY.MultiAgentFramework.Infrastructure.Embedding;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MAF.Tests.Infrastructure.Embedding;

public class OpenAiEmbeddingServiceTests
{
    private readonly Mock<ILogger<OpenAiEmbeddingService>> _loggerMock = new();

    private OpenAiEmbeddingService CreateService(
        HttpClient httpClient,
        string model = OpenAiEmbeddingService.DefaultModel,
        int dimension = OpenAiEmbeddingService.DefaultDimension)
    {
        return new OpenAiEmbeddingService(httpClient, _loggerMock.Object, model, dimension);
    }

    private static HttpClient CreateHttpClient(MockHttpMessageHandler handler, string baseUrl = "https://api.openai.com/v1/")
    {
        return new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    private static string BuildEmbeddingResponse(float[][] embeddings)
    {
        var data = embeddings.Select((e, i) => new
        {
            @object = "embedding",
            index = i,
            embedding = e
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            @object = "list",
            data,
            model = "text-embedding-3-small",
            usage = new { prompt_tokens = 10, total_tokens = 10 }
        });
    }

    #region Constructor

    [Fact]
    public void Constructor_NullHttpClient_Throws()
    {
        var act = () => new OpenAiEmbeddingService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var handler = new MockHttpMessageHandler("{}");
        using var client = CreateHttpClient(handler);
        var act = () => new OpenAiEmbeddingService(client, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void VectorDimension_ReturnsConfiguredValue()
    {
        var handler = new MockHttpMessageHandler("{}");
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client, dimension: 3072);
        svc.VectorDimension.Should().Be(3072);
    }

    [Fact]
    public void VectorDimension_DefaultIs1536()
    {
        var handler = new MockHttpMessageHandler("{}");
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);
        svc.VectorDimension.Should().Be(1536);
    }

    #endregion

    #region GetEmbeddingAsync

    [Fact]
    public async Task GetEmbeddingAsync_ReturnsEmbedding()
    {
        var expected = new float[] { 0.1f, 0.2f, 0.3f };
        var responseJson = BuildEmbeddingResponse([expected]);
        var handler = new MockHttpMessageHandler(responseJson);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);

        var result = await svc.GetEmbeddingAsync("hello world");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetEmbeddingAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(BuildEmbeddingResponse([[1.0f]]));
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client, model: "text-embedding-3-large");

        await svc.GetEmbeddingAsync("test text");

        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Post);
        handler.CapturedRequest.RequestUri!.PathAndQuery.Should().Contain("embeddings");

        handler.CapturedBody.Should().NotBeNull();
        using var doc = JsonDocument.Parse(handler.CapturedBody!);
        doc.RootElement.GetProperty("model").GetString().Should().Be("text-embedding-3-large");
        doc.RootElement.GetProperty("input").GetString().Should().Be("test text");
    }

    [Fact]
    public async Task GetEmbeddingAsync_EmptyData_ReturnsEmptyArray()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            @object = "list",
            data = Array.Empty<object>(),
            model = "text-embedding-3-small"
        });
        var handler = new MockHttpMessageHandler(responseJson);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);

        var result = await svc.GetEmbeddingAsync("hello");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEmbeddingAsync_NullOrWhitespace_Throws()
    {
        var handler = new MockHttpMessageHandler("{}");
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);

        var actNull = () => svc.GetEmbeddingAsync(null!);
        var actEmpty = () => svc.GetEmbeddingAsync("  ");

        await actNull.Should().ThrowAsync<ArgumentException>();
        await actEmpty.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetEmbeddingAsync_ApiError_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler("{\"error\":\"bad\"}", HttpStatusCode.BadRequest);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);

        var act = () => svc.GetEmbeddingAsync("hello");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region GetEmbeddingsAsync

    [Fact]
    public async Task GetEmbeddingsAsync_ReturnsBatchEmbeddings()
    {
        var emb1 = new float[] { 0.1f, 0.2f };
        var emb2 = new float[] { 0.3f, 0.4f };
        var responseJson = BuildEmbeddingResponse([emb1, emb2]);
        var handler = new MockHttpMessageHandler(responseJson);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client, dimension: 2);

        var result = await svc.GetEmbeddingsAsync(["hello", "world"]);

        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(emb1);
        result[1].Should().BeEquivalentTo(emb2);
    }

    [Fact]
    public async Task GetEmbeddingsAsync_EmptyInput_ReturnsEmptyList()
    {
        var handler = new MockHttpMessageHandler("{}");
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);

        var result = await svc.GetEmbeddingsAsync([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEmbeddingsAsync_FiltersWhitespaceTexts()
    {
        var emb = new float[] { 1.0f };
        var responseJson = BuildEmbeddingResponse([emb]);
        var handler = new MockHttpMessageHandler(responseJson);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client, dimension: 1);

        var result = await svc.GetEmbeddingsAsync(["hello", "  ", "", "world"]);

        // "  " and "" are filtered; only 2 texts should be sent
        // However our mock returns 1 embedding; batch handler will pad with zero vectors
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEmbeddingsAsync_NullInput_Throws()
    {
        var handler = new MockHttpMessageHandler("{}");
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client);

        var act = () => svc.GetEmbeddingsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetEmbeddingsAsync_SendsBatchRequest()
    {
        var responseJson = BuildEmbeddingResponse([[1.0f], [2.0f]]);
        var handler = new MockHttpMessageHandler(responseJson);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client, dimension: 1);

        await svc.GetEmbeddingsAsync(["text1", "text2"]);

        handler.CapturedBody.Should().NotBeNull();
        using var doc = JsonDocument.Parse(handler.CapturedBody!);
        var inputArr = doc.RootElement.GetProperty("input");
        inputArr.ValueKind.Should().Be(JsonValueKind.Array);
        inputArr.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetEmbeddingsAsync_ResponseOrderedByIndex()
    {
        // Return data in reverse order to verify index-based sorting
        var responseJson = JsonSerializer.Serialize(new
        {
            @object = "list",
            data = new[]
            {
                new { @object = "embedding", index = 1, embedding = new float[] { 2.0f } },
                new { @object = "embedding", index = 0, embedding = new float[] { 1.0f } }
            },
            model = "text-embedding-3-small",
            usage = new { prompt_tokens = 5, total_tokens = 5 }
        });
        var handler = new MockHttpMessageHandler(responseJson);
        using var client = CreateHttpClient(handler);
        var svc = CreateService(client, dimension: 1);

        var result = await svc.GetEmbeddingsAsync(["first", "second"]);

        result[0].Should().BeEquivalentTo(new float[] { 1.0f });
        result[1].Should().BeEquivalentTo(new float[] { 2.0f });
    }

    #endregion

    #region MockHttpMessageHandler

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedBody { get; private set; }

        public MockHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content != null)
                CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
            return response;
        }
    }

    #endregion
}

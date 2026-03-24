using CKY.MultiAgentFramework.Demos.CustomerService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using CKY.MultiAgentFramework.Demos.CustomerService.Api;

namespace CKY.MAF.Tests.CustomerService;

/// <summary>
/// Chat API 端点逻辑测试
/// 通过直接调用静态端点方法验证请求处理逻辑
/// </summary>
public class ChatApiEndpointTests
{
    private readonly Mock<IChatService> _mockChatService;

    public ChatApiEndpointTests()
    {
        _mockChatService = new Mock<IChatService>();
    }

    [Fact]
    public async Task SendMessage_WithValidRequest_ReturnsOk()
    {
        var expected = new ChatServiceResponse { Content = "您好！" };
        _mockChatService.Setup(x => x.SendMessageAsync(
                "user1", "session1", "你好", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = new ChatRequest("user1", "session1", "你好");

        // 使用反射调用私有静态方法（Minimal API 端点）
        var method = typeof(ChatApiEndpoints).GetMethod("SendMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull("SendMessage endpoint method should exist");

        var result = await (Task<IResult>)method!.Invoke(null,
            [request, _mockChatService.Object, CancellationToken.None])!;

        result.Should().BeOfType<Ok<ChatServiceResponse>>();
        var okResult = (Ok<ChatServiceResponse>)result;
        okResult.Value!.Content.Should().Be("您好！");
    }

    [Theory]
    [InlineData(null, "session1", "hello")]
    [InlineData("user1", null, "hello")]
    [InlineData("user1", "session1", null)]
    [InlineData("", "session1", "hello")]
    [InlineData("user1", "", "hello")]
    [InlineData("user1", "session1", "")]
    [InlineData("   ", "session1", "hello")]
    public async Task SendMessage_WithInvalidRequest_ReturnsBadRequest(
        string? userId, string? sessionId, string? message)
    {
        var request = new ChatRequest(userId!, sessionId!, message!);

        var method = typeof(ChatApiEndpoints).GetMethod("SendMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)method!.Invoke(null,
            [request, _mockChatService.Object, CancellationToken.None])!;

        // Results.BadRequest with anonymous type — verify it's not Ok
        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task RequestHumanHandoff_WithValidRequest_ReturnsOk()
    {
        var expected = new ChatServiceResponse { Content = "已转接人工客服" };
        _mockChatService.Setup(x => x.RequestHumanHandoffAsync(
                "user1", "session1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = new HandoffRequest("user1", "session1");

        var method = typeof(ChatApiEndpoints).GetMethod("RequestHumanHandoff",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull("RequestHumanHandoff endpoint method should exist");

        var result = await (Task<IResult>)method!.Invoke(null,
            [request, _mockChatService.Object, CancellationToken.None])!;

        result.Should().BeOfType<Ok<ChatServiceResponse>>();
    }

    [Theory]
    [InlineData(null, "session1")]
    [InlineData("user1", null)]
    [InlineData("", "session1")]
    [InlineData("user1", "")]
    public async Task RequestHumanHandoff_WithInvalidRequest_ReturnsBadRequest(
        string? userId, string? sessionId)
    {
        var request = new HandoffRequest(userId!, sessionId!);

        var method = typeof(ChatApiEndpoints).GetMethod("RequestHumanHandoff",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)method!.Invoke(null,
            [request, _mockChatService.Object, CancellationToken.None])!;

        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task SendMessage_WithClarification_ReturnsClarificationOptions()
    {
        var expected = new ChatServiceResponse
        {
            Content = "请提供您的订单号",
            NeedsClarification = true,
            ClarificationOptions = ["ORD-2024-001", "ORD-2024-002"],
        };
        _mockChatService.Setup(x => x.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var request = new ChatRequest("user1", "session1", "查询订单");

        var method = typeof(ChatApiEndpoints).GetMethod("SendMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)method!.Invoke(null,
            [request, _mockChatService.Object, CancellationToken.None])!;

        var okResult = (Ok<ChatServiceResponse>)result;
        okResult.Value!.NeedsClarification.Should().BeTrue();
        okResult.Value.ClarificationOptions.Should().HaveCount(2);
    }
}

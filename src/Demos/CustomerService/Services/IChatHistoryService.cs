using CKY.MultiAgentFramework.Demos.CustomerService.Entities;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services;

/// <summary>
/// 对话历史服务接口
/// </summary>
public interface IChatHistoryService
{
    /// <summary>创建新的对话会话</summary>
    Task<string> CreateSessionAsync(string? customerId = null, CancellationToken ct = default);

    /// <summary>保存对话消息</summary>
    Task SaveMessageAsync(string sessionId, string role, string content,
        string? intent = null, Dictionary<string, string>? entities = null,
        CancellationToken ct = default);

    /// <summary>获取会话的对话历史</summary>
    Task<List<ChatMessageEntity>> GetSessionMessagesAsync(string sessionId,
        int? limit = null, CancellationToken ct = default);

    /// <summary>关闭会话并可选生成摘要</summary>
    Task CloseSessionAsync(string sessionId, string? summary = null,
        CancellationToken ct = default);

    /// <summary>获取客户的历史会话列表</summary>
    Task<List<ChatSessionEntity>> GetCustomerSessionsAsync(string customerId,
        int pageSize = 20, CancellationToken ct = default);
}

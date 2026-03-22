using CKY.MultiAgentFramework.Demos.CustomerService.Entities;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services;

/// <summary>
/// 客户信息服务接口
/// </summary>
public interface ICustomerService
{
    /// <summary>根据业务客户ID查询</summary>
    Task<CustomerEntity?> GetCustomerAsync(string customerId, CancellationToken ct = default);

    /// <summary>创建或更新客户信息</summary>
    Task<CustomerEntity> UpsertCustomerAsync(string customerId, string name,
        string? email = null, string? phone = null, CancellationToken ct = default);

    /// <summary>更新最后活跃时间</summary>
    Task UpdateLastActiveAsync(string customerId, CancellationToken ct = default);

    /// <summary>获取客户列表</summary>
    Task<List<CustomerEntity>> GetCustomersAsync(int pageSize = 20, CancellationToken ct = default);
}

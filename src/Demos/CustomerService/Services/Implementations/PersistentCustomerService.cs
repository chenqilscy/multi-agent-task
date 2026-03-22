using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// 持久化客户信息服务 - EF Core 实现
/// </summary>
public class PersistentCustomerService : ICustomerService
{
    private readonly CustomerServiceDbContext _db;
    private readonly ILogger<PersistentCustomerService> _logger;

    public PersistentCustomerService(CustomerServiceDbContext db, ILogger<PersistentCustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CustomerEntity?> GetCustomerAsync(string customerId, CancellationToken ct = default)
    {
        return await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
    }

    public async Task<CustomerEntity> UpsertCustomerAsync(string customerId, string name,
        string? email = null, string? phone = null, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

        if (customer != null)
        {
            customer.Name = name;
            if (email != null) customer.Email = email;
            if (phone != null) customer.Phone = phone;
            customer.LastActiveAt = DateTime.UtcNow;
        }
        else
        {
            customer = new CustomerEntity
            {
                CustomerId = customerId,
                Name = name,
                Email = email,
                Phone = phone,
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("客户信息已保存: {CustomerId}", customerId);
        return customer;
    }

    public async Task UpdateLastActiveAsync(string customerId, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (customer == null) return;

        customer.LastActiveAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<CustomerEntity>> GetCustomersAsync(int pageSize = 20, CancellationToken ct = default)
    {
        return await _db.Customers
            .OrderByDescending(c => c.LastActiveAt ?? c.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}

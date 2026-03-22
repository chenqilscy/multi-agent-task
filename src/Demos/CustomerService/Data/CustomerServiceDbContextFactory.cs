using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data;

/// <summary>
/// 设计时工厂，用于 EF Core 迁移命令
/// dotnet ef migrations add InitialCreate --project src/Demos/CustomerService --context CustomerServiceDbContext --output-dir Data/Migrations
/// </summary>
public class CustomerServiceDbContextFactory : IDesignTimeDbContextFactory<CustomerServiceDbContext>
{
    public CustomerServiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CustomerServiceDbContext>();
        optionsBuilder.UseSqlite("Data Source=customer_service.db");

        return new CustomerServiceDbContext(optionsBuilder.Options);
    }
}

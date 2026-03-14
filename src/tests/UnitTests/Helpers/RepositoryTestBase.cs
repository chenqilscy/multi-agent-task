// src/tests/UnitTests/Helpers/RepositoryTestBase.cs
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Tests.Helpers;

public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected MafDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MafDbContext>()
            .UseSqlite("DataSource=:memory:")
            .EnableSensitiveDataLogging()
            .Options;

        DbContext = new MafDbContext(options);
        await DbContext.Database.OpenConnectionAsync();
        await DbContext.Database.EnsureCreatedAsync();

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.CloseConnectionAsync();
        await DbContext.DisposeAsync();
    }

    protected virtual async Task SeedTestDataAsync()
    {
    }
}

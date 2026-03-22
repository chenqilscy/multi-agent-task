using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Data;

/// <summary>
/// 客服系统业务数据库上下文
/// 独立于 MAF 框架的 MafDbContext，管理客服业务实体
/// </summary>
public class CustomerServiceDbContext : DbContext
{
    public CustomerServiceDbContext(DbContextOptions<CustomerServiceDbContext> options)
        : base(options)
    {
    }

    // 客户
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    // 订单
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();
    public DbSet<TrackingEventEntity> TrackingEvents => Set<TrackingEventEntity>();

    // 工单
    public DbSet<TicketEntity> Tickets => Set<TicketEntity>();
    public DbSet<TicketCommentEntity> TicketComments => Set<TicketCommentEntity>();

    // 对话
    public DbSet<ChatSessionEntity> ChatSessions => Set<ChatSessionEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    // 知识库
    public DbSet<FaqEntryEntity> FaqEntries => Set<FaqEntryEntity>();

    // 用户行为
    public DbSet<UserBehaviorRecordEntity> UserBehaviorRecords => Set<UserBehaviorRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerServiceDbContext).Assembly);
    }
}

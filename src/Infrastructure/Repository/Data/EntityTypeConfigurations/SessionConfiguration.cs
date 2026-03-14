using CKY.MultiAgentFramework.Core.Models.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Infrastructure.Repository.Data.EntityTypeConfigurations
{
    /// <summary>
    /// 会话实体配置（统一模型）
    /// </summary>
    /// <remarks>
    /// 直接配置 MafSessionState 为 EF Core 实体
    /// 不再使用单独的 SessionEntity
    /// </remarks>
    public class SessionConfiguration : IEntityTypeConfiguration<MafSessionState>
    {
        public void Configure(EntityTypeBuilder<MafSessionState> builder)
        {
            builder.ToTable("Sessions");

            // 配置主键（使用 SessionId 作为主键）
            builder.HasKey(x => x.SessionId);
            builder.Property(x => x.SessionId)
                .HasMaxLength(100)
                .ValueGeneratedNever();

            // 配置属性
            builder.Property(x => x.UserId)
                .HasMaxLength(100)
                .IsRequired(false);

            // 配置复杂类型的 JSON 序列化
            builder.Property(x => x.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => string.IsNullOrEmpty(v)
                        ? new Dictionary<string, object>()
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, object>()
                )
                .HasMaxLength(4000);

            builder.Property(x => x.Items)
                .HasConversion(
                    v => v != null ? JsonSerializer.Serialize(v, (JsonSerializerOptions)null) : null,
                    v => v != null ? JsonSerializer.Deserialize<IDictionary<string, object>>(v, (JsonSerializerOptions)null) : null
                )
                .HasMaxLength(8000);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>(); // 枚举转 int

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.LastActivityAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired(false);

            builder.Property(x => x.TotalTokensUsed)
                .IsRequired();

            builder.Property(x => x.TurnCount)
                .IsRequired();

            // 索引
            builder.HasIndex(x => x.SessionId).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => x.LastActivityAt);
            builder.HasIndex(x => x.ExpiresAt);

            // 复合索引 - 优化常见查询模式
            builder.HasIndex(x => new { x.UserId, x.LastActivityAt })
                .HasDatabaseName("IX_Sessions_UserId_LastActivityAt");

            builder.HasIndex(x => new { x.Status, x.ExpiresAt })
                .HasDatabaseName("IX_Sessions_Status_ExpiresAt");
        }
    }
}

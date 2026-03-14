using CKY.MultiAgentFramework.Core.Models.Persisted;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CKY.MultiAgentFramework.Repository.Data.EntityTypeConfigurations
{
    /// <summary>
    /// LLM 提供商配置实体配置
    /// </summary>
    public class LlmProviderConfigConfiguration : IEntityTypeConfiguration<LlmProviderConfigEntity>
    {
        public void Configure(EntityTypeBuilder<LlmProviderConfigEntity> builder)
        {
            builder.ToTable("LlmProviderConfigs");

            // 主键
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // 提供商名称（唯一标识）
            builder.Property(x => x.ProviderName)
                .IsRequired()
                .HasMaxLength(50);

            // 显示名称
            builder.Property(x => x.ProviderDisplayName)
                .IsRequired()
                .HasMaxLength(100);

            // API 基础 URL
            builder.Property(x => x.ApiBaseUrl)
                .IsRequired()
                .HasMaxLength(500);

            // API 密钥（加密存储，设置足够长度）
            builder.Property(x => x.ApiKey)
                .IsRequired()
                .HasMaxLength(500);

            // 模型 ID
            builder.Property(x => x.ModelId)
                .IsRequired()
                .HasMaxLength(100);

            // 模型显示名称
            builder.Property(x => x.ModelDisplayName)
                .IsRequired()
                .HasMaxLength(100);

            // 支持的场景（JSON 数组）
            builder.Property(x => x.SupportedScenariosJson)
                .IsRequired()
                .HasMaxLength(200);

            // 最大 token 数
            builder.Property(x => x.MaxTokens)
                .IsRequired();

            // 温度参数
            builder.Property(x => x.Temperature)
                .IsRequired();

            // 是否启用
            builder.Property(x => x.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            // 优先级
            builder.Property(x => x.Priority)
                .IsRequired()
                .HasDefaultValue(0);

            // 成本
            builder.Property(x => x.CostPer1kTokens)
                .IsRequired()
                .HasDefaultValue(0);

            // 附加参数（JSON）
            builder.Property(x => x.AdditionalParametersJson)
                .HasMaxLength(2000);

            // 创建时间
            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 更新时间
            builder.Property(x => x.UpdatedAt);

            // 最后使用时间
            builder.Property(x => x.LastUsedAt);

            // 备注
            builder.Property(x => x.Notes)
                .HasMaxLength(500);

            // 索引
            builder.HasIndex(x => x.ProviderName)
                .IsUnique(); // 提供商名称唯一

            builder.HasIndex(x => x.IsEnabled)
                .HasDatabaseName("idx_llm_provider_is_enabled");

            builder.HasIndex(x => x.Priority)
                .HasDatabaseName("idx_llm_provider_priority");

            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("idx_llm_provider_created_at");

            builder.HasIndex(x => x.LastUsedAt)
                .HasDatabaseName("idx_llm_provider_last_used_at");
        }
    }
}

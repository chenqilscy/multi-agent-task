using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.LLM;
using CKY.MultiAgentFramework.Core.Models.Persisted;
using CKY.MultiAgentFramework.Infrastructure.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace CKY.MultiAgentFramework.Infrastructure.Repository.Repositories
{
    /// <summary>
    /// LLM 提供商配置仓储实现
    /// </summary>
    public class LlmProviderConfigRepository : ILlmProviderConfigRepository
    {
        private readonly MafDbContext _context;

        public LlmProviderConfigRepository(MafDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 根据提供商名称获取配置
        /// </summary>
        public async Task<LlmProviderConfig?> GetByNameAsync(
            string providerName,
            CancellationToken ct = default)
        {
            var entity = await _context.LlmProviderConfigs
                .FirstOrDefaultAsync(x => x.ProviderName == providerName, ct);

            return entity?.ToDomainModel();
        }

        /// <summary>
        /// 获取所有启用的提供商配置
        /// </summary>
        public async Task<List<LlmProviderConfig>> GetAllEnabledAsync(
            CancellationToken ct = default)
        {
            var entities = await _context.LlmProviderConfigs
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.Priority) // 按优先级排序
                .ToListAsync(ct);

            return entities.Select(e => e.ToDomainModel()).ToList();
        }

        /// <summary>
        /// 获取所有提供商配置
        /// </summary>
        public async Task<List<LlmProviderConfig>> GetAllAsync(
            CancellationToken ct = default)
        {
            var entities = await _context.LlmProviderConfigs
                .OrderBy(x => x.Priority)
                .ToListAsync(ct);

            return entities.Select(e => e.ToDomainModel()).ToList();
        }

        /// <summary>
        /// 根据场景获取支持的提供商配置
        /// </summary>
        public async Task<List<LlmProviderConfig>> GetByScenarioAsync(
            LlmScenario scenario,
            CancellationToken ct = default)
        {
            // 对于 PostgreSQL，使用 JSON 操作符在数据库层面筛选
            // 这避免了加载所有配置到内存中（N+1 查询问题）
            var scenarioId = (int)scenario;

            try
            {
                // PostgreSQL JSON 操作符: @> 包含, ? 检查键存在
                // "SupportedScenariosJson" @> '[1]' 意味着 JSON 数组包含值 1
                var entities = await _context.LlmProviderConfigs
                    .Where(x => x.IsEnabled &&
                               x.SupportedScenariosJson.Contains($"\"{scenarioId}\""))
                    .OrderBy(x => x.Priority)
                    .ToListAsync(ct);

                return entities.Select(e => e.ToDomainModel()).ToList();
            }
            catch
            {
                // 如果数据库不支持 JSON 查询，回退到内存筛选
                var allConfigs = await GetAllEnabledAsync(ct);
                return allConfigs
                    .Where(c => c.SupportedScenarios.Contains(scenario))
                    .OrderBy(c => c.Priority)
                    .ToList();
            }
        }

        /// <summary>
        /// 保存或更新提供商配置
        /// </summary>
        public async Task<LlmProviderConfig> SaveAsync(
            LlmProviderConfig config,
            CancellationToken ct = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // 验证配置
            config.Validate();

            // 查找是否已存在
            var existing = await _context.LlmProviderConfigs
                .FirstOrDefaultAsync(x => x.ProviderName == config.ProviderName, ct);

            if (existing != null)
            {
                // 更新
                existing.UpdateFromDomainModel(config);
                await _context.SaveChangesAsync(ct);
                return existing.ToDomainModel();
            }
            else
            {
                // 新增
                var entity = LlmProviderConfigEntity.FromDomainModel(config);
                _context.LlmProviderConfigs.Add(entity);
                await _context.SaveChangesAsync(ct);
                return entity.ToDomainModel();
            }
        }

        /// <summary>
        /// 删除提供商配置
        /// </summary>
        public async Task<bool> DeleteAsync(
            string providerName,
            CancellationToken ct = default)
        {
            var entity = await _context.LlmProviderConfigs
                .FirstOrDefaultAsync(x => x.ProviderName == providerName, ct);

            if (entity == null)
                return false;

            _context.LlmProviderConfigs.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// 检查提供商是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(
            string providerName,
            CancellationToken ct = default)
        {
            return await _context.LlmProviderConfigs
                .AnyAsync(x => x.ProviderName == providerName, ct);
        }

        /// <summary>
        /// 更新最后使用时间
        /// </summary>
        public async Task UpdateLastUsedAsync(
            string providerName,
            CancellationToken ct = default)
        {
            var entity = await _context.LlmProviderConfigs
                .FirstOrDefaultAsync(x => x.ProviderName == providerName, ct);

            if (entity != null)
            {
                entity.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}

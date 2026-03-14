using CKY.MultiAgentFramework.Core.Models.LLM;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Core.Models.Persisted
{
    /// <summary>
    /// LLM 提供商配置实体（用于持久化）
    /// </summary>
    public class LlmProviderConfigEntity
    {
        /// <summary>主键</summary>
        public int Id { get; set; }

        /// <summary>提供商唯一标识（如 zhipuai, tongyi, qwen）</summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>提供商显示名称（如 智谱AI, 通义千问）</summary>
        public string ProviderDisplayName { get; set; } = string.Empty;

        /// <summary>API 基础 URL（如 https://open.bigmodel.cn/api/）</summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>API 密钥（加密存储）</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>模型 ID（如 glm-4, qwen-max）</summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>模型显示名称（如 GLM-4, Qwen-Max）</summary>
        public string ModelDisplayName { get; set; } = string.Empty;

        /// <summary>支持的场景（JSON 数组，如 [1,2,3]）</summary>
        public string SupportedScenariosJson { get; set; } = string.Empty;

        /// <summary>最大 token 数</summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>温度参数（0-2，控制随机性）</summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>优先级（数字越小优先级越高，用于 fallback）</summary>
        public int Priority { get; set; } = 0;

        /// <summary>每 1k tokens 成本（用于成本统计）</summary>
        public decimal CostPer1kTokens { get; set; } = 0;

        /// <summary>附加配置参数（JSON 格式）</summary>
        public string? AdditionalParametersJson { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>更新时间</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>最后使用时间</summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>备注</summary>
        public string? Notes { get; set; }

        #region 转换方法

        /// <summary>
        /// 转换为领域模型 LlmProviderConfig
        /// </summary>
        public LlmProviderConfig ToDomainModel()
        {
            var config = new LlmProviderConfig
            {
                Id = Id,  // 包含数据库主键 ID
                ProviderName = ProviderName,
                ProviderDisplayName = ProviderDisplayName,
                ApiBaseUrl = ApiBaseUrl,
                ApiKey = ApiKey,
                ModelId = ModelId,
                ModelDisplayName = ModelDisplayName,
                SupportedScenarios = ParseSupportedScenarios(),
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                IsEnabled = IsEnabled,
                Priority = Priority,
                CostPer1kTokens = CostPer1kTokens,
                AdditionalParameters = ParseAdditionalParameters()
            };

            return config;
        }

        /// <summary>
        /// 从领域模型 LlmProviderConfig 创建实体
        /// </summary>
        public static LlmProviderConfigEntity FromDomainModel(LlmProviderConfig config)
        {
            return new LlmProviderConfigEntity
            {
                ProviderName = config.ProviderName,
                ProviderDisplayName = config.ProviderDisplayName,
                ApiBaseUrl = config.ApiBaseUrl,
                ApiKey = config.ApiKey,
                ModelId = config.ModelId,
                ModelDisplayName = config.ModelDisplayName,
                SupportedScenariosJson = SerializeSupportedScenarios(config.SupportedScenarios),
                MaxTokens = config.MaxTokens,
                Temperature = config.Temperature,
                IsEnabled = config.IsEnabled,
                Priority = config.Priority,
                CostPer1kTokens = config.CostPer1kTokens,
                AdditionalParametersJson = SerializeAdditionalParameters(config.AdditionalParameters),
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 更新实体的字段
        /// </summary>
        public void UpdateFromDomainModel(LlmProviderConfig config)
        {
            ProviderDisplayName = config.ProviderDisplayName;
            ApiBaseUrl = config.ApiBaseUrl;
            ApiKey = config.ApiKey;
            ModelId = config.ModelId;
            ModelDisplayName = config.ModelDisplayName;
            SupportedScenariosJson = SerializeSupportedScenarios(config.SupportedScenarios);
            MaxTokens = config.MaxTokens;
            Temperature = config.Temperature;
            IsEnabled = config.IsEnabled;
            Priority = config.Priority;
            CostPer1kTokens = config.CostPer1kTokens;
            AdditionalParametersJson = SerializeAdditionalParameters(config.AdditionalParameters);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 解析支持的场景列表
        /// </summary>
        private List<LlmScenario> ParseSupportedScenarios()
        {
            if (string.IsNullOrWhiteSpace(SupportedScenariosJson))
                return new List<LlmScenario>();

            try
            {
                var scenarioIds = JsonSerializer.Deserialize<int[]>(SupportedScenariosJson);
                if (scenarioIds == null)
                    return new List<LlmScenario>();

                // 过滤掉无效的枚举值
                return scenarioIds
                    .Where(id => Enum.IsDefined(typeof(LlmScenario), id))
                    .Select(id => (LlmScenario)id)
                    .ToList();
            }
            catch (JsonException)
            {
                // JSON 反序列化失败，返回空列表
                return new List<LlmScenario>();
            }
        }

        /// <summary>
        /// 序列化支持的场景列表
        /// </summary>
        private static string SerializeSupportedScenarios(List<LlmScenario> scenarios)
        {
            if (scenarios == null || scenarios.Count == 0)
                return "[]";

            var ids = scenarios.Select(s => (int)s).ToArray();
            return JsonSerializer.Serialize(ids);
        }

        /// <summary>
        /// 解析附加参数
        /// </summary>
        private Dictionary<string, object> ParseAdditionalParameters()
        {
            if (string.IsNullOrWhiteSpace(AdditionalParametersJson))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalParametersJson)
                    ?? new Dictionary<string, object>();
            }
            catch (JsonException)
            {
                // JSON 反序列化失败，返回空字典
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 序列化附加参数
        /// </summary>
        private static string? SerializeAdditionalParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            return JsonSerializer.Serialize(parameters);
        }

        #endregion
    }
}

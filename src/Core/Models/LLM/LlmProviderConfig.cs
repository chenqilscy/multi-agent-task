namespace CKY.MultiAgentFramework.Core.Models.LLM
{
    /// <summary>
    /// LLM 提供商配置
    /// 存储在数据库中，支持动态加载和切换
    /// </summary>
    public class LlmProviderConfig
    {
        /// <summary>数据库主键 ID（用于更新现有配置）</summary>
        public int? Id { get; set; }

        /// <summary>提供商唯一标识（如 zhipuai, tongyi, qwen）</summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>提供商显示名称（如 智谱AI, 通义千问）</summary>
        public string ProviderDisplayName { get; set; } = string.Empty;

        /// <summary>API 基础 URL（如 https://open.bigmodel.cn/api/）</summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>API 密钥</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>模型 ID（如 glm-4, qwen-max）</summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>模型显示名称（如 GLM-4, Qwen-Max）</summary>
        public string ModelDisplayName { get; set; } = string.Empty;

        /// <summary>支持的场景（chat, embed, intent, image, video 等）</summary>
        public List<LlmScenario> SupportedScenarios { get; set; } = new();

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
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();

        #region 验证和脱敏方法

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ProviderName))
                errors.Add("ProviderName cannot be empty");

            if (string.IsNullOrWhiteSpace(ApiKey))
                errors.Add("ApiKey cannot be empty");

            if (string.IsNullOrWhiteSpace(ModelId))
                errors.Add("ModelId cannot be empty");

            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
                errors.Add("ApiBaseUrl cannot be empty");

            if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
                errors.Add("ApiBaseUrl must be a valid absolute URI");

            if (SupportedScenarios == null || SupportedScenarios.Count == 0)
                errors.Add("At least one scenario must be supported");

            if (Temperature < 0 || Temperature > 2)
                errors.Add("Temperature must be between 0 and 2");

            if (MaxTokens <= 0)
                errors.Add("MaxTokens must be positive");

            if (errors.Count > 0)
                throw new ArgumentException($"Invalid {nameof(LlmProviderConfig)}: {string.Join("; ", errors)}");
        }

        /// <summary>
        /// 获取用于日志的脱敏 API Key
        /// </summary>
        public string GetApiKeyForLogging()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return "[NOT SET]";

            return ApiKey.Length > 8
                ? $"{ApiKey[..4]}...{ApiKey[^4..]}"
                : "[REDACTED]";
        }

        #endregion
    }
}

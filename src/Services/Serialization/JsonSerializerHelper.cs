using System.Text.Json;
using System.Text.Json.Serialization;
using CKY.MultiAgentFramework.Core.Constants;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Services.Serialization
{
    /// <summary>
    /// JSON 序列化助手
    /// 提供安全的 JSON 序列化和反序列化功能
    /// </summary>
    public static class JsonSerializerHelper
    {
        private static readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            // 安全选项：禁止处理特殊类型
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// 安全地序列化对象为 JSON
        /// </summary>
        public static string Serialize<T>(T obj, ILogger? logger = null)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            try
            {
                return JsonSerializer.Serialize(obj, _serializerOptions);
            }
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Failed to serialize {TypeName}", typeof(T).Name);
                throw new InvalidOperationException(
                    ErrorMessages.PersistenceFailed,
                    ex);
            }
        }

        /// <summary>
        /// 安全地反序列化 JSON 为对象
        /// </summary>
        public static T? Deserialize<T>(string json, string? context = null, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                logger?.LogWarning("Attempted to deserialize empty or null JSON for {TypeName}", typeof(T).Name);
                return default;
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(json, _serializerOptions);
                logger?.LogDebug("Successfully deserialized {TypeName}", typeof(T).Name);
                return result;
            }
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Failed to deserialize JSON for {TypeName}. Context: {Context}. JSON length: {Length}",
                    typeof(T).Name, context ?? "none", json.Length);
                throw new InvalidOperationException(
                    $"{ErrorMessages.DeserializationFailed}: {typeof(T).Name}",
                    ex);
            }
        }

        /// <summary>
        /// 尝试反序列化，失败时返回默认值
        /// </summary>
        public static T? TryDeserialize<T>(string json, string? context = null, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(json, _serializerOptions);
            }
            catch (JsonException ex)
            {
                logger?.LogWarning(ex, "Failed to deserialize JSON for {TypeName}. Context: {Context}",
                    typeof(T).Name, context ?? "none");
                return default;
            }
        }

        /// <summary>
        /// 安全地序列化对象为 JSON（带版本信息）
        /// </summary>
        public static string SerializeWithVersion<T>(T obj, string version = PersistenceConstants.Strategies.SerializationVersion, ILogger? logger = null)
        {
            var wrapper = new VersionedJsonWrapper
            {
                Version = version,
                Data = JsonSerializer.Serialize(obj, _serializerOptions)
            };

            return JsonSerializer.Serialize(wrapper, _serializerOptions);
        }

        /// <summary>
        /// 安全地反序列化带版本信息的 JSON
        /// </summary>
        public static T? DeserializeWithVersion<T>(string json, string expectedVersion = PersistenceConstants.Strategies.SerializationVersion, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                logger?.LogWarning("Attempted to deserialize empty or null JSON for versioned {TypeName}", typeof(T).Name);
                return default;
            }

            try
            {
                var wrapper = JsonSerializer.Deserialize<VersionedJsonWrapper>(json, _serializerOptions);
                if (wrapper == null)
                {
                    logger?.LogWarning("Failed to deserialize version wrapper for {TypeName}", typeof(T).Name);
                    return default;
                }

                // 验证版本
                if (!string.Equals(wrapper.Version, expectedVersion, StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogWarning("Version mismatch for {TypeName}. Expected: {Expected}, Got: {Actual}",
                        typeof(T).Name, expectedVersion, wrapper.Version ?? "null");
                }

                return JsonSerializer.Deserialize<T>(wrapper.Data, _serializerOptions);
            }
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Failed to deserialize versioned JSON for {TypeName}", typeof(T).Name);
                throw new InvalidOperationException(
                    $"{ErrorMessages.DeserializationFailed}: {typeof(T).Name}",
                    ex);
            }
        }

        /// <summary>
        /// 版本化 JSON 包装器
        /// </summary>
        private class VersionedJsonWrapper
        {
            public string Version { get; set; } = string.Empty;
            public string Data { get; set; } = string.Empty;
        }
    }
}

using System.Linq;

namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图到实体模式提供者的映射实现
    /// </summary>
    /// <remarks>
    /// Intent matching is case-insensitive for user convenience.
    /// Registering the same intent (different case) will overwrite the previous mapping.
    /// </remarks>
    public class IntentProviderMapping : IIntentProviderMapping
    {
        private readonly Dictionary<string, Type> _mapping = new(StringComparer.OrdinalIgnoreCase); // Case-insensitive intent matching

        /// <inheritdoc />
        public void Register(string intent, Type providerType)
        {
            if (string.IsNullOrWhiteSpace(intent))
                throw new ArgumentException("Intent cannot be null or whitespace.", nameof(intent));

            if (providerType == null)
                throw new ArgumentNullException(nameof(providerType));

            if (!typeof(IEntityPatternProvider).IsAssignableFrom(providerType))
                throw new ArgumentException($"Type must implement {nameof(IEntityPatternProvider)}: {providerType.Name}", nameof(providerType));

            _mapping[intent] = providerType;
        }

        /// <inheritdoc />
        public Type? GetProviderType(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return null;

            return _mapping.TryGetValue(intent, out var type) ? type : null;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetRegisteredIntents()
        {
            return _mapping.Keys.ToList();
        }
    }
}

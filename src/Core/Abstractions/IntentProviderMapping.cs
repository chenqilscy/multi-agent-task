namespace CKY.MultiAgentFramework.Core.Abstractions
{
    /// <summary>
    /// 意图到实体模式提供者的映射实现
    /// </summary>
    public class IntentProviderMapping : IIntentProviderMapping
    {
        private readonly Dictionary<string, Type> _mapping = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public void Register(string intent, Type providerType)
        {
            if (string.IsNullOrWhiteSpace(intent))
                throw new ArgumentException("Intent cannot be null or whitespace.", nameof(intent));

            if (providerType == null)
                throw new ArgumentNullException(nameof(providerType));

            if (!typeof(IEntityPatternProvider).IsAssignableFrom(providerType))
                throw new ArgumentException($"Type must implement IEntityPatternProvider: {providerType.Name}", nameof(providerType));

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
            return _mapping.Keys;
        }
    }
}

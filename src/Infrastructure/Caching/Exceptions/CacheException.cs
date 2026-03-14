namespace CKY.MultiAgentFramework.Infrastructure.Caching.Exceptions
{
    /// <summary>
    /// 缓存操作异常
    /// </summary>
    public class CacheException : Exception
    {
        public string? Operation { get; }
        public string? Key { get; }

        public CacheException()
        {
        }

        public CacheException(string message) : base(message)
        {
        }

        public CacheException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CacheException(string operation, string key, string message, Exception innerException)
            : base(message, innerException)
        {
            Operation = operation;
            Key = key;
        }

        public CacheException(string operation, string key, Exception innerException)
            : base($"Cache operation '{operation}' failed for key '{key}'", innerException)
        {
            Operation = operation;
            Key = key;
        }
    }
}

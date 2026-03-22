using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Parsing;

/// <summary>
/// 文档解析器工厂 — 根据文件扩展名选择合适的解析器
/// </summary>
public class DocumentParserFactory
{
    private readonly Dictionary<string, IDocumentParser> _parsers;
    private readonly ILogger<DocumentParserFactory> _logger;

    public DocumentParserFactory(
        IEnumerable<IDocumentParser> parsers,
        ILogger<DocumentParserFactory> logger)
    {
        _logger = logger;
        _parsers = parsers.ToDictionary(
            p => p.SupportedExtension.ToLowerInvariant(),
            p => p);
    }

    /// <summary>
    /// 根据文件路径获取合适的解析器
    /// </summary>
    public IDocumentParser GetParser(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".htm") extension = ".html";

        if (_parsers.TryGetValue(extension, out var parser))
        {
            _logger.LogDebug("使用 {ParserType} 解析: {FilePath}",
                parser.GetType().Name, filePath);
            return parser;
        }

        throw new NotSupportedException($"不支持的文件类型: {extension}");
    }

    /// <summary>
    /// 检查是否支持指定文件类型
    /// </summary>
    public bool IsSupported(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".htm") extension = ".html";
        return _parsers.ContainsKey(extension);
    }

    /// <summary>
    /// 获取所有支持的扩展名
    /// </summary>
    public IReadOnlyList<string> SupportedExtensions =>
        _parsers.Keys.ToList().AsReadOnly();
}

using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Parsing;

/// <summary>
/// 纯文本文档解析器 (.txt)
/// </summary>
public class TxtDocumentParser : IDocumentParser
{
    public string SupportedExtension => ".txt";

    private readonly ILogger<TxtDocumentParser> _logger;

    public TxtDocumentParser(ILogger<TxtDocumentParser> logger)
    {
        _logger = logger;
    }

    public Task<DocumentParseResult> ParseAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogDebug("解析 TXT 文件: {FilePath}", filePath);

        var text = File.ReadAllText(filePath);
        return Task.FromResult(new DocumentParseResult
        {
            Text = text,
            Metadata = new Dictionary<string, object>
            {
                ["file_type"] = "txt",
                ["text_length"] = text.Length
            }
        });
    }
}

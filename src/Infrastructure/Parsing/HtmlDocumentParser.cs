using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace CKY.MultiAgentFramework.Infrastructure.Parsing;

/// <summary>
/// HTML 文档解析器 (.html/.htm) — 使用正则模式提取文本（无第三方依赖）
/// </summary>
public partial class HtmlDocumentParser : IDocumentParser
{
    public string SupportedExtension => ".html";

    private readonly ILogger<HtmlDocumentParser> _logger;

    public HtmlDocumentParser(ILogger<HtmlDocumentParser> logger)
    {
        _logger = logger;
    }

    public Task<DocumentParseResult> ParseAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogDebug("解析 HTML 文件: {FilePath}", filePath);

        var html = File.ReadAllText(filePath);
        var text = StripHtml(html);

        return Task.FromResult(new DocumentParseResult
        {
            Text = text,
            Metadata = new Dictionary<string, object>
            {
                ["file_type"] = "html",
                ["text_length"] = text.Length
            }
        });
    }

    private static string StripHtml(string html)
    {
        // 移除 script/style 标签及内容
        var noScript = ScriptStyleRegex().Replace(html, " ");
        // 移除 HTML 标签
        var noTags = TagRegex().Replace(noScript, " ");
        // 解码 HTML 实体
        var decoded = System.Net.WebUtility.HtmlDecode(noTags);
        // 合并多余空白
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

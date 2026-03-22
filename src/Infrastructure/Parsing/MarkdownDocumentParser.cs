using CKY.MultiAgentFramework.Core.Abstractions;
using Markdig;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Infrastructure.Parsing;

/// <summary>
/// Markdown 文档解析器 (.md) — 使用 Markdig 提取纯文本并解析标题结构
/// </summary>
public class MarkdownDocumentParser : IDocumentParser
{
    public string SupportedExtension => ".md";

    private readonly ILogger<MarkdownDocumentParser> _logger;
    private readonly MarkdownPipeline _pipeline;

    public MarkdownDocumentParser(ILogger<MarkdownDocumentParser> logger)
    {
        _logger = logger;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public Task<DocumentParseResult> ParseAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogDebug("解析 Markdown 文件: {FilePath}", filePath);

        var markdown = File.ReadAllText(filePath);
        var text = Markdown.ToPlainText(markdown, _pipeline);
        var headings = ExtractHeadings(markdown);

        return Task.FromResult(new DocumentParseResult
        {
            Text = text,
            Metadata = new Dictionary<string, object>
            {
                ["file_type"] = "markdown",
                ["text_length"] = text.Length,
                ["headings"] = headings
            }
        });
    }

    private static List<string> ExtractHeadings(string markdown)
    {
        var headings = new List<string>();
        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                int level = 0;
                while (level < trimmed.Length && trimmed[level] == '#') level++;
                var heading = trimmed[level..].Trim();
                if (!string.IsNullOrEmpty(heading))
                    headings.Add(heading);
            }
        }
        return headings;
    }
}

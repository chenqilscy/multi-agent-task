using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CKY.MultiAgentFramework.Infrastructure.Parsing;

/// <summary>
/// CSV 文档解析器 (.csv) — 将 CSV 转为结构化文本（无第三方依赖）
/// </summary>
public class CsvDocumentParser : IDocumentParser
{
    public string SupportedExtension => ".csv";

    private readonly ILogger<CsvDocumentParser> _logger;

    public CsvDocumentParser(ILogger<CsvDocumentParser> logger)
    {
        _logger = logger;
    }

    public Task<DocumentParseResult> ParseAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogDebug("解析 CSV 文件: {FilePath}", filePath);

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        if (lines.Length == 0)
        {
            return Task.FromResult(new DocumentParseResult { Text = string.Empty });
        }

        var headers = ParseCsvLine(lines[0]);
        var textParts = new List<string>();
        int rowCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            rowCount++;
            var fields = ParseCsvLine(lines[i]);
            var parts = new List<string>();

            for (int j = 0; j < Math.Min(headers.Count, fields.Count); j++)
            {
                var header = string.IsNullOrWhiteSpace(headers[j]) ? $"Column{j + 1}" : headers[j];
                parts.Add($"{header}: {fields[j]}");
            }

            textParts.Add($"[Row {rowCount}] {string.Join(" | ", parts)}");
        }

        var text = string.Join("\n", textParts);
        return Task.FromResult(new DocumentParseResult
        {
            Text = text,
            Metadata = new Dictionary<string, object>
            {
                ["file_type"] = "csv",
                ["row_count"] = rowCount,
                ["headers"] = headers
            }
        });
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            else
            {
                if (ch == '"') { inQuotes = true; }
                else if (ch == ',') { fields.Add(current.ToString().Trim()); current.Clear(); }
                else { current.Append(ch); }
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}

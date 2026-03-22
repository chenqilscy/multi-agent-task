namespace CKY.MultiAgentFramework.Core.Abstractions;

/// <summary>
/// 文档解析结果
/// </summary>
public class DocumentParseResult
{
    /// <summary>提取的文本内容</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>文档元数据（标题、页码等）</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 文档解析器接口 — 将不同格式的文件解析为纯文本
/// </summary>
/// <remarks>
/// <para>Core 层抽象，Infrastructure 层提供具体实现（PDF、DOCX、Markdown 等）</para>
/// </remarks>
public interface IDocumentParser
{
    /// <summary>
    /// 异步解析文档，提取文本内容
    /// </summary>
    /// <param name="filePath">文档文件的完整路径</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>解析结果</returns>
    Task<DocumentParseResult> ParseAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// 解析器支持的文件扩展名（含点号，如 ".pdf"）
    /// </summary>
    string SupportedExtension { get; }
}

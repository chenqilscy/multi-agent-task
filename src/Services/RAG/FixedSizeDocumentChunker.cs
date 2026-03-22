using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;

namespace CKY.MultiAgentFramework.Services.RAG;

/// <summary>
/// 固定大小文档分块器 — 按固定字符数切分文本，支持重叠
/// </summary>
public class FixedSizeDocumentChunker : IDocumentChunker
{
    public Task<List<DocumentChunk>> ChunkAsync(
        string text,
        string documentId,
        ChunkingConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new ChunkingConfig();
        var chunks = new List<DocumentChunk>();

        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(chunks);

        int maxSize = config.MaxChunkSize;
        int overlap = (int)(maxSize * config.OverlapRatio);
        int step = maxSize - overlap;
        if (step <= 0) step = 1;

        int index = 0;
        int position = 0;

        while (position < text.Length)
        {
            ct.ThrowIfCancellationRequested();

            int length = Math.Min(maxSize, text.Length - position);
            string content = text.Substring(position, length);

            if (config.RespectStructure)
            {
                content = AdjustToStructureBoundary(text, position, length);
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    ChunkIndex = index,
                    Content = content.Trim()
                });
                index++;
            }

            position += step;
        }

        return Task.FromResult(chunks);
    }

    /// <summary>
    /// 调整分块边界到段落或句子结尾
    /// </summary>
    private static string AdjustToStructureBoundary(string text, int start, int length)
    {
        int end = Math.Min(start + length, text.Length);
        string segment = text.Substring(start, end - start);

        // 尝试在段落边界截断
        int lastNewline = segment.LastIndexOf('\n');
        if (lastNewline > segment.Length / 2)
        {
            return segment[..lastNewline];
        }

        // 尝试在句子边界截断
        int lastPeriod = segment.LastIndexOfAny(['.', '。', '！', '？', '!', '?']);
        if (lastPeriod > segment.Length / 2)
        {
            return segment[..(lastPeriod + 1)];
        }

        return segment;
    }
}

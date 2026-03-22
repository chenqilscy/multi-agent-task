using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Core.Models.RAG;
using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// RAG增强的知识库服务
/// 结合EF Core关键词匹配 + RAG语义检索，提供混合搜索能力
/// </summary>
public class RagEnhancedKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly CustomerServiceDbContext _db;
    private readonly IRagPipeline _ragPipeline;
    private readonly ILogger<RagEnhancedKnowledgeBaseService> _logger;

    /// <summary>RAG知识库集合名称</summary>
    public const string CollectionName = "customer-service-knowledge";

    public RagEnhancedKnowledgeBaseService(
        CustomerServiceDbContext db,
        IRagPipeline ragPipeline,
        ILogger<RagEnhancedKnowledgeBaseService> logger)
    {
        _db = db;
        _ragPipeline = ragPipeline;
        _logger = logger;
    }

    public async Task<KnowledgeSearchResult> SearchAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        // 并行执行关键词匹配和RAG语义检索
        var keywordTask = KeywordSearchAsync(query, topK, ct);
        var ragTask = RagSearchAsync(query, topK, ct);

        await Task.WhenAll(keywordTask, ragTask);

        var keywordResults = keywordTask.Result;
        var ragResults = ragTask.Result;

        // 合并结果：去重 + 加权评分
        var merged = MergeResults(keywordResults, ragResults, topK);

        var result = new KnowledgeSearchResult
        {
            RelevantFaqs = merged,
            Confidence = merged.Count > 0 ? merged.Max(f => f.Relevance) : 0,
            SourceReferences = merged.Select(f => f.Id).ToList()
        };

        // 如果最高置信度 > 0.6，直接生成回答
        if (result.Confidence > 0.6 && merged.Count > 0)
        {
            result.GeneratedAnswer = merged.First().Answer;
        }

        return result;
    }

    public async Task<bool> HasDefinitiveAnswerAsync(string query, CancellationToken ct = default)
    {
        var result = await SearchAsync(query, 1, ct);
        return result.Confidence > 0.7;
    }

    public async Task UpsertFaqAsync(FaqEntry entry, CancellationToken ct = default)
    {
        // 保存到数据库
        FaqEntryEntity? existing = null;
        if (int.TryParse(entry.Id, out var id))
            existing = await _db.FaqEntries.FindAsync([id], ct);

        if (existing != null)
        {
            existing.Question = entry.Question;
            existing.Answer = entry.Answer;
            existing.Category = entry.Category;
            existing.KeywordsJson = JsonSerializer.Serialize(entry.Keywords);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.FaqEntries.Add(new FaqEntryEntity
            {
                Question = entry.Question,
                Answer = entry.Answer,
                Category = entry.Category,
                KeywordsJson = JsonSerializer.Serialize(entry.Keywords),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        // 同步摄入到RAG管线
        try
        {
            var docId = $"faq-{entry.Id ?? Guid.NewGuid().ToString()}";
            var text = $"问题：{entry.Question}\n回答：{entry.Answer}\n分类：{entry.Category}";
            await _ragPipeline.IngestAsync(docId, text, CollectionName, ct: ct);
            _logger.LogInformation("FAQ 已同步到 RAG 管线: {Question}", entry.Question);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FAQ 同步到 RAG 管线失败: {Question}", entry.Question);
        }
    }

    /// <summary>关键词匹配搜索</summary>
    private async Task<List<FaqEntry>> KeywordSearchAsync(string query, int topK, CancellationToken ct)
    {
        try
        {
            var activeFaqs = await _db.FaqEntries
                .Where(f => f.IsActive)
                .ToListAsync(ct);

            return activeFaqs
                .Select(faq => new { Faq = faq, Score = CalculateRelevance(query, faq) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(m => new FaqEntry
                {
                    Id = m.Faq.Id.ToString(),
                    Question = m.Faq.Question,
                    Answer = m.Faq.Answer,
                    Category = m.Faq.Category,
                    Keywords = DeserializeKeywords(m.Faq.KeywordsJson),
                    Relevance = m.Score
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "关键词搜索失败，降级为仅RAG搜索");
            return [];
        }
    }

    /// <summary>RAG语义检索</summary>
    private async Task<List<FaqEntry>> RagSearchAsync(string query, int topK, CancellationToken ct)
    {
        try
        {
            var ragRequest = new RagQueryRequest
            {
                Query = query,
                CollectionName = CollectionName,
                TopK = topK,
                ScoreThreshold = 0.3f
            };

            var ragResponse = await _ragPipeline.QueryAsync(ragRequest, ct);

            return ragResponse.RetrievedChunks.Select(chunk => new FaqEntry
            {
                Id = chunk.DocumentId,
                Question = "",
                Answer = chunk.Content,
                Category = "rag",
                Relevance = chunk.Score
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAG语义检索失败，降级为仅关键词匹配");
            return [];
        }
    }

    /// <summary>合并关键词和RAG结果</summary>
    private static List<FaqEntry> MergeResults(
        List<FaqEntry> keywordResults,
        List<FaqEntry> ragResults,
        int topK)
    {
        var merged = new Dictionary<string, FaqEntry>();

        // 关键词结果（权重 0.4）
        foreach (var item in keywordResults)
        {
            var key = item.Id;
            if (merged.TryGetValue(key, out var existing))
            {
                existing.Relevance = Math.Max(existing.Relevance, item.Relevance * 0.4 + existing.Relevance * 0.6);
            }
            else
            {
                item.Relevance *= 0.4;
                merged[key] = item;
            }
        }

        // RAG结果（权重 0.6）
        foreach (var item in ragResults)
        {
            var key = item.Id;
            if (merged.TryGetValue(key, out var existing))
            {
                existing.Relevance = Math.Max(existing.Relevance, item.Relevance * 0.6 + existing.Relevance * 0.4);
            }
            else
            {
                item.Relevance *= 0.6;
                merged[key] = item;
            }
        }

        return merged.Values
            .OrderByDescending(f => f.Relevance)
            .Take(topK)
            .ToList();
    }

    private static double CalculateRelevance(string query, FaqEntryEntity faq)
    {
        var queryLower = query.ToLower();
        double score = 0;

        var keywords = DeserializeKeywords(faq.KeywordsJson);
        foreach (var keyword in keywords)
        {
            if (queryLower.Contains(keyword.ToLower()))
                score += 0.3;
        }

        if (queryLower.Contains(faq.Question.ToLower()))
            score += 0.5;

        return Math.Min(score, 1.0);
    }

    private static List<string> DeserializeKeywords(string keywordsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(keywordsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

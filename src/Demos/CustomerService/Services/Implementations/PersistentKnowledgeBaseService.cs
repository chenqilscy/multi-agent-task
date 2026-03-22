using System.Text.Json;
using CKY.MultiAgentFramework.Demos.CustomerService.Data;
using CKY.MultiAgentFramework.Demos.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CKY.MultiAgentFramework.Demos.CustomerService.Services.Implementations;

/// <summary>
/// 持久化知识库服务 - EF Core 实现（关键词匹配）
/// 生产环境可集成向量数据库实现 RAG 语义检索
/// </summary>
public class PersistentKnowledgeBaseService : IKnowledgeBaseService
{
    private readonly CustomerServiceDbContext _db;
    private readonly ILogger<PersistentKnowledgeBaseService> _logger;

    public PersistentKnowledgeBaseService(CustomerServiceDbContext db, ILogger<PersistentKnowledgeBaseService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<KnowledgeSearchResult> SearchAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        var activeFaqs = await _db.FaqEntries
            .Where(f => f.IsActive)
            .ToListAsync(ct);

        var matches = activeFaqs
            .Select(faq => new
            {
                Faq = faq,
                Score = CalculateRelevance(query, faq)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        var result = new KnowledgeSearchResult
        {
            RelevantFaqs = matches.Select(m => new FaqEntry
            {
                Id = m.Faq.Id.ToString(),
                Question = m.Faq.Question,
                Answer = m.Faq.Answer,
                Category = m.Faq.Category,
                Keywords = DeserializeKeywords(m.Faq.KeywordsJson),
                Relevance = m.Score
            }).ToList(),
            Confidence = matches.Count > 0 ? matches.Max(m => m.Score) : 0
        };

        if (result.RelevantFaqs.Count > 0 && result.Confidence > 0.7)
            result.GeneratedAnswer = result.RelevantFaqs.First().Answer;

        return result;
    }

    public async Task<bool> HasDefinitiveAnswerAsync(string query, CancellationToken ct = default)
    {
        var result = await SearchAsync(query, 1, ct);
        return result.Confidence > 0.7;
    }

    public async Task UpsertFaqAsync(FaqEntry entry, CancellationToken ct = default)
    {
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
        _logger.LogInformation("FAQ 条目已保存: {Question}", entry.Question);
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

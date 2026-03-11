using System.Text.Json;
using CanonGuard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services.AI;

public class SemanticSearchResult
{
    public int ChapterId { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class SemanticSearchService
{
    private readonly AppDbContext _db;
    private readonly EmbeddingService _embeddingService;

    public SemanticSearchService(
        AppDbContext db,
        EmbeddingService embeddingService)
    {
        _db = db;
        _embeddingService = embeddingService;
    }

    public async Task<List<SemanticSearchResult>> SearchAsync(
        int projectId,
        string query,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SemanticSearchResult>();

        var queryVector = await _embeddingService.CreateEmbeddingAsync(query, cancellationToken);

        var embeddings = await _db.Embeddings
            .Where(e => e.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var ranked = embeddings
            .Select(e =>
            {
                var vector = JsonSerializer.Deserialize<float[]>(e.VectorJson) ?? Array.Empty<float>();

                return new SemanticSearchResult
                {
                    ChapterId = e.ChapterId ?? 0,
                    Text = e.Text,
                    Score = vector.Length == 0
                        ? 0
                        : VectorMath.CosineSimilarity(queryVector, vector)
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(top)
            .ToList();

        return ranked;
    }
}
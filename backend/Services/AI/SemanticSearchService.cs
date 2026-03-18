using System.Text.Json;
using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services.AI;

public class SemanticSearchService : ISemanticSearchService
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

    public async Task<SemanticSearchResponse> SearchAllProjectsAsync(
        string userId,
        string query,
        int top = 8,
        CancellationToken cancellationToken = default)
    {
        query = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            return new SemanticSearchResponse
            {
                Query = query,
                ProjectId = null,
                Results = new List<SemanticSearchItemResponse>()
            };
        }

        var queryVector = await _embeddingService.CreateEmbeddingAsync(query, cancellationToken);

        var rows = await _db.Embeddings
            .AsNoTracking()
            .Where(e => e.ChapterId != null)
            .Join(
                _db.Chapters.AsNoTracking(),
                embedding => embedding.ChapterId,
                chapter => chapter.Id,
                (embedding, chapter) => new
                {
                    embedding.ProjectId,
                    embedding.ChapterId,
                    embedding.Text,
                    embedding.VectorJson,
                    ChapterTitle = chapter.Title
                })
            .Join(
                _db.Projects.AsNoTracking().Where(p => p.OwnerId == userId),
                row => row.ProjectId,
                project => project.Id,
                (row, project) => new
                {
                    row.ProjectId,
                    ProjectTitle = project.Title,
                    ChapterId = row.ChapterId!.Value,
                    row.ChapterTitle,
                    row.Text,
                    row.VectorJson
                })
            .ToListAsync(cancellationToken);

        var ranked = rows
            .Select(row =>
            {
                var vector = DeserializeVector(row.VectorJson);

                var score = vector.Length == 0 || vector.Length != queryVector.Length
                    ? 0
                    : VectorMath.CosineSimilarity(queryVector, vector);

                return new SemanticSearchItemResponse
                {
                    ProjectId = row.ProjectId,
                    ProjectTitle = row.ProjectTitle,
                    ChapterId = row.ChapterId,
                    ChapterTitle = row.ChapterTitle,
                    Text = BuildSnippet(row.Text),
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(Math.Max(1, top))
            .ToList();

        return new SemanticSearchResponse
        {
            Query = query,
            ProjectId = null,
            Results = ranked
        };
    }

    public async Task<SemanticSearchResponse?> SearchProjectAsync(
        string userId,
        int projectId,
        string query,
        int top = 8,
        CancellationToken cancellationToken = default)
    {
        query = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            return new SemanticSearchResponse
            {
                Query = query,
                ProjectId = projectId,
                Results = new List<SemanticSearchItemResponse>()
            };
        }

        var project = await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == projectId && p.OwnerId == userId,
                cancellationToken);

        if (project == null)
        {
            return null;
        }

        var queryVector = await _embeddingService.CreateEmbeddingAsync(query, cancellationToken);

        var rows = await _db.Embeddings
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId && e.ChapterId != null)
            .Join(
                _db.Chapters.AsNoTracking(),
                embedding => embedding.ChapterId,
                chapter => chapter.Id,
                (embedding, chapter) => new
                {
                    ProjectId = embedding.ProjectId,
                    ProjectTitle = project.Title,
                    ChapterId = chapter.Id,
                    ChapterTitle = chapter.Title,
                    embedding.Text,
                    embedding.VectorJson
                })
            .ToListAsync(cancellationToken);

        var ranked = rows
            .Select(row =>
            {
                var vector = DeserializeVector(row.VectorJson);

                var score = vector.Length == 0 || vector.Length != queryVector.Length
                    ? 0
                    : VectorMath.CosineSimilarity(queryVector, vector);

                return new SemanticSearchItemResponse
                {
                    ProjectId = row.ProjectId,
                    ProjectTitle = row.ProjectTitle,
                    ChapterId = row.ChapterId,
                    ChapterTitle = row.ChapterTitle,
                    Text = BuildSnippet(row.Text),
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(Math.Max(1, top))
            .ToList();

        return new SemanticSearchResponse
        {
            Query = query,
            ProjectId = projectId,
            Results = ranked
        };
    }

    private static float[] DeserializeVector(string vectorJson)
    {
        try
        {
            return JsonSerializer.Deserialize<float[]>(vectorJson) ?? Array.Empty<float>();
        }
        catch
        {
            return Array.Empty<float>();
        }
    }

    private static string BuildSnippet(string text, int maxLength = 280)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim();

        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..maxLength].TrimEnd() + "...";
    }
}
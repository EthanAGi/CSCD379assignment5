using System.Text.Json;
using CanonGuard.Api.Data;
using CanonGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services.AI;

public class ChapterEmbeddingService
{
    private readonly AppDbContext _db;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<ChapterEmbeddingService> _logger;

    public ChapterEmbeddingService(
        AppDbContext db,
        EmbeddingService embeddingService,
        ILogger<ChapterEmbeddingService> logger)
    {
        _db = db;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task RegenerateChapterEmbeddingsAsync(
        int chapterId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await _db.Chapters
            .FirstOrDefaultAsync(c => c.Id == chapterId, cancellationToken);

        if (chapter == null)
            throw new InvalidOperationException("Chapter not found.");

        var oldEmbeddings = await _db.Embeddings
            .Where(e => e.ChapterId == chapter.Id)
            .ToListAsync(cancellationToken);

        if (oldEmbeddings.Count > 0)
        {
            _db.Embeddings.RemoveRange(oldEmbeddings);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var chunks = TextChunker.ChunkText(chapter.Content)
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .ToList();

        _logger.LogInformation(
            "Regenerating embeddings for chapter {ChapterId} with {ChunkCount} chunks.",
            chapter.Id,
            chunks.Count);

        foreach (var chunk in chunks)
        {
            var vector = await _embeddingService.CreateEmbeddingAsync(chunk, cancellationToken);

            _db.Embeddings.Add(new Embedding
            {
                ProjectId = chapter.ProjectId,
                ChapterId = chapter.Id,
                Text = chunk,
                VectorJson = JsonSerializer.Serialize(vector),
                CreatedAt = DateTime.UtcNow
            });

            // Small throttle to reduce Azure rate-limit spikes on lower tiers.
            await Task.Delay(250, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated {Count} embeddings for chapter {ChapterId}.",
            chunks.Count,
            chapter.Id);
    }
}
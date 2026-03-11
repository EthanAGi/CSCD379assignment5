using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using CanonGuard.Api.Models;
using CanonGuard.Api.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services;

public class StoryBibleService : IStoryBibleService
{
    private readonly AppDbContext _db;
    private readonly AzureChapterExtractionService _azureChapterExtractionService;

    public StoryBibleService(
        AppDbContext db,
        AzureChapterExtractionService azureChapterExtractionService)
    {
        _db = db;
        _azureChapterExtractionService = azureChapterExtractionService;
    }

    public async Task<ChapterEntityExtractionResponse?> ExtractFromChapterAsync(
        string userId,
        int chapterId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c =>
                c.Id == chapterId &&
                c.Project.OwnerId == userId,
                cancellationToken);

        if (chapter == null)
        {
            return null;
        }

        var aiResult = await _azureChapterExtractionService.ExtractAsync(
            chapter.Title,
            chapter.Content,
            cancellationToken);

        foreach (var character in aiResult.Characters)
        {
            var entity = await UpsertEntityAsync(
                chapter.ProjectId,
                EntityType.Character,
                character.Name,
                cancellationToken);

            await AddFactIfMissingAsync(
                chapter.ProjectId,
                entity.Id,
                "mention",
                character.Name,
                chapter.Id,
                character.SourceQuote,
                character.Confidence,
                cancellationToken);
        }

        foreach (var location in aiResult.Locations)
        {
            var entity = await UpsertEntityAsync(
                chapter.ProjectId,
                EntityType.Location,
                location.Name,
                cancellationToken);

            await AddFactIfMissingAsync(
                chapter.ProjectId,
                entity.Id,
                "mention",
                location.Name,
                chapter.Id,
                location.SourceQuote,
                location.Confidence,
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ChapterEntityExtractionResponse
        {
            ChapterId = chapter.Id,
            ProjectId = chapter.ProjectId,
            Characters = aiResult.Characters.Select(c => new ExtractedEntityDto
            {
                Name = c.Name,
                SourceQuote = c.SourceQuote,
                Confidence = c.Confidence
            }).ToList(),
            Locations = aiResult.Locations.Select(l => new ExtractedEntityDto
            {
                Name = l.Name,
                SourceQuote = l.SourceQuote,
                Confidence = l.Confidence
            }).ToList()
        };
    }

    private async Task<Entity> UpsertEntityAsync(
        int projectId,
        EntityType type,
        string rawName,
        CancellationToken cancellationToken)
    {
        var name = rawName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Entity name cannot be empty.");
        }

        var existing = await _db.Entities.FirstOrDefaultAsync(e =>
            e.ProjectId == projectId &&
            e.Type == type &&
            e.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (existing != null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var entity = new Entity
        {
            ProjectId = projectId,
            Type = type,
            Name = name,
            SummaryJson = "{}",
            UpdatedAt = DateTime.UtcNow
        };

        _db.Entities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task AddFactIfMissingAsync(
        int projectId,
        int entityId,
        string factType,
        string value,
        int sourceChapterId,
        string sourceQuote,
        double confidence,
        CancellationToken cancellationToken)
    {
        var exists = await _db.Facts.AnyAsync(f =>
            f.ProjectId == projectId &&
            f.EntityId == entityId &&
            f.SourceChapterId == sourceChapterId &&
            f.Value == value &&
            f.SourceQuote == sourceQuote,
            cancellationToken);

        if (exists)
        {
            return;
        }

        _db.Facts.Add(new Fact
        {
            ProjectId = projectId,
            EntityId = entityId,
            FactType = factType,
            Value = value,
            SourceChapterId = sourceChapterId,
            SourceQuote = sourceQuote,
            Confidence = confidence
        });
    }
}
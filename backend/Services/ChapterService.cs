using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using CanonGuard.Api.Models;
using CanonGuard.Api.Services.AI;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services;

public class ChapterService : IChapterService
{
    private readonly AppDbContext _db;
    private readonly ChapterEmbeddingService _chapterEmbeddingService;

    public ChapterService(
        AppDbContext db,
        ChapterEmbeddingService chapterEmbeddingService)
    {
        _db = db;
        _chapterEmbeddingService = chapterEmbeddingService;
    }

    public async Task<List<ChapterResponse>> GetForProjectAsync(string userId, int projectId)
    {
        var ownsProject = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId);

        if (!ownsProject)
        {
            return new List<ChapterResponse>();
        }

        return await _db.Chapters
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ChapterResponse
            {
                Id = c.Id,
                ProjectId = c.ProjectId,
                Title = c.Title,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ChapterResponse?> GetByIdAsync(string userId, int chapterId)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c =>
                c.Id == chapterId &&
                c.Project.OwnerId == userId);

        if (chapter == null)
        {
            return null;
        }

        return new ChapterResponse
        {
            Id = chapter.Id,
            ProjectId = chapter.ProjectId,
            Title = chapter.Title,
            Content = chapter.Content,
            CreatedAt = chapter.CreatedAt,
            UpdatedAt = chapter.UpdatedAt
        };
    }

    public async Task<ChapterResponse?> CreateAsync(string userId, int projectId, CreateChapterRequest request)
    {
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);

        if (project == null)
        {
            return null;
        }

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var chapter = new Chapter
        {
            ProjectId = projectId,
            Title = title,
            Content = request.Content?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        await _chapterEmbeddingService.RegenerateChapterEmbeddingsAsync(chapter.Id);

        return new ChapterResponse
        {
            Id = chapter.Id,
            ProjectId = chapter.ProjectId,
            Title = chapter.Title,
            Content = chapter.Content,
            CreatedAt = chapter.CreatedAt,
            UpdatedAt = chapter.UpdatedAt
        };
    }

    public async Task<ChapterResponse?> UpdateAsync(string userId, int chapterId, UpdateChapterRequest request)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c =>
                c.Id == chapterId &&
                c.Project.OwnerId == userId);

        if (chapter == null)
        {
            return null;
        }

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        chapter.Title = title;
        chapter.Content = request.Content ?? string.Empty;
        chapter.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _chapterEmbeddingService.RegenerateChapterEmbeddingsAsync(chapter.Id);

        return new ChapterResponse
        {
            Id = chapter.Id,
            ProjectId = chapter.ProjectId,
            Title = chapter.Title,
            Content = chapter.Content,
            CreatedAt = chapter.CreatedAt,
            UpdatedAt = chapter.UpdatedAt
        };
    }
}
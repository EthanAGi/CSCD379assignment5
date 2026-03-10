using CanonGuard.Api.DTOs;

namespace CanonGuard.Api.Interfaces;

public interface IChapterService
{
    Task<List<ChapterResponse>> GetForProjectAsync(string userId, int projectId);
    Task<ChapterResponse?> GetByIdAsync(string userId, int chapterId);
    Task<ChapterResponse?> CreateAsync(string userId, int projectId, CreateChapterRequest request);
    Task<ChapterResponse?> UpdateAsync(string userId, int chapterId, UpdateChapterRequest request);
}
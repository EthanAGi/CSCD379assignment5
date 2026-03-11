using CanonGuard.Api.DTOs;

namespace CanonGuard.Api.Interfaces;

public interface IStoryBibleService
{
    Task<ChapterEntityExtractionResponse?> ExtractFromChapterAsync(
        string userId,
        int chapterId,
        CancellationToken cancellationToken = default);
}
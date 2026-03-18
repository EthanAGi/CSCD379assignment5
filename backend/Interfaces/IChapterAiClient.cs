using CanonGuard.Api.Services.AI;

namespace CanonGuard.Api.Interfaces;

public interface IChapterAiClient
{
    Task<AiChapterExtractionResult> ExtractEntitiesAsync(
        string chapterTitle,
        string chapterContent,
        CancellationToken cancellationToken = default);
}
using CanonGuard.Api.DTOs;

namespace CanonGuard.Api.Interfaces;

public interface ISemanticSearchService
{
    Task<SemanticSearchResponse> SearchAllProjectsAsync(
        string userId,
        string query,
        int top = 8,
        CancellationToken cancellationToken = default);

    Task<SemanticSearchResponse?> SearchProjectAsync(
        string userId,
        int projectId,
        string query,
        int top = 8,
        CancellationToken cancellationToken = default);
}
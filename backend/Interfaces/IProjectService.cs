using CanonGuard.Api.DTOs;

namespace CanonGuard.Api.Interfaces;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(string userId, CreateProjectRequest request);
    Task<List<ProjectResponse>> GetForUserAsync(string userId);
}
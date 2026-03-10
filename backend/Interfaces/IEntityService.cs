using CanonGuard.Api.DTOs;

namespace CanonGuard.Api.Interfaces;

public interface IEntityService
{
    Task<List<EntityResponse>> GetForProjectAsync(string userId, int projectId);
    Task<EntityResponse?> CreateAsync(string userId, int projectId, CreateEntityRequest request);
    Task<EntityResponse?> UpdateAsync(string userId, int entityId, UpdateEntityRequest request);
    Task<bool> DeleteAsync(string userId, int entityId);
}
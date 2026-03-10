using System.Text.Json;
using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using CanonGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services;

public class EntityService : IEntityService
{
    private readonly AppDbContext _db;

    public EntityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EntityResponse>> GetForProjectAsync(string userId, int projectId)
    {
        var ownsProject = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId);

        if (!ownsProject)
        {
            return new List<EntityResponse>();
        }

        var entities = await _db.Entities
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Type)
            .ThenBy(e => e.Name)
            .ToListAsync();

        return entities.Select(MapToResponse).ToList();
    }

    public async Task<EntityResponse?> CreateAsync(string userId, int projectId, CreateEntityRequest request)
    {
        var ownsProject = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.OwnerId == userId);

        if (!ownsProject)
        {
            return null;
        }

        var trimmedName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return null;
        }

        if (!Enum.TryParse<EntityType>(request.Type, true, out var parsedType))
        {
            return null;
        }

        var entity = new Entity
        {
            ProjectId = projectId,
            Type = parsedType,
            Name = trimmedName,
            SummaryJson = BuildSummaryJson(request.Summary),
            UpdatedAt = DateTime.UtcNow
        };

        _db.Entities.Add(entity);
        await _db.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<EntityResponse?> UpdateAsync(string userId, int entityId, UpdateEntityRequest request)
    {
        var entity = await _db.Entities
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.Project.OwnerId == userId);

        if (entity == null)
        {
            return null;
        }

        var targetProject = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.OwnerId == userId);

        if (targetProject == null)
        {
            return null;
        }

        var trimmedName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return null;
        }

        if (!Enum.TryParse<EntityType>(request.Type, true, out var parsedType))
        {
            return null;
        }

        entity.ProjectId = request.ProjectId;
        entity.Type = parsedType;
        entity.Name = trimmedName;
        entity.SummaryJson = BuildSummaryJson(request.Summary);
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(string userId, int entityId)
    {
        var entity = await _db.Entities
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == entityId && e.Project.OwnerId == userId);

        if (entity == null)
        {
            return false;
        }

        _db.Entities.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static EntityResponse MapToResponse(Entity entity)
    {
        return new EntityResponse
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            Type = entity.Type.ToString(),
            Name = entity.Name,
            Summary = ExtractSummary(entity.SummaryJson),
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static string BuildSummaryJson(string? summary)
    {
        var payload = new
        {
            summary = summary?.Trim() ?? string.Empty
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string ExtractSummary(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(summaryJson);

            if (doc.RootElement.TryGetProperty("summary", out var summaryElement))
            {
                return summaryElement.GetString() ?? string.Empty;
            }

            return summaryJson;
        }
        catch
        {
            return summaryJson;
        }
    }
}
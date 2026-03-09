using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using CanonGuard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProjectResponse> CreateAsync(string userId, CreateProjectRequest request)
    {
        var project = new Project
        {
            OwnerId = userId,
            Title = request.Title,
            Description = request.Description
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return new ProjectResponse
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            CreatedAt = project.CreatedAt
        };
    }

    public async Task<List<ProjectResponse>> GetForUserAsync(string userId)
    {
        return await _db.Projects
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }
}
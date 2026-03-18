using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanonGuard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;
    private readonly AppDbContext _db;

    public ProjectsController(IProjectService projects, AppDbContext db)
    {
        _projects = projects;
        _db = db;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        return Ok(await _projects.GetForUserAsync(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var project = await _db.Projects
            .Where(p => p.Id == id && p.OwnerId == userId)
            .Select(p => new ProjectDetailResponse
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return NotFound(new { message = "Project not found." });
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Project title is required." });
        }

        var created = await _projects.CreateAsync(userId, request);
        return Ok(created);
    }
}
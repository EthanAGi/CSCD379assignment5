using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanonGuard.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EntitiesController : ControllerBase
{
    private readonly IEntityService _entities;

    public EntitiesController(IEntityService entities)
    {
        _entities = entities;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet("projects/{projectId:int}/entities")]
    public async Task<IActionResult> GetForProject(int projectId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var entities = await _entities.GetForProjectAsync(userId, projectId);
        return Ok(entities);
    }

    [HttpPost("projects/{projectId:int}/entities")]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateEntityRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest(new { message = "Entity type is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Entity name is required." });
        }

        var created = await _entities.CreateAsync(userId, projectId, request);

        if (created == null)
        {
            return BadRequest(new { message = "Project not found or request was invalid." });
        }

        return Ok(created);
    }

    [HttpPut("entities/{entityId:int}")]
    public async Task<IActionResult> Update(int entityId, [FromBody] UpdateEntityRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Entity name is required." });
        }

        var updated = await _entities.UpdateAsync(userId, entityId, request);

        if (updated == null)
        {
            return NotFound(new { message = "Entity not found or request was invalid." });
        }

        return Ok(updated);
    }

    [HttpDelete("entities/{entityId:int}")]
    public async Task<IActionResult> Delete(int entityId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var deleted = await _entities.DeleteAsync(userId, entityId);

        if (!deleted)
        {
            return NotFound(new { message = "Entity not found." });
        }

        return Ok(new { message = "Entity deleted successfully." });
    }
}
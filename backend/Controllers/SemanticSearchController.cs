using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CanonGuard.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanonGuard.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class SemanticSearchController : ControllerBase
{
    private readonly ISemanticSearchService _semanticSearchService;

    public SemanticSearchController(ISemanticSearchService semanticSearchService)
    {
        _semanticSearchService = semanticSearchService;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet("semantic-search")]
    public async Task<IActionResult> SearchAll(
        [FromQuery] string query,
        [FromQuery] int top = 8,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Query is required." });
        }

        var result = await _semanticSearchService.SearchAllProjectsAsync(
            userId,
            query,
            top,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("projects/{projectId:int}/semantic-search")]
    public async Task<IActionResult> SearchProject(
        int projectId,
        [FromQuery] string query,
        [FromQuery] int top = 8,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Query is required." });
        }

        var result = await _semanticSearchService.SearchProjectAsync(
            userId,
            projectId,
            query,
            top,
            cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = "Project not found." });
        }

        return Ok(result);
    }
}
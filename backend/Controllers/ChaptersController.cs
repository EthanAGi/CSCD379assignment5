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
public class ChaptersController : ControllerBase
{
    private readonly IChapterService _chapters;

    public ChaptersController(IChapterService chapters)
    {
        _chapters = chapters;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet("projects/{projectId:int}/chapters")]
    public async Task<IActionResult> GetForProject(int projectId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var chapters = await _chapters.GetForProjectAsync(userId, projectId);
        return Ok(chapters);
    }

    [HttpPost("projects/{projectId:int}/chapters")]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateChapterRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Chapter title is required." });
        }

        var created = await _chapters.CreateAsync(userId, projectId, request);

        if (created == null)
        {
            return NotFound(new { message = "Project not found or title was invalid." });
        }

        return Ok(created);
    }

    [HttpGet("chapters/{chapterId:int}")]
    public async Task<IActionResult> GetById(int chapterId)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var chapter = await _chapters.GetByIdAsync(userId, chapterId);

        if (chapter == null)
        {
            return NotFound(new { message = "Chapter not found." });
        }

        return Ok(chapter);
    }

    [HttpPut("chapters/{chapterId:int}")]
    public async Task<IActionResult> Update(int chapterId, [FromBody] UpdateChapterRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Chapter title is required." });
        }

        var updated = await _chapters.UpdateAsync(userId, chapterId, request);

        if (updated == null)
        {
            return NotFound(new { message = "Chapter not found or title was invalid." });
        }

        return Ok(updated);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Interfaces;
using CanonGuard.Api.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanonGuard.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ChaptersController : ControllerBase
{
    private readonly IChapterService _chapters;
    private readonly IStoryBibleService _storyBibleService;
    private readonly CanonConsistencyService _canonConsistencyService;

    public ChaptersController(
        IChapterService chapters,
        IStoryBibleService storyBibleService,
        CanonConsistencyService canonConsistencyService)
    {
        _chapters = chapters;
        _storyBibleService = storyBibleService;
        _canonConsistencyService = canonConsistencyService;
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

    [HttpPost("chapters/{chapterId:int}/extract-entities")]
    public async Task<IActionResult> ExtractEntities(
        int chapterId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _storyBibleService.ExtractFromChapterAsync(
                userId,
                chapterId,
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Chapter not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = "An unexpected error occurred while extracting entities."
            });
        }
    }

    [HttpPost("chapters/{chapterId:int}/canon-checks")]
    public async Task<ActionResult<CanonCheckResponse>> CheckCanon(
        int chapterId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _canonConsistencyService.CheckChapterAsync(
                userId,
                chapterId,
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Chapter not found." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = "An unexpected error occurred while checking canon consistency."
            });
        }
    }
}
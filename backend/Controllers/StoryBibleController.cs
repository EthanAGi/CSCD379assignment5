using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CanonGuard.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanonGuard.Api.Controllers;

[ApiController]
[Route("api/story-bible")]
[Authorize]
public class StoryBibleController : ControllerBase
{
    private readonly IStoryBibleService _storyBibleService;

    public StoryBibleController(IStoryBibleService storyBibleService)
    {
        _storyBibleService = storyBibleService;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
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
}
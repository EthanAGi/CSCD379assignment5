using CanonGuard.Api.Services.AI;
using Microsoft.AspNetCore.Mvc;

namespace CanonGuard.Api.Controllers;

[ApiController]
[Route("api/ai-test")]
public class AiTestController : ControllerBase
{
    private readonly EmbeddingService _embeddingService;

    public AiTestController(EmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    [HttpGet]
    public async Task<IActionResult> Test(CancellationToken cancellationToken)
    {
        var vector = await _embeddingService.CreateEmbeddingAsync(
            "Hello world",
            cancellationToken);

        return Ok(new
        {
            message = "Embedding generated successfully",
            vectorLength = vector.Length
        });
    }
}
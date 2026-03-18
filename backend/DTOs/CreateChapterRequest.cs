namespace CanonGuard.Api.DTOs;

public class CreateChapterRequest
{
    public string Title { get; set; } = default!;
    public string? Content { get; set; }
}
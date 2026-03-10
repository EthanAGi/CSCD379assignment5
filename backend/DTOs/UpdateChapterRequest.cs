namespace CanonGuard.Api.DTOs;

public class UpdateChapterRequest
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = string.Empty;
}
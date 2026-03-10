namespace CanonGuard.Api.DTOs;

public class ChapterResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
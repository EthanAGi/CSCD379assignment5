namespace CanonGuard.Api.DTOs;

public class ProjectResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
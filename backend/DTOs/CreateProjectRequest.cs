namespace CanonGuard.Api.DTOs;

public class CreateProjectRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
}
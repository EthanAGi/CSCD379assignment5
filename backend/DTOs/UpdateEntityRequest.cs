namespace CanonGuard.Api.DTOs;

public class UpdateEntityRequest
{
    public int ProjectId { get; set; }
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Summary { get; set; }
}
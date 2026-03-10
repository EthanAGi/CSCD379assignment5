namespace CanonGuard.Api.DTOs;

public class CreateEntityRequest
{
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Summary { get; set; }
}
namespace CanonGuard.Api.DTOs;

public class EntityResponse
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Summary { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
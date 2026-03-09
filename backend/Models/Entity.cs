namespace CanonGuard.Api.Models;

public enum EntityType
{
    Character,
    Location,
    Theme,
    Arc
}

public class Entity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public EntityType Type { get; set; }
    public string Name { get; set; } = default!;
    public string SummaryJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
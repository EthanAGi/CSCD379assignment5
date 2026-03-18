namespace CanonGuard.Api.Models;

public enum EntityType
{
    Character = 0,
    Location = 1,
    Theme = 2
}

public class Entity
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public EntityType Type { get; set; }

    public string Name { get; set; } = default!;

    // Stores summary/description JSON (used for Story Bible display)
    public string SummaryJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
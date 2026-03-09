namespace CanonGuard.Api.Models;

public class Project
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Chapter> Chapters { get; set; } = new();
    public List<Entity> Entities { get; set; } = new();
    public List<Fact> Facts { get; set; } = new();
}
namespace CanonGuard.Api.Models;

public class Project
{
    public int Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;
    public AppUser? Owner { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Chapter> Chapters { get; set; } = new();
}
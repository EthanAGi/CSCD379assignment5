namespace CanonGuard.Api.Models;

public class Embedding
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? ChapterId { get; set; }

    public string Text { get; set; } = default!;
    public string VectorJson { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
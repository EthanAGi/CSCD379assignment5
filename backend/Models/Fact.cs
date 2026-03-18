namespace CanonGuard.Api.Models;

public class Fact
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? EntityId { get; set; }
    public Entity? Entity { get; set; }

    public string FactType { get; set; } = default!;
    public string Value { get; set; } = default!;
    public int SourceChapterId { get; set; }
    public string SourceQuote { get; set; } = default!;
    public double Confidence { get; set; }
}
namespace CanonGuard.Api.Services.AI;

public class AiExtractedEntity
{
    public string Name { get; set; } = string.Empty;
    public string SourceQuote { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class AiChapterExtractionResult
{
    public List<AiExtractedEntity> Characters { get; set; } = new();
    public List<AiExtractedEntity> Locations { get; set; } = new();
}
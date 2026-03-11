namespace CanonGuard.Api.DTOs;

public class ExtractedEntityDto
{
    public string Name { get; set; } = string.Empty;
    public string SourceQuote { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class ChapterEntityExtractionResponse
{
    public int ChapterId { get; set; }
    public int ProjectId { get; set; }

    public List<ExtractedEntityDto> Characters { get; set; } = new();
    public List<ExtractedEntityDto> Locations { get; set; } = new();
}
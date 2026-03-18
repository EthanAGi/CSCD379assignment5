namespace CanonGuard.Api.DTOs;

public class SemanticSearchItemResponse
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public int ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class SemanticSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public List<SemanticSearchItemResponse> Results { get; set; } = new();
}
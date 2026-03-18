namespace CanonGuard.Api.DTOs;

public class CanonIssueResponse
{
    public string PassageText { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string ExpectedCanon { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";

    public int? SupportingChapterId { get; set; }
    public string SupportingQuote { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;
}

public class CanonCheckResponse
{
    public int ChapterId { get; set; }
    public int ProjectId { get; set; }
    public List<CanonIssueResponse> Issues { get; set; } = new();
}
using System.Text;
using System.Text.Json;
using CanonGuard.Api.Data;
using CanonGuard.Api.DTOs;
using CanonGuard.Api.Models.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CanonGuard.Api.Services.AI;

public class CanonConsistencyService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly AzureAiOptions _options;
    private readonly ILogger<CanonConsistencyService> _logger;

    public CanonConsistencyService(
        AppDbContext db,
        HttpClient httpClient,
        IOptions<AzureAiOptions> options,
        ILogger<CanonConsistencyService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CanonCheckResponse?> CheckChapterAsync(
        string userId,
        int chapterId,
        CancellationToken cancellationToken = default)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Project)
            .FirstOrDefaultAsync(
                c => c.Id == chapterId && c.Project.OwnerId == userId,
                cancellationToken);

        if (chapter == null)
        {
            return null;
        }

        var priorEntities = await _db.Entities
            .Where(e => e.ProjectId == chapter.ProjectId)
            .Select(e => new
            {
                e.Id,
                Type = e.Type.ToString(),
                e.Name,
                e.SummaryJson
            })
            .ToListAsync(cancellationToken);

        var priorFacts = await _db.Facts
            .Where(f => f.ProjectId == chapter.ProjectId && f.SourceChapterId != chapterId)
            .OrderByDescending(f => f.Confidence)
            .ThenBy(f => f.SourceChapterId)
            .Select(f => new
            {
                f.EntityId,
                f.FactType,
                f.Value,
                f.SourceChapterId,
                f.SourceQuote,
                f.Confidence
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        var entityLookup = priorEntities.ToDictionary(e => e.Id, e => e.Name);

        var canonEntities = priorEntities.Select(e => new
        {
            e.Type,
            e.Name,
            Summary = ExtractSummary(e.SummaryJson)
        }).ToList();

        var canonFacts = priorFacts.Select(f => new
        {
            EntityName = f.EntityId.HasValue && entityLookup.TryGetValue(f.EntityId.Value, out var entityName)
                ? entityName
                : string.Empty,
            f.FactType,
            f.Value,
            f.SourceChapterId,
            f.SourceQuote,
            f.Confidence
        }).ToList();

        return await RunCanonCheckAsync(
            chapter.Id,
            chapter.ProjectId,
            chapter.Title,
            chapter.Content,
            canonEntities,
            canonFacts,
            cancellationToken);
    }

    private async Task<CanonCheckResponse> RunCanonCheckAsync(
        int chapterId,
        int projectId,
        string chapterTitle,
        string chapterContent,
        object canonEntities,
        object canonFacts,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("AzureAI:Endpoint is missing.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AzureAI:ApiKey is missing.");

        if (string.IsNullOrWhiteSpace(_options.ChatDeployment))
            throw new InvalidOperationException("AzureAI:ChatDeployment is missing.");

        if (string.IsNullOrWhiteSpace(_options.ApiVersion))
            throw new InvalidOperationException("AzureAI:ApiVersion is missing.");

        var url =
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.ChatDeployment}/chat/completions?api-version={_options.ApiVersion}";

        var safeContent = TruncateForCheck(chapterContent, 12000);

        var systemPrompt = """
You are a continuity and canon checking assistant for a fiction-writing application.

Your job:
- Compare the current chapter against prior canon.
- Flag only likely contradictions or canon risks.
- Be conservative. Do not flag harmless ambiguity.
- Only flag an issue if the chapter text contains a specific passage that appears to conflict with prior canon.

Return strict JSON only.

Each issue must include:
- passageText: exact short text from the current chapter that should be underlined in the editor
- issue: short explanation of the contradiction
- expectedCanon: what prior canon says
- severity: "warning" or "error"
- supportingChapterId: chapter id of the strongest prior evidence if available
- supportingQuote: short prior-canon quote
- entityName: related character/location/theme if applicable

Rules:
- Use exact text from the current chapter for passageText.
- Keep passageText short enough to highlight in the UI.
- If there are no meaningful contradictions, return an empty issues array.
- Do not invent evidence.
- Prefer contradictions about names, traits, injuries, relationships, locations, timeline, and other concrete facts.

JSON shape:
{
  "issues": [
    {
      "passageText": "string",
      "issue": "string",
      "expectedCanon": "string",
      "severity": "warning",
      "supportingChapterId": 1,
      "supportingQuote": "string",
      "entityName": "string"
    }
  ]
}
""";

        var userPrompt = $"""
Current chapter title:
{chapterTitle}

Current chapter content:
{safeContent}

Prior canon entities:
{JsonSerializer.Serialize(canonEntities)}

Prior canon facts:
{JsonSerializer.Serialize(canonFacts)}
""";

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1,
            response_format = new
            {
                type = "json_object"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Canon check request failed. Status {StatusCode}. Body: {Body}",
                response.StatusCode,
                raw);

            throw new InvalidOperationException($"Canon check failed. Response: {raw}");
        }

        using var doc = JsonDocument.Parse(raw);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        content = StripCodeFences(content);

        using var resultDoc = JsonDocument.Parse(content);
        var issues = new List<CanonIssueResponse>();

        if (resultDoc.RootElement.TryGetProperty("issues", out var issuesElement) &&
            issuesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in issuesElement.EnumerateArray())
            {
                issues.Add(new CanonIssueResponse
                {
                    PassageText = item.TryGetProperty("passageText", out var passageText) ? passageText.GetString() ?? string.Empty : string.Empty,
                    Issue = item.TryGetProperty("issue", out var issue) ? issue.GetString() ?? string.Empty : string.Empty,
                    ExpectedCanon = item.TryGetProperty("expectedCanon", out var expectedCanon) ? expectedCanon.GetString() ?? string.Empty : string.Empty,
                    Severity = item.TryGetProperty("severity", out var severity) ? severity.GetString() ?? "warning" : "warning",
                    SupportingChapterId = item.TryGetProperty("supportingChapterId", out var supportingChapterId) && supportingChapterId.ValueKind != JsonValueKind.Null
                        ? supportingChapterId.GetInt32()
                        : null,
                    SupportingQuote = item.TryGetProperty("supportingQuote", out var supportingQuote) ? supportingQuote.GetString() ?? string.Empty : string.Empty,
                    EntityName = item.TryGetProperty("entityName", out var entityName) ? entityName.GetString() ?? string.Empty : string.Empty
                });
            }
        }

        issues = issues
            .Where(i => !string.IsNullOrWhiteSpace(i.PassageText) && !string.IsNullOrWhiteSpace(i.Issue))
            .Take(20)
            .ToList();

        return new CanonCheckResponse
        {
            ChapterId = chapterId,
            ProjectId = projectId,
            Issues = issues
        };
    }

    private static string ExtractSummary(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(summaryJson);

            if (doc.RootElement.TryGetProperty("summary", out var summaryElement))
            {
                return summaryElement.GetString() ?? string.Empty;
            }

            return summaryJson;
        }
        catch
        {
            return summaryJson;
        }
    }

    private static string TruncateForCheck(string? text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();

        if (trimmed.Length <= maxChars)
        {
            return trimmed;
        }

        return trimmed[..maxChars] + "\n\n[Truncated for canon check due to size.]";
    }

    private static string StripCodeFences(string text)
    {
        text = text.Trim();

        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
            {
                text = text[(firstNewline + 1)..];
            }

            if (text.EndsWith("```"))
            {
                text = text[..^3];
            }
        }

        return text.Trim();
    }
}
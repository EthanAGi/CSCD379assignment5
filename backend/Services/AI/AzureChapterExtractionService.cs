using System.Text;
using System.Text.Json;
using CanonGuard.Api.Models.Configuration;
using Microsoft.Extensions.Options;

namespace CanonGuard.Api.Services.AI;

public class AzureChapterExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly AzureAiOptions _options;
    private readonly ILogger<AzureChapterExtractionService> _logger;

    public AzureChapterExtractionService(
        HttpClient httpClient,
        IOptions<AzureAiOptions> options,
        ILogger<AzureChapterExtractionService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiChapterExtractionResult> ExtractAsync(
        string chapterTitle,
        string chapterContent,
        Dictionary<string, string>? existingCharacterProfiles = null,
        Dictionary<string, string>? existingLocationProfiles = null,
        CancellationToken cancellationToken = default)
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

        var safeContent = TruncateForExtraction(chapterContent, 12000);

        var existingCharactersJson = JsonSerializer.Serialize(existingCharacterProfiles ?? new Dictionary<string, string>());
        var existingLocationsJson = JsonSerializer.Serialize(existingLocationProfiles ?? new Dictionary<string, string>());

        var systemPrompt = """
You are an information extraction assistant for a fiction-writing application.

Your job is to extract and update STORY BIBLE information from a chapter.

Extract only:
1. characters
2. locations

Important behavior:
- Use the chapter text as the source of truth for new evidence.
- You may be given existing character and location profiles from earlier chapters.
- If an entity already exists, update its description by combining old known information with new chapter evidence.
- If the chapter reveals new details, include them in the updated description.
- If the chapter changes or deepens understanding of a character or place, revise the description to reflect that.
- Do not remove earlier facts unless the chapter clearly contradicts them.
- Keep descriptions concise, cumulative, and factual.
- Prefer a single canonical identity for the same entity when obvious from context.

Rules:
- Return only strict JSON.
- Do not include markdown code fences.
- A character is a person, named creature, or named sentient being.
- A location is a place, building, city, room, kingdom, region, or named setting.
- Ignore generic capitalized words unless they clearly refer to a named entity.
- For each entity, include:
  - name
  - description: a short cumulative profile updated with the new chapter evidence
  - sourceQuote: a short supporting quote from the current chapter
  - confidence: a number from 0.0 to 1.0
- Do not invent facts not supported by the chapter or prior provided profile.
- Deduplicate results.
- If none are found, return empty arrays.

JSON shape:
{
  "characters": [
    {
      "name": "string",
      "description": "string",
      "sourceQuote": "string",
      "confidence": 0.0
    }
  ],
  "locations": [
    {
      "name": "string",
      "description": "string",
      "sourceQuote": "string",
      "confidence": 0.0
    }
  ]
}
""";

        var userPrompt = $"""
Existing character profiles:
{existingCharactersJson}

Existing location profiles:
{existingLocationsJson}

Chapter title: {chapterTitle}

Chapter content:
{safeContent}
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

        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _options.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(raw);

                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "{}";

                content = StripCodeFences(content);

                try
                {
                    var result = JsonSerializer.Deserialize<AiChapterExtractionResult>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    return result ?? new AiChapterExtractionResult();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to deserialize Azure extraction response content: {Content}",
                        content);

                    throw new InvalidOperationException("AI extraction returned invalid JSON.");
                }
            }

            if ((int)response.StatusCode == 429 && attempt < maxAttempts)
            {
                var retryDelaySeconds = GetRetryDelaySeconds(response, attempt);

                _logger.LogWarning(
                    "Extraction request rate-limited on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    retryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                continue;
            }

            _logger.LogError(
                "Azure extraction request failed. Status {StatusCode}. Body: {Body}",
                response.StatusCode,
                raw);

            throw new InvalidOperationException($"AI extraction failed. Response: {raw}");
        }

        throw new InvalidOperationException("AI extraction failed after retries.");
    }

    private static int GetRetryDelaySeconds(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.TryGetValues("retry-after", out var values))
        {
            var retryAfter = values.FirstOrDefault();
            if (int.TryParse(retryAfter, out var retryAfterSeconds) && retryAfterSeconds > 0)
            {
                return retryAfterSeconds;
            }
        }

        return Math.Min(10 * attempt, 60);
    }

    private static string TruncateForExtraction(string? text, int maxChars)
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

        return trimmed[..maxChars] + "\n\n[Truncated for extraction due to size.]";
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
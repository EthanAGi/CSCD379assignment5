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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("AzureAI:Endpoint is missing.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AzureAI:ApiKey is missing.");

        if (string.IsNullOrWhiteSpace(_options.ChatDeployment))
            throw new InvalidOperationException("AzureAI:ChatDeployment is missing.");

        var url =
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.ChatDeployment}/chat/completions?api-version={_options.ApiVersion}";

        var systemPrompt = """
You are an information extraction assistant for a fiction-writing application.

Extract only:
1. characters
2. locations

Rules:
- Return only strict JSON.
- Do not include markdown code fences.
- A character is a person, named creature, or named sentient being.
- A location is a place, building, city, room, kingdom, region, or named setting.
- Ignore generic capitalized words unless they clearly refer to a named entity.
- Include a short supporting quote from the chapter for each result.
- Confidence must be a number from 0.0 to 1.0.
- Deduplicate results.
- If none are found, return empty arrays.

JSON shape:
{
  "characters": [
    { "name": "string", "sourceQuote": "string", "confidence": 0.0 }
  ],
  "locations": [
    { "name": "string", "sourceQuote": "string", "confidence": 0.0 }
  ]
}
""";

        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = $"Chapter title: {chapterTitle}\n\nChapter content:\n{chapterContent}"
                }
            },
            temperature = 0.1
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
                "Azure extraction request failed. Status {StatusCode}. Body: {Body}",
                response.StatusCode,
                raw);

            throw new InvalidOperationException($"AI extraction failed. Response: {raw}");
        }

        using var doc = JsonDocument.Parse(raw);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        content = StripCodeFences(content);

        var result = JsonSerializer.Deserialize<AiChapterExtractionResult>(
            content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result ?? new AiChapterExtractionResult();
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
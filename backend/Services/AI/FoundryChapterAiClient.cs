using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CanonGuard.Api.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CanonGuard.Api.Services.AI;

public class FoundryChapterAiClient : IChapterAiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<FoundryChapterAiClient> _logger;

    public FoundryChapterAiClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<FoundryChapterAiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<AiChapterExtractionResult> ExtractEntitiesAsync(
        string chapterTitle,
        string chapterContent,
        CancellationToken cancellationToken = default)
    {
        var endpoint = _config["Foundry:Endpoint"];
        var apiKey = _config["Foundry:ApiKey"];
        var model = _config["Foundry:Model"];
        var apiVersion = _config["Foundry:ApiVersion"] ?? "2024-05-01-preview";

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Foundry:Endpoint is missing.");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Foundry:ApiKey is missing.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Foundry:Model is missing.");

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

        var userPrompt = $"""
Chapter title: {chapterTitle}

Chapter content:
{chapterContent}
""";

        var requestBody = new
        {
            model = model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1
        };

        var url =
            $"{endpoint.TrimEnd('/')}/models/chat/completions?api-version={apiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Common Foundry/Azure pattern for key auth
        request.Headers.Add("api-key", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Foundry request failed. Status {StatusCode}. Body: {Body}",
                response.StatusCode, raw);

            throw new InvalidOperationException("AI extraction request failed.");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new AiChapterExtractionResult();
            }

            content = StripCodeFences(content);

            var result = JsonSerializer.Deserialize<AiChapterExtractionResult>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result ?? new AiChapterExtractionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not parse Foundry AI response. Raw: {Raw}", raw);
            throw;
        }
    }

    private static string StripCodeFences(string text)
    {
        text = text.Trim();

        if (text.StartsWith("```"))
        {
            var firstNewLine = text.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                text = text[(firstNewLine + 1)..];
            }

            if (text.EndsWith("```"))
            {
                text = text[..^3];
            }
        }

        return text.Trim();
    }
}
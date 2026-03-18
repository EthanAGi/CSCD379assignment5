using System.Text;
using System.Text.Json;
using CanonGuard.Api.Models.Configuration;
using Microsoft.Extensions.Options;

namespace CanonGuard.Api.Services.AI;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly AzureAiOptions _options;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        HttpClient httpClient,
        IOptions<AzureAiOptions> options,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("AzureAI:Endpoint is missing.");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AzureAI:ApiKey is missing.");

        if (string.IsNullOrWhiteSpace(_options.EmbeddingDeployment))
            throw new InvalidOperationException("AzureAI:EmbeddingDeployment is missing.");

        if (string.IsNullOrWhiteSpace(_options.ApiVersion))
            throw new InvalidOperationException("AzureAI:ApiVersion is missing.");

        var url =
            $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.EmbeddingDeployment}/embeddings?api-version={_options.ApiVersion}";

        var body = new
        {
            input = text
        };

        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", _options.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(raw);

                var embedding = doc.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding")
                    .EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToArray();

                return embedding;
            }

            if ((int)response.StatusCode == 429 && attempt < maxAttempts)
            {
                var retryDelaySeconds = GetRetryDelaySeconds(response, attempt);

                _logger.LogWarning(
                    "Embedding request rate-limited on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    retryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                continue;
            }

            _logger.LogError(
                "Embedding request failed. Status: {StatusCode}, Body: {Body}",
                response.StatusCode,
                raw);

            throw new InvalidOperationException($"Failed to generate embedding. Response: {raw}");
        }

        throw new InvalidOperationException("Failed to generate embedding after retries.");
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

        return Math.Min(5 * attempt, 30);
    }
}
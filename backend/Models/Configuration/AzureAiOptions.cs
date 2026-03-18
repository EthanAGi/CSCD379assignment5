namespace CanonGuard.Api.Models.Configuration;

public class AzureAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-06-01";
    public string ChatDeployment { get; set; } = string.Empty;
    public string EmbeddingDeployment { get; set; } = string.Empty;
}
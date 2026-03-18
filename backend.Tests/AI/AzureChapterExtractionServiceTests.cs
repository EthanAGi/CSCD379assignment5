using System.Net;
using System.Reflection;
using System.Text.Json;
using CanonGuard.Api.Models.Configuration;
using CanonGuard.Api.Services.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CanonGuard.Tests.AI;

public class AzureChapterExtractionServiceTests
{
    private static readonly Type ServiceType = typeof(AzureChapterExtractionService);

    private static object? InvokePrivateStatic(string methodName, params object?[] args)
    {
        var method = ServiceType.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        return method!.Invoke(null, args);
    }

    // ── TruncateForExtraction ──

    [Fact]
    public void TruncateForExtraction_Null_ReturnsEmpty()
    {
        var result = (string)InvokePrivateStatic("TruncateForExtraction", (string?)null, 100)!;
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TruncateForExtraction_Whitespace_ReturnsEmpty()
    {
        var result = (string)InvokePrivateStatic("TruncateForExtraction", "   ", 100)!;
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TruncateForExtraction_ShortText_ReturnsTrimmed()
    {
        var result = (string)InvokePrivateStatic("TruncateForExtraction", "  Hello  ", 100)!;
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TruncateForExtraction_ExactLimit_ReturnsFull()
    {
        var text = new string('a', 100);
        var result = (string)InvokePrivateStatic("TruncateForExtraction", text, 100)!;
        Assert.Equal(100, result.Length);
    }

    [Fact]
    public void TruncateForExtraction_OverLimit_TruncatesWithMessage()
    {
        var text = new string('a', 200);
        var result = (string)InvokePrivateStatic("TruncateForExtraction", text, 100)!;

        Assert.StartsWith(new string('a', 100), result);
        Assert.Contains("[Truncated for extraction due to size.]", result);
    }

    // ── StripCodeFences ──

    [Fact]
    public void StripCodeFences_NoFences_ReturnsInput()
    {
        var result = (string)InvokePrivateStatic("StripCodeFences", "{\"test\": true}")!;
        Assert.Equal("{\"test\": true}", result);
    }

    [Fact]
    public void StripCodeFences_WithJsonFences_StripsTherm()
    {
        var input = "```json\n{\"test\": true}\n```";
        var result = (string)InvokePrivateStatic("StripCodeFences", input)!;
        Assert.Equal("{\"test\": true}", result);
    }

    [Fact]
    public void StripCodeFences_WithPlainFences_StripsThem()
    {
        var input = "```\n{\"test\": true}\n```";
        var result = (string)InvokePrivateStatic("StripCodeFences", input)!;
        Assert.Equal("{\"test\": true}", result);
    }

    [Fact]
    public void StripCodeFences_OnlyOpenFence_StripsOpen()
    {
        var input = "```json\n{\"test\": true}";
        var result = (string)InvokePrivateStatic("StripCodeFences", input)!;
        Assert.Equal("{\"test\": true}", result);
    }

    // ── GetRetryDelaySeconds ──

    [Fact]
    public void GetRetryDelaySeconds_NoHeader_ReturnsExponential()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        var method = ServiceType.GetMethod("GetRetryDelaySeconds",
            BindingFlags.Static | BindingFlags.NonPublic);

        var result = (int)method!.Invoke(null, new object[] { response, 1 })!;
        Assert.Equal(10, result); // 10 * 1 = 10

        result = (int)method!.Invoke(null, new object[] { response, 3 })!;
        Assert.Equal(30, result); // 10 * 3 = 30
    }

    [Fact]
    public void GetRetryDelaySeconds_WithHeader_UsesHeaderValue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("retry-after", "5");

        var method = ServiceType.GetMethod("GetRetryDelaySeconds",
            BindingFlags.Static | BindingFlags.NonPublic);

        var result = (int)method!.Invoke(null, new object[] { response, 1 })!;
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetRetryDelaySeconds_CapsAt60()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        var method = ServiceType.GetMethod("GetRetryDelaySeconds",
            BindingFlags.Static | BindingFlags.NonPublic);

        var result = (int)method!.Invoke(null, new object[] { response, 10 })!;
        Assert.Equal(60, result); // min(100, 60) = 60
    }

    // ── ExtractAsync validation ──

    [Fact]
    public async Task ExtractAsync_MissingEndpoint_Throws()
    {
        var options = Options.Create(new AzureAiOptions
        {
            Endpoint = "",
            ApiKey = "key",
            ChatDeployment = "deploy",
            ApiVersion = "2024-06-01"
        });
        var service = new AzureChapterExtractionService(
            new HttpClient(), options, Mock.Of<ILogger<AzureChapterExtractionService>>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExtractAsync("Title", "Content"));
    }

    [Fact]
    public async Task ExtractAsync_MissingApiKey_Throws()
    {
        var options = Options.Create(new AzureAiOptions
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "",
            ChatDeployment = "deploy",
            ApiVersion = "2024-06-01"
        });
        var service = new AzureChapterExtractionService(
            new HttpClient(), options, Mock.Of<ILogger<AzureChapterExtractionService>>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExtractAsync("Title", "Content"));
    }

    [Fact]
    public async Task ExtractAsync_MissingChatDeployment_Throws()
    {
        var options = Options.Create(new AzureAiOptions
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "key",
            ChatDeployment = "",
            ApiVersion = "2024-06-01"
        });
        var service = new AzureChapterExtractionService(
            new HttpClient(), options, Mock.Of<ILogger<AzureChapterExtractionService>>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExtractAsync("Title", "Content"));
    }

    [Fact]
    public async Task ExtractAsync_MissingApiVersion_Throws()
    {
        var options = Options.Create(new AzureAiOptions
        {
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "key",
            ChatDeployment = "deploy",
            ApiVersion = ""
        });
        var service = new AzureChapterExtractionService(
            new HttpClient(), options, Mock.Of<ILogger<AzureChapterExtractionService>>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExtractAsync("Title", "Content"));
    }
}

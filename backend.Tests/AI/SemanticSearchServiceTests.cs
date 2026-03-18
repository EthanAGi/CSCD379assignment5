using System.Reflection;
using CanonGuard.Api.Services.AI;

namespace CanonGuard.Tests.AI;

public class SemanticSearchServiceTests
{
    private static readonly Type ServiceType = typeof(SemanticSearchService);

    private static object? InvokePrivateStatic(string methodName, params object?[] args)
    {
        var method = ServiceType.GetMethod(methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        return method!.Invoke(null, args);
    }

    // ── DeserializeVector ──

    [Fact]
    public void DeserializeVector_ValidJson_ReturnsFloatArray()
    {
        var json = "[1.0, 2.0, 3.0]";
        var result = (float[])InvokePrivateStatic("DeserializeVector", json)!;

        Assert.Equal(3, result.Length);
        Assert.Equal(1.0f, result[0]);
        Assert.Equal(2.0f, result[1]);
        Assert.Equal(3.0f, result[2]);
    }

    [Fact]
    public void DeserializeVector_EmptyArray_ReturnsEmpty()
    {
        var json = "[]";
        var result = (float[])InvokePrivateStatic("DeserializeVector", json)!;
        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeVector_InvalidJson_ReturnsEmpty()
    {
        var result = (float[])InvokePrivateStatic("DeserializeVector", "not json")!;
        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeVector_NullJson_ReturnsEmpty()
    {
        var result = (float[])InvokePrivateStatic("DeserializeVector", (string)null!)!;
        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeVector_SingleElement_ReturnsArray()
    {
        var json = "[0.5]";
        var result = (float[])InvokePrivateStatic("DeserializeVector", json)!;

        Assert.Single(result);
        Assert.Equal(0.5f, result[0]);
    }

    // ── BuildSnippet ──

    [Fact]
    public void BuildSnippet_ShortText_ReturnsFull()
    {
        var result = (string)InvokePrivateStatic("BuildSnippet", "Hello world", 280)!;
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void BuildSnippet_ExactLength_ReturnsFull()
    {
        var text = new string('a', 280);
        var result = (string)InvokePrivateStatic("BuildSnippet", text, 280)!;
        Assert.Equal(280, result.Length);
    }

    [Fact]
    public void BuildSnippet_LongText_TruncatesWithEllipsis()
    {
        var text = new string('a', 500);
        var result = (string)InvokePrivateStatic("BuildSnippet", text, 280)!;

        Assert.EndsWith("...", result);
        Assert.True(result.Length <= 283); // 280 + "..."
    }

    [Fact]
    public void BuildSnippet_EmptyText_ReturnsEmpty()
    {
        var result = (string)InvokePrivateStatic("BuildSnippet", "", 280)!;
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildSnippet_WhitespaceOnly_ReturnsEmpty()
    {
        var result = (string)InvokePrivateStatic("BuildSnippet", "   ", 280)!;
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildSnippet_TrimsInput()
    {
        var result = (string)InvokePrivateStatic("BuildSnippet", "  Hello  ", 280)!;
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void BuildSnippet_CustomMaxLength()
    {
        var text = "Hello world this is a test";
        var result = (string)InvokePrivateStatic("BuildSnippet", text, 10)!;

        Assert.EndsWith("...", result);
    }
}

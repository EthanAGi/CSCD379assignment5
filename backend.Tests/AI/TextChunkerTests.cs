using CanonGuard.Api.Services.AI;

namespace CanonGuard.Tests.AI;

public class TextChunkerTests
{
    [Fact]
    public void ChunkText_EmptyString_ReturnsEmptyList()
    {
        var result = TextChunker.ChunkText("");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_Null_ReturnsEmptyList()
    {
        var result = TextChunker.ChunkText(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_WhitespaceOnly_ReturnsEmptyList()
    {
        var result = TextChunker.ChunkText("   \n\t  ");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var text = "Hello world";
        var result = TextChunker.ChunkText(text);

        Assert.Single(result);
        Assert.Equal("Hello world", result[0]);
    }

    [Fact]
    public void ChunkText_ExactChunkSize_ReturnsSingleChunk()
    {
        var text = new string('a', 800);
        var result = TextChunker.ChunkText(text, chunkSize: 800, overlap: 120);

        Assert.Single(result);
        Assert.Equal(800, result[0].Length);
    }

    [Fact]
    public void ChunkText_LargerThanChunkSize_ReturnsMultipleChunks()
    {
        var text = new string('a', 1600);
        var result = TextChunker.ChunkText(text, chunkSize: 800, overlap: 120);

        Assert.True(result.Count >= 2);
    }

    [Fact]
    public void ChunkText_OverlapProducesOverlappingContent()
    {
        // Create text with distinct sections
        var text = new string('A', 400) + new string('B', 600);
        var result = TextChunker.ChunkText(text, chunkSize: 500, overlap: 200);

        Assert.True(result.Count >= 2);
        // First chunk is 0-500 (400 A's + 100 B's), second starts at 300 (100 A's + remaining B's)
        Assert.Contains('A', result[1]);
    }

    [Fact]
    public void ChunkText_CustomChunkSize_RespectsSize()
    {
        var text = new string('x', 300);
        var result = TextChunker.ChunkText(text, chunkSize: 100, overlap: 20);

        Assert.True(result.Count >= 3);
        Assert.Equal(100, result[0].Length);
    }

    [Fact]
    public void ChunkText_ZeroOverlap_ProducesNonOverlappingChunks()
    {
        var text = new string('z', 200);
        var result = TextChunker.ChunkText(text, chunkSize: 100, overlap: 0);

        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[0].Length);
        Assert.Equal(100, result[1].Length);
    }

    [Fact]
    public void ChunkText_TrimsWhitespace()
    {
        var text = "  Hello world  ";
        var result = TextChunker.ChunkText(text);

        Assert.Single(result);
        Assert.Equal("Hello world", result[0]);
    }

    [Fact]
    public void ChunkText_AllChunksAreNonEmpty()
    {
        var text = new string('a', 2000);
        var result = TextChunker.ChunkText(text, chunkSize: 800, overlap: 120);

        Assert.All(result, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk)));
    }
}

namespace CanonGuard.Api.Services.AI;

public static class TextChunker
{
    public static List<string> ChunkText(string text, int chunkSize = 800, int overlap = 120)
    {
        var chunks = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        text = text.Trim();

        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            var chunk = text.Substring(start, length).Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (start + length >= text.Length)
                break;

            start += chunkSize - overlap;
        }

        return chunks;
    }
}
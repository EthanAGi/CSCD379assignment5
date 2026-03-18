namespace CanonGuard.Api.Services.AI;

public static class VectorMath
{
    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same length.");

        double dot = 0;
        double magA = 0;
        double magB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
            return 0;

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
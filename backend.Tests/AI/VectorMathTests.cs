using CanonGuard.Api.Services.AI;

namespace CanonGuard.Tests.AI;

public class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { 1, 2, 3 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(1.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { -1, -2, -3 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(-1.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var a = new float[] { 1, 0 };
        var b = new float[] { 0, 1 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(0.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_ZeroVectorA_ReturnsZero()
    {
        var a = new float[] { 0, 0, 0 };
        var b = new float[] { 1, 2, 3 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(0.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_ZeroVectorB_ReturnsZero()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { 0, 0, 0 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(0.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_BothZeroVectors_ReturnsZero()
    {
        var a = new float[] { 0, 0 };
        var b = new float[] { 0, 0 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(0.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_DifferentLengths_Throws()
    {
        var a = new float[] { 1, 2 };
        var b = new float[] { 1, 2, 3 };

        Assert.Throws<ArgumentException>(() => VectorMath.CosineSimilarity(a, b));
    }

    [Fact]
    public void CosineSimilarity_ScaledVectors_ReturnsOne()
    {
        var a = new float[] { 1, 2, 3 };
        var b = new float[] { 2, 4, 6 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(1.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_SingleElement_ReturnsOne()
    {
        var a = new float[] { 5 };
        var b = new float[] { 10 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(1.0, result, 5);
    }

    [Fact]
    public void CosineSimilarity_PartiallyOverlapping_ReturnsBetweenZeroAndOne()
    {
        var a = new float[] { 1, 1, 0 };
        var b = new float[] { 1, 0, 1 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.True(result > 0 && result < 1);
    }
}

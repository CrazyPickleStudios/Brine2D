using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Core;

public class MathTests
{
    [Theory]
    [InlineData(0, 10, 0.5f, 5f)]
    [InlineData(0, 100, 0.25f, 25f)]
    public void Lerp_ShouldInterpolateCorrectly(float a, float b, float t, float expected)
    {
        // Act
        var result = MathHelper.Lerp(a, b, t);

        // Assert
        result.Should().BeApproximately(expected, 0.001f);
    }
}
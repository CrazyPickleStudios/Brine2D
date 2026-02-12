using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Tests.Core;

public class ColorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithBytes_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var color = new Color(255, 128, 64, 200);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
        Assert.Equal(200, color.A);
    }

    [Fact]
    public void Constructor_WithBytesNoAlpha_DefaultsToOpaque()
    {
        // Arrange & Act
        var color = new Color(255, 128, 64);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void Constructor_WithFloats_ConvertsToBytes()
    {
        // Arrange & Act
        var color = new Color(1.0f, 0.5f, 0.25f, 0.8f);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(127, color.G); // 0.5 * 255 = 127.5, truncated to 127
        Assert.Equal(63, color.B);  // 0.25 * 255 = 63.75, truncated to 63
        Assert.Equal(204, color.A); // 0.8 * 255 = 204
    }

    [Fact]
    public void Constructor_WithFloatsNoAlpha_DefaultsToOpaque()
    {
        // Arrange & Act
        var color = new Color(1.0f, 0.5f, 0.25f);

        // Assert
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void Constructor_WithFloatsOutOfRange_ClampsValues()
    {
        // Arrange & Act
        var color = new Color(1.5f, -0.5f, 0.5f, 2.0f);

        // Assert
        Assert.Equal(255, color.R); // Clamped from 1.5
        Assert.Equal(0, color.G);   // Clamped from -0.5
        Assert.Equal(127, color.B);
        Assert.Equal(255, color.A); // Clamped from 2.0
    }

    [Fact]
    public void Constructor_WithUInt_UnpacksCorrectly()
    {
        // Arrange & Act
        var color = new Color(0xFF8040C8); // R=255, G=128, B=64, A=200

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
        Assert.Equal(200, color.A);
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void ToVector4_ReturnsNormalizedValues()
    {
        // Arrange
        var color = new Color(255, 128, 64, 200);

        // Act
        var vector = color.ToVector4();

        // Assert
        Assert.Equal(1.0f, vector.X, precision: 3);
        Assert.Equal(128f / 255f, vector.Y, precision: 3);
        Assert.Equal(64f / 255f, vector.Z, precision: 3);
        Assert.Equal(200f / 255f, vector.W, precision: 3);
    }

    [Fact]
    public void ToVector3_ReturnsNormalizedRGB()
    {
        // Arrange
        var color = new Color(255, 128, 64, 200);

        // Act
        var vector = color.ToVector3();

        // Assert
        Assert.Equal(1.0f, vector.X, precision: 3);
        Assert.Equal(128f / 255f, vector.Y, precision: 3);
        Assert.Equal(64f / 255f, vector.Z, precision: 3);
    }

    [Fact]
    public void FromVector4_CreatesCorrectColor()
    {
        // Arrange
        var vector = new Vector4(1.0f, 0.5f, 0.25f, 0.8f);

        // Act
        var color = Color.FromVector4(vector);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(127, color.G);
        Assert.Equal(63, color.B);
        Assert.Equal(204, color.A);
    }

    [Fact]
    public void FromVector3_CreatesColorWithFullAlpha()
    {
        // Arrange
        var vector = new Vector3(1.0f, 0.5f, 0.25f);

        // Act
        var color = Color.FromVector3(vector);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(127, color.G);
        Assert.Equal(63, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void ToRgba_PacksCorrectly()
    {
        // Arrange
        var color = new Color(255, 128, 64, 200);

        // Act
        var packed = color.ToRgba();

        // Assert
        Assert.Equal(0xFF8040C8u, packed);
    }

    [Fact]
    public void ToRgba_RoundTrip_PreservesColor()
    {
        // Arrange
        var original = new Color(255, 128, 64, 200);

        // Act
        var packed = original.ToRgba();
        var reconstructed = new Color(packed);

        // Assert
        Assert.Equal(original, reconstructed);
    }

    #endregion

    #region WithAlpha Tests

    [Fact]
    public void WithAlpha_Byte_ReturnsNewColorWithChangedAlpha()
    {
        // Arrange
        var original = new Color(255, 128, 64, 200);

        // Act
        var modified = original.WithAlpha(100);

        // Assert
        Assert.Equal(255, modified.R);
        Assert.Equal(128, modified.G);
        Assert.Equal(64, modified.B);
        Assert.Equal(100, modified.A);
        Assert.Equal(200, original.A); // Original unchanged
    }

    [Fact]
    public void WithAlpha_Float_ReturnsNewColorWithChangedAlpha()
    {
        // Arrange
        var original = new Color(255, 128, 64, 200);

        // Act
        var modified = original.WithAlpha(0.5f);

        // Assert
        Assert.Equal(255, modified.R);
        Assert.Equal(128, modified.G);
        Assert.Equal(64, modified.B);
        Assert.Equal(127, modified.A);
    }

    [Fact]
    public void WithAlpha_FloatOutOfRange_ClampsAlpha()
    {
        // Arrange
        var original = new Color(255, 128, 64);

        // Act
        var tooHigh = original.WithAlpha(1.5f);
        var tooLow = original.WithAlpha(-0.5f);

        // Assert
        Assert.Equal(255, tooHigh.A);
        Assert.Equal(0, tooLow.A);
    }

    #endregion

    #region Lerp Tests

    [Fact]
    public void Lerp_AtZero_ReturnsFirstColor()
    {
        // Arrange
        var colorA = new Color(0, 0, 0, 0);
        var colorB = new Color(255, 255, 255, 255);

        // Act
        var result = Color.Lerp(colorA, colorB, 0f);

        // Assert
        Assert.Equal(colorA, result);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsSecondColor()
    {
        // Arrange
        var colorA = new Color(0, 0, 0, 0);
        var colorB = new Color(255, 255, 255, 255);

        // Act
        var result = Color.Lerp(colorA, colorB, 1f);

        // Assert
        Assert.Equal(colorB, result);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMidpoint()
    {
        // Arrange
        var colorA = new Color(0, 0, 0, 0);
        var colorB = new Color(200, 100, 50, 200);

        // Act
        var result = Color.Lerp(colorA, colorB, 0.5f);

        // Assert
        Assert.Equal(100, result.R);
        Assert.Equal(50, result.G);
        Assert.Equal(25, result.B);
        Assert.Equal(100, result.A);
    }

    [Fact]
    public void Lerp_ValueOutOfRange_ClampsToZeroAndOne()
    {
        // Arrange
        var colorA = new Color(100, 100, 100, 100);
        var colorB = new Color(200, 200, 200, 200);

        // Act
        var belowZero = Color.Lerp(colorA, colorB, -0.5f);
        var aboveOne = Color.Lerp(colorA, colorB, 1.5f);

        // Assert
        Assert.Equal(colorA, belowZero);
        Assert.Equal(colorB, aboveOne);
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void MultiplyOperator_TwoColors_MultipliesComponentWise()
    {
        // Arrange
        var color1 = new Color(200, 100, 50, 255);
        var color2 = new Color(255, 128, 255, 128);

        // Act
        var result = color1 * color2;

        // Assert
        Assert.Equal(200, result.R); // (200 * 255) / 255
        Assert.Equal(50, result.G);  // (100 * 128) / 255 = 50.19
        Assert.Equal(50, result.B);  // (50 * 255) / 255
        Assert.Equal(128, result.A); // (255 * 128) / 255
    }

    [Fact]
    public void MultiplyOperator_ColorAndScalar_ScalesColor()
    {
        // Arrange
        var color = new Color(200, 100, 50, 255);

        // Act
        var result = color * 0.5f;

        // Assert
        Assert.Equal(100, result.R);
        Assert.Equal(50, result.G);
        Assert.Equal(25, result.B);
        Assert.Equal(255, result.A); // Alpha unchanged
    }

    [Fact]
    public void MultiplyOperator_ScalarOutOfRange_ClampsToZeroAndOne()
    {
        // Arrange
        var color = new Color(200, 100, 50, 255);

        // Act
        var tooHigh = color * 1.5f;
        var tooLow = color * -0.5f;

        // Assert
        Assert.Equal(200, tooHigh.R);
        Assert.Equal(0, tooLow.R);
        Assert.Equal(0, tooLow.G);
        Assert.Equal(0, tooLow.B);
    }

    #endregion

    #region Predefined Colors Tests

    [Fact]
    public void PredefinedColors_HaveCorrectValues()
    {
        // Assert common colors
        Assert.Equal(new Color(255, 255, 255, 255), Color.White);
        Assert.Equal(new Color(0, 0, 0, 255), Color.Black);
        Assert.Equal(new Color(255, 0, 0, 255), Color.Red);
        Assert.Equal(new Color(0, 255, 128, 255), Color.Green);
        Assert.Equal(new Color(0, 0, 255, 255), Color.Blue);
        Assert.Equal(new Color(0, 0, 0, 0), Color.Transparent);
    }

    [Fact]
    public void PredefinedColors_Yellow_IsCorrect()
    {
        // Arrange & Act
        var yellow = Color.Yellow;

        // Assert
        Assert.Equal(255, yellow.R);
        Assert.Equal(255, yellow.G);
        Assert.Equal(0, yellow.B);
        Assert.Equal(255, yellow.A);
    }

    [Fact]
    public void PredefinedColors_Cyan_IsCorrect()
    {
        // Arrange & Act
        var cyan = Color.Cyan;

        // Assert
        Assert.Equal(0, cyan.R);
        Assert.Equal(255, cyan.G);
        Assert.Equal(255, cyan.B);
    }

    [Fact]
    public void PredefinedColors_Magenta_IsCorrect()
    {
        // Arrange & Act
        var magenta = Color.Magenta;

        // Assert
        Assert.Equal(255, magenta.R);
        Assert.Equal(0, magenta.G);
        Assert.Equal(255, magenta.B);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var color1 = new Color(255, 128, 64, 200);
        var color2 = new Color(255, 128, 64, 200);

        // Act & Assert
        Assert.True(color1.Equals(color2));
        Assert.True(color1 == color2);
        Assert.False(color1 != color2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var color1 = new Color(255, 128, 64, 200);
        var color2 = new Color(255, 128, 64, 201);

        // Act & Assert
        Assert.False(color1.Equals(color2));
        Assert.False(color1 == color2);
        Assert.True(color1 != color2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var color1 = new Color(255, 128, 64, 200);
        var color2 = new Color(255, 128, 64, 200);

        // Act & Assert
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var color = new Color(255, 128, 64, 200);

        // Act
        var result = color.ToString();

        // Assert
        Assert.Equal("Color(R:255, G:128, B:64, A:200)", result);
    }

    #endregion
}
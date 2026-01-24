using System.Drawing;
using Brine2D.Rendering;
using NSubstitute;
using Xunit;

namespace Brine2D.Integration.Tests.Rendering;

public class RendererTests
{
    [Fact(Skip = "SDL3 initialization fails in CI environment - needs investigation")]
    public void Renderer_ShouldBatchSimilarSprites()
    {
        // Arrange
        var mockTexture = Substitute.For<ITexture>();
        // TODO: var renderer = CreateTestRenderer();

        // Act
        //renderer.BeginFrame();
        //renderer.DrawSprite(mockTexture, new RectangleF(0, 0, 10, 10));
        //renderer.DrawSprite(mockTexture, new RectangleF(20, 20, 10, 10));
        //renderer.EndFrame();

        // Assert
        //renderer.BatchCount.Should().Be(1); // Same texture = 1 batch
    }
}
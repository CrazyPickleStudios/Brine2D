using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;
using NSubstitute;

namespace Brine2D.Tests.Rendering;

public class HeadlessRendererTests
{
    [Fact]
    public void DefaultConstructor_SetsViewportToOne()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Equal(1, renderer.Width);
        Assert.Equal(1, renderer.Height);
    }

    [Theory]
    [InlineData(800, 600)]
    [InlineData(1, 1)]
    public void Constructor_SetsViewportDimensions(int width, int height)
    {
        using var renderer = new HeadlessRenderer(width, height);

        Assert.Equal(width, renderer.Width);
        Assert.Equal(height, renderer.Height);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-5, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -1)]
    public void Constructor_ClampsViewportToMinimumOne(int width, int height)
    {
        using var renderer = new HeadlessRenderer(width, height);

        Assert.True(renderer.Width >= 1);
        Assert.True(renderer.Height >= 1);
    }

    [Fact]
    public void IsInitialized_AlwaysTrue()
    {
        using var renderer = new HeadlessRenderer();

        Assert.True(renderer.IsInitialized);
    }

    [Fact]
    public void CreateRenderTarget_ThrowsNotSupportedException()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Throws<NotSupportedException>(() => renderer.CreateRenderTarget(64, 64));
    }

    [Fact]
    public void SetBlendMode_ValidValue_RoundTrips()
    {
        using var renderer = new HeadlessRenderer();

        renderer.SetBlendMode(BlendMode.Additive);

        Assert.Equal(BlendMode.Additive, renderer.GetBlendMode());
    }

    [Fact]
    public void SetBlendMode_InvalidValue_Throws()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.SetBlendMode((BlendMode)999));
    }

    [Fact]
    public void SetRenderLayer_RoundTrips()
    {
        using var renderer = new HeadlessRenderer();

        renderer.SetRenderLayer(42);

        Assert.Equal(42, renderer.GetRenderLayer());
    }

    [Fact]
    public void SetRenderTarget_RoundTrips()
    {
        using var renderer = new HeadlessRenderer();
        var mockTarget = Substitute.For<IRenderTarget>();

        renderer.SetRenderTarget(mockTarget);

        Assert.Same(mockTarget, renderer.GetRenderTarget());
    }

    [Fact]
    public void SetRenderTarget_Null_ClearsTarget()
    {
        using var renderer = new HeadlessRenderer();
        renderer.SetRenderTarget(Substitute.For<IRenderTarget>());

        renderer.SetRenderTarget(null);

        Assert.Null(renderer.GetRenderTarget());
    }

    [Fact]
    public void PushPopRenderTarget_StackBehavior()
    {
        using var renderer = new HeadlessRenderer();
        var target1 = Substitute.For<IRenderTarget>();
        var target2 = Substitute.For<IRenderTarget>();

        renderer.PushRenderTarget(target1);
        Assert.Same(target1, renderer.GetRenderTarget());

        renderer.PushRenderTarget(target2);
        Assert.Same(target2, renderer.GetRenderTarget());

        renderer.PopRenderTarget();
        Assert.Same(target1, renderer.GetRenderTarget());

        renderer.PopRenderTarget();
        Assert.Null(renderer.GetRenderTarget());
    }

    [Fact]
    public void PopRenderTarget_EmptyStack_Throws()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.PopRenderTarget());
    }

    [Fact]
    public void SetScissorRect_RoundTrips()
    {
        using var renderer = new HeadlessRenderer(800, 600);
        var rect = new Rectangle(10, 10, 200, 200);

        renderer.SetScissorRect(rect);

        Assert.Equal(rect, renderer.GetScissorRect());
    }

    [Fact]
    public void SetScissorRect_Null_ClearsRect()
    {
        using var renderer = new HeadlessRenderer(800, 600);
        renderer.SetScissorRect(new Rectangle(10, 10, 50, 50));

        renderer.SetScissorRect(null);

        Assert.Null(renderer.GetScissorRect());
    }

    [Fact]
    public void SetScissorRect_NegativeDimensions_Throws()
    {
        using var renderer = new HeadlessRenderer(800, 600);

        Assert.Throws<ArgumentException>(() =>
            renderer.SetScissorRect(new Rectangle(0, 0, -10, 50)));
    }

    [Fact]
    public void SetScissorRect_ClampsToViewport()
    {
        using var renderer = new HeadlessRenderer(100, 100);

        renderer.SetScissorRect(new Rectangle(50, 50, 200, 200));

        var result = renderer.GetScissorRect();
        Assert.NotNull(result);
        Assert.Equal(50, result.Value.X);
        Assert.Equal(50, result.Value.Y);
        Assert.Equal(50, result.Value.Width);
        Assert.Equal(50, result.Value.Height);
    }

    [Fact]
    public void PushPopScissorRect_StackBehavior()
    {
        using var renderer = new HeadlessRenderer(800, 600);
        var outer = new Rectangle(10, 10, 400, 400);

        renderer.PushScissorRect(outer);
        Assert.Equal(outer, renderer.GetScissorRect());

        renderer.PopScissorRect();
        Assert.Null(renderer.GetScissorRect());
    }

    [Fact]
    public void PushScissorRect_IntersectsWithCurrent()
    {
        using var renderer = new HeadlessRenderer(800, 600);
        renderer.PushScissorRect(new Rectangle(0, 0, 100, 100));

        renderer.PushScissorRect(new Rectangle(50, 50, 100, 100));

        var result = renderer.GetScissorRect();
        Assert.NotNull(result);
        Assert.Equal(50, result.Value.X);
        Assert.Equal(50, result.Value.Y);
        Assert.Equal(50, result.Value.Width);
        Assert.Equal(50, result.Value.Height);

        renderer.PopScissorRect();
        renderer.PopScissorRect();
    }

    [Fact]
    public void PopScissorRect_EmptyStack_Throws()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.PopScissorRect());
    }

    [Fact]
    public void BeginFrame_ResetsState()
    {
        using var renderer = new HeadlessRenderer(800, 600);
        renderer.SetBlendMode(BlendMode.Additive);
        renderer.SetRenderLayer(5);
        renderer.SetScissorRect(new Rectangle(0, 0, 50, 50));

        renderer.BeginFrame();

        Assert.Equal(IRenderer.DefaultBlendMode, renderer.GetBlendMode());
        Assert.Equal(IRenderer.DefaultRenderLayer, renderer.GetRenderLayer());
        Assert.Null(renderer.GetScissorRect());
        Assert.Null(renderer.GetRenderTarget());
    }

    [Fact]
    public void EndFrame_ClearsLeakedStacks()
    {
        using var renderer = new HeadlessRenderer(800, 600);
        renderer.PushRenderTarget(null);
        renderer.PushScissorRect(new Rectangle(0, 0, 10, 10));

        renderer.EndFrame();

        Assert.Null(renderer.GetRenderTarget());
        Assert.Null(renderer.GetScissorRect());
    }

    [Fact]
    public void MeasureText_EmptyOrNull_ReturnsZero()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Equal(Vector2.Zero, renderer.MeasureText(""));
        Assert.Equal(Vector2.Zero, renderer.MeasureText(null!));
    }

    [Fact]
    public void MeasureText_SingleLine_ReturnsNonZero()
    {
        using var renderer = new HeadlessRenderer();

        var size = renderer.MeasureText("Hello");

        Assert.True(size.X > 0);
        Assert.True(size.Y > 0);
    }

    [Fact]
    public void MeasureText_MultiLine_IncreasesHeight()
    {
        using var renderer = new HeadlessRenderer();

        var single = renderer.MeasureText("A");
        var multi = renderer.MeasureText("A\nB\nC");

        Assert.True(multi.Y > single.Y);
    }

    [Fact]
    public void MeasureText_CustomFontSize_ScalesResult()
    {
        using var renderer = new HeadlessRenderer();

        var small = renderer.MeasureText("X", 10f);
        var large = renderer.MeasureText("X", 40f);

        Assert.True(large.X > small.X);
        Assert.True(large.Y > small.Y);
    }

    [Fact]
    public void MeasureText_WithOptions_UsesFontSize()
    {
        using var renderer = new HeadlessRenderer();
        var options = new TextRenderOptions { FontSize = 32f };

        var result = renderer.MeasureText("Test", options);
        var direct = renderer.MeasureText("Test", 32f);

        Assert.Equal(direct, result);
    }

    [Fact]
    public void DrawTexture_NullTexture_Throws()
    {
        using var renderer = new HeadlessRenderer();

        Assert.Throws<ArgumentNullException>(() => renderer.DrawTexture(null!, Vector2.Zero));
        Assert.Throws<ArgumentNullException>(() => renderer.DrawTexture(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => renderer.DrawTexture(null!, 0, 0, 32, 32));
    }

    [Fact]
    public void Camera_RoundTrips()
    {
        using var renderer = new HeadlessRenderer();
        var camera = Substitute.For<ICamera>();

        renderer.Camera = camera;

        Assert.Same(camera, renderer.Camera);
    }

    [Fact]
    public void ClearColor_RoundTrips()
    {
        using var renderer = new HeadlessRenderer();
        var color = new Color(255, 0, 0, 255);

        renderer.ClearColor = color;

        Assert.Equal(color, renderer.ClearColor);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var renderer = new HeadlessRenderer();

        renderer.Dispose();
        renderer.Dispose();
    }

    [Fact]
    public void Methods_AfterDispose_Throw()
    {
        var renderer = new HeadlessRenderer();
        renderer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => renderer.GetBlendMode());
        Assert.Throws<ObjectDisposedException>(() => renderer.SetBlendMode(BlendMode.Alpha));
        Assert.Throws<ObjectDisposedException>(() => renderer.GetRenderLayer());
        Assert.Throws<ObjectDisposedException>(() => renderer.SetRenderLayer(0));
        Assert.Throws<ObjectDisposedException>(() => renderer.GetRenderTarget());
        Assert.Throws<ObjectDisposedException>(() => renderer.SetRenderTarget(null));
        Assert.Throws<ObjectDisposedException>(() => renderer.PushRenderTarget(null));
        Assert.Throws<ObjectDisposedException>(() => renderer.PopRenderTarget());
        Assert.Throws<ObjectDisposedException>(() => renderer.GetScissorRect());
        Assert.Throws<ObjectDisposedException>(() => renderer.SetScissorRect(null));
        Assert.Throws<ObjectDisposedException>(() => renderer.PushScissorRect(null));
        Assert.Throws<ObjectDisposedException>(() => renderer.PopScissorRect());
        Assert.Throws<ObjectDisposedException>(() => renderer.CreateRenderTarget(1, 1));
        Assert.Throws<ObjectDisposedException>(() => renderer.MeasureText("x"));
        Assert.Throws<ObjectDisposedException>(() => renderer.DrawTexture(Substitute.For<ITexture>(), Vector2.Zero));
    }
}
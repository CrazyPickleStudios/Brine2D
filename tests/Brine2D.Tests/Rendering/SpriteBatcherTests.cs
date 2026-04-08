using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using NSubstitute;

namespace Brine2D.Tests.Rendering;

public class SpriteBatcherTests
{
    private static ITexture CreateMockTexture(int sortKey = 1, bool isLoaded = true)
    {
        var tex = Substitute.For<ITexture>();
        tex.IsLoaded.Returns(isLoaded);
        tex.Width.Returns(32);
        tex.Height.Returns(32);
        tex.Bounds.Returns(new Rectangle(0, 0, 32, 32));
        tex.SortKey.Returns(sortKey);
        return tex;
    }

    private static void AddSprite(SpriteBatcher batcher, ITexture texture, byte layer = 0, BlendMode blendMode = BlendMode.Alpha)
    {
        batcher.Draw(
            texture,
            Vector2.Zero,
            sourceRect: null,
            scale: Vector2.One,
            rotation: 0f,
            origin: Vector2.Zero,
            tint: Color.White,
            layer: layer,
            blendMode: blendMode);
    }

    [Fact]
    public void Draw_NullTexture_Throws()
    {
        using var batcher = new SpriteBatcher();

        Assert.Throws<ArgumentNullException>(() =>
            AddSprite(batcher, null!));
    }

    [Fact]
    public void Draw_InvalidBlendMode_Throws()
    {
        using var batcher = new SpriteBatcher();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            batcher.Draw(
                CreateMockTexture(),
                Vector2.Zero,
                sourceRect: null,
                scale: Vector2.One,
                rotation: 0f,
                origin: Vector2.Zero,
                tint: Color.White,
                layer: 0,
                blendMode: (BlendMode)999));
    }

    [Fact]
    public void Draw_AfterDispose_Throws()
    {
        var batcher = new SpriteBatcher();
        batcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            AddSprite(batcher, CreateMockTexture()));
    }

    [Fact]
    public void Count_ReflectsQueuedItems()
    {
        using var batcher = new SpriteBatcher();
        var texture = CreateMockTexture();

        Assert.Equal(0, batcher.Count);

        AddSprite(batcher, texture);
        Assert.Equal(1, batcher.Count);

        AddSprite(batcher, texture);
        Assert.Equal(2, batcher.Count);
    }

    [Fact]
    public void Clear_ResetsCount()
    {
        using var batcher = new SpriteBatcher();
        AddSprite(batcher, CreateMockTexture());
        AddSprite(batcher, CreateMockTexture());

        batcher.Clear();

        Assert.Equal(0, batcher.Count);
    }

    [Fact]
    public void Clear_AfterDispose_Throws()
    {
        var batcher = new SpriteBatcher();
        batcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => batcher.Clear());
    }

    [Fact]
    public void Flush_EmptyBatch_ZeroDrawCalls()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();

        batcher.Flush(drawContext);

        Assert.Equal(0, batcher.EstimatedDrawCalls);
    }

    [Fact]
    public void Flush_NullDrawContext_Throws()
    {
        using var batcher = new SpriteBatcher();

        Assert.Throws<ArgumentNullException>(() => batcher.Flush(null!));
    }

    [Fact]
    public void Flush_SingleSprite_OneDrawCall()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var texture = CreateMockTexture();
        AddSprite(batcher, texture);

        batcher.Flush(drawContext);

        Assert.Equal(1, batcher.EstimatedDrawCalls);
        drawContext.Received(1).DrawTexture(
            texture,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    [Fact]
    public void Flush_SameTexture_OneDrawCall()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var texture = CreateMockTexture();

        AddSprite(batcher, texture);
        AddSprite(batcher, texture);
        AddSprite(batcher, texture);

        batcher.Flush(drawContext);

        Assert.Equal(1, batcher.EstimatedDrawCalls);
        drawContext.Received(3).DrawTexture(
            texture,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    [Fact]
    public void Flush_DifferentTextures_MultipleDrawCalls()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var tex1 = CreateMockTexture(sortKey: 1);
        var tex2 = CreateMockTexture(sortKey: 2);

        AddSprite(batcher, tex1);
        AddSprite(batcher, tex2);

        batcher.Flush(drawContext);

        Assert.Equal(2, batcher.EstimatedDrawCalls);
    }

    [Fact]
    public void Flush_DifferentLayers_SeparateDrawCalls()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var texture = CreateMockTexture();

        AddSprite(batcher, texture, layer: 0);
        AddSprite(batcher, texture, layer: 1);

        batcher.Flush(drawContext);

        Assert.Equal(2, batcher.EstimatedDrawCalls);
    }

    [Fact]
    public void Flush_DifferentBlendModes_SeparateDrawCalls()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var texture = CreateMockTexture();

        AddSprite(batcher, texture, blendMode: BlendMode.Alpha);
        AddSprite(batcher, texture, blendMode: BlendMode.Additive);

        batcher.Flush(drawContext);

        Assert.Equal(2, batcher.EstimatedDrawCalls);
    }

    [Fact]
    public void Flush_RestoresLayerAndBlendMode()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        drawContext.GetRenderLayer().Returns((byte)42);
        drawContext.GetBlendMode().Returns(BlendMode.Multiply);

        AddSprite(batcher, CreateMockTexture(), layer: 0, blendMode: BlendMode.Alpha);
        batcher.Flush(drawContext);

        Received.InOrder(() =>
        {
            drawContext.SetRenderLayer(42);
            drawContext.SetBlendMode(BlendMode.Multiply);
        });
    }

    [Fact]
    public void Flush_SkipsUnloadedTextures()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var loaded = CreateMockTexture(sortKey: 1, isLoaded: true);
        var unloaded = CreateMockTexture(sortKey: 2, isLoaded: false);

        AddSprite(batcher, loaded);
        AddSprite(batcher, unloaded);

        batcher.Flush(drawContext);

        Assert.Equal(1, batcher.EstimatedDrawCalls);
        drawContext.Received(1).DrawTexture(
            loaded,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    [Fact]
    public void Flush_AllUnloaded_ZeroDrawCalls()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var unloaded = CreateMockTexture(isLoaded: false);

        AddSprite(batcher, unloaded);
        AddSprite(batcher, unloaded);

        batcher.Flush(drawContext);

        Assert.Equal(0, batcher.EstimatedDrawCalls);
        drawContext.DidNotReceive().DrawTexture(
            Arg.Any<ITexture>(),
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    [Fact]
    public void Flush_ClearsQueueAfterFlush()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        AddSprite(batcher, CreateMockTexture());

        batcher.Flush(drawContext);

        Assert.Equal(0, batcher.Count);
    }

    [Fact]
    public void Flush_SortsLayersAscending()
    {
        using var batcher = new SpriteBatcher();
        var drawContext = Substitute.For<IDrawContext>();
        var texture = CreateMockTexture();

        AddSprite(batcher, texture, layer: 5);
        AddSprite(batcher, texture, layer: 1);
        AddSprite(batcher, texture, layer: 3);

        batcher.Flush(drawContext);

        Received.InOrder(() =>
        {
            drawContext.SetRenderLayer(1);
            drawContext.SetRenderLayer(3);
            drawContext.SetRenderLayer(5);
        });
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var batcher = new SpriteBatcher();

        batcher.Dispose();
        batcher.Dispose();
    }
}
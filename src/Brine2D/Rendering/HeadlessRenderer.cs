using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Rendering.Text;

namespace Brine2D.Rendering;

/// <summary>
/// No-op renderer for headless mode (servers, testing).
/// All rendering operations are ignored.
/// </summary>
internal sealed class HeadlessRenderer : IRenderer
{
    public bool IsInitialized => true;
    public int Width => 0;
    public int Height => 0;
    public Color ClearColor { get; set; }
    public ICamera? Camera { get; set; }
    
    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    
    public void BeginFrame() { }
    public void EndFrame() { }
    public void Clear() { }
    public void Present() { }
    
    public void SetRenderTarget(RenderTarget? target) { }
    public void ApplyPostProcessing() { }
    
    public void DrawTexture(ITexture texture, Vector2 position, Rectangle? sourceRect = null,
        Vector2? origin = null, float rotation = 0, Vector2? scale = null, 
        Color? color = null, SpriteFlip flip = SpriteFlip.None) { }
    
    public void DrawTexture(ITexture texture, Rectangle destinationRect, Rectangle? sourceRect = null,
        Color? color = null, float rotation = 0, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None) { }
    
    public void DrawRectangleFilled(float x, float y, float width, float height, Color color) { }
    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1) { }
    
    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1) { }
    
    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color) { }
    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1) { }
    
    public void DrawPolygonFilled(Vector2[] points, Color color) { }
    public void DrawPolygonOutline(Vector2[] points, Color color, float thickness = 1, bool closed = true) { }
    
    public void DrawText(string text, float x, float y, Color color) { }
    public void DrawText(string text, Vector2 position, Font? font = null, Color? color = null, 
        float scale = 1.0f, float rotation = 0, Vector2? origin = null) { }
    
    public void SetScissorRect(Rectangle? rect) { }
    
    public void Dispose() { }

    public void SetRenderLayer(byte layer)
    {
        throw new NotImplementedException();
    }

    public byte GetRenderLayer()
    {
        throw new NotImplementedException();
    }

    public void DrawTexture(ITexture texture, Vector2 position)
    {
        throw new NotImplementedException();
    }

    public void DrawTexture(ITexture texture, float x, float y)
    {
        throw new NotImplementedException();
    }

    public void DrawTexture(ITexture texture, float x, float y, float width, float height)
    {
        throw new NotImplementedException();
    }

    public void DrawText(string text, float x, float y, TextRenderOptions options)
    {
        throw new NotImplementedException();
    }

    public void SetDefaultFont(Font? font)
    {
        throw new NotImplementedException();
    }

    public Vector2 MeasureText(string text, float? fontSize = null)
    {
        throw new NotImplementedException();
    }

    public Vector2 MeasureText(string text, TextRenderOptions options)
    {
        throw new NotImplementedException();
    }

    public void DrawRectangleFilled(Rectangle rect, Color color)
    {
        throw new NotImplementedException();
    }

    public void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1)
    {
        throw new NotImplementedException();
    }

    public void DrawCircleFilled(Vector2 center, float radius, Color color)
    {
        throw new NotImplementedException();
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1)
    {
        throw new NotImplementedException();
    }

    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1)
    {
        throw new NotImplementedException();
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        throw new NotImplementedException();
    }

    public IRenderTarget CreateRenderTarget(int width, int height)
    {
        throw new NotImplementedException();
    }

    public void SetRenderTarget(IRenderTarget? target)
    {
        throw new NotImplementedException();
    }

    public IRenderTarget? GetRenderTarget()
    {
        throw new NotImplementedException();
    }

    public void PushRenderTarget(IRenderTarget? target)
    {
        throw new NotImplementedException();
    }

    public void PopRenderTarget()
    {
        throw new NotImplementedException();
    }

    public Rectangle? GetScissorRect()
    {
        throw new NotImplementedException();
    }

    public void PushScissorRect(Rectangle? rect)
    {
        throw new NotImplementedException();
    }

    public void PopScissorRect()
    {
        throw new NotImplementedException();
    }
}
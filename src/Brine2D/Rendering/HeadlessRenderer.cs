using Brine2D.Core;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Rendering.Text;
using System.Numerics;

namespace Brine2D.Rendering;

/// <summary>
/// No-op renderer for headless mode (servers, testing).
/// All rendering operations are silently ignored.
/// <para>
/// <see cref="CreateRenderTarget"/> is the sole exception: render targets require GPU
/// infrastructure that does not exist in headless mode and throw <see cref="NotSupportedException"/>.
/// </para>
/// </summary>
internal sealed class HeadlessRenderer : IRenderer
{
    // Width/Height are intentionally 0 — there is no window in headless mode.
    // The camera factory in AddBrineEngine detects this and falls back to 1280×720.
    public bool IsInitialized => true;
    public int Width => 0;
    public int Height => 0;
    public Color ClearColor { get; set; }
    public ICamera? Camera { get; set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void BeginFrame() { }
    public void EndFrame() { }
    public void Clear() { }
    public void Present() { }
    public void ApplyPostProcessing() { }

    // ── Render layers ────────────────────────────────────────────────────────
    public void SetRenderLayer(byte layer) { }
    public byte GetRenderLayer() => 0;

    // ── Texture drawing ──────────────────────────────────────────────────────
    public void DrawTexture(ITexture texture, Vector2 position,
        Rectangle? sourceRect = null, Vector2? origin = null,
        float rotation = 0f, Vector2? scale = null,
        Color? color = null, SpriteFlip flip = SpriteFlip.None) { }

    public void DrawTexture(ITexture texture, Vector2 position) { }
    public void DrawTexture(ITexture texture, float x, float y) { }
    public void DrawTexture(ITexture texture, float x, float y, float width, float height) { }

    public void DrawTexture(ITexture texture, Rectangle destinationRect,
        Rectangle? sourceRect = null, Color? color = null,
        float rotation = 0f, Vector2? origin = null, SpriteFlip flip = SpriteFlip.None) { }

    // ── Text rendering ───────────────────────────────────────────────────────
    public void DrawText(string text, float x, float y, Color color) { }
    public void DrawText(string text, float x, float y, TextRenderOptions options) { }
    public void DrawText(string text, Vector2 position, Font? font = null,
        Color? color = null, float scale = 1.0f, float rotation = 0f, Vector2? origin = null) { }

    public void SetDefaultFont(Font? font) { }
    public Vector2 MeasureText(string text, float? fontSize = null) => Vector2.Zero;
    public Vector2 MeasureText(string text, TextRenderOptions options) => Vector2.Zero;

    // ── Shapes ───────────────────────────────────────────────────────────────
    public void DrawRectangleFilled(float x, float y, float width, float height, Color color) { }
    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f) { }
    public void DrawRectangleFilled(Rectangle rect, Color color) { }
    public void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f) { }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color) { }
    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f) { }
    public void DrawCircleFilled(Vector2 center, float radius, Color color) { }
    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f) { }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f) { }
    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f) { }

    public void DrawPolygonFilled(Vector2[] points, Color color) { }
    public void DrawPolygonOutline(Vector2[] points, Color color, float thickness = 1f, bool closed = true) { }

    // ── Blend modes ──────────────────────────────────────────────────────────
    public void SetBlendMode(BlendMode blendMode) { }

    // ── Render targets ───────────────────────────────────────────────────────
    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Always thrown — render targets require GPU infrastructure unavailable in headless mode.
    /// </exception>
    public IRenderTarget CreateRenderTarget(int width, int height)
        => throw new NotSupportedException(
            "Render targets are not supported in headless mode. " +
            "Guard CreateRenderTarget calls with a renderer capability check, " +
            "or do not use render targets in headless/test scenarios.");

    public void SetRenderTarget(RenderTarget? target) { }
    public void SetRenderTarget(IRenderTarget? target) { }
    public IRenderTarget? GetRenderTarget() => null;
    public void PushRenderTarget(IRenderTarget? target) { }
    public void PopRenderTarget() { }

    // ── Scissor rectangles ───────────────────────────────────────────────────
    public void SetScissorRect(Rectangle? rect) { }
    public Rectangle? GetScissorRect() => null;
    public void PushScissorRect(Rectangle? rect) { }
    public void PopScissorRect() { }

    public void Dispose() { }
}
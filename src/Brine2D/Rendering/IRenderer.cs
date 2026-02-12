using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering.Text;

namespace Brine2D.Rendering;

/// <summary>
/// Core rendering interface for Brine2D.
/// </summary>
public interface IRenderer : IDisposable
{
    // ============================================================
    // PROPERTIES
    // ============================================================

    bool IsInitialized { get; }
    ICamera? Camera { get; set; }
    Color ClearColor { get; set; }
    int Width { get; }
    int Height { get; }

    // ============================================================
    // LIFECYCLE
    // ============================================================

    Task InitializeAsync(CancellationToken cancellationToken = default);
    void BeginFrame();
    void ApplyPostProcessing();
    void EndFrame();

    // ============================================================
    // RENDER LAYERS
    // ============================================================

    void SetRenderLayer(byte layer);
    byte GetRenderLayer();

    // ============================================================
    // TEXTURE DRAWING (4 Clean Overloads)
    // ============================================================

    /// <summary>
    /// Draw a texture with full control (primary API).
    /// </summary>
    void DrawTexture(
        ITexture texture,
        Vector2 position,
        Rectangle? sourceRect = null,
        Vector2? origin = null,
        float rotation = 0f,
        Vector2? scale = null,
        Color? color = null,
        SpriteFlip flip = SpriteFlip.None);

    /// <summary>
    /// Draw texture at position (Vector2, top-left).
    /// </summary>
    void DrawTexture(ITexture texture, Vector2 position);

    /// <summary>
    /// Draw texture at position (float x, y, top-left).
    /// </summary>
    void DrawTexture(ITexture texture, float x, float y);

    /// <summary>
    /// Draw texture with explicit size (top-left).
    /// </summary>
    void DrawTexture(ITexture texture, float x, float y, float width, float height);

    // ============================================================
    // TEXT RENDERING
    // ============================================================

    /// <summary>
    /// Draw plain text at the specified position.
    /// </summary>
    /// <param name="text">Text to render (supports \n for newlines)</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="color">Text color</param>
    void DrawText(string text, float x, float y, Color color);
    
    /// <summary>
    /// Draw text with advanced formatting options.
    /// </summary>
    /// <param name="text">Text to render (with optional BBCode markup if ParseMarkup=true)</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="options">Rendering options (alignment, wrapping, effects, etc.)</param>
    void DrawText(string text, float x, float y, TextRenderOptions options);
    
    /// <summary>
    /// Set the default font for text rendering.
    /// </summary>
    void SetDefaultFont(IFont? font);
    
    /// <summary>
    /// Measure the size of plain text.
    /// </summary>
    /// <param name="text">Text to measure (markup tags are NOT parsed)</param>
    /// <param name="fontSize">Font size in points (default uses current font size)</param>
    /// <returns>Width and height in pixels</returns>
    Vector2 MeasureText(string text, float? fontSize = null);
    
    /// <summary>
    /// Measure the size of text with full layout options.
    /// </summary>
    /// <param name="text">Text to measure</param>
    /// <param name="options">Layout options (wrapping, alignment, etc.)</param>
    /// <returns>Width and height in pixels</returns>
    Vector2 MeasureText(string text, TextRenderOptions options);

    // ============================================================
    // SHAPES
    // ============================================================

    void DrawRectangleFilled(float x, float y, float width, float height, Color color);
    void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f);
    void DrawCircleFilled(float centerX, float centerY, float radius, Color color);
    void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f);
    void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f);

    // Vector2 overloads
    void DrawRectangleFilled(Rectangle rect, Color color);
    void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f);
    void DrawCircleFilled(Vector2 center, float radius, Color color);
    void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f);
    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f);

    // ============================================================
    // BLEND MODES
    // ============================================================

    void SetBlendMode(BlendMode blendMode);

    // ============================================================
    // RENDER TARGETS
    // ============================================================

    /// <summary>
    /// Create a render target for off-screen rendering.
    /// GPU renderer only - throws NotSupportedException on legacy renderer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Render targets are fixed-size and do not automatically resize when the window resizes.
    /// If you need a render target that matches the window size, recreate it in response to
    /// window resize events.
    /// </para>
    /// <para>
    /// The internal post-processing render targets automatically resize with the window.
    /// </para>
    /// </remarks>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <returns>A new render target that you must dispose when done</returns>
    /// <exception cref="NotSupportedException">Thrown if renderer doesn't support render targets</exception>
    /// <example>
    /// <code>
    /// // Create minimap render target
    /// using var minimap = renderer.CreateRenderTarget(256, 256);
    /// 
    /// // Render scene to minimap
    /// renderer.PushRenderTarget(minimap);
    /// RenderMinimapView();
    /// renderer.PopRenderTarget();
    /// 
    /// // Draw minimap texture to screen
    /// renderer.DrawTexture(minimap.Texture, 10, 10);
    /// </code>
    /// </example>
    IRenderTarget CreateRenderTarget(int width, int height);

    /// <summary>
    /// Set the active render target (null = render to screen).
    /// GPU renderer only - throws NotSupportedException on legacy renderer.
    /// </summary>
    /// <param name="target">Render target to draw to, or null for screen</param>
    /// <exception cref="NotSupportedException">Thrown if renderer doesn't support render targets</exception>
    void SetRenderTarget(IRenderTarget? target);

    /// <summary>
    /// Get the currently active render target (null = rendering to screen).
    /// </summary>
    /// <returns>Current render target or null if rendering to screen</returns>
    IRenderTarget? GetRenderTarget();

    /// <summary>
    /// Push current render target onto stack and set a new one.
    /// Useful for nested render-to-texture operations.
    /// GPU renderer only - throws NotSupportedException on legacy renderer.
    /// </summary>
    /// <param name="target">New render target to set, or null for screen</param>
    /// <exception cref="NotSupportedException">Thrown if renderer doesn't support render targets</exception>
    void PushRenderTarget(IRenderTarget? target);

    /// <summary>
    /// Restore the previous render target from the stack.
    /// GPU renderer only - throws NotSupportedException on legacy renderer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if stack is empty</exception>
    /// <exception cref="NotSupportedException">Thrown if renderer doesn't support render targets</exception>
    void PopRenderTarget();

    // ============================================================
    // SCISSOR RECTANGLES
    // ============================================================

    /// <summary>
    /// Set a scissor rectangle to clip all rendering to a specific region.
    /// All draw calls will be clipped to this rectangle until disabled with null.
    /// </summary>
    /// <param name="rect">Rectangle to clip to, or null to disable clipping</param>
    /// <remarks>
    /// <para>
    /// Scissor rects are useful for:
    /// - UI scroll views and panels
    /// - Text clipping in bounded areas
    /// - Split-screen rendering
    /// - Preventing rendering outside specific regions
    /// </para>
    /// <para>
    /// The scissor rect is in screen coordinates (not world coordinates).
    /// It is not affected by camera transforms.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clip to a 200x200 region
    /// renderer.SetScissorRect(new Rectangle(10, 10, 200, 200));
    /// renderer.DrawTexture(largeTexture, 0, 0); // Only visible part inside rect
    /// 
    /// // Disable clipping
    /// renderer.SetScissorRect(null);
    /// renderer.DrawTexture(largeTexture, 0, 0); // Fully visible
    /// 
    /// // Nested clipping for scroll view
    /// renderer.SetScissorRect(scrollViewBounds);
    /// foreach (var item in scrollItems)
    /// {
    ///     renderer.DrawText(item.Text, item.X, item.Y, Color.White);
    /// }
    /// renderer.SetScissorRect(null);
    /// </code>
    /// </example>
    void SetScissorRect(Rectangle? rect);

    /// <summary>
    /// Get the current scissor rectangle (null if clipping is disabled).
    /// </summary>
    /// <returns>Current scissor rectangle or null</returns>
    Rectangle? GetScissorRect();

    /// <summary>
    /// Push the current scissor rect onto a stack and set a new one.
    /// Useful for nested clipping regions (e.g., nested scroll views).
    /// </summary>
    /// <param name="rect">New scissor rectangle, or null to disable clipping</param>
    /// <remarks>
    /// This is particularly useful when rendering UI hierarchies where child
    /// elements need to be clipped to parent bounds.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Outer panel
    /// renderer.PushScissorRect(outerPanelBounds);
    /// RenderOuterPanel();
    /// 
    ///     // Inner scroll view (clipped to both outer and inner)
    ///     renderer.PushScissorRect(innerScrollViewBounds);
    ///     RenderScrollContent();
    ///     renderer.PopScissorRect();
    /// 
    /// renderer.PopScissorRect();
    /// </code>
    /// </example>
    void PushScissorRect(Rectangle? rect);

    /// <summary>
    /// Restore the previous scissor rectangle from the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if stack is empty</exception>
    void PopScissorRect();
}
using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering.Text;

namespace Brine2D.Rendering;

/// <summary>
/// Drawing operations surface for Brine2D renderers.
/// Consumers that only need to draw shapes, textures, and text should depend on this
/// interface rather than the full <see cref="IRenderer"/>.
/// </summary>
public interface IDrawContext
{
    /// <summary>
    /// Sets the render layer for subsequent draw calls.
    /// </summary>
    /// <remarks>
    /// Render layers control draw ordering only when sprites are submitted through
    /// <see cref="SpriteBatcher"/>. Direct draw calls (e.g. <c>DrawRectangleFilled</c>,
    /// <c>DrawTexture</c>) are rendered in submission order regardless of the active layer.
    /// </remarks>
    void SetRenderLayer(byte layer);

    /// <summary>
    /// Gets the current render layer.
    /// </summary>
    byte GetRenderLayer();

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

    void DrawRectangleFilled(float x, float y, float width, float height, Color color);
    void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f);
    void DrawCircleFilled(float centerX, float centerY, float radius, Color color);
    void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f);
    void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f);

    void DrawRectangleFilled(Rectangle rect, Color color);
    void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f);
    void DrawCircleFilled(Vector2 center, float radius, Color color);
    void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f);
    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f);

    void SetBlendMode(BlendMode blendMode);

    /// <summary>
    /// Get the current blend mode.
    /// </summary>
    BlendMode GetBlendMode();
}
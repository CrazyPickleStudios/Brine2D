using Brine2D.Core.Components;
using Brine2D.Core.Math;

namespace Brine2D.Core.Graphics;

/// <summary>
///     Renders textured sprites/quads in a Begin/Draw/End batching pattern.
///     Call <see cref="Begin(int?, int?)" /> or <see cref="Begin(Camera2D?, int?, int?)" />,
///     issue one or more <see cref="Draw(ITexture2D, Rectangle?, Rectangle, Color, float, Vector2?)" /> calls,
///     then finish with <see cref="End()" />.
/// </summary>
public interface ISpriteRenderer
{
    /// <summary>
    ///     <para>Begins a sprite batch in screen-space.</para>
    ///     <para>Use this when you want to draw directly in backbuffer pixel coordinates.</para>
    /// </summary>
    /// <param name="targetWidth">Optional render target width in pixels. If null, uses the current backbuffer/viewport width.</param>
    /// <param name="targetHeight">
    ///     Optional render target height in pixels. If null, uses the current backbuffer/viewport
    ///     height.
    /// </param>
    void Begin(int? targetWidth = null, int? targetHeight = null);

    /// <summary>
    ///     Begins a sprite batch transformed by a world-space <paramref name="camera" />.
    ///     If <paramref name="camera" /> is null, behaves the same as <see cref="Begin(int?, int?)" /> (screen-space).
    /// </summary>
    /// <param name="camera">World-space camera used to transform world coordinates to screen pixels.</param>
    /// <param name="targetWidth">Optional render target width in pixels. If null, uses the current backbuffer/viewport width.</param>
    /// <param name="targetHeight">
    ///     Optional render target height in pixels. If null, uses the current backbuffer/viewport
    ///     height.
    /// </param>
    void Begin(Camera2D? camera, int? targetWidth = null, int? targetHeight = null);

    /// <summary>
    ///     Enqueues a sprite draw.
    ///     When the batch was started with a world-space camera, <paramref name="dst" /> is interpreted in world units
    ///     (pixels)
    ///     and transformed by the camera to screen pixels; otherwise it is used as screen-space pixels.
    /// </summary>
    /// <param name="texture">Texture to draw.</param>
    /// <param name="src">Optional source rectangle in texture pixels. If null, the entire texture is used.</param>
    /// <param name="dst">Destination rectangle in world- or screen-space pixels (see remarks).</param>
    /// <param name="color">Tint color multiplied with the texture (premultiplied alpha not assumed).</param>
    /// <param name="rotationRadians">Rotation in radians applied around <paramref name="origin" />.</param>
    /// <param name="origin">
    ///     Rotation/scaling origin relative to the destination rectangle's top-left in pixels. If null,
    ///     defaults to (0,0).
    /// </param>
    void Draw(ITexture2D texture, Rectangle? src, Rectangle dst, Color color, float rotationRadians = 0f,
        Vector2? origin = null);

    /// <summary>
    ///     Enqueues a sprite draw with explicit layer/depth sorting metadata.
    /// </summary>
    /// <param name="texture">Texture to draw.</param>
    /// <param name="src">Optional source rectangle in texture pixels. If null, the entire texture is used.</param>
    /// <param name="dst">Destination rectangle in world- or screen-space pixels (see Begin).</param>
    /// <param name="color">Tint color multiplied with the texture.</param>
    /// <param name="layer">Integer layer bucket used for coarse sorting/grouping.</param>
    /// <param name="depth">
    ///     Per-layer depth (normalized value, e.g., 0..1) used for fine sorting. Exact ordering is
    ///     renderer-defined.
    /// </param>
    /// <param name="rotationRadians">Rotation in radians applied around <paramref name="origin" />.</param>
    /// <param name="origin">
    ///     Rotation/scaling origin relative to the destination rectangle's top-left in pixels. If null,
    ///     defaults to (0,0).
    /// </param>
    void Draw(ITexture2D texture, Rectangle? src, Rectangle dst, Color color, int layer, float depth,
        float rotationRadians = 0f, Vector2? origin = null);

    /// <summary>
    ///     Ends the current sprite batch and flushes all pending draw commands.
    ///     Must be called once for every <see cref="Begin(int?, int?)" /> or <see cref="Begin(Camera2D?, int?, int?)" /> call.
    /// </summary>
    void End();
}
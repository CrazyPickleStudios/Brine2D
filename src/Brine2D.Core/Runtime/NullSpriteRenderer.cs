using Brine2D.Core.Components;
using Brine2D.Core.Graphics;
using Brine2D.Core.Math;

namespace Brine2D.Core.Runtime;

/// <summary>
///     Null-object implementation of <see cref="ISpriteRenderer" />.
///     All operations are no-ops. Useful for headless/testing scenarios or when rendering is intentionally disabled.
/// </summary>
internal sealed class NullSpriteRenderer : ISpriteRenderer
{
    /// <inheritdoc />
    public void Begin(int? targetWidth = null, int? targetHeight = null)
    {
    }

    /// <inheritdoc />
    public void Begin(Camera2D? camera, int? targetWidth = null, int? targetHeight = null)
    {
    }

    /// <inheritdoc />
    public void Draw(ITexture2D texture, Rectangle? src, Rectangle dst, Color color, float rotationRadians = 0f,
        Vector2? origin = null)
    {
    }

    /// <inheritdoc />
    public void Draw(ITexture2D texture, Rectangle? src, Rectangle dst, Color color, int layer, float depth,
        float rotationRadians = 0f, Vector2? origin = null)
    {
    }

    /// <inheritdoc />
    public void End()
    {
    }
}
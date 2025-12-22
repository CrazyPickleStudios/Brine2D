using System.Numerics;

namespace Brine2D.Core.Animation;

/// <summary>
/// Represents a single frame in a sprite animation.
/// </summary>
public class SpriteFrame
{
    /// <summary>
    /// Source rectangle in the sprite sheet (in pixels).
    /// </summary>
    public Rectangle SourceRect { get; set; }

    /// <summary>
    /// Duration to display this frame (in seconds).
    /// </summary>
    public float Duration { get; set; } = 0.1f;

    /// <summary>
    /// Optional origin/pivot point for this frame (relative to frame, 0-1 range).
    /// </summary>
    public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

    public SpriteFrame(Rectangle sourceRect, float duration = 0.1f)
    {
        SourceRect = sourceRect;
        Duration = duration;
    }
}

/// <summary>
/// Simple rectangle structure for frame bounds.
/// </summary>
public struct Rectangle
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
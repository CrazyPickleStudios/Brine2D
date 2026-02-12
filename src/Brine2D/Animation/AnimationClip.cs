using Brine2D.Core;

namespace Brine2D.Animation;

/// <summary>
/// Represents an animation clip with multiple frames.
/// </summary>
public class AnimationClip
{
    /// <summary>
    /// Name of the animation (e.g., "walk", "jump", "idle").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// List of frames in this animation.
    /// </summary>
    public List<SpriteFrame> Frames { get; set; } = new();

    /// <summary>
    /// Whether the animation should loop.
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Total duration of the animation (calculated from frames).
    /// </summary>
    public float TotalDuration => Frames.Sum(f => f.Duration);

    public AnimationClip(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Creates an animation from a sprite sheet with uniform frame sizes.
    /// </summary>
    public static AnimationClip FromSpriteSheet(
        string name,
        int frameWidth,
        int frameHeight,
        int frameCount,
        int columns,
        float frameDuration = 0.1f,
        bool loop = true)
    {
        var clip = new AnimationClip(name) { Loop = loop };

        for (int i = 0; i < frameCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            
            var rect = new Rectangle(
                col * frameWidth,
                row * frameHeight,
                frameWidth,
                frameHeight);

            clip.Frames.Add(new SpriteFrame(rect, frameDuration));
        }

        return clip;
    }
}
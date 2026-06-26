using System.Numerics;
using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Image UI component for displaying textures.
/// </summary>
public class UIImage : IUIComponent, IAnchoredUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

    /// <summary>
    /// Texture to display.
    /// </summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Source rectangle in the texture (<c>null</c> = entire texture).
    /// </summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>
    /// Whether to maintain aspect ratio when scaling.
    /// </summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Rotation in radians, applied around the center of the image.
    /// </summary>
    public float Rotation { get; set; } = 0f;

    /// <summary>
    /// Opacity (0 = transparent, 1 = opaque).
    /// </summary>
    public float Alpha
    {
        get => _alpha;
        set => _alpha = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Tint color combined with <see cref="Alpha"/> for the final modulation.
    /// </summary>
    public Color Tint { get; set; } = Color.White;

    private float _alpha = 1f;

    public UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Fired when the image is clicked.
    /// </summary>
    public event Action? OnClick;

    public UIImage(ITexture? texture, Vector2 position, Vector2 size)
    {
        Texture = texture;
        Position = position;
        Size = size;
    }

    public UIImage(ITexture? texture, Vector2 position)
        : this(texture, position, Vector2.Zero)
    {
        if (texture != null)
        {
            Size = new Vector2(texture.Width, texture.Height);
        }
    }

    /// <summary>
    /// Optional sprite animator. When set, the current frame's source rect overrides
    /// <see cref="SourceRect"/> during rendering.
    /// </summary>
    public SpriteAnimator? Animator { get; set; }

    public void Update(float deltaTime)
    {
        Animator?.Update(deltaTime);
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible || Texture == null) return;

        var animSourceRect = Animator?.CurrentFrame?.SourceRect;
        if (animSourceRect.HasValue)
            SourceRect = animSourceRect;

        Vector2 renderSize = Size;

        if (MaintainAspectRatio && Size.X > 0 && Size.Y > 0)
        {
            float textureAspect = (float)Texture.Width / Texture.Height;
            float targetAspect = Size.X / Size.Y;

            if (textureAspect > targetAspect)
                renderSize.Y = Size.X / textureAspect;
            else
                renderSize.X = Size.Y * textureAspect;
        }

        Vector2 renderPos = Position;
        if (MaintainAspectRatio)
        {
            renderPos.X += (Size.X - renderSize.X) / 2;
            renderPos.Y += (Size.Y - renderSize.Y) / 2;
        }

        var sourceSize = SourceRect.HasValue
            ? new Vector2(SourceRect.Value.Width, SourceRect.Value.Height)
            : new Vector2(Texture.Width, Texture.Height);

        var scale = new Vector2(
            renderSize.X / sourceSize.X,
            renderSize.Y / sourceSize.Y);

        var modulationColor = new Color(
            (byte)(Tint.R),
            (byte)(Tint.G),
            (byte)(Tint.B),
            (byte)(Tint.A * _alpha));

        // Center is the rotation origin — pass center position and use 0.5,0.5 origin.
        var center = new Vector2(renderPos.X + renderSize.X / 2, renderPos.Y + renderSize.Y / 2);

        renderer.DrawTexture(
            Texture,
            position: center,
            sourceRect: SourceRect,
            origin: new Vector2(0.5f, 0.5f),
            rotation: Rotation,
            scale: scale,
            color: modulationColor,
            flip: SpriteFlip.None);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    internal bool HasOnClick => OnClick != null;

    internal void Click()
    {
        if (Enabled)
            OnClick?.Invoke();
    }

    /// <summary>
    /// Sets the texture and optionally auto-sizes the component.
    /// </summary>
    public void SetTexture(ITexture? texture, bool autoSize = false)
    {
        Texture = texture;

        if (autoSize && texture != null)
        {
            Size = new Vector2(texture.Width, texture.Height);
        }
    }
}
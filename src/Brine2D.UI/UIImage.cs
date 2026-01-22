using System.Drawing;
using System.Numerics;
using Brine2D.Animation;
using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Image UI component for displaying textures.
/// </summary>
public class UIImage : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Texture to display.
    /// </summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Source rectangle in texture (null = use entire texture).
    /// </summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>
    /// Whether to maintain aspect ratio when scaling.
    /// </summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Rotation in degrees (not yet supported by IRenderer).
    /// </summary>
    public float Rotation { get; set; } = 0f;

    /// <summary>
    /// Alpha transparency (not yet supported by IRenderer - for future use).
    /// </summary>
    public float Alpha
    {
        get => _alpha;
        set => _alpha = Math.Clamp(value, 0f, 1f);
    }

    private float _alpha = 1f;

    public UITooltip? Tooltip { get; set; }

    public UIImage(ITexture? texture, Vector2 position, Vector2 size)
    {
        Texture = texture;
        Position = position;
        Size = size;
    }

    public UIImage(ITexture? texture, Vector2 position)
        : this(texture, position, Vector2.Zero)
    {
        // Auto-size to texture dimensions if available
        if (texture != null)
        {
            Size = new Vector2(texture.Width, texture.Height);
        }
    }

    public void Update(float deltaTime)
    {
        // Images are typically static, but could be animated
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible || Texture == null) return;

        Vector2 renderSize = Size;

        // Calculate aspect ratio if needed
        if (MaintainAspectRatio && Size.X > 0 && Size.Y > 0)
        {
            float textureAspect = (float)Texture.Width / Texture.Height;
            float targetAspect = Size.X / Size.Y;

            if (textureAspect > targetAspect)
            {
                // Texture is wider, fit to width
                renderSize.Y = Size.X / textureAspect;
            }
            else
            {
                // Texture is taller, fit to height
                renderSize.X = Size.Y * textureAspect;
            }
        }

        // Center the image if aspect ratio caused size change
        Vector2 renderPos = Position;
        if (MaintainAspectRatio)
        {
            renderPos.X += (Size.X - renderSize.X) / 2;
            renderPos.Y += (Size.Y - renderSize.Y) / 2;
        }

        if (SourceRect.HasValue)
        {
            // Draw with source rectangle (9 params)
            var src = SourceRect.Value;
            renderer.DrawTexture(
                Texture,
                src.X, src.Y, src.Width, src.Height,
                renderPos.X, renderPos.Y, renderSize.X, renderSize.Y);
        }
        else
        {
            // Draw scaled texture (5 params)
            renderer.DrawTexture(
                Texture,
                renderPos.X, renderPos.Y, renderSize.X, renderSize.Y);
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
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
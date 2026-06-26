using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Panel/container UI component with background.
/// </summary>
public class UIPanel : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }
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
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(50, 50, 50, 200);

    /// <summary>
    /// Border color (optional).
    /// </summary>
    public Color? BorderColor { get; set; }

    /// <summary>
    /// Border thickness in pixels.
    /// </summary>
    public float BorderThickness { get; set; } = 2f;

    /// <summary>
    /// Optional nine-slice background texture. When set, replaces the solid
    /// <see cref="BackgroundColor"/> fill. The solid color border (if any) is
    /// still drawn on top.
    /// </summary>
    public ITexture? BackgroundTexture { get; set; }

    /// <summary>
    /// Nine-slice border insets (texels) for <see cref="BackgroundTexture"/>.
    /// </summary>
    public NineSliceBorder BackgroundTextureBorder { get; set; }

    /// <summary>
    /// Tint color applied to <see cref="BackgroundTexture"/>. Defaults to white (no tint).
    /// </summary>
    public Color BackgroundTextureTint { get; set; } = Color.White;

    /// <summary>
    /// When true, mouse events over this panel are consumed.
    /// Defaults to false.
    /// </summary>
    public bool BlocksInput { get; set; } = false;

    /// <summary>
    /// When true, child components are clipped to the panel's bounds during rendering.
    /// Defaults to false.
    /// </summary>
    public bool ClipChildren { get; set; } = false;

    private readonly List<IUIComponent> _children = new();

    /// <summary>
    /// Last screen size pushed by <see cref="UICanvas"/>. Used when propagating to
    /// children added after this panel is already on a canvas.
    /// </summary>
    internal Vector2 LastKnownScreenSize { get; set; } = new Vector2(1280, 720);

    public UIPanel(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    /// <summary>
    /// Adds a child component. Child positions are parent-relative: (0, 0) places the
    /// child at the top-left corner of this panel.
    /// </summary>
    public void AddChild(IUIComponent child)
    {
        _children.Add(child);
        UICanvas.PropagateScreenSize(child, LastKnownScreenSize);
    }

    /// <summary>
    /// Removes a child component.
    /// </summary>
    public void RemoveChild(IUIComponent child) => _children.Remove(child);

    /// <summary>
    /// Removes all child components.
    /// </summary>
    public void ClearChildren() => _children.Clear();

    /// <summary>
    /// Gets all child components.
    /// </summary>
    public IReadOnlyList<IUIComponent> GetChildren() => _children.AsReadOnly();

    public void Update(float deltaTime)
    {
        foreach (var child in _children)
        {
            if (child.Enabled)
                child.Update(deltaTime);
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        if (BackgroundTexture != null)
            renderer.DrawNineSlice(BackgroundTexture, new Rectangle(Position.X, Position.Y, Size.X, Size.Y), BackgroundTextureBorder, BackgroundTextureTint);
        else
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        if (BorderColor.HasValue && BorderThickness > 0)
        {
            var border = BorderColor.Value;
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, BorderThickness, border);
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - BorderThickness, Size.X, BorderThickness, border);
            renderer.DrawRectangleFilled(Position.X, Position.Y, BorderThickness, Size.Y, border);
            renderer.DrawRectangleFilled(Position.X + Size.X - BorderThickness, Position.Y, BorderThickness, Size.Y, border);
        }

        if (ClipChildren)
            renderer.PushScissorRect(new Rectangle(Position.X, Position.Y, Size.X, Size.Y));

        foreach (var child in _children)
        {
            if (!child.Visible) continue;

            if (child is IAnchoredUIComponent anchoredChild &&
                (anchoredChild.Anchor != UIAnchor.TopLeft || anchoredChild.AnchorOffset != Vector2.Zero))
            {
                var saved = anchoredChild.Position;
                var anchorOrigin = UIAnchorResolver.Resolve(anchoredChild.Anchor, Size.X, Size.Y);
                anchoredChild.Position = Position + anchorOrigin + anchoredChild.AnchorOffset;
                child.Render(renderer);
                anchoredChild.Position = saved;
            }
            else
            {
                var saved = child.Position;
                child.Position = Position + saved;
                child.Render(renderer);
                child.Position = saved;
            }
        }

        if (ClipChildren)
            renderer.PopScissorRect();
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }
}
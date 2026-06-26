using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Orientation for <see cref="UIStackPanel"/>.
/// </summary>
public enum StackOrientation
{
    Vertical,
    Horizontal
}

/// <summary>
/// A container that stacks its children automatically in a single direction,
/// computing positions from the <see cref="Spacing"/> and each child's
/// <see cref="IUIComponent.Size"/>. <see cref="Size"/> is updated to fit
/// the content on every <see cref="Render"/> call.
/// </summary>
public class UIStackPanel : IUIComponent, IAnchoredUIComponent
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
    /// Stacking direction. Defaults to <see cref="StackOrientation.Vertical"/>.
    /// </summary>
    public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;

    /// <summary>
    /// Gap in pixels between successive children.
    /// </summary>
    public float Spacing { get; set; } = 4f;

    /// <summary>
    /// Padding in pixels on all sides inside the panel background.
    /// </summary>
    public float Padding { get; set; } = 0f;

    /// <summary>
    /// Optional background color. When null, no background is drawn.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// When true, mouse events over this panel are consumed.
    /// Defaults to false.
    /// </summary>
    public bool BlocksInput { get; set; } = false;

    /// <summary>
    /// When true, children are clipped to the panel bounds during rendering.
    /// Defaults to false.
    /// </summary>
    public bool ClipChildren { get; set; } = false;

    private readonly List<IUIComponent> _children = new();

    /// <summary>
    /// Last screen size pushed by <see cref="UICanvas"/>. Used when propagating to
    /// children added after this stack panel is already on a canvas.
    /// </summary>
    internal Vector2 LastKnownScreenSize { get; set; } = new Vector2(1280, 720);

    public UIStackPanel(Vector2 position)
    {
        Position = position;
    }

    /// <summary>
    /// Adds a child. Children are laid out in the order they are added.
    /// </summary>
    public void AddChild(IUIComponent child)
    {
        _children.Add(child);
        UICanvas.PropagateScreenSize(child, LastKnownScreenSize);
    }

    /// <summary>
    /// Removes a child.
    /// </summary>
    public void RemoveChild(IUIComponent child) => _children.Remove(child);

    /// <summary>
    /// Removes all children.
    /// </summary>
    public void ClearChildren() => _children.Clear();

    /// <summary>
    /// Gets all children in layout order.
    /// </summary>
    public IReadOnlyList<IUIComponent> GetChildren() => _children.AsReadOnly();

    /// <summary>
    /// Recomputes each child's position and returns the resulting content size.
    /// Can be called before the first <see cref="Render"/> if you need sizes early.
    /// </summary>
    public Vector2 PerformLayout()
    {
        float cursor = Padding;
        float crossMax = 0f;

        foreach (var child in _children)
        {
            if (!child.Visible) continue;

            if (Orientation == StackOrientation.Vertical)
            {
                child.Position = new Vector2(Padding, cursor);
                cursor += child.Size.Y + Spacing;
                crossMax = Math.Max(crossMax, child.Size.X);
            }
            else
            {
                child.Position = new Vector2(cursor, Padding);
                cursor += child.Size.X + Spacing;
                crossMax = Math.Max(crossMax, child.Size.Y);
            }
        }

        // Remove trailing spacing that was added after the last visible child.
        if (cursor > Padding)
            cursor -= Spacing;

        return Orientation == StackOrientation.Vertical
            ? new Vector2(crossMax + Padding * 2f, cursor + Padding)
            : new Vector2(cursor + Padding, crossMax + Padding * 2f);
    }

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

        var contentSize = PerformLayout();
        Size = contentSize;

        if (BackgroundColor.HasValue)
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor.Value);

        if (ClipChildren)
            renderer.PushScissorRect(new Rectangle(Position.X, Position.Y, Size.X, Size.Y));

        foreach (var child in _children)
        {
            if (!child.Visible) continue;

            var saved = child.Position;
            child.Position = Position + saved;
            child.Render(renderer);
            child.Position = saved;
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

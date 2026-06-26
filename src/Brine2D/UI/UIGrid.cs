using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// A container that arranges its children in a fixed-column grid, flowing
/// left-to-right and top-to-bottom. Each cell is sized to the largest child
/// currently in the grid. <see cref="Size"/> is updated to fit all rows and
/// columns on every <see cref="Render"/> call.
/// </summary>
public class UIGrid : IUIComponent, IAnchoredUIComponent
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
    /// Number of columns. Defaults to 2.
    /// </summary>
    public int Columns { get; set; } = 2;

    /// <summary>
    /// Horizontal gap between cells in pixels.
    /// </summary>
    public float HorizontalSpacing { get; set; } = 4f;

    /// <summary>
    /// Vertical gap between rows in pixels.
    /// </summary>
    public float VerticalSpacing { get; set; } = 4f;

    /// <summary>
    /// Padding in pixels on all sides inside the grid background.
    /// </summary>
    public float Padding { get; set; } = 0f;

    /// <summary>
    /// Optional background color. When null, no background is drawn.
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// When true, mouse events over this grid are consumed.
    /// Defaults to false.
    /// </summary>
    public bool BlocksInput { get; set; } = false;

    /// <summary>
    /// When true, children are clipped to the grid bounds during rendering.
    /// Defaults to false.
    /// </summary>
    public bool ClipChildren { get; set; } = false;

    private readonly List<IUIComponent> _children = new();

    /// <summary>
    /// Last screen size pushed by <see cref="UICanvas"/>. Used when propagating to
    /// children added after this grid is already on a canvas.
    /// </summary>
    internal Vector2 LastKnownScreenSize { get; set; } = new Vector2(1280, 720);

    public UIGrid(Vector2 position)
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
    /// Recomputes each child's position from <see cref="Columns"/>, spacing, and padding.
    /// Cell dimensions come from the widest/tallest visible child.
    /// Returns the total content size including padding. Can be called before the first
    /// <see cref="Render"/> if you need sizes early.
    /// </summary>
    public Vector2 PerformLayout()
    {
        int cols = Math.Max(1, Columns);

        float cellW = 0f;
        float cellH = 0f;
        foreach (var child in _children)
        {
            if (!child.Visible) continue;
            cellW = Math.Max(cellW, child.Size.X);
            cellH = Math.Max(cellH, child.Size.Y);
        }

        int col = 0;
        int row = 0;
        foreach (var child in _children)
        {
            if (!child.Visible) continue;

            float x = Padding + col * (cellW + HorizontalSpacing);
            float y = Padding + row * (cellH + VerticalSpacing);
            child.Position = new Vector2(x, y);

            col++;
            if (col >= cols)
            {
                col = 0;
                row++;
            }
        }

        int visibleCount = _children.Count(c => c.Visible);
        if (visibleCount == 0)
            return new Vector2(Padding * 2f, Padding * 2f);

        int rows = (visibleCount + cols - 1) / cols;
        float totalW = cols * cellW + (cols - 1) * HorizontalSpacing + Padding * 2f;
        float totalH = rows * cellH + (rows - 1) * VerticalSpacing + Padding * 2f;
        return new Vector2(totalW, totalH);
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

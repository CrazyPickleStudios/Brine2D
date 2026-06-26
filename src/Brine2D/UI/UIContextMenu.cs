using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Right-click context menu rendered as a canvas-managed overlay.
/// Add items with <see cref="AddItem"/> and separators with <see cref="AddSeparator"/>.
/// Show via <see cref="UICanvas.ShowContextMenu"/>; it closes when an item is selected,
/// a click lands outside, or Escape is pressed.
/// </summary>
public class UIContextMenu
{
    private readonly List<ContextMenuItem> _items = new();

    /// <summary>
    /// Width of the menu in pixels. Height is computed automatically from the item count.
    /// </summary>
    public float Width { get; set; } = 160f;

    /// <summary>
    /// Height of each non-separator item in pixels.
    /// </summary>
    public float ItemHeight { get; set; } = 28f;

    /// <summary>
    /// Height of separator items in pixels.
    /// </summary>
    public float SeparatorHeight { get; set; } = 8f;

    /// <summary>
    /// Background fill color of the menu panel.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(45, 45, 45, 245);

    /// <summary>
    /// Border color drawn around the menu panel.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(160, 160, 160, 255);

    /// <summary>
    /// Background color used when the pointer is over an enabled item.
    /// </summary>
    public Color HoverColor { get; set; } = new Color(80, 120, 200, 255);

    /// <summary>
    /// Text color for enabled items.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Text color for disabled items.
    /// </summary>
    public Color DisabledTextColor { get; set; } = new Color(110, 110, 110, 255);

    /// <summary>
    /// Color of separator lines.
    /// </summary>
    public Color SeparatorColor { get; set; } = new Color(100, 100, 100, 200);

    /// <summary>
    /// Horizontal text padding from the left edge of menu items.
    /// </summary>
    public float TextPadding { get; set; } = 10f;

    /// <summary>
    /// Optional font for item labels. Null = renderer default.
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Screen-space position of the top-left corner. Set by <see cref="UICanvas.ShowContextMenu"/>.
    /// </summary>
    public Vector2 Position { get; internal set; }

    /// <summary>
    /// Total height of the menu in pixels, computed from the current item list.
    /// </summary>
    public float Height
    {
        get
        {
            float h = 0f;
            foreach (var item in _items)
                h += item.IsSeparator ? SeparatorHeight : ItemHeight;
            return h;
        }
    }

    /// <summary>
    /// Fired when an enabled item is selected. Parameters are the zero-based item index
    /// (separators are counted) and the item label.
    /// </summary>
    public event Action<int, string>? OnItemSelected;

    /// <summary>
    /// Fired when the menu is closed (item selected, outside click, or Escape).
    /// </summary>
    public event Action? OnClosed;

    /// <summary>
    /// Adds a labeled item. Use <paramref name="enabled"/> to grey it out without removing it.
    /// Returns <c>this</c> for fluent chaining.
    /// </summary>
    public UIContextMenu AddItem(string label, bool enabled = true)
    {
        _items.Add(new ContextMenuItem(label, false, enabled));
        return this;
    }

    /// <summary>
    /// Adds a visual separator line. Returns <c>this</c> for fluent chaining.
    /// </summary>
    public UIContextMenu AddSeparator()
    {
        _items.Add(new ContextMenuItem(string.Empty, true, false));
        return this;
    }

    /// <summary>
    /// Returns the number of items (including separators).
    /// </summary>
    public int ItemCount => _items.Count;

    /// <summary>
    /// Returns the label of the item at <paramref name="index"/>.
    /// Separators return an empty string.
    /// </summary>
    public string GetItemLabel(int index) => _items[index].Label;

    /// <summary>
    /// Returns whether the item at <paramref name="index"/> is enabled.
    /// </summary>
    public bool IsItemEnabled(int index) => !_items[index].IsSeparator && _items[index].Enabled;

    /// <summary>
    /// Returns whether the item at <paramref name="index"/> is a separator.
    /// </summary>
    public bool IsItemSeparator(int index) => _items[index].IsSeparator;

    /// <summary>
    /// Returns true when <paramref name="screenPosition"/> lies within the menu bounds.
    /// </summary>
    public bool Contains(Vector2 screenPosition) =>
        screenPosition.X >= Position.X && screenPosition.X <= Position.X + Width &&
        screenPosition.Y >= Position.Y && screenPosition.Y <= Position.Y + Height;

    /// <summary>
    /// Returns the index of the item whose row contains <paramref name="screenPosition"/>,
    /// or -1 if the position is outside the menu.
    /// </summary>
    public int HitTestItem(Vector2 screenPosition)
    {
        if (!Contains(screenPosition)) return -1;

        float y = Position.Y;
        for (int i = 0; i < _items.Count; i++)
        {
            float rowHeight = _items[i].IsSeparator ? SeparatorHeight : ItemHeight;
            if (screenPosition.Y < y + rowHeight) return i;
            y += rowHeight;
        }
        return -1;
    }

    internal void Render(IRenderer renderer, int hoveredIndex)
    {
        float totalHeight = Height;
        if (totalHeight <= 0f) return;

        renderer.DrawRectangleFilled(Position.X, Position.Y, Width, totalHeight, BackgroundColor);
        renderer.DrawRectangleOutline(Position.X, Position.Y, Width, totalHeight, BorderColor);

        float y = Position.Y;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];

            if (item.IsSeparator)
            {
                float lineY = y + SeparatorHeight / 2f;
                renderer.DrawLine(Position.X + 4f, lineY, Position.X + Width - 4f, lineY, SeparatorColor);
                y += SeparatorHeight;
                continue;
            }

            if (i == hoveredIndex && item.Enabled)
                renderer.DrawRectangleFilled(Position.X + 1f, y, Width - 2f, ItemHeight, HoverColor);

            var textColor = item.Enabled ? TextColor : DisabledTextColor;
            var opts = new TextRenderOptions { Color = textColor, Font = Font };
            var textSize = renderer.MeasureText(item.Label, opts);
            float textY = y + (ItemHeight - textSize.Y) / 2f;
            renderer.DrawText(item.Label, Position.X + TextPadding, textY, opts);

            y += ItemHeight;
        }
    }

    internal void FireItemSelected(int index)
    {
        if (index >= 0 && index < _items.Count && !_items[index].IsSeparator && _items[index].Enabled)
            OnItemSelected?.Invoke(index, _items[index].Label);
    }

    internal void FireClosed() => OnClosed?.Invoke();

    private sealed record ContextMenuItem(string Label, bool IsSeparator, bool Enabled);
}

using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Virtualized scrolling list that renders only visible rows.
/// Bind a data source via <see cref="SetItems"/> and supply a row renderer via
/// <see cref="RowRenderer"/>.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public class UIVirtualList<T> : UIVirtualListBase
{
    private IReadOnlyList<T> _items = [];

    /// <summary>
    /// Delegate that draws one row. Parameters: renderer, item, x, y, width, height, selected, hovered.
    /// </summary>
    public Action<IRenderer, T, float, float, float, float, bool, bool>? RowRenderer { get; set; }

    /// <summary>
    /// Text color used by the built-in fallback renderer when <see cref="RowRenderer"/> is null.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Font used by the built-in fallback renderer when <see cref="RowRenderer"/> is null.
    /// </summary>
    public IFont? Font { get; set; }

    /// <inheritdoc/>
    public override int ItemCount => _items.Count;

    /// <summary>Returns the item at <paramref name="index"/>.</summary>
    public T this[int index] => _items[index];

    /// <summary>
    /// Returns the currently selected item, or <see langword="default"/> when no row is selected.
    /// </summary>
    public T? SelectedItem => SelectedIndex >= 0 && SelectedIndex < _items.Count
        ? _items[SelectedIndex]
        : default;

    /// <summary>
    /// Replaces the list data source and resets scroll position and selection.
    /// </summary>
    public void SetItems(IReadOnlyList<T> items)
    {
        _items = items ?? [];
        ClearSelection();
        ScrollToTop();
    }

    /// <summary>
    /// Replaces the data source and resets scroll and selection.
    /// </summary>
    public void SetItems(IEnumerable<T> items) =>
        SetItems(items.ToList() as IReadOnlyList<T>);

    protected override void RenderRow(IRenderer renderer, int index,
        float x, float y, float width, float height,
        bool selected, bool hovered)
    {
        if (RowRenderer is not null)
        {
            RowRenderer(renderer, _items[index], x, y, width, height, selected, hovered);
            return;
        }

        // Built-in fallback: render ToString() with left padding
        string text = _items[index]?.ToString() ?? string.Empty;
        var options = new TextRenderOptions
        {
            Color = TextColor,
            Font = Font,
            MaxWidth = width - 8f
        }; // MaxWidth is float? — implicit conversion from float is valid
        renderer.DrawText(text, x + 4f, y + (height - 14f) / 2f, options);
    }
}

namespace Brine2D.UI;

/// <summary>
/// A single top-level menu in a <see cref="UIMenuBar"/> (e.g. "File", "Edit", "View").
/// Contains a list of <see cref="UIMenuItem"/> entries shown in a dropdown when the title
/// is clicked.
/// </summary>
public class UIMenuBarMenu
{
    private readonly List<UIMenuItem> _items = new();

    /// <summary>Text displayed in the menu bar title strip.</summary>
    public string Title { get; set; }

    /// <summary>Read-only view of the menu items.</summary>
    public IReadOnlyList<UIMenuItem> Items => _items;

    /// <summary>
    /// Fired when any enabled item in this menu is selected.
    /// Parameters are the zero-based item index and the item label.
    /// </summary>
    public event Action<int, string>? OnItemSelected;

    /// <param name="title">Title shown in the menu bar strip.</param>
    public UIMenuBarMenu(string title)
    {
        Title = title;
    }

    /// <summary>
    /// Adds a labeled item and returns <c>this</c> for fluent chaining.
    /// </summary>
    public UIMenuBarMenu Add(string label, bool enabled = true, Action? onClick = null)
    {
        _items.Add(new UIMenuItem(label, enabled, onClick));
        return this;
    }

    /// <summary>
    /// Adds a pre-built <see cref="UIMenuItem"/> and returns <c>this</c>.
    /// </summary>
    public UIMenuBarMenu Add(UIMenuItem item)
    {
        _items.Add(item);
        return this;
    }

    /// <summary>
    /// Adds a horizontal separator line and returns <c>this</c>.
    /// </summary>
    public UIMenuBarMenu AddSeparator()
    {
        _items.Add(UIMenuItem.Separator());
        return this;
    }

    /// <summary>
    /// Internal: fires <see cref="OnItemSelected"/> for the item at <paramref name="index"/>
    /// and invokes its <see cref="UIMenuItem.OnClick"/> callback.
    /// </summary>
    internal void FireItemSelected(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        var item = _items[index];
        if (!item.Enabled || item.IsSeparator) return;
        item.OnClick?.Invoke();
        OnItemSelected?.Invoke(index, item.Label);
    }
}

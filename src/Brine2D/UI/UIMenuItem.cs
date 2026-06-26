namespace Brine2D.UI;

/// <summary>
/// A single entry in a <see cref="UIMenuBarMenu"/> dropdown.
/// Can be a labeled action, a separator, or a disabled placeholder.
/// </summary>
public class UIMenuItem
{
    /// <summary>Display text for this item. Ignored for separator items.</summary>
    public string Label { get; set; }

    /// <summary>
    /// Disabled items are rendered dimmed and cannot be activated.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>When <c>true</c> this entry renders as a horizontal divider line.</summary>
    public bool IsSeparator { get; init; }

    /// <summary>
    /// Optional callback invoked when this item is selected.
    /// </summary>
    public Action? OnClick { get; set; }

    /// <param name="label">Display text.</param>
    /// <param name="enabled">Whether the item is interactive. Defaults to <c>true</c>.</param>
    /// <param name="onClick">Optional click handler.</param>
    public UIMenuItem(string label, bool enabled = true, Action? onClick = null)
    {
        Label = label;
        Enabled = enabled;
        IsSeparator = false;
        OnClick = onClick;
    }

    private UIMenuItem()
    {
        Label = string.Empty;
        Enabled = false;
        IsSeparator = true;
    }

    /// <summary>Creates a separator item.</summary>
    public static UIMenuItem Separator() => new();
}

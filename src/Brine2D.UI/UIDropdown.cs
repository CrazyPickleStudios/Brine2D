using System.Drawing;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Dropdown/ComboBox UI component for selecting from a list of options.
/// </summary>
public class UIDropdown : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// List of items in the dropdown.
    /// </summary>
    public List<string> Items { get; } = new();

    /// <summary>
    /// Currently selected item index (-1 = none selected).
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value && value >= -1 && value < Items.Count)
            {
                _selectedIndex = value;
                OnSelectionChanged?.Invoke(_selectedIndex, SelectedText);
            }
        }
    }

    /// <summary>
    /// Currently selected item text (null if none selected).
    /// </summary>
    public string? SelectedText => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

    /// <summary>
    /// Whether the dropdown list is currently expanded.
    /// </summary>
    public bool IsExpanded { get; private set; }

    /// <summary>
    /// Maximum number of visible items before scrolling (0 = show all).
    /// </summary>
    public int MaxVisibleItems { get; set; } = 5;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.FromArgb(60, 60, 60);

    /// <summary>
    /// Hover color for items.
    /// </summary>
    public Color HoverColor { get; set; } = Color.FromArgb(80, 80, 80);

    /// <summary>
    /// Selected item color.
    /// </summary>
    public Color SelectedColor { get; set; } = Color.FromArgb(100, 150, 255);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = Color.FromArgb(150, 150, 150);

    /// <summary>
    /// Event fired when selection changes.
    /// </summary>
    public event Action<int, string?>? OnSelectionChanged;

    public UITooltip? Tooltip { get; set; }

    private int _selectedIndex = -1;
    private int _hoveredItemIndex = -1;

    public UIDropdown(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        // Dropdown state is managed by UICanvas
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        float itemHeight = Size.Y;

        // Draw main dropdown box
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Draw border
        float borderThickness = 2f;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, BorderColor);

        // Draw selected item text
        var displayText = SelectedText ?? "(Select)";
        var textX = Position.X + 10;
        var textY = Position.Y + (Size.Y / 2) - 8;
        renderer.DrawText(displayText, textX, textY, Enabled ? TextColor : Color.FromArgb(100, 100, 100));

        // Draw dropdown arrow
        var arrowX = Position.X + Size.X - 20;
        var arrowY = Position.Y + (Size.Y / 2) - 4;
        renderer.DrawText(IsExpanded ? "▲" : "▼", arrowX, arrowY, TextColor);

        // Draw expanded list if open
        if (IsExpanded && Items.Count > 0)
        {
            int visibleCount = MaxVisibleItems > 0 ? Math.Min(MaxVisibleItems, Items.Count) : Items.Count;
            float listHeight = visibleCount * itemHeight;

            // Draw list background
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y, Size.X, listHeight, BackgroundColor);

            // Draw list border
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y, Size.X, borderThickness, BorderColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y + listHeight - borderThickness, Size.X, borderThickness, BorderColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y, borderThickness, listHeight, BorderColor);
            renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y + Size.Y, borderThickness, listHeight, BorderColor);

            // Draw items
            for (int i = 0; i < visibleCount; i++)
            {
                float itemY = Position.Y + Size.Y + (i * itemHeight);

                // Determine item color
                Color itemColor = BackgroundColor;
                if (i == _hoveredItemIndex)
                {
                    itemColor = HoverColor;
                }
                else if (i == _selectedIndex)
                {
                    itemColor = SelectedColor;
                }

                // Draw item background
                renderer.DrawRectangleFilled(Position.X, itemY, Size.X, itemHeight, itemColor);

                // Draw item text
                var itemTextX = Position.X + 10;
                var itemTextY = itemY + (itemHeight / 2) - 8;
                renderer.DrawText(Items[i], itemTextX, itemTextY, TextColor);
            }
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        // Check main dropdown box
        bool inMainBox = screenPosition.X >= Position.X &&
                        screenPosition.X <= Position.X + Size.X &&
                        screenPosition.Y >= Position.Y &&
                        screenPosition.Y <= Position.Y + Size.Y;

        if (inMainBox) return true;

        // Check expanded list if open
        if (IsExpanded && Items.Count > 0)
        {
            int visibleCount = MaxVisibleItems > 0 ? Math.Min(MaxVisibleItems, Items.Count) : Items.Count;
            float listHeight = visibleCount * Size.Y;

            return screenPosition.X >= Position.X &&
                   screenPosition.X <= Position.X + Size.X &&
                   screenPosition.Y >= Position.Y + Size.Y &&
                   screenPosition.Y <= Position.Y + Size.Y + listHeight;
        }

        return false;
    }

    /// <summary>
    /// Adds an item to the dropdown.
    /// </summary>
    public void AddItem(string item)
    {
        Items.Add(item);
    }

    /// <summary>
    /// Removes an item from the dropdown.
    /// </summary>
    public bool RemoveItem(string item)
    {
        return Items.Remove(item);
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
        SelectedIndex = -1;
    }

    /// <summary>
    /// Called by UICanvas when dropdown is clicked.
    /// </summary>
    internal void Toggle()
    {
        if (!Enabled) return;
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Called by UICanvas when an item is clicked.
    /// </summary>
    internal void SelectItem(Vector2 mousePosition)
    {
        if (!Enabled || !IsExpanded || Items.Count == 0) return;

        // Calculate which item was clicked
        float relativeY = mousePosition.Y - (Position.Y + Size.Y);
        int itemIndex = (int)(relativeY / Size.Y);

        int visibleCount = MaxVisibleItems > 0 ? Math.Min(MaxVisibleItems, Items.Count) : Items.Count;

        if (itemIndex >= 0 && itemIndex < visibleCount)
        {
            SelectedIndex = itemIndex;
            IsExpanded = false;
        }
    }

    /// <summary>
    /// Called by UICanvas to update hover state.
    /// </summary>
    internal void UpdateHover(Vector2 mousePosition)
    {
        if (!IsExpanded || Items.Count == 0)
        {
            _hoveredItemIndex = -1;
            return;
        }

        // Check if mouse is over the list
        float relativeY = mousePosition.Y - (Position.Y + Size.Y);
        int itemIndex = (int)(relativeY / Size.Y);

        int visibleCount = MaxVisibleItems > 0 ? Math.Min(MaxVisibleItems, Items.Count) : Items.Count;

        if (itemIndex >= 0 && itemIndex < visibleCount)
        {
            _hoveredItemIndex = itemIndex;
        }
        else
        {
            _hoveredItemIndex = -1;
        }
    }

    /// <summary>
    /// Called by UICanvas to close the dropdown.
    /// </summary>
    internal void Close()
    {
        IsExpanded = false;
        _hoveredItemIndex = -1;
    }
}
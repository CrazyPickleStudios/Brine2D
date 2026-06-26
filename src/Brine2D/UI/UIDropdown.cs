using Brine2D.Core;
using System.Collections.Generic;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Dropdown/ComboBox UI component for selecting from a list of options.
/// </summary>
public class UIDropdown : IUIComponent, IAnchoredUIComponent
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
    public Color BackgroundColor { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Hover color for items.
    /// </summary>
    public Color HoverColor { get; set; } = new Color(80, 80, 80);

    /// <summary>
    /// Selected item color.
    /// </summary>
    public Color SelectedColor { get; set; } = new Color(100, 150, 255);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(150, 150, 150);

    /// <summary>
    /// Scrollbar width in pixels.
    /// </summary>
    public float ScrollbarWidth { get; set; } = 8f;

    /// <summary>
    /// Scrollbar thumb color.
    /// </summary>
    public Color ScrollbarColor { get; set; } = new Color(120, 120, 120);

    /// <summary>
    /// Event fired when selection changes.
    /// </summary>
    public event Action<int, string?>? OnSelectionChanged;

    /// <summary>
    /// Event fired when this dropdown gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this dropdown loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    public UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Screen height in pixels. Used to flip the list above the header when it would
    /// overflow the bottom of the screen. Set by <see cref="UICanvas"/>. Defaults to 720.
    /// </summary>
    public float ScreenHeight { get; set; } = 720f;

    /// <summary>
    /// Text displayed in the header when no item is selected. Defaults to "(Select)".
    /// </summary>
    public string Placeholder { get; set; } = "(Select)";

    /// <summary>
    /// Optional font for rendering text (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    private int _selectedIndex = -1;
    private int _hoveredItemIndex = -1;
    private int _keyboardCursorIndex = -1;
    private int _scrollOffset;
    private bool _isFocused;
    private readonly HashSet<int> _disabledIndices = new();

    /// <summary>
    /// Color used to render item text for items that have been disabled via <see cref="SetItemEnabled"/>.
    /// </summary>
    public Color DisabledItemColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Whether this dropdown currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring drawn around the header when the dropdown has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    public UIDropdown(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        // Dropdown state is managed by UICanvas
    }

    /// <summary>
    /// When <c>true</c>, the expanded list is not rendered inside <see cref="Render"/>.
    /// Set by <see cref="UICanvas"/> when this dropdown is inside a scroll view so the
    /// list can be drawn as a top-level overlay without scissor clipping.
    /// </summary>
    internal bool SuppressListRender { get; set; }

    /// <summary>Returns true when the expanded list should render above the header to avoid overflowing the screen.</summary>
    private bool IsFlipped => IsExpanded && Items.Count > 0 &&
                              Position.Y + Size.Y + GetVisibleCount() * Size.Y > ScreenHeight;

    private int GetVisibleCount() =>
        MaxVisibleItems > 0 ? Math.Min(MaxVisibleItems, Items.Count) : Items.Count;

    private bool NeedsScrollbar() =>
        MaxVisibleItems > 0 && Items.Count > MaxVisibleItems;

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        float itemHeight = Size.Y;

        // Draw main dropdown box
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        float borderThickness = 2f;
        var headerBorderColor = _isFocused ? FocusColor : BorderColor;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, headerBorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, headerBorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, headerBorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, headerBorderColor);

        // Draw selected item text
        var displayText = SelectedText ?? Placeholder;
        var textX = Position.X + 10;
        var headerTextOpts = new TextRenderOptions { Color = Enabled ? TextColor : new Color(100, 100, 100), Font = Font, LineSpacing = 1.0f };
        var headerTextSize = renderer.MeasureText(displayText, headerTextOpts);
        var textY = MathF.Round(Position.Y + (Size.Y - headerTextSize.Y) / 2f);
        renderer.DrawText(displayText, textX, textY, headerTextOpts);

        var arrowStr = IsExpanded ? "▲" : "▼";
        var arrowOpts = new TextRenderOptions { Color = TextColor, Font = Font, LineSpacing = 1.0f };
        var arrowSize = renderer.MeasureText(arrowStr, arrowOpts);
        var arrowX = MathF.Round(Position.X + Size.X - arrowSize.X - 8f);
        var arrowY = MathF.Round(Position.Y + (Size.Y - arrowSize.Y) / 2f);
        renderer.DrawText(arrowStr, arrowX, arrowY, arrowOpts);

        if (!IsExpanded || Items.Count == 0 || SuppressListRender)
            return;

        RenderList(renderer);
    }

    private void RenderList(IRenderer renderer)
    {
        float itemHeight = Size.Y;
        float borderThickness = 2f;

        int visibleCount = GetVisibleCount();
        float listHeight = visibleCount * itemHeight;
        bool needsScrollbar = NeedsScrollbar();
        float itemDrawWidth = needsScrollbar ? Size.X - ScrollbarWidth : Size.X;
        bool flipped = IsFlipped;
        float listY = flipped ? Position.Y - listHeight : Position.Y + Size.Y;

        // Draw list background
        renderer.DrawRectangleFilled(Position.X, listY, Size.X, listHeight, BackgroundColor);

        // Draw list border
        renderer.DrawRectangleFilled(Position.X, listY, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, listY + listHeight - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, listY, borderThickness, listHeight, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, listY, borderThickness, listHeight, BorderColor);

        // Draw visible items
        for (int i = 0; i < visibleCount; i++)
        {
            int itemIndex = i + _scrollOffset;
            if (itemIndex >= Items.Count) break;

            float itemY = listY + (i * itemHeight);

            Color itemColor = BackgroundColor;
            if (i == _hoveredItemIndex)
                itemColor = HoverColor;
            else if (itemIndex == _keyboardCursorIndex && _isFocused)
                itemColor = HoverColor;
            else if (itemIndex == _selectedIndex)
                itemColor = SelectedColor;

            renderer.DrawRectangleFilled(Position.X, itemY, itemDrawWidth, itemHeight, itemColor);

            bool itemEnabled = IsItemEnabled(itemIndex);
            var itemTextColor = itemEnabled ? TextColor : DisabledItemColor;
            var itemTextOpts = new TextRenderOptions { Color = itemTextColor, Font = Font, LineSpacing = 1.0f };
            var itemTextSize = renderer.MeasureText(Items[itemIndex], itemTextOpts);
            var itemTextX = Position.X + 10;
            var itemTextY = MathF.Round(itemY + (itemHeight - itemTextSize.Y) / 2f);
            renderer.DrawText(Items[itemIndex], itemTextX, itemTextY, itemTextOpts);
        }

        // Draw scrollbar if needed
        if (needsScrollbar)
        {
            float scrollbarX = Position.X + Size.X - ScrollbarWidth;
            float thumbRatio = (float)visibleCount / Items.Count;
            float thumbHeight = listHeight * thumbRatio;
            float thumbOffsetRatio = Items.Count > visibleCount
                ? (float)_scrollOffset / (Items.Count - visibleCount)
                : 0f;
            float thumbY = listY + (listHeight - thumbHeight) * thumbOffsetRatio;

            renderer.DrawRectangleFilled(scrollbarX, listY, ScrollbarWidth, listHeight, new Color(40, 40, 40));
            renderer.DrawRectangleFilled(scrollbarX, thumbY, ScrollbarWidth, thumbHeight, ScrollbarColor);
        }
    }

    /// <summary>
    /// Renders only the expanded item list at <paramref name="screenPosition"/>, bypassing any
    /// scissor rectangle. Called by <see cref="UICanvas"/> after all other components are drawn
    /// when this dropdown is nested inside a <see cref="UIScrollView"/>.
    /// </summary>
    internal void RenderListOverlay(IRenderer renderer, Vector2 screenPosition)
    {
        if (!IsExpanded || Items.Count == 0) return;

        var savedPosition = Position;
        Position = screenPosition;
        RenderList(renderer);
        Position = savedPosition;
    }

    public bool Contains(Vector2 screenPosition)
    {
        bool inMainBox = screenPosition.X >= Position.X &&
                        screenPosition.X <= Position.X + Size.X &&
                        screenPosition.Y >= Position.Y &&
                        screenPosition.Y <= Position.Y + Size.Y;

        if (inMainBox) return true;

        if (IsExpanded && Items.Count > 0)
        {
            int visibleCount = GetVisibleCount();
            float listHeight = visibleCount * Size.Y;
            bool flipped = IsFlipped;
            float listTop = flipped ? Position.Y - listHeight : Position.Y + Size.Y;
            float listBottom = flipped ? Position.Y : Position.Y + Size.Y + listHeight;

            return screenPosition.X >= Position.X &&
                   screenPosition.X <= Position.X + Size.X &&
                   screenPosition.Y >= listTop &&
                   screenPosition.Y <= listBottom;
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
        int removedIndex = Items.IndexOf(item);
        if (removedIndex < 0) return false;
        Items.RemoveAt(removedIndex);
        RebuildDisabledIndicesAfterRemoval(removedIndex);
        ClampScrollOffset();
        return true;
    }

    /// <summary>
    /// Clears all items.
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
        SelectedIndex = -1;
        _scrollOffset = 0;
        _disabledIndices.Clear();
    }

    /// <summary>
    /// Enables or disables a single item by its index.
    /// Disabled items are rendered in <see cref="DisabledItemColor"/> and cannot be selected
    /// by mouse click or keyboard. If a currently selected item is disabled the selection
    /// is not automatically cleared — the developer should handle that case explicitly.
    /// </summary>
    public void SetItemEnabled(int index, bool enabled)
    {
        if (index < 0 || index >= Items.Count) return;
        if (enabled) _disabledIndices.Remove(index);
        else _disabledIndices.Add(index);
    }

    /// <summary>
    /// Returns whether the item at <paramref name="index"/> is enabled.
    /// Items are enabled by default.
    /// </summary>
    public bool IsItemEnabled(int index) => !_disabledIndices.Contains(index);

    /// <summary>
    /// Returns true when <paramref name="screenPosition"/> is over the expanded item list
    /// but not over the header row itself. Works for both normal (below header) and
    /// flipped (above header) layouts.
    /// </summary>
    internal bool IsOverExpandedList(Vector2 screenPosition)
    {
        if (!IsExpanded || Items.Count == 0) return false;

        int visibleCount = GetVisibleCount();
        float listHeight = visibleCount * Size.Y;
        bool flipped = IsFlipped;
        float listTop = flipped ? Position.Y - listHeight : Position.Y + Size.Y;
        float listBottom = flipped ? Position.Y : Position.Y + Size.Y + listHeight;

        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= listTop &&
               screenPosition.Y < listBottom;
    }

    /// <summary>
    /// Called by UICanvas when dropdown header is clicked.
    /// </summary>
    internal void Toggle()
    {
        if (!Enabled) return;
        IsExpanded = !IsExpanded;
        if (IsExpanded)
            _keyboardCursorIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
        else
            _hoveredItemIndex = -1;
    }

    /// <summary>
    /// Called by UICanvas when an item in the expanded list is clicked.
    /// </summary>
    internal void SelectItem(Vector2 mousePosition)
    {
        if (!Enabled || !IsExpanded || Items.Count == 0) return;

        int visibleCount = GetVisibleCount();
        float listHeight = visibleCount * Size.Y;
        float listY = IsFlipped ? Position.Y - listHeight : Position.Y + Size.Y;
        float relativeY = mousePosition.Y - listY;
        int visibleIndex = (int)(relativeY / Size.Y);

        if (visibleIndex >= 0 && visibleIndex < visibleCount)
        {
            int itemIndex = visibleIndex + _scrollOffset;
            if (itemIndex < Items.Count && IsItemEnabled(itemIndex))
            {
                SelectedIndex = itemIndex;
                IsExpanded = false;
                _hoveredItemIndex = -1;
            }
        }
    }

    /// <summary>
    /// Called by UICanvas to update hover state for the expanded list.
    /// </summary>
    internal void UpdateHover(Vector2 mousePosition)
    {
        if (!IsExpanded || Items.Count == 0)
        {
            _hoveredItemIndex = -1;
            return;
        }

        int visibleCount = GetVisibleCount();
        float listHeight = visibleCount * Size.Y;
        float listY = IsFlipped ? Position.Y - listHeight : Position.Y + Size.Y;
        float relativeY = mousePosition.Y - listY;
        int visibleIndex = (int)(relativeY / Size.Y);

        _hoveredItemIndex = (visibleIndex >= 0 && visibleIndex < visibleCount) ? visibleIndex : -1;
    }

    /// <summary>
    /// Called by UICanvas to scroll the expanded list via the mouse wheel.
    /// </summary>
    internal void Scroll(float delta)
    {
        if (!IsExpanded || !NeedsScrollbar()) return;

        int direction = delta < 0 ? 1 : -1;
        int steps = Math.Max(1, (int)Math.Abs(delta));
        _scrollOffset = Math.Clamp(_scrollOffset + direction * steps, 0, Items.Count - GetVisibleCount());
    }

    /// <summary>
    /// Called by UICanvas to close the dropdown.
    /// </summary>
    internal void Close()
    {
        IsExpanded = false;
        _hoveredItemIndex = -1;
        _keyboardCursorIndex = -1;
    }

    private void ClampScrollOffset()
    {
        int maxScroll = Math.Max(0, Items.Count - GetVisibleCount());
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
    }

    private void RebuildDisabledIndicesAfterRemoval(int removedIndex)
    {
        var updated = new HashSet<int>();
        foreach (var i in _disabledIndices)
        {
            if (i < removedIndex) updated.Add(i);
            else if (i > removedIndex) updated.Add(i - 1);
            // i == removedIndex: that item is gone, drop it
        }
        _disabledIndices.Clear();
        foreach (var i in updated)
            _disabledIndices.Add(i);
    }

    /// <summary>
    /// Moves the keyboard cursor by <paramref name="direction"/> steps (+1 = down, -1 = up)
    /// within the expanded list. If the list is not open, opens it first.
    /// Called by UICanvas when Up/Down arrow keys are pressed while the dropdown is focused.
    /// </summary>
    internal void NavigateItem(int direction)
    {
        if (!Enabled) return;

        if (!IsExpanded)
        {
            IsExpanded = true;
            _keyboardCursorIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
            return;
        }

        if (Items.Count == 0) return;

        if (_keyboardCursorIndex < 0)
            _keyboardCursorIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
        else
        {
            int next = _keyboardCursorIndex;
            for (int attempts = 0; attempts < Items.Count; attempts++)
            {
                next = Math.Clamp(next + direction, 0, Items.Count - 1);
                if (IsItemEnabled(next)) break;
                if (next == 0 || next == Items.Count - 1) break;
            }
            _keyboardCursorIndex = next;
        }

        // Scroll the list so the cursor is visible.
        int visibleCount = GetVisibleCount();
        if (_keyboardCursorIndex < _scrollOffset)
            _scrollOffset = _keyboardCursorIndex;
        else if (_keyboardCursorIndex >= _scrollOffset + visibleCount)
            _scrollOffset = _keyboardCursorIndex - visibleCount + 1;
    }

    /// <summary>
    /// Selects the item currently under the keyboard cursor and closes the list.
    /// Called by UICanvas when Enter/Space is pressed while the dropdown is expanded and focused.
    /// </summary>
    internal void ConfirmKeyboardSelection()
    {
        if (!IsExpanded || Items.Count == 0) return;

        int target = _keyboardCursorIndex >= 0 ? _keyboardCursorIndex : _selectedIndex;
        if (target >= 0 && target < Items.Count && IsItemEnabled(target))
        {
            SelectedIndex = target;
            IsExpanded = false;
            _hoveredItemIndex = -1;
            _keyboardCursorIndex = -1;
        }
    }

    /// <summary>
    /// Called by UICanvas to set keyboard focus on this dropdown.
    /// </summary>
    internal void SetFocused(bool focused)
    {
        bool newFocused = focused && Enabled;
        if (newFocused == _isFocused) return;
        _isFocused = newFocused;
        if (_isFocused) OnFocusGained?.Invoke();
        else
        {
            OnFocusLost?.Invoke();
            Close();
        }
    }
}

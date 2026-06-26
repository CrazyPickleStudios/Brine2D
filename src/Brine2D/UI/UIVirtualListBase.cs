using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Non-generic base for <see cref="UIVirtualList{T}"/> that holds all rendering,
/// scrolling, and hit-test logic. Use the typed subclass in application code.
/// </summary>
public abstract class UIVirtualListBase : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    // ── Geometry ────────────────────────────────────────────────────────────

    /// <summary>Pixel height of each row. Defaults to 24.</summary>
    public float RowHeight { get; set; } = 24f;

    /// <summary>Width of the vertical scrollbar track in pixels. Defaults to 10.</summary>
    public float ScrollbarWidth { get; set; } = 10f;

    /// <summary>Minimum scrollbar thumb height in pixels. Defaults to 10.</summary>
    public float MinThumbSize { get; set; } = 10f;

    // ── Colors ───────────────────────────────────────────────────────────────

    public Color BackgroundColor { get; set; } = new Color(30, 30, 30);
    public Color RowColor { get; set; } = new Color(40, 40, 40);
    public Color AlternateRowColor { get; set; } = new Color(45, 45, 45);
    public Color HoverColor { get; set; } = new Color(70, 70, 100);
    public Color SelectionColor { get; set; } = new Color(60, 100, 180);
    public Color BorderColor { get; set; } = new Color(80, 80, 80);
    public Color ScrollbarTrackColor { get; set; } = new Color(50, 50, 50);
    public Color ScrollbarThumbColor { get; set; } = new Color(100, 100, 100);
    public Color ScrollbarThumbHoverColor { get; set; } = new Color(140, 140, 140);
    public Color FocusBorderColor { get; set; } = new Color(120, 180, 255);

    // ── State ────────────────────────────────────────────────────────────────

    private float _scrollOffsetY;
    private bool _isFocused;
    private bool _isThumbHovered;

    /// <summary>Current scroll position in pixels.</summary>
    public float ScrollOffsetY
    {
        get => _scrollOffsetY;
        set => _scrollOffsetY = Math.Clamp(value, 0f, MaxScrollY);
    }

    /// <summary>Index of the hovered row, or -1 if none.</summary>
    public int HoveredIndex { get; private set; } = -1;

    /// <summary>Index of the selected row, or -1 if none.</summary>
    public int SelectedIndex { get; private set; } = -1;

    /// <summary>Total number of items in the list.</summary>
    public abstract int ItemCount { get; }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired when keyboard focus is gained.</summary>
    public event Action? OnFocusGained;

    /// <summary>Fired when keyboard focus is lost.</summary>
    public event Action? OnFocusLost;

    /// <summary>Fired when the selected row changes. Argument is the new selected index (−1 = none).</summary>
    public event Action<int>? OnSelectionChanged;

    // ── Computed ─────────────────────────────────────────────────────────────

    private float TotalContentHeight => ItemCount * RowHeight;

    private float MaxScrollY => Math.Max(0f, TotalContentHeight - ListAreaHeight);

    /// <summary>Height of the list content area (excluding scrollbar).</summary>
    private float ListAreaHeight => Size.Y;

    /// <summary>Width of the content area (excluding scrollbar when visible).</summary>
    private float ListAreaWidth => NeedsScrollbar ? Size.X - ScrollbarWidth : Size.X;

    private bool NeedsScrollbar => TotalContentHeight > ListAreaHeight;

    // ── IUIComponent ─────────────────────────────────────────────────────────

    public void Update(float deltaTime) { }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Scissor-clip content rows
        renderer.PushScissorRect(new Core.Rectangle
        {
            X = Position.X, Y = Position.Y,
            Width = ListAreaWidth, Height = Size.Y
        });

        int firstVisible = (int)(_scrollOffsetY / RowHeight);
        int lastVisible = Math.Min(ItemCount - 1,
            (int)Math.Ceiling((_scrollOffsetY + ListAreaHeight) / RowHeight) - 1);

        for (int i = firstVisible; i <= lastVisible; i++)
        {
            float rowY = Position.Y + i * RowHeight - _scrollOffsetY;
            float rowW = ListAreaWidth;

            var bg = i == SelectedIndex ? SelectionColor
                : i == HoveredIndex ? HoverColor
                : i % 2 == 0 ? RowColor : AlternateRowColor;

            renderer.DrawRectangleFilled(Position.X, rowY, rowW, RowHeight, bg);
            RenderRow(renderer, i, Position.X, rowY, rowW, RowHeight,
                selected: i == SelectedIndex, hovered: i == HoveredIndex);
        }

        renderer.PopScissorRect();

        // Border / focus ring
        var borderCol = _isFocused ? FocusBorderColor : BorderColor;
        renderer.DrawRectangleOutline(Position.X, Position.Y, Size.X, Size.Y, borderCol);

        // Scrollbar
        if (NeedsScrollbar)
            RenderScrollbar(renderer);
    }

    private void RenderScrollbar(IRenderer renderer)
    {
        float trackX = Position.X + Size.X - ScrollbarWidth;
        renderer.DrawRectangleFilled(trackX, Position.Y, ScrollbarWidth, Size.Y, ScrollbarTrackColor);

        float thumbHeight = Math.Max(MinThumbSize, (ListAreaHeight / TotalContentHeight) * Size.Y);
        float thumbRange = Size.Y - thumbHeight;
        float thumbY = thumbRange > 0f
            ? Position.Y + (_scrollOffsetY / MaxScrollY) * thumbRange
            : Position.Y;

        var thumbColor = _isThumbHovered ? ScrollbarThumbHoverColor : ScrollbarThumbColor;
        renderer.DrawRectangleFilled(trackX, thumbY, ScrollbarWidth, thumbHeight, thumbColor);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    // ── Abstract row rendering ────────────────────────────────────────────────

    /// <summary>
    /// Implemented by the typed subclass to draw a single row.
    /// Called only for rows that are currently visible.
    /// </summary>
    protected abstract void RenderRow(IRenderer renderer, int index,
        float x, float y, float width, float height,
        bool selected, bool hovered);

    // ── Canvas internal surface ───────────────────────────────────────────────

    internal void SetFocused(bool focused)
    {
        if (focused == _isFocused) return;
        _isFocused = focused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }

    internal bool IsFocused => _isFocused;

    /// <summary>Updates <see cref="HoveredIndex"/> and scrollbar thumb hover.</summary>
    internal void UpdateHover(Vector2 screenPos)
    {
        if (!Enabled || !Visible) { HoveredIndex = -1; _isThumbHovered = false; return; }

        // Check if hovering the scrollbar thumb
        _isThumbHovered = NeedsScrollbar && IsOverScrollbarThumb(screenPos);

        // Row hover
        if (screenPos.X >= Position.X && screenPos.X <= Position.X + ListAreaWidth &&
            screenPos.Y >= Position.Y && screenPos.Y <= Position.Y + Size.Y)
        {
            float relY = screenPos.Y - Position.Y + _scrollOffsetY;
            int idx = (int)(relY / RowHeight);
            HoveredIndex = idx >= 0 && idx < ItemCount ? idx : -1;
        }
        else
        {
            HoveredIndex = -1;
        }
    }

    /// <summary>Clears hover state (called when mouse leaves the list).</summary>
    internal void ClearHover()
    {
        HoveredIndex = -1;
        _isThumbHovered = false;
    }

    /// <summary>Selects the hovered row on left-button press. Ignores clicks on the scrollbar.</summary>
    internal void HandleClick(Vector2 screenPos)
    {
        if (!Enabled) return;

        // Ignore clicks on scrollbar
        if (NeedsScrollbar && screenPos.X >= Position.X + ListAreaWidth) return;

        UpdateHover(screenPos);
        if (HoveredIndex >= 0)
            Select(HoveredIndex);
    }

    /// <summary>Scrolls by <paramref name="delta"/> rows (positive = down).</summary>
    internal void HandleScroll(float delta) =>
        ScrollOffsetY += delta * RowHeight;

    /// <summary>Scrolls by one visible page (positive = down).</summary>
    internal void HandlePageScroll(float direction) =>
        ScrollOffsetY += direction * ListAreaHeight;

    public void ScrollToTop() => ScrollOffsetY = 0f;

    public void ScrollToBottom() => ScrollOffsetY = MaxScrollY;

    /// <summary>Moves selection up by one row, scrolling into view.</summary>
    internal void NavigateUp()
    {
        if (!Enabled) return;
        if (SelectedIndex > 0)
        {
            Select(SelectedIndex - 1);
            ScrollIntoView(SelectedIndex);
        }
        else if (SelectedIndex == -1 && ItemCount > 0)
        {
            Select(ItemCount - 1);
            ScrollToBottom();
        }
    }

    /// <summary>Moves selection down by one row, scrolling into view.</summary>
    internal void NavigateDown()
    {
        if (!Enabled) return;
        if (SelectedIndex == -1 && ItemCount > 0)
        {
            Select(0);
            ScrollToTop();
        }
        else if (SelectedIndex < ItemCount - 1)
        {
            Select(SelectedIndex + 1);
            ScrollIntoView(SelectedIndex);
        }
    }

    /// <summary>Selects the row at <paramref name="index"/>.</summary>
    public void Select(int index)
    {
        if (index < 0 || index >= ItemCount) return;
        if (index == SelectedIndex) return;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke(SelectedIndex);
    }

    /// <summary>Clears the current selection.</summary>
    internal void ClearSelection()
    {
        if (SelectedIndex == -1) return;
        SelectedIndex = -1;
        OnSelectionChanged?.Invoke(-1);
    }

    /// <summary>Ensures the row at <paramref name="index"/> is fully visible.</summary>
    public void ScrollIntoView(int index)
    {
        if (index < 0 || index >= ItemCount) return;
        float rowTop = index * RowHeight;
        float rowBottom = rowTop + RowHeight;

        if (rowTop < _scrollOffsetY)
            ScrollOffsetY = rowTop;
        else if (rowBottom > _scrollOffsetY + ListAreaHeight)
            ScrollOffsetY = rowBottom - ListAreaHeight;
    }

    // ── Scrollbar geometry helpers ────────────────────────────────────────────

    internal bool IsOverScrollbarTrack(Vector2 pos) =>
        NeedsScrollbar &&
        pos.X >= Position.X + Size.X - ScrollbarWidth &&
        pos.X <= Position.X + Size.X &&
        pos.Y >= Position.Y && pos.Y <= Position.Y + Size.Y;

    internal bool IsOverScrollbarThumb(Vector2 pos)
    {
        if (!NeedsScrollbar) return false;
        float trackX = Position.X + Size.X - ScrollbarWidth;
        float thumbHeight = Math.Max(MinThumbSize, (ListAreaHeight / TotalContentHeight) * Size.Y);
        float thumbRange = Size.Y - thumbHeight;
        float thumbY = thumbRange > 0f ? Position.Y + (_scrollOffsetY / MaxScrollY) * thumbRange : Position.Y;
        return pos.X >= trackX && pos.X <= trackX + ScrollbarWidth &&
               pos.Y >= thumbY && pos.Y <= thumbY + thumbHeight;
    }

    private float _thumbDragStartY;
    private float _thumbDragStartOffset;
    private bool _isDraggingThumb;

    internal bool IsDraggingThumb => _isDraggingThumb;

    internal void StartThumbDrag(float mouseY)
    {
        _isDraggingThumb = true;
        _thumbDragStartY = mouseY;
        _thumbDragStartOffset = _scrollOffsetY;
    }

    internal void UpdateThumbDrag(float mouseY)
    {
        if (!_isDraggingThumb) return;
        float thumbHeight = Math.Max(MinThumbSize, (ListAreaHeight / TotalContentHeight) * Size.Y);
        float trackRange = Size.Y - thumbHeight;
        float delta = mouseY - _thumbDragStartY;
        float scrollRatio = trackRange > 0f ? delta / trackRange : 0f;
        ScrollOffsetY = _thumbDragStartOffset + scrollRatio * MaxScrollY;
    }

    internal void EndThumbDrag()
    {
        _isDraggingThumb = false;
    }

    internal void JumpScrollTo(float mouseY)
    {
        float relY = mouseY - Position.Y;
        float ratio = Math.Clamp(relY / Size.Y, 0f, 1f);
        ScrollOffsetY = ratio * MaxScrollY;
    }
}

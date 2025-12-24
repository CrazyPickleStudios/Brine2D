using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Scrollable container for UI components that exceed visible area.
/// </summary>
public class UIScrollView : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Total content height (can be larger than Size.Y for scrolling).
    /// </summary>
    public float ContentHeight { get; set; }

    /// <summary>
    /// Total content width (can be larger than Size.X for horizontal scrolling).
    /// </summary>
    public float ContentWidth { get; set; }

    /// <summary>
    /// Current scroll offset (0 = top/left).
    /// </summary>
    public Vector2 ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            _scrollOffset = new Vector2(
                Math.Clamp(value.X, 0, Math.Max(0, ContentWidth - Size.X)),
                Math.Clamp(value.Y, 0, Math.Max(0, ContentHeight - Size.Y)));
        }
    }

    /// <summary>
    /// Scroll speed multiplier for mouse wheel.
    /// </summary>
    public float ScrollSpeed { get; set; } = 20f;

    /// <summary>
    /// Width of the scrollbar.
    /// </summary>
    public float ScrollbarWidth { get; set; } = 10f;

    /// <summary>
    /// Whether to show horizontal scrollbar.
    /// </summary>
    public bool ShowHorizontalScrollbar { get; set; } = false;

    /// <summary>
    /// Whether to show vertical scrollbar.
    /// </summary>
    public bool ShowVerticalScrollbar { get; set; } = true;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40);

    /// <summary>
    /// Scrollbar color.
    /// </summary>
    public Color ScrollbarColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Scrollbar hover color.
    /// </summary>
    public Color ScrollbarHoverColor { get; set; } = new Color(120, 120, 120);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(80, 80, 80);

    private Vector2 _scrollOffset;
    private readonly List<IUIComponent> _children = new();
    private bool _isDraggingVerticalScrollbar;
    private bool _isDraggingHorizontalScrollbar;
    private float _dragStartOffset;

    public UIScrollView(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        ContentHeight = size.Y; // Default to view size
        ContentWidth = size.X;
    }

    public void Update(float deltaTime)
    {
        // Update child components (with offset applied)
        foreach (var child in _children)
        {
            if (child.Enabled)
            {
                child.Update(deltaTime);
            }
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        float borderThickness = 2f;

        // Draw background
        renderer.DrawRectangle(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Draw border
        renderer.DrawRectangle(Position.X, Position.Y, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangle(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangle(Position.X, Position.Y, borderThickness, Size.Y, BorderColor);
        renderer.DrawRectangle(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, BorderColor);

        // TODO: Implement clipping/scissor test to prevent child rendering outside bounds
        // For now, children will render outside if they exceed bounds

        // Render children with scroll offset
        foreach (var child in _children)
        {
            if (child.Visible)
            {
                // Save original position
                var originalPos = child.Position;

                // Apply scroll offset
                child.Position = new Vector2(
                    Position.X + originalPos.X - _scrollOffset.X,
                    Position.Y + originalPos.Y - _scrollOffset.Y);

                // Only render if within visible area (simple culling)
                if (IsChildVisible(child))
                {
                    child.Render(renderer);
                }

                // Restore original position
                child.Position = originalPos;
            }
        }

        // Draw scrollbars
        if (ShowVerticalScrollbar && ContentHeight > Size.Y)
        {
            DrawVerticalScrollbar(renderer);
        }

        if (ShowHorizontalScrollbar && ContentWidth > Size.X)
        {
            DrawHorizontalScrollbar(renderer);
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Adds a child component to the scroll view.
    /// Child positions are relative to the scroll view's content area.
    /// </summary>
    public void AddChild(IUIComponent child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Removes a child component.
    /// </summary>
    public void RemoveChild(IUIComponent child)
    {
        _children.Remove(child);
    }

    /// <summary>
    /// Gets all child components.
    /// </summary>
    public IReadOnlyList<IUIComponent> GetChildren() => _children.AsReadOnly();

    /// <summary>
    /// Scrolls to make the specified child visible.
    /// </summary>
    public void ScrollToChild(IUIComponent child)
    {
        if (!_children.Contains(child)) return;

        // Scroll vertically if needed
        if (child.Position.Y < _scrollOffset.Y)
        {
            _scrollOffset.Y = child.Position.Y;
        }
        else if (child.Position.Y + child.Size.Y > _scrollOffset.Y + Size.Y)
        {
            _scrollOffset.Y = child.Position.Y + child.Size.Y - Size.Y;
        }

        // Clamp scroll offset
        ScrollOffset = _scrollOffset;
    }

    private bool IsChildVisible(IUIComponent child)
    {
        var childScreenPos = new Vector2(
            Position.X + child.Position.X - _scrollOffset.X,
            Position.Y + child.Position.Y - _scrollOffset.Y);

        // Simple bounds check
        return childScreenPos.X + child.Size.X >= Position.X &&
               childScreenPos.X <= Position.X + Size.X &&
               childScreenPos.Y + child.Size.Y >= Position.Y &&
               childScreenPos.Y <= Position.Y + Size.Y;
    }

    private void DrawVerticalScrollbar(IRenderer renderer)
    {
        float scrollbarHeight = (Size.Y / ContentHeight) * Size.Y;
        float scrollbarY = Position.Y + (_scrollOffset.Y / ContentHeight) * Size.Y;

        var scrollbarX = Position.X + Size.X - ScrollbarWidth;
        renderer.DrawRectangle(scrollbarX, scrollbarY, ScrollbarWidth, scrollbarHeight, ScrollbarColor);
    }

    private void DrawHorizontalScrollbar(IRenderer renderer)
    {
        float scrollbarWidth = (Size.X / ContentWidth) * Size.X;
        float scrollbarX = Position.X + (_scrollOffset.X / ContentWidth) * Size.X;

        var scrollbarY = Position.Y + Size.Y - ScrollbarWidth;
        renderer.DrawRectangle(scrollbarX, scrollbarY, scrollbarWidth, ScrollbarWidth, ScrollbarColor);
    }

    /// <summary>
    /// Called by UICanvas to handle scroll wheel input.
    /// </summary>
    internal void HandleScroll(float scrollDelta)
    {
        if (!Enabled) return;

        ScrollOffset = new Vector2(
            _scrollOffset.X,
            _scrollOffset.Y - scrollDelta * ScrollSpeed);
    }

    /// <summary>
    /// Called by UICanvas when scrollbar is clicked.
    /// </summary>
    internal void StartScrollbarDrag(Vector2 mousePosition, bool isVertical)
    {
        if (!Enabled) return;

        if (isVertical)
        {
            _isDraggingVerticalScrollbar = true;
            _dragStartOffset = mousePosition.Y;
        }
        else
        {
            _isDraggingHorizontalScrollbar = true;
            _dragStartOffset = mousePosition.X;
        }
    }

    /// <summary>
    /// Called by UICanvas during scrollbar drag.
    /// </summary>
    internal void UpdateScrollbarDrag(Vector2 mousePosition)
    {
        if (_isDraggingVerticalScrollbar)
        {
            float delta = mousePosition.Y - _dragStartOffset;
            float scrollRatio = delta / Size.Y;
            ScrollOffset = new Vector2(_scrollOffset.X, _scrollOffset.Y + scrollRatio * ContentHeight);
            _dragStartOffset = mousePosition.Y;
        }

        if (_isDraggingHorizontalScrollbar)
        {
            float delta = mousePosition.X - _dragStartOffset;
            float scrollRatio = delta / Size.X;
            ScrollOffset = new Vector2(_scrollOffset.X + scrollRatio * ContentWidth, _scrollOffset.Y);
            _dragStartOffset = mousePosition.X;
        }
    }

    /// <summary>
    /// Called by UICanvas when scrollbar drag ends.
    /// </summary>
    internal void EndScrollbarDrag()
    {
        _isDraggingVerticalScrollbar = false;
        _isDraggingHorizontalScrollbar = false;
    }
}
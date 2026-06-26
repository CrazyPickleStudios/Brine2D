using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Scrollable container for UI components that exceed visible area.
/// </summary>
public class UIScrollView : IUIComponent, IAnchoredUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public UITooltip? Tooltip { get; set; }
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <inheritdoc />
    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;

    /// <inheritdoc />
    public Vector2 AnchorOffset { get; set; }

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
            var clamped = new Vector2(
                Math.Clamp(value.X, 0, Math.Max(0, ContentWidth - Size.X)),
                Math.Clamp(value.Y, 0, Math.Max(0, ContentHeight - Size.Y)));

            if (clamped != _scrollOffset)
            {
                _scrollOffset = clamped;
                OnScrollChanged?.Invoke(_scrollOffset);
            }
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
    /// Minimum scrollbar thumb size in pixels.
    /// </summary>
    public float MinScrollbarThumbSize { get; set; } = 10f;

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
    /// Scrollbar track (background behind the thumb) color.
    /// </summary>
    public Color ScrollbarTrackColor { get; set; } = new Color(30, 30, 30);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(80, 80, 80);

    private Vector2 _scrollOffset;
    private readonly List<IUIComponent> _children = new();
    private bool _isDraggingVerticalScrollbar;
    private bool _isDraggingHorizontalScrollbar;
    private float _dragStartOffset;
    private bool _isHoveringVerticalScrollbar;
    private bool _isHoveringHorizontalScrollbar;
    private bool _isFocused;

    /// <summary>
    /// Last screen size pushed by <see cref="UICanvas"/>. Used when propagating to
    /// children added after this scroll view is already on a canvas.
    /// </summary>
    internal Vector2 LastKnownScreenSize { get; set; } = new Vector2(1280, 720);

    /// <summary>
    /// Fired whenever <see cref="ScrollOffset"/> changes, with the new offset as the argument.
    /// </summary>
    public event Action<Vector2>? OnScrollChanged;

    /// <summary>
    /// Event fired when this scroll view gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this scroll view loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    /// <summary>
    /// Whether this scroll view currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring drawn around the scroll view when it has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Optional nine-slice background texture. When set, replaces the solid
    /// <see cref="BackgroundColor"/> fill.
    /// </summary>
    public ITexture? BackgroundTexture { get; set; }

    /// <summary>
    /// Nine-slice border insets (texels) for <see cref="BackgroundTexture"/>.
    /// </summary>
    public NineSliceBorder BackgroundTextureBorder { get; set; }

    /// <summary>
    /// Tint color applied to <see cref="BackgroundTexture"/>. Defaults to white.
    /// </summary>
    public Color BackgroundTextureTint { get; set; } = Color.White;

    public UIScrollView(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        ContentHeight = size.Y;
        ContentWidth = size.X;
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

        const float borderThickness = 2f;

        if (BackgroundTexture != null)
            renderer.DrawNineSlice(BackgroundTexture, new Rectangle(Position.X, Position.Y, Size.X, Size.Y), BackgroundTextureBorder, BackgroundTextureTint);
        else
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, BorderColor);

        if (_isFocused)
        {
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, FocusColor);
            renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, FocusColor);
        }

        // Shrink the clip rect so children don't overdraw the scrollbars.
        float clipWidth = ShowVerticalScrollbar && ContentHeight > Size.Y ? Size.X - ScrollbarWidth : Size.X;
        float clipHeight = ShowHorizontalScrollbar && ContentWidth > Size.X ? Size.Y - ScrollbarWidth : Size.Y;
        var clipRect = new Rectangle(Position.X, Position.Y, clipWidth, clipHeight);

        renderer.PushScissorRect(clipRect);

        foreach (var child in _children)
        {
            if (!child.Visible) continue;

            var originalPos = child.Position;

            child.Position = new Vector2(
                Position.X + originalPos.X - _scrollOffset.X,
                Position.Y + originalPos.Y - _scrollOffset.Y);

            // child.Position is now screen-space — check directly against the clip rect.
            if (IsChildVisible(child, Position, new Vector2(clipWidth, clipHeight)))
                child.Render(renderer);

            child.Position = originalPos;
        }

        renderer.PopScissorRect();

        if (ShowVerticalScrollbar && ContentHeight > Size.Y)
            DrawVerticalScrollbar(renderer);

        if (ShowHorizontalScrollbar && ContentWidth > Size.X)
            DrawHorizontalScrollbar(renderer);

        // Redraw border edges on top of the scrollbar tracks so they are never overwritten.
        var finalBorderColor = _isFocused ? FocusColor : BorderColor;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, finalBorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, finalBorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, finalBorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, finalBorderColor);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// When <c>true</c>, <see cref="ContentHeight"/> and <see cref="ContentWidth"/> are
    /// automatically recalculated via <see cref="FitContentToChildren"/> every time a child
    /// is added or removed. This is the recommended setting for dynamic lists.
    /// Defaults to <c>false</c> to preserve the previous manual behaviour.
    /// </summary>
    public bool AutoFitContent { get; set; } = false;

    /// <summary>
    /// Adds a child component to the scroll view.
    /// Child positions are relative to the scroll view's content area.
    /// </summary>
    /// <remarks>
    /// <b>Known limitation:</b> <see cref="UIDropdown"/> widgets placed inside a
    /// <see cref="UIScrollView"/> will have their expanded item list clipped by the scroll
    /// view's scissor rectangle. As a workaround, avoid nesting dropdowns inside scroll
    /// views; use a list-box pattern (e.g. a <see cref="UIScrollView"/> containing
    /// <see cref="UIButton"/> rows) instead.
    /// </remarks>
    public void AddChild(IUIComponent child)
    {
        _children.Add(child);
        UICanvas.PropagateScreenSize(child, LastKnownScreenSize);
        if (AutoFitContent)
            FitContentToChildren();
    }

    /// <summary>
    /// Removes a child component.
    /// </summary>
    public void RemoveChild(IUIComponent child)
    {
        _children.Remove(child);
        if (AutoFitContent)
            FitContentToChildren();
    }

    /// <summary>
    /// Removes all child components.
    /// </summary>
    public void ClearChildren()
    {
        _children.Clear();
        if (AutoFitContent)
            FitContentToChildren();
    }

    /// <summary>
    /// Expands <see cref="ContentHeight"/> and <see cref="ContentWidth"/> to fit all current
    /// children based on their <see cref="IUIComponent.Position"/> and <see cref="IUIComponent.Size"/>.
    /// Call this after finishing layout so scrolling bounds are set correctly.
    /// </summary>
    public void FitContentToChildren()
    {
        float maxX = Size.X;
        float maxY = Size.Y;
        foreach (var child in _children)
        {
            maxX = Math.Max(maxX, child.Position.X + child.Size.X);
            maxY = Math.Max(maxY, child.Position.Y + child.Size.Y);
        }
        ContentWidth = maxX;
        ContentHeight = maxY;
        // Re-clamp the scroll offset through the property setter so it stays within
        // the new content bounds and OnScrollChanged fires if the position changed.
        ScrollOffset = _scrollOffset;
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

        var desired = _scrollOffset;

        if (child.Position.Y < desired.Y)
            desired.Y = child.Position.Y;
        else if (child.Position.Y + child.Size.Y > desired.Y + Size.Y)
            desired.Y = child.Position.Y + child.Size.Y - Size.Y;

        if (child.Position.X < desired.X)
            desired.X = child.Position.X;
        else if (child.Position.X + child.Size.X > desired.X + Size.X)
            desired.X = child.Position.X + child.Size.X - Size.X;

        ScrollOffset = desired;
    }

    /// <summary>
    /// Returns whether the given screen position is over the vertical scrollbar thumb.
    /// </summary>
    internal bool IsOverVerticalScrollbar(Vector2 screenPosition)
    {
        if (!ShowVerticalScrollbar || ContentHeight <= Size.Y) return false;
        var (x, y, w, h) = GetVerticalScrollbarThumbBounds();
        return screenPosition.X >= x && screenPosition.X <= x + w &&
               screenPosition.Y >= y && screenPosition.Y <= y + h;
    }

    /// <summary>
    /// Returns whether the given screen position is over the horizontal scrollbar thumb.
    /// </summary>
    internal bool IsOverHorizontalScrollbar(Vector2 screenPosition)
    {
        if (!ShowHorizontalScrollbar || ContentWidth <= Size.X) return false;
        var (x, y, w, h) = GetHorizontalScrollbarThumbBounds();
        return screenPosition.X >= x && screenPosition.X <= x + w &&
               screenPosition.Y >= y && screenPosition.Y <= y + h;
    }

    /// <summary>
    /// Returns whether the given screen position is over the vertical scrollbar track area
    /// (the full scrollbar column, including the thumb).
    /// </summary>
    internal bool IsOverVerticalScrollbarTrack(Vector2 screenPosition)
    {
        if (!ShowVerticalScrollbar || ContentHeight <= Size.Y) return false;
        bool hasHorizontal = ShowHorizontalScrollbar && ContentWidth > Size.X;
        float trackHeight = hasHorizontal ? Size.Y - ScrollbarWidth : Size.Y;
        float trackX = Position.X + Size.X - ScrollbarWidth;
        return screenPosition.X >= trackX && screenPosition.X <= trackX + ScrollbarWidth &&
               screenPosition.Y >= Position.Y && screenPosition.Y <= Position.Y + trackHeight;
    }

    /// <summary>
    /// Returns whether the given screen position is over the horizontal scrollbar track area
    /// (the full scrollbar row, including the thumb).
    /// </summary>
    internal bool IsOverHorizontalScrollbarTrack(Vector2 screenPosition)
    {
        if (!ShowHorizontalScrollbar || ContentWidth <= Size.X) return false;
        bool hasVertical = ShowVerticalScrollbar && ContentHeight > Size.Y;
        float trackWidth = hasVertical ? Size.X - ScrollbarWidth : Size.X;
        float trackY = Position.Y + Size.Y - ScrollbarWidth;
        return screenPosition.X >= Position.X && screenPosition.X <= Position.X + trackWidth &&
               screenPosition.Y >= trackY && screenPosition.Y <= trackY + ScrollbarWidth;
    }

    /// <summary>
    /// Jumps the vertical scroll position proportionally to where the given screen-space Y
    /// falls within the track (top of track = scroll top, bottom = scroll bottom).
    /// </summary>
    internal void JumpScrollToVertical(float screenY)
    {
        bool hasHorizontal = ShowHorizontalScrollbar && ContentWidth > Size.X;
        float trackHeight = hasHorizontal ? Size.Y - ScrollbarWidth : Size.Y;
        float ratio = trackHeight > 0f ? Math.Clamp((screenY - Position.Y) / trackHeight, 0f, 1f) : 0f;
        ScrollOffset = new Vector2(_scrollOffset.X, ratio * (ContentHeight - Size.Y));
    }

    /// <summary>
    /// Jumps the horizontal scroll position proportionally to where the given screen-space X
    /// falls within the track (left of track = scroll left, right = scroll right).
    /// </summary>
    internal void JumpScrollToHorizontal(float screenX)
    {
        bool hasVertical = ShowVerticalScrollbar && ContentHeight > Size.Y;
        float trackWidth = hasVertical ? Size.X - ScrollbarWidth : Size.X;
        float ratio = trackWidth > 0f ? Math.Clamp((screenX - Position.X) / trackWidth, 0f, 1f) : 0f;
        ScrollOffset = new Vector2(ratio * (ContentWidth - Size.X), _scrollOffset.Y);
    }

    /// <summary>
    /// Checks whether a child (with its Position already translated to screen space) overlaps
    /// the visible clip region. Must be called after the screen-space position has been applied.
    /// </summary>
    private static bool IsChildVisible(IUIComponent child, Vector2 viewPosition, Vector2 viewSize)
    {
        return child.Position.X + child.Size.X >= viewPosition.X &&
               child.Position.X <= viewPosition.X + viewSize.X &&
               child.Position.Y + child.Size.Y >= viewPosition.Y &&
               child.Position.Y <= viewPosition.Y + viewSize.Y;
    }

    private (float x, float y, float w, float h) GetVerticalScrollbarThumbBounds()
    {
        bool hasHorizontal = ShowHorizontalScrollbar && ContentWidth > Size.X;
        float trackHeight = hasHorizontal ? Size.Y - ScrollbarWidth : Size.Y;
        return GetVerticalScrollbarThumbBounds(trackHeight);
    }

    private (float x, float y, float w, float h) GetVerticalScrollbarThumbBounds(float trackHeight)
    {
        float thumbHeight = Math.Max(MinScrollbarThumbSize, (Size.Y / ContentHeight) * trackHeight);
        float maxOffset = trackHeight - thumbHeight;
        float thumbY = Position.Y + (_scrollOffset.Y / Math.Max(1f, ContentHeight - Size.Y)) * maxOffset;
        float thumbX = Position.X + Size.X - ScrollbarWidth;
        return (thumbX, thumbY, ScrollbarWidth, thumbHeight);
    }

    private (float x, float y, float w, float h) GetHorizontalScrollbarThumbBounds()
    {
        bool hasVertical = ShowVerticalScrollbar && ContentHeight > Size.Y;
        float trackWidth = hasVertical ? Size.X - ScrollbarWidth : Size.X;
        return GetHorizontalScrollbarThumbBounds(trackWidth);
    }

    private (float x, float y, float w, float h) GetHorizontalScrollbarThumbBounds(float trackWidth)
    {
        float thumbWidth = Math.Max(MinScrollbarThumbSize, (Size.X / ContentWidth) * trackWidth);
        float maxOffset = trackWidth - thumbWidth;
        float thumbX = Position.X + (_scrollOffset.X / Math.Max(1f, ContentWidth - Size.X)) * maxOffset;
        float thumbY = Position.Y + Size.Y - ScrollbarWidth;
        return (thumbX, thumbY, thumbWidth, ScrollbarWidth);
    }

    private void DrawVerticalScrollbar(IRenderer renderer)
    {
        bool hasHorizontal = ShowHorizontalScrollbar && ContentWidth > Size.X;
        float trackHeight = hasHorizontal ? Size.Y - ScrollbarWidth : Size.Y;

        // Track
        float trackX = Position.X + Size.X - ScrollbarWidth;
        renderer.DrawRectangleFilled(trackX, Position.Y, ScrollbarWidth, trackHeight, ScrollbarTrackColor);

        // Thumb
        var (x, y, w, h) = GetVerticalScrollbarThumbBounds(trackHeight);
        var color = (_isHoveringVerticalScrollbar || _isDraggingVerticalScrollbar) ? ScrollbarHoverColor : ScrollbarColor;
        renderer.DrawRectangleFilled(x, y, w, h, color);

        // Corner filler square when both scrollbars are visible
        if (hasHorizontal)
        {
            renderer.DrawRectangleFilled(
                Position.X + Size.X - ScrollbarWidth,
                Position.Y + Size.Y - ScrollbarWidth,
                ScrollbarWidth,
                ScrollbarWidth,
                ScrollbarTrackColor);
        }
    }

    private void DrawHorizontalScrollbar(IRenderer renderer)
    {
        bool hasVertical = ShowVerticalScrollbar && ContentHeight > Size.Y;
        float trackWidth = hasVertical ? Size.X - ScrollbarWidth : Size.X;

        // Track
        float trackY = Position.Y + Size.Y - ScrollbarWidth;
        renderer.DrawRectangleFilled(Position.X, trackY, trackWidth, ScrollbarWidth, ScrollbarTrackColor);

        // Thumb
        var (x, y, w, h) = GetHorizontalScrollbarThumbBounds(trackWidth);
        var color = (_isHoveringHorizontalScrollbar || _isDraggingHorizontalScrollbar) ? ScrollbarHoverColor : ScrollbarColor;
        renderer.DrawRectangleFilled(x, y, w, h, color);
    }

    /// <summary>
    /// Called by UICanvas to set keyboard focus on this scroll view.
    /// </summary>
    internal void SetFocused(bool focused)
    {
        if (focused == _isFocused) return;
        _isFocused = focused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
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
    /// Scrolls by one page (the visible height) in the given direction.
    /// Positive <paramref name="direction"/> scrolls down; negative scrolls up.
    /// </summary>
    internal void HandlePageScroll(float direction)
    {
        if (!Enabled) return;

        ScrollOffset = new Vector2(
            _scrollOffset.X,
            _scrollOffset.Y + Math.Sign(direction) * Size.Y);
    }

    /// <summary>
    /// Scrolls to the very top of the content.
    /// </summary>
    internal void ScrollToTop()
    {
        if (!Enabled) return;
        ScrollOffset = new Vector2(_scrollOffset.X, 0f);
    }

    /// <summary>
    /// Scrolls to the very bottom of the content.
    /// </summary>
    internal void ScrollToBottom()
    {
        if (!Enabled) return;
        ScrollOffset = new Vector2(_scrollOffset.X, Math.Max(0f, ContentHeight - Size.Y));
    }

    /// <summary>
    /// Called by UICanvas when scrollbar thumb drag begins.
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
    /// Called by UICanvas each frame while scrollbar thumb is being dragged.
    /// </summary>
    internal void UpdateScrollbarDrag(Vector2 mousePosition)
    {
        if (_isDraggingVerticalScrollbar)
        {
            bool hasHorizontal = ShowHorizontalScrollbar && ContentWidth > Size.X;
            float trackHeight = hasHorizontal ? Size.Y - ScrollbarWidth : Size.Y;
            float delta = mousePosition.Y - _dragStartOffset;
            float thumbHeight = Math.Max(MinScrollbarThumbSize, (Size.Y / ContentHeight) * trackHeight);
            float trackRange = trackHeight - thumbHeight;
            float scrollRatio = trackRange > 0f ? delta / trackRange : 0f;
            ScrollOffset = new Vector2(_scrollOffset.X, _scrollOffset.Y + scrollRatio * (ContentHeight - Size.Y));
            _dragStartOffset = mousePosition.Y;
        }

        if (_isDraggingHorizontalScrollbar)
        {
            bool hasVertical = ShowVerticalScrollbar && ContentHeight > Size.Y;
            float trackWidth = hasVertical ? Size.X - ScrollbarWidth : Size.X;
            float delta = mousePosition.X - _dragStartOffset;
            float thumbWidth = Math.Max(MinScrollbarThumbSize, (Size.X / ContentWidth) * trackWidth);
            float trackRange = trackWidth - thumbWidth;
            float scrollRatio = trackRange > 0f ? delta / trackRange : 0f;
            ScrollOffset = new Vector2(_scrollOffset.X + scrollRatio * (ContentWidth - Size.X), _scrollOffset.Y);
            _dragStartOffset = mousePosition.X;
        }
    }

    /// <summary>
    /// Called by UICanvas when scrollbar thumb drag ends.
    /// </summary>
    internal void EndScrollbarDrag()
    {
        _isDraggingVerticalScrollbar = false;
        _isDraggingHorizontalScrollbar = false;
    }

    /// <summary>
    /// Called by UICanvas each frame to update scrollbar hover state.
    /// </summary>
    internal void UpdateScrollbarHover(Vector2 screenPosition)
    {
        _isHoveringVerticalScrollbar = IsOverVerticalScrollbar(screenPosition);
        _isHoveringHorizontalScrollbar = IsOverHorizontalScrollbar(screenPosition);
    }

    internal bool IsHoveringVerticalScrollbarForTest => _isHoveringVerticalScrollbar;
    internal bool IsHoveringHorizontalScrollbarForTest => _isHoveringHorizontalScrollbar;
}
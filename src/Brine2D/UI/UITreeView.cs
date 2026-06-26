using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Hierarchical tree widget with expand/collapse, single-row selection, keyboard
/// navigation, and a virtual scrollbar.
/// </summary>
/// <remarks>
/// Populate via <see cref="AddRoot"/> or <see cref="SetRoots"/>. Add to a
/// <see cref="UICanvas"/> with <see cref="UICanvas.Add"/>; the canvas handles
/// all input routing automatically.
/// </remarks>
public class UITreeView : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;
    public Vector2 AnchorOffset { get; set; }

    // ── Geometry ───────────────────────────────────────────────────────────────

    /// <summary>Pixel height of each tree row. Defaults to 22.</summary>
    public float RowHeight { get; set; } = 22f;

    /// <summary>Indent step per depth level in pixels. Defaults to 16.</summary>
    public float IndentWidth { get; set; } = 16f;

    /// <summary>Width of the vertical scrollbar. Defaults to 10.</summary>
    public float ScrollbarWidth { get; set; } = 10f;

    /// <summary>Minimum scrollbar thumb height in pixels. Defaults to 10.</summary>
    public float MinThumbSize { get; set; } = 10f;

    // ── Colors ─────────────────────────────────────────────────────────────────

    public Color BackgroundColor { get; set; } = new Color(28, 28, 28);
    public Color RowColor { get; set; } = new Color(38, 38, 38);
    public Color AlternateRowColor { get; set; } = new Color(42, 42, 42);
    public Color HoverColor { get; set; } = new Color(65, 80, 110);
    public Color SelectionColor { get; set; } = new Color(55, 95, 175);
    public Color BorderColor { get; set; } = new Color(80, 80, 80);
    public Color FocusBorderColor { get; set; } = new Color(120, 180, 255);
    public Color TextColor { get; set; } = Color.White;
    public Color DisabledTextColor { get; set; } = new Color(130, 130, 130);
    public Color ExpanderColor { get; set; } = new Color(180, 180, 180);
    public Color ScrollbarTrackColor { get; set; } = new Color(50, 50, 50);
    public Color ScrollbarThumbColor { get; set; } = new Color(100, 100, 100);
    public Color ScrollbarThumbHoverColor { get; set; } = new Color(140, 140, 140);

    /// <summary>Optional font. Null uses the renderer default.</summary>
    public IFont? Font { get; set; }

    // ── Data ───────────────────────────────────────────────────────────────────

    private readonly List<UITreeNode> _roots = new();
    private List<(UITreeNode Node, int Depth)> _flat = new();
    private bool _dirty = true;

    // ── State ──────────────────────────────────────────────────────────────────

    private float _scrollOffsetY;
    private bool _isFocused;
    private bool _isThumbHovered;

    private float _thumbDragStartY;
    private float _thumbDragStartOffset;
    private bool _isDraggingThumb;

    public float ScrollOffsetY
    {
        get => _scrollOffsetY;
        set => _scrollOffsetY = Math.Clamp(value, 0f, MaxScrollY);
    }

    /// <summary>Row index (in the flattened visible list) that is currently hovered, or -1.</summary>
    public int HoveredIndex { get; private set; } = -1;

    /// <summary>Row index (in the flattened visible list) that is currently selected, or -1.</summary>
    public int SelectedIndex { get; private set; } = -1;

    /// <summary>The currently selected node, or <c>null</c>.</summary>
    public UITreeNode? SelectedNode => SelectedIndex >= 0 && SelectedIndex < _flat.Count
        ? _flat[SelectedIndex].Node
        : null;

    // ── Events ─────────────────────────────────────────────────────────────────

    /// <summary>Fired when the selected node changes. Argument is the newly selected node (null = deselected).</summary>
    public event Action<UITreeNode?>? OnSelectionChanged;

    /// <summary>Fired when a node is expanded or collapsed. Argument is the toggled node.</summary>
    public event Action<UITreeNode>? OnNodeToggled;

    /// <summary>Fired when keyboard focus is gained.</summary>
    public event Action? OnFocusGained;

    /// <summary>Fired when keyboard focus is lost.</summary>
    public event Action? OnFocusLost;

    // ── Computed ───────────────────────────────────────────────────────────────

    private float TotalContentHeight => _flat.Count * RowHeight;
    private float ListAreaHeight => Size.Y;
    private float ListAreaWidth => NeedsScrollbar ? Size.X - ScrollbarWidth : Size.X;
    private float MaxScrollY => Math.Max(0f, TotalContentHeight - ListAreaHeight);
    private bool NeedsScrollbar => TotalContentHeight > ListAreaHeight;

    // ── Public data API ────────────────────────────────────────────────────────

    /// <summary>Adds a root-level node and marks the flat list as dirty.</summary>
    public void AddRoot(UITreeNode node)
    {
        _roots.Add(node);
        _dirty = true;
    }

    /// <summary>Replaces all roots and resets scroll/selection.</summary>
    public void SetRoots(IEnumerable<UITreeNode> roots)
    {
        _roots.Clear();
        _roots.AddRange(roots);
        _dirty = true;
        _scrollOffsetY = 0f;
        SelectedIndex = -1;
    }

    /// <summary>Removes all root nodes.</summary>
    public void ClearRoots()
    {
        _roots.Clear();
        _dirty = true;
        _scrollOffsetY = 0f;
        SelectedIndex = -1;
    }

    /// <summary>Read-only access to the root nodes.</summary>
    public IReadOnlyList<UITreeNode> Roots => _roots;

    // ── IUIComponent ───────────────────────────────────────────────────────────

    public void Update(float deltaTime) { }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        RebuildFlatIfDirty();

        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        renderer.PushScissorRect(new Rectangle
        {
            X = Position.X, Y = Position.Y,
            Width = ListAreaWidth, Height = Size.Y
        });

        int firstVisible = (int)(_scrollOffsetY / RowHeight);
        int lastVisible = Math.Min(_flat.Count - 1, (int)((_scrollOffsetY + ListAreaHeight) / RowHeight));

        for (int i = firstVisible; i <= lastVisible; i++)
        {
            var (node, depth) = _flat[i];
            float rowY = Position.Y + i * RowHeight - _scrollOffsetY;

            var bg = i == SelectedIndex ? SelectionColor
                : i == HoveredIndex ? HoverColor
                : i % 2 == 0 ? RowColor : AlternateRowColor;

            renderer.DrawRectangleFilled(Position.X, rowY, ListAreaWidth, RowHeight, bg);
            RenderRow(renderer, node, depth, rowY, i == SelectedIndex, i == HoveredIndex);
        }

        renderer.PopScissorRect();

        var borderCol = _isFocused ? FocusBorderColor : BorderColor;
        renderer.DrawRectangleOutline(Position.X, Position.Y, Size.X, Size.Y, borderCol);

        if (NeedsScrollbar)
            RenderScrollbar(renderer);
    }

    private void RenderRow(IRenderer renderer, UITreeNode node, int depth, float rowY, bool selected, bool hovered)
    {
        float indent = Position.X + depth * IndentWidth + 4f;
        float expanderSize = RowHeight * 0.4f;
        float cx = indent + expanderSize * 0.5f;
        float cy = rowY + RowHeight * 0.5f;

        if (node.HasChildren)
        {
            // Draw a simple > or v arrow using two lines
            if (node.IsExpanded)
            {
                // Down-pointing arrow: /\
                renderer.DrawLine(cx - expanderSize * 0.5f, cy - expanderSize * 0.3f,
                    cx, cy + expanderSize * 0.3f, ExpanderColor);
                renderer.DrawLine(cx, cy + expanderSize * 0.3f,
                    cx + expanderSize * 0.5f, cy - expanderSize * 0.3f, ExpanderColor);
            }
            else
            {
                // Right-pointing arrow: >
                renderer.DrawLine(cx - expanderSize * 0.3f, cy - expanderSize * 0.5f,
                    cx + expanderSize * 0.3f, cy, ExpanderColor);
                renderer.DrawLine(cx + expanderSize * 0.3f, cy,
                    cx - expanderSize * 0.3f, cy + expanderSize * 0.5f, ExpanderColor);
            }
        }

        float textX = indent + IndentWidth;
        float textY = rowY + (RowHeight - 14f) / 2f;
        var textColor = Enabled ? TextColor : DisabledTextColor;
        renderer.DrawText(node.Text, textX, textY, new TextRenderOptions
        {
            Color = textColor,
            Font = Font,
            MaxWidth = ListAreaWidth - (textX - Position.X) - 4f
        });
    }

    private void RenderScrollbar(IRenderer renderer)
    {
        float trackX = Position.X + Size.X - ScrollbarWidth;
        renderer.DrawRectangleFilled(trackX, Position.Y, ScrollbarWidth, Size.Y, ScrollbarTrackColor);

        float thumbH = Math.Max(MinThumbSize, (ListAreaHeight / TotalContentHeight) * Size.Y);
        float thumbRange = Size.Y - thumbH;
        float thumbY = thumbRange > 0f && MaxScrollY > 0f
            ? Position.Y + (_scrollOffsetY / MaxScrollY) * thumbRange
            : Position.Y;

        var thumbColor = _isThumbHovered ? ScrollbarThumbHoverColor : ScrollbarThumbColor;
        renderer.DrawRectangleFilled(trackX, thumbY, ScrollbarWidth, thumbH, thumbColor);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X && screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y && screenPosition.Y <= Position.Y + Size.Y;
    }

    // ── Flat-list rebuild ──────────────────────────────────────────────────────

    private void RebuildFlatIfDirty()
    {
        if (!_dirty) return;
        _flat.Clear();
        foreach (var root in _roots)
            AppendVisible(root, 0);
        _dirty = false;

        // Clamp selection index after a structural change.
        if (SelectedIndex >= _flat.Count)
        {
            SelectedIndex = -1;
            OnSelectionChanged?.Invoke(null);
        }
    }

    private void AppendVisible(UITreeNode node, int depth)
    {
        _flat.Add((node, depth));
        if (node.IsExpanded)
            foreach (var child in node.Children)
                AppendVisible(child, depth + 1);
    }

    // ── Canvas internal surface ────────────────────────────────────────────────

    internal bool IsFocused => _isFocused;
    internal bool IsDraggingThumb => _isDraggingThumb;

    internal void SetFocused(bool focused)
    {
        if (focused == _isFocused) return;
        _isFocused = focused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }

    internal void UpdateHover(Vector2 screenPos)
    {
        if (!Enabled || !Visible) { HoveredIndex = -1; _isThumbHovered = false; return; }
        RebuildFlatIfDirty();
        _isThumbHovered = NeedsScrollbar && IsOverScrollbarThumb(screenPos);

        if (screenPos.X >= Position.X && screenPos.X <= Position.X + ListAreaWidth &&
            screenPos.Y >= Position.Y && screenPos.Y <= Position.Y + Size.Y)
        {
            float relY = screenPos.Y - Position.Y + _scrollOffsetY;
            int idx = (int)(relY / RowHeight);
            HoveredIndex = idx >= 0 && idx < _flat.Count ? idx : -1;
        }
        else
        {
            HoveredIndex = -1;
        }
    }

    internal void ClearHover()
    {
        HoveredIndex = -1;
        _isThumbHovered = false;
    }

    internal void HandleClick(Vector2 screenPos)
    {
        if (!Enabled) return;
        RebuildFlatIfDirty();

        // Ignore clicks on the scrollbar track.
        if (NeedsScrollbar && screenPos.X >= Position.X + ListAreaWidth) return;

        UpdateHover(screenPos);
        if (HoveredIndex < 0 || HoveredIndex >= _flat.Count) return;

        var (node, depth) = _flat[HoveredIndex];

        // Check if the click landed on the expander icon.
        float indent = Position.X + depth * IndentWidth + 4f;
        float expanderRight = indent + IndentWidth;
        if (node.HasChildren && screenPos.X <= expanderRight)
        {
            ToggleNode(HoveredIndex);
            return;
        }

        SelectRow(HoveredIndex);
    }

    internal void HandleScroll(float delta) => ScrollOffsetY += delta * RowHeight;

    internal void HandlePageScroll(float direction) => ScrollOffsetY += direction * ListAreaHeight;

    internal void NavigateUp()
    {
        if (!Enabled) return;
        RebuildFlatIfDirty();
        if (_flat.Count == 0) return;

        if (SelectedIndex > 0)
        {
            SelectRow(SelectedIndex - 1);
            ScrollIntoView(SelectedIndex);
        }
        else if (SelectedIndex == -1)
        {
            SelectRow(_flat.Count - 1);
            ScrollToBottom();
        }
    }

    internal void NavigateDown()
    {
        if (!Enabled) return;
        RebuildFlatIfDirty();
        if (_flat.Count == 0) return;

        if (SelectedIndex == -1)
        {
            SelectRow(0);
            ScrollToTop();
        }
        else if (SelectedIndex < _flat.Count - 1)
        {
            SelectRow(SelectedIndex + 1);
            ScrollIntoView(SelectedIndex);
        }
    }

    internal void ExpandSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _flat.Count) return;
        var node = _flat[SelectedIndex].Node;
        if (node.HasChildren && !node.IsExpanded)
            ToggleNode(SelectedIndex);
    }

    internal void CollapseSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= _flat.Count) return;
        var node = _flat[SelectedIndex].Node;
        if (node.IsExpanded)
            ToggleNode(SelectedIndex);
    }

    private void ToggleNode(int flatIndex)
    {
        if (flatIndex < 0 || flatIndex >= _flat.Count) return;
        var node = _flat[flatIndex].Node;
        if (!node.HasChildren) return;

        // Store selected node to re-find it after rebuild.
        var selectedNode = SelectedNode;

        node.IsExpanded = !node.IsExpanded;
        _dirty = true;
        OnNodeToggled?.Invoke(node);

        RebuildFlatIfDirty();

        // Re-sync selection index.
        if (selectedNode != null)
        {
            int newIdx = _flat.FindIndex(e => ReferenceEquals(e.Node, selectedNode));
            SelectedIndex = newIdx; // -1 if the node collapsed out of view
        }
    }

    private void SelectRow(int index)
    {
        if (index < 0 || index >= _flat.Count) return;
        if (index == SelectedIndex) return;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke(_flat[index].Node);
    }

    private void ScrollIntoView(int index)
    {
        if (index < 0 || index >= _flat.Count) return;
        float rowTop = index * RowHeight;
        float rowBottom = rowTop + RowHeight;

        if (rowTop < _scrollOffsetY)
            ScrollOffsetY = rowTop;
        else if (rowBottom > _scrollOffsetY + ListAreaHeight)
            ScrollOffsetY = rowBottom - ListAreaHeight;
    }

    private void ScrollToTop() => ScrollOffsetY = 0f;
    private void ScrollToBottom() => ScrollOffsetY = MaxScrollY;

    internal bool IsOverScrollbarTrack(Vector2 pos) =>
        NeedsScrollbar &&
        pos.X >= Position.X + Size.X - ScrollbarWidth &&
        pos.X <= Position.X + Size.X &&
        pos.Y >= Position.Y && pos.Y <= Position.Y + Size.Y;

    internal bool IsOverScrollbarThumb(Vector2 pos)
    {
        if (!NeedsScrollbar) return false;
        RebuildFlatIfDirty();
        float trackX = Position.X + Size.X - ScrollbarWidth;
        float thumbH = Math.Max(MinThumbSize, (ListAreaHeight / TotalContentHeight) * Size.Y);
        float thumbRange = Size.Y - thumbH;
        float thumbY = thumbRange > 0f && MaxScrollY > 0f
            ? Position.Y + (_scrollOffsetY / MaxScrollY) * thumbRange
            : Position.Y;
        return pos.X >= trackX && pos.X <= trackX + ScrollbarWidth &&
               pos.Y >= thumbY && pos.Y <= thumbY + thumbH;
    }

    internal void StartThumbDrag(float mouseY)
    {
        _isDraggingThumb = true;
        _thumbDragStartY = mouseY;
        _thumbDragStartOffset = _scrollOffsetY;
    }

    internal void UpdateThumbDrag(float mouseY)
    {
        if (!_isDraggingThumb) return;
        RebuildFlatIfDirty();
        float thumbH = Math.Max(MinThumbSize, (ListAreaHeight / TotalContentHeight) * Size.Y);
        float trackRange = Size.Y - thumbH;
        float delta = mouseY - _thumbDragStartY;
        float scrollRatio = trackRange > 0f ? delta / trackRange : 0f;
        ScrollOffsetY = _thumbDragStartOffset + scrollRatio * MaxScrollY;
    }

    internal void EndThumbDrag() => _isDraggingThumb = false;

    internal void JumpScrollTo(float mouseY)
    {
        float relY = mouseY - Position.Y;
        float ratio = Math.Clamp(relY / Size.Y, 0f, 1f);
        ScrollOffsetY = ratio * MaxScrollY;
    }

    /// <summary>
    /// Forces the flat visible list to be rebuilt on the next render or hit-test call.
    /// Call this after externally modifying node children or expanded state.
    /// </summary>
    public void Invalidate() => _dirty = true;
}

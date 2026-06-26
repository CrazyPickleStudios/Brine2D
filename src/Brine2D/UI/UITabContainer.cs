using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;
using System.Numerics;

namespace Brine2D.UI;

/// <summary>
/// Tab container UI component for organizing content into tabs.
/// </summary>
public class UITabContainer : IUIComponent, IAnchoredUIComponent
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
    /// Height of the tab bar.
    /// </summary>
    public float TabHeight { get; set; } = 30f;

    /// <summary>
    /// Background color for tabs.
    /// </summary>
    public Color TabBackgroundColor { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Active tab color.
    /// </summary>
    public Color ActiveTabColor { get; set; } = new Color(80, 80, 80);

    /// <summary>
    /// Hover tab color.
    /// </summary>
    public Color HoverTabColor { get; set; } = new Color(70, 70, 70);

    /// <summary>
    /// Content area background color.
    /// </summary>
    public Color ContentBackgroundColor { get; set; } = new Color(50, 50, 50);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Minimum tab width in pixels. When tabs would be narrower the bar becomes scrollable,
    /// showing arrow buttons of width <see cref="TabArrowWidth"/>.
    /// Set to 0 to let tabs shrink freely.
    /// </summary>
    public float MinTabWidth { get; set; } = 80f;

    /// <summary>
    /// Width of each scroll arrow button at the ends of the tab bar.
    /// Only visible when the tab bar is scrollable.
    /// </summary>
    public float TabArrowWidth { get; set; } = 20f;

    /// <summary>
    /// Currently selected tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (value >= 0 && value < _tabs.Count && _selectedTabIndex != value)
            {
                _selectedTabIndex = value;
                OnTabChanged?.Invoke(_selectedTabIndex, _tabs[_selectedTabIndex].Title);
            }
        }
    }

    /// <summary>
    /// Event fired when the selected tab changes.
    /// </summary>
    public event Action<int, string>? OnTabChanged;

    /// <summary>
    /// Event fired when this tab container gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this tab container loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    public UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Screen size used to resolve <see cref="UIAnchor"/> positions for anchored children.
    /// Kept in sync by <see cref="UICanvas"/>. Defaults to 1280×720.
    /// </summary>
    public Vector2 ScreenSize { get; set; } = new Vector2(1280, 720);
    /// <summary>
    /// Whether this tab container currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring drawn around the tab bar when the container has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Optional nine-slice texture for the content area background.
    /// When set, replaces the solid <see cref="ContentBackgroundColor"/> fill.
    /// </summary>
    public ITexture? ContentBackgroundTexture { get; set; }

    /// <summary>
    /// Nine-slice border insets (texels) for <see cref="ContentBackgroundTexture"/>.
    /// </summary>
    public NineSliceBorder ContentBackgroundTextureBorder { get; set; }

    /// <summary>
    /// Tint color applied to <see cref="ContentBackgroundTexture"/>. Defaults to white.
    /// </summary>
    public Color ContentBackgroundTextureTint { get; set; } = Color.White;

    /// <summary>
    /// Optional nine-slice texture used for each tab button background.
    /// When set, replaces the solid per-state tab color fills.
    /// The <see cref="ActiveTabColor"/>, <see cref="HoverTabColor"/>, and
    /// <see cref="TabBackgroundColor"/> tints are still applied as a tint on top.
    /// </summary>
    public ITexture? TabTexture { get; set; }

    /// <summary>
    /// Nine-slice border insets (texels) for <see cref="TabTexture"/>.
    /// </summary>
    public NineSliceBorder TabTextureBorder { get; set; }

    private readonly List<TabData> _tabs = new();
    private int _selectedTabIndex = 0;
    private int _hoveredTabIndex = -1;
    private int _tabScrollOffset = 0;
    private bool _isFocused;

    private class TabData
    {
        public string Title { get; set; } = string.Empty;
        public List<IUIComponent> Components { get; } = new();
    }

    public UITabContainer(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    private bool IsScrollable =>
        MinTabWidth > 0 && _tabs.Count > 0 && (Size.X / _tabs.Count) < MinTabWidth;

    private float EffectiveTabBarWidth =>
        IsScrollable ? Size.X - TabArrowWidth * 2f : Size.X;

    private int VisibleTabCount =>
        IsScrollable ? (int)(EffectiveTabBarWidth / MinTabWidth) : _tabs.Count;

    private float ActualTabWidth =>
        IsScrollable
            ? Math.Min(EffectiveTabBarWidth / Math.Max(1, VisibleTabCount), EffectiveTabBarWidth)
            : (_tabs.Count > 0 ? Size.X / _tabs.Count : Size.X);

    internal int TabScrollOffsetForTest => _tabScrollOffset;
    internal bool IsScrollableForTest => IsScrollable;

    public void Update(float deltaTime)
    {
        if (_selectedTabIndex >= 0 && _selectedTabIndex < _tabs.Count)
        {
            foreach (var component in _tabs[_selectedTabIndex].Components)
            {
                if (component.Enabled)
                    component.Update(deltaTime);
            }
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        const float borderThickness = 2f;

        float contentY = Position.Y + TabHeight;
        float contentHeight = Size.Y - TabHeight;
        if (ContentBackgroundTexture != null)
            renderer.DrawNineSlice(ContentBackgroundTexture, new Rectangle(Position.X, contentY, Size.X, contentHeight), ContentBackgroundTextureBorder, ContentBackgroundTextureTint);
        else
            renderer.DrawRectangleFilled(Position.X, contentY, Size.X, contentHeight, ContentBackgroundColor);

        renderer.DrawRectangleFilled(Position.X, contentY, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, contentY, borderThickness, contentHeight, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, contentY, borderThickness, contentHeight, BorderColor);

        bool scrollable = IsScrollable;
        float tabBarOffsetX = scrollable ? TabArrowWidth : 0f;
        float tabBarWidth = scrollable ? EffectiveTabBarWidth : Size.X;
        float tabWidth = ActualTabWidth;
        int visibleCount = scrollable ? VisibleTabCount : _tabs.Count;
        int maxScrollOffset = scrollable ? Math.Max(0, _tabs.Count - visibleCount) : 0;

        if (scrollable)
        {
            renderer.DrawRectangleFilled(Position.X, Position.Y, TabArrowWidth, TabHeight, TabBackgroundColor);
            renderer.DrawRectangleFilled(Position.X + Size.X - TabArrowWidth, Position.Y, TabArrowWidth, TabHeight, TabBackgroundColor);

            var leftArrow = "<";
            var rightArrow = ">";
            var arrowSize = renderer.MeasureText(leftArrow);
            renderer.DrawText(leftArrow,
                Position.X + (TabArrowWidth - arrowSize.X) / 2f,
                Position.Y + (TabHeight - arrowSize.Y) / 2f,
                _tabScrollOffset > 0 ? TextColor : new Color(100, 100, 100));
            renderer.DrawText(rightArrow,
                Position.X + Size.X - TabArrowWidth + (TabArrowWidth - arrowSize.X) / 2f,
                Position.Y + (TabHeight - arrowSize.Y) / 2f,
                _tabScrollOffset < maxScrollOffset ? TextColor : new Color(100, 100, 100));

            var tabClip = new Rectangle(Position.X + TabArrowWidth, Position.Y, tabBarWidth, TabHeight);
            renderer.PushScissorRect(tabClip);
        }

        for (int i = 0; i < _tabs.Count; i++)
        {
            int visibleIndex = i - _tabScrollOffset;
            if (scrollable && (visibleIndex < 0 || visibleIndex >= visibleCount)) continue;

            float tabX = Position.X + tabBarOffsetX + (visibleIndex * tabWidth);

            Color tabColor = TabBackgroundColor;
            if (i == _selectedTabIndex)
                tabColor = ActiveTabColor;
            else if (i == _hoveredTabIndex)
                tabColor = HoverTabColor;

            if (TabTexture != null)
            {
                var tabTint = i == _selectedTabIndex ? ActiveTabColor :
                              i == _hoveredTabIndex  ? HoverTabColor :
                              TabBackgroundColor;
                renderer.DrawNineSlice(TabTexture, new Rectangle(tabX, Position.Y, tabWidth, TabHeight), TabTextureBorder, tabTint);
            }
            else
            {
                renderer.DrawRectangleFilled(tabX, Position.Y, tabWidth, TabHeight, tabColor);
            }

            renderer.DrawRectangleFilled(tabX, Position.Y, tabWidth, borderThickness, BorderColor);
            renderer.DrawRectangleFilled(tabX, Position.Y, borderThickness, TabHeight, BorderColor);
            renderer.DrawRectangleFilled(tabX + tabWidth - borderThickness, Position.Y, borderThickness, TabHeight, BorderColor);

            if (i != _selectedTabIndex)
                renderer.DrawRectangleFilled(tabX, Position.Y + TabHeight - borderThickness, tabWidth, borderThickness, BorderColor);

            var title = _tabs[i].Title;
            var titleOpts = new TextRenderOptions { Color = TextColor, LineSpacing = 1.0f };
            var titleSize = renderer.MeasureText(title, titleOpts);
            var textX = MathF.Round(tabX + (tabWidth - titleSize.X) / 2f);
            var textY = MathF.Round(Position.Y + (TabHeight - titleSize.Y) / 2f);
            renderer.DrawText(title, textX, textY, titleOpts);
        }

        if (scrollable)
            renderer.PopScissorRect();

        if (_isFocused)
        {
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y + TabHeight - borderThickness, Size.X, borderThickness, FocusColor);
            renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, TabHeight, FocusColor);
            renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, TabHeight, FocusColor);
        }

        if (_selectedTabIndex >= 0 && _selectedTabIndex < _tabs.Count)
        {
            var contentClip = new Rectangle(Position.X, contentY, Size.X, contentHeight);
            renderer.PushScissorRect(contentClip);

            var contentOrigin = new Vector2(Position.X, contentY);

            foreach (var component in _tabs[_selectedTabIndex].Components)
            {
                if (!component.Visible) continue;

                if (component is IAnchoredUIComponent anchoredComponent &&
                    (anchoredComponent.Anchor != UIAnchor.TopLeft || anchoredComponent.AnchorOffset != Vector2.Zero))
                {
                    var saved = anchoredComponent.Position;
                    var contentSize = new Vector2(Size.X, Size.Y - TabHeight);
                    var anchorOrigin = UIAnchorResolver.Resolve(anchoredComponent.Anchor, contentSize.X, contentSize.Y);
                    anchoredComponent.Position = contentOrigin + anchorOrigin + anchoredComponent.AnchorOffset;
                    component.Render(renderer);
                    anchoredComponent.Position = saved;
                }
                else
                {
                    var saved = component.Position;
                    component.Position = contentOrigin + saved;
                    component.Render(renderer);
                    component.Position = saved;
                }
            }

            renderer.PopScissorRect();
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
    /// Adds a new tab with the given title.
    /// </summary>
    public void AddTab(string title)
    {
        _tabs.Add(new TabData { Title = title });

        if (_tabs.Count == 1)
            _selectedTabIndex = 0;
    }

    /// <summary>
    /// Removes the tab at <paramref name="tabIndex"/>. If the removed tab was selected,
    /// the selection moves to index 0 (or -1 if no tabs remain). If a tab before the
    /// selected one is removed, the selected index is decremented so the same tab stays
    /// selected. <see cref="OnTabChanged"/> fires only when the selected tab actually changes.
    /// </summary>
    public void RemoveTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= _tabs.Count)
            return;

        _tabs.RemoveAt(tabIndex);
        _tabScrollOffset = Math.Clamp(_tabScrollOffset, 0, Math.Max(0, _tabs.Count - 1));

        if (_tabs.Count == 0)
        {
            _selectedTabIndex = -1;
            return;
        }

        if (tabIndex < _selectedTabIndex)
        {
            _selectedTabIndex--;
        }
        else if (tabIndex == _selectedTabIndex)
        {
            _selectedTabIndex = Math.Max(0, tabIndex - 1);
            OnTabChanged?.Invoke(_selectedTabIndex, _tabs[_selectedTabIndex].Title);
        }
    }

    /// <summary>
    /// Adds a component to the specified tab by index.
    /// </summary>
    /// <remarks>
    /// Component positions are <b>content-origin-relative</b>: <c>(0, 0)</c> places the
    /// component at the top-left corner of the tab's content area (directly below the tab bar).
    /// Use <see cref="GetContentOrigin"/> to convert a content-relative position to an
    /// absolute screen position when needed elsewhere.
    /// </remarks>
    public void AddComponentToTab(int tabIndex, IUIComponent component)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
        {
            _tabs[tabIndex].Components.Add(component);
            UICanvas.PropagateScreenSize(component, ScreenSize);
        }
    }

    /// <summary>
    /// Adds a component to the specified tab by title.
    /// </summary>
    /// <remarks>
    /// Component positions are content-origin-relative. See <see cref="AddComponentToTab(int,IUIComponent)"/>.
    /// </remarks>
    public void AddComponentToTab(string tabTitle, IUIComponent component)
    {
        var tab = _tabs.FirstOrDefault(t => t.Title == tabTitle);
        if (tab != null)
        {
            tab.Components.Add(component);
            UICanvas.PropagateScreenSize(component, ScreenSize);
        }
    }

    /// <summary>
    /// Removes a component from the specified tab.
    /// </summary>
    public void RemoveComponentFromTab(int tabIndex, IUIComponent component)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
            _tabs[tabIndex].Components.Remove(component);
    }

    /// <summary>
    /// Gets all components in the specified tab.
    /// </summary>
    public IReadOnlyList<IUIComponent> GetTabComponents(int tabIndex)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
            return _tabs[tabIndex].Components.AsReadOnly();
        return Array.Empty<IUIComponent>();
    }

    /// <summary>
    /// Called by UICanvas when a tab is clicked.
    /// </summary>
    internal void SelectTab(Vector2 mousePosition)
    {
        if (!Enabled || _tabs.Count == 0) return;

        if (mousePosition.Y < Position.Y || mousePosition.Y > Position.Y + TabHeight)
            return;

        if (IsScrollable)
        {
            // Left arrow
            if (mousePosition.X >= Position.X && mousePosition.X < Position.X + TabArrowWidth)
            {
                _tabScrollOffset = Math.Max(0, _tabScrollOffset - 1);
                return;
            }

            // Right arrow
            int maxScrollOffset = Math.Max(0, _tabs.Count - VisibleTabCount);
            if (mousePosition.X >= Position.X + Size.X - TabArrowWidth && mousePosition.X <= Position.X + Size.X)
            {
                _tabScrollOffset = Math.Min(maxScrollOffset, _tabScrollOffset + 1);
                return;
            }

            float tabBarStart = Position.X + TabArrowWidth;
            float tabWidth = ActualTabWidth;
            int visibleIndex = (int)((mousePosition.X - tabBarStart) / tabWidth);
            int tabIndex = visibleIndex + _tabScrollOffset;
            if (tabIndex >= 0 && tabIndex < _tabs.Count)
                SelectedTabIndex = tabIndex;
        }
        else
        {
            float tabWidth = _tabs.Count > 0 ? Size.X / _tabs.Count : Size.X;
            int tabIndex = (int)((mousePosition.X - Position.X) / tabWidth);
            if (tabIndex >= 0 && tabIndex < _tabs.Count)
                SelectedTabIndex = tabIndex;
        }
    }

    /// <summary>
    /// Called by UICanvas to update hover state.
    /// </summary>
    internal void UpdateHover(Vector2 mousePosition)
    {
        if (_tabs.Count == 0)
        {
            _hoveredTabIndex = -1;
            return;
        }

        if (mousePosition.Y >= Position.Y && mousePosition.Y <= Position.Y + TabHeight)
        {
            if (IsScrollable)
            {
                float tabBarStart = Position.X + TabArrowWidth;
                float tabBarEnd = Position.X + Size.X - TabArrowWidth;
                if (mousePosition.X >= tabBarStart && mousePosition.X < tabBarEnd)
                {
                    float tabWidth = ActualTabWidth;
                    int visibleIndex = (int)((mousePosition.X - tabBarStart) / tabWidth);
                    int tabIndex = visibleIndex + _tabScrollOffset;
                    if (tabIndex >= 0 && tabIndex < _tabs.Count)
                    {
                        _hoveredTabIndex = tabIndex;
                        return;
                    }
                }
            }
            else
            {
                float tabWidth = _tabs.Count > 0 ? Size.X / _tabs.Count : Size.X;
                int tabIndex = (int)((mousePosition.X - Position.X) / tabWidth);
                if (tabIndex >= 0 && tabIndex < _tabs.Count)
                {
                    _hoveredTabIndex = tabIndex;
                    return;
                }
            }
        }

        _hoveredTabIndex = -1;
    }

    /// <summary>
    /// Gets the number of tabs.
    /// </summary>
    public int TabCount => _tabs.Count;

    /// <summary>
    /// Gets the title of the specified tab.
    /// </summary>
    public string? GetTabTitle(int tabIndex)
    {
        return tabIndex >= 0 && tabIndex < _tabs.Count ? _tabs[tabIndex].Title : null;
    }

    /// <summary>
    /// Renames the tab at <paramref name="tabIndex"/>. No-op when the index is out of range.
    /// </summary>
    public void RenameTab(int tabIndex, string newTitle)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
            _tabs[tabIndex].Title = newTitle;
    }

    /// <summary>
    /// Returns the absolute screen-space position of the top-left corner of the content area
    /// (below the tab bar). Useful when you need to convert a content-relative child position
    /// to an absolute screen position (e.g. for debug overlays or external hit-tests).
    /// Child components added via <see cref="AddComponentToTab(int,IUIComponent)"/> use
    /// content-origin-relative coordinates, so <c>(0,0)</c> is already the content origin.
    /// </summary>
    public Vector2 GetContentOrigin() => new Vector2(Position.X, Position.Y + TabHeight);

    /// <summary>
    /// Selects the next tab, wrapping around to the first when the last is selected.
    /// No-op when there are fewer than two tabs.
    /// </summary>
    public void SelectNextTab()
    {
        if (_tabs.Count < 2) return;
        SelectedTabIndex = (_selectedTabIndex + 1) % _tabs.Count;
    }

    /// <summary>
    /// Selects the previous tab, wrapping around to the last when the first is selected.
    /// No-op when there are fewer than two tabs.
    /// </summary>
    public void SelectPreviousTab()
    {
        if (_tabs.Count < 2) return;
        SelectedTabIndex = (_selectedTabIndex - 1 + _tabs.Count) % _tabs.Count;
    }

    /// <summary>
    /// Called by <see cref="UICanvas"/> to set keyboard focus on this tab container.
    /// </summary>
    internal void SetFocused(bool focused)
    {
        if (focused == _isFocused) return;
        _isFocused = focused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }
}
using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Tab container UI component for organizing content into tabs.
/// </summary>
public class UITabContainer : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

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

    public UITooltip? Tooltip { get; set; }

    private readonly List<TabData> _tabs = new();
    private int _selectedTabIndex = 0;
    private int _hoveredTabIndex = -1;

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

    public void Update(float deltaTime)
    {
        // Update components in the active tab
        if (_selectedTabIndex >= 0 && _selectedTabIndex < _tabs.Count)
        {
            foreach (var component in _tabs[_selectedTabIndex].Components)
            {
                if (component.Enabled)
                {
                    component.Update(deltaTime);
                }
            }
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        float borderThickness = 2f;

        // Draw content area background
        float contentY = Position.Y + TabHeight;
        float contentHeight = Size.Y - TabHeight;
        renderer.DrawRectangleFilled(Position.X, contentY, Size.X, contentHeight, ContentBackgroundColor);

        // Draw content area border
        renderer.DrawRectangleFilled(Position.X, contentY, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, contentY, borderThickness, contentHeight, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, contentY, borderThickness, contentHeight, BorderColor);

        // Calculate tab width
        float tabWidth = _tabs.Count > 0 ? Size.X / _tabs.Count : Size.X;

        // Draw tabs
        for (int i = 0; i < _tabs.Count; i++)
        {
            float tabX = Position.X + (i * tabWidth);

            // Determine tab color
            Color tabColor = TabBackgroundColor;
            if (i == _selectedTabIndex)
            {
                tabColor = ActiveTabColor;
            }
            else if (i == _hoveredTabIndex)
            {
                tabColor = HoverTabColor;
            }

            // Draw tab background
            renderer.DrawRectangleFilled(tabX, Position.Y, tabWidth, TabHeight, tabColor);

            // Draw tab border
            renderer.DrawRectangleFilled(tabX, Position.Y, tabWidth, borderThickness, BorderColor);
            renderer.DrawRectangleFilled(tabX, Position.Y, borderThickness, TabHeight, BorderColor);
            renderer.DrawRectangleFilled(tabX + tabWidth - borderThickness, Position.Y, borderThickness, TabHeight, BorderColor);

            // Don't draw bottom border for active tab (connects to content)
            if (i != _selectedTabIndex)
            {
                renderer.DrawRectangleFilled(tabX, Position.Y + TabHeight - borderThickness, tabWidth, borderThickness, BorderColor);
            }

            // Draw tab title (centered)
            var title = _tabs[i].Title;
            var textX = tabX + (tabWidth / 2) - (title.Length * 4);
            var textY = Position.Y + (TabHeight / 2) - 8;
            renderer.DrawText(title, textX, textY, TextColor);
        }

        // Render active tab content
        if (_selectedTabIndex >= 0 && _selectedTabIndex < _tabs.Count)
        {
            foreach (var component in _tabs[_selectedTabIndex].Components)
            {
                if (component.Visible)
                {
                    component.Render(renderer);
                }
            }
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

        // Select first tab by default
        if (_tabs.Count == 1)
        {
            _selectedTabIndex = 0;
        }
    }

    /// <summary>
    /// Adds a component to the specified tab.
    /// </summary>
    public void AddComponentToTab(int tabIndex, IUIComponent component)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
        {
            _tabs[tabIndex].Components.Add(component);
        }
    }

    /// <summary>
    /// Adds a component to the specified tab by title.
    /// </summary>
    public void AddComponentToTab(string tabTitle, IUIComponent component)
    {
        var tab = _tabs.FirstOrDefault(t => t.Title == tabTitle);
        if (tab != null)
        {
            tab.Components.Add(component);
        }
    }

    /// <summary>
    /// Removes a component from the specified tab.
    /// </summary>
    public void RemoveComponentFromTab(int tabIndex, IUIComponent component)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
        {
            _tabs[tabIndex].Components.Remove(component);
        }
    }

    /// <summary>
    /// Gets all components in the specified tab.
    /// </summary>
    public IReadOnlyList<IUIComponent> GetTabComponents(int tabIndex)
    {
        if (tabIndex >= 0 && tabIndex < _tabs.Count)
        {
            return _tabs[tabIndex].Components.AsReadOnly();
        }
        return Array.Empty<IUIComponent>();
    }

    /// <summary>
    /// Called by UICanvas when a tab is clicked.
    /// </summary>
    internal void SelectTab(Vector2 mousePosition)
    {
        if (!Enabled || _tabs.Count == 0) return;

        // Check if click is in tab bar
        if (mousePosition.Y < Position.Y || mousePosition.Y > Position.Y + TabHeight)
            return;

        // Calculate which tab was clicked
        float tabWidth = Size.X / _tabs.Count;
        int tabIndex = (int)((mousePosition.X - Position.X) / tabWidth);

        if (tabIndex >= 0 && tabIndex < _tabs.Count)
        {
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

        // Check if mouse is over tab bar
        if (mousePosition.Y >= Position.Y && mousePosition.Y <= Position.Y + TabHeight)
        {
            float tabWidth = Size.X / _tabs.Count;
            int tabIndex = (int)((mousePosition.X - Position.X) / tabWidth);

            if (tabIndex >= 0 && tabIndex < _tabs.Count)
            {
                _hoveredTabIndex = tabIndex;
                return;
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
}
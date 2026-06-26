using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// A horizontal menu bar containing top-level menus (e.g. File, Edit, View).
/// Clicking a title opens a dropdown submenu rendered as a canvas-managed overlay.
/// </summary>
/// <remarks>
/// Add menus with <see cref="AddMenu"/>. Set <c>Size = new Vector2(canvasWidth, BarHeight)</c>
/// after constructing. The canvas renders the open submenu as a top-level overlay
/// and routes all input to the menu bar while a submenu is open.
/// </remarks>
public class UIMenuBar : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 100;
    public string? Name { get; set; }

    public UIAnchor Anchor { get; set; } = UIAnchor.TopLeft;
    public Vector2 AnchorOffset { get; set; }

    // ── Geometry / appearance ─────────────────────────────────────────────────

    /// <summary>Height of the menu bar title strip in pixels. Defaults to 28.</summary>
    public float BarHeight { get; set; } = 28f;

    /// <summary>Horizontal padding around each title label in pixels. Defaults to 12.</summary>
    public float TitlePadding { get; set; } = 12f;

    /// <summary>Width of the dropdown submenu panel. Defaults to 180.</summary>
    public float SubmenuWidth { get; set; } = 180f;

    /// <summary>Height of each non-separator submenu item. Defaults to 28.</summary>
    public float ItemHeight { get; set; } = 28f;

    /// <summary>Height of separator items. Defaults to 8.</summary>
    public float SeparatorHeight { get; set; } = 8f;

    /// <summary>Horizontal text padding inside submenu items.</summary>
    public float ItemTextPadding { get; set; } = 10f;

    // ── Colors ─────────────────────────────────────────────────────────────────

    public Color BarColor { get; set; } = new Color(45, 45, 48);
    public Color BarBorderColor { get; set; } = new Color(68, 68, 68);
    public Color TitleTextColor { get; set; } = Color.White;
    public Color TitleHoverColor { get; set; } = new Color(65, 65, 80);
    public Color TitleActiveColor { get; set; } = new Color(55, 95, 175);
    public Color SubmenuBackgroundColor { get; set; } = new Color(45, 45, 48, 250);
    public Color SubmenuBorderColor { get; set; } = new Color(90, 90, 100);
    public Color ItemHoverColor { get; set; } = new Color(55, 95, 175);
    public Color ItemTextColor { get; set; } = Color.White;
    public Color ItemDisabledColor { get; set; } = new Color(110, 110, 110);
    public Color SeparatorColor { get; set; } = new Color(90, 90, 100);

    /// <summary>Optional font for all text. Null uses the renderer default.</summary>
    public IFont? Font { get; set; }

    // ── Screen bounds (set by canvas) ─────────────────────────────────────────

    internal float ScreenHeight { get; set; } = 720f;

    // ── Data ──────────────────────────────────────────────────────────────────

    private readonly List<UIMenuBarMenu> _menus = new();

    /// <summary>Read-only view of registered menus.</summary>
    public IReadOnlyList<UIMenuBarMenu> Menus => _menus;

    /// <summary>Adds a top-level menu and returns <c>this</c> for fluent chaining.</summary>
    public UIMenuBar AddMenu(UIMenuBarMenu menu)
    {
        _menus.Add(menu);
        return this;
    }

    /// <summary>Adds a top-level menu by title and returns the new menu.</summary>
    public UIMenuBarMenu AddMenu(string title)
    {
        var menu = new UIMenuBarMenu(title);
        _menus.Add(menu);
        return menu;
    }

    // ── State ──────────────────────────────────────────────────────────────────

    private int _openMenuIndex = -1;
    private int _hoveredTitleIndex = -1;
    private int _hoveredItemIndex = -1;
    private int _keyboardItemIndex = -1;
    private float[] _titleWidths = [];

    /// <summary>Index of the currently open menu, or -1 if none.</summary>
    public int OpenMenuIndex => _openMenuIndex;

    /// <summary>Whether any submenu is currently open.</summary>
    public bool IsOpen => _openMenuIndex >= 0;

    // ── IUIComponent ──────────────────────────────────────────────────────────

    public void Update(float deltaTime) { }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Title bar background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, BarHeight, BarColor);
        renderer.DrawLine(Position.X, Position.Y + BarHeight, Position.X + Size.X, Position.Y + BarHeight, BarBorderColor);

        // Title labels — measure all widths once and cache for hit-testing / submenu placement.
        if (_titleWidths.Length != _menus.Count)
            _titleWidths = new float[_menus.Count];
        for (int i = 0; i < _menus.Count; i++)
            _titleWidths[i] = MeasureTitleWidth(renderer, _menus[i].Title);

        float x = Position.X;
        for (int i = 0; i < _menus.Count; i++)
        {
            float titleWidth = _titleWidths[i];
            var titleBg = i == _openMenuIndex ? TitleActiveColor
                : i == _hoveredTitleIndex ? TitleHoverColor
                : BarColor;

            if (titleBg != BarColor)
                renderer.DrawRectangleFilled(x, Position.Y, titleWidth, BarHeight, titleBg);

            var opts = new TextRenderOptions { Color = TitleTextColor, Font = Font, LineSpacing = 1.0f };
            var textSize = renderer.MeasureText(_menus[i].Title, opts);
            float textY = MathF.Round(Position.Y + (BarHeight - textSize.Y) / 2f);
            renderer.DrawText(_menus[i].Title, MathF.Round(x + TitlePadding), textY, opts);

            x += titleWidth;
        }
    }

    /// <summary>
    /// Draws the open submenu as a top-level overlay. Called by <see cref="UICanvas"/>.
    /// </summary>
    internal void RenderSubmenuOverlay(IRenderer renderer)
    {
        if (_openMenuIndex < 0 || _openMenuIndex >= _menus.Count) return;

        var menu = _menus[_openMenuIndex];
        var subPos = GetSubmenuPosition(_openMenuIndex, menu);

        float subHeight = ComputeSubmenuHeight(menu);

        renderer.DrawRectangleFilled(subPos.X, subPos.Y, SubmenuWidth, subHeight, SubmenuBackgroundColor);
        renderer.DrawRectangleOutline(subPos.X, subPos.Y, SubmenuWidth, subHeight, SubmenuBorderColor);

        float y = subPos.Y;
        for (int i = 0; i < menu.Items.Count; i++)
        {
            var item = menu.Items[i];

            if (item.IsSeparator)
            {
                float lineY = y + SeparatorHeight / 2f;
                renderer.DrawLine(subPos.X + 4f, lineY, subPos.X + SubmenuWidth - 4f, lineY, SeparatorColor);
                y += SeparatorHeight;
                continue;
            }

            bool hovered = i == _hoveredItemIndex || i == _keyboardItemIndex;
            if (hovered && item.Enabled)
                renderer.DrawRectangleFilled(subPos.X + 1f, y, SubmenuWidth - 2f, ItemHeight, ItemHoverColor);

            var textColor = item.Enabled ? ItemTextColor : ItemDisabledColor;
            var opts = new TextRenderOptions { Color = textColor, Font = Font, LineSpacing = 1.0f };
            var textSize = renderer.MeasureText(item.Label, opts);
            float textY = MathF.Round(y + (ItemHeight - textSize.Y) / 2f);
            renderer.DrawText(item.Label, MathF.Round(subPos.X + ItemTextPadding), textY, opts);

            y += ItemHeight;
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X && screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y && screenPosition.Y <= Position.Y + BarHeight;
    }

    // ── Canvas internal surface ───────────────────────────────────────────────

    /// <summary>Returns true if <paramref name="pos"/> is within the open submenu's bounds.</summary>
    internal bool SubmenuContains(Vector2 pos)
    {
        if (_openMenuIndex < 0) return false;
        var menu = _menus[_openMenuIndex];
        var subPos = GetSubmenuPosition(_openMenuIndex, menu);
        float h = ComputeSubmenuHeight(menu);
        return pos.X >= subPos.X && pos.X <= subPos.X + SubmenuWidth &&
               pos.Y >= subPos.Y && pos.Y <= subPos.Y + h;
    }

    /// <summary>Handles mouse movement: updates hovered title and hovered item.</summary>
    internal void UpdateHover(Vector2 mousePos)
    {
        if (!Visible || !Enabled) return;

        // Title hover
        _hoveredTitleIndex = HitTestTitle(mousePos);

        // Item hover (only when a submenu is open)
        if (_openMenuIndex >= 0)
            _hoveredItemIndex = HitTestItem(mousePos);
        else
            _hoveredItemIndex = -1;

        // Hover-switch: if mouse moves over a different title while a menu is open, switch menus.
        if (_openMenuIndex >= 0 && _hoveredTitleIndex >= 0 && _hoveredTitleIndex != _openMenuIndex)
            OpenMenu(_hoveredTitleIndex);
    }

    /// <summary>
    /// Handles a left-button click. Returns <c>true</c> if the click was consumed.
    /// </summary>
    internal bool HandleClick(Vector2 mousePos)
    {
        if (!Visible || !Enabled) return false;

        // Click on a title
        int titleIdx = HitTestTitle(mousePos);
        if (titleIdx >= 0)
        {
            if (_openMenuIndex == titleIdx)
                CloseMenu();
            else
                OpenMenu(titleIdx);
            return true;
        }

        // Click on an item in the open submenu
        if (_openMenuIndex >= 0)
        {
            int itemIdx = HitTestItem(mousePos);
            if (itemIdx >= 0)
            {
                var item = _menus[_openMenuIndex].Items[itemIdx];
                if (!item.IsSeparator && item.Enabled)
                {
                    var menu = _menus[_openMenuIndex];
                    CloseMenu();
                    menu.FireItemSelected(itemIdx);
                    return true;
                }
                return true; // consumed even if disabled
            }

            // Click outside — close the menu.
            CloseMenu();
            return false;
        }

        return false;
    }

    internal void KeyboardMoveLeft()
    {
        if (!IsOpen) return;
        int next = (_openMenuIndex - 1 + _menus.Count) % _menus.Count;
        OpenMenu(next);
    }

    internal void KeyboardMoveRight()
    {
        if (!IsOpen) return;
        int next = (_openMenuIndex + 1) % _menus.Count;
        OpenMenu(next);
    }

    internal void KeyboardMoveDown()
    {
        if (!IsOpen || _openMenuIndex < 0) return;
        var items = _menus[_openMenuIndex].Items;
        if (items.Count == 0) return;

        int start = _keyboardItemIndex < 0 ? -1 : _keyboardItemIndex;
        for (int d = 1; d <= items.Count; d++)
        {
            int idx = (start + d) % items.Count;
            if (!items[idx].IsSeparator && items[idx].Enabled)
            { _keyboardItemIndex = idx; return; }
        }
    }

    internal void KeyboardMoveUp()
    {
        if (!IsOpen || _openMenuIndex < 0) return;
        var items = _menus[_openMenuIndex].Items;
        if (items.Count == 0) return;

        int start = _keyboardItemIndex < 0 ? items.Count : _keyboardItemIndex;
        for (int d = 1; d <= items.Count; d++)
        {
            int idx = ((start - d) % items.Count + items.Count) % items.Count;
            if (!items[idx].IsSeparator && items[idx].Enabled)
            { _keyboardItemIndex = idx; return; }
        }
    }

    internal void KeyboardActivate()
    {
        if (!IsOpen || _openMenuIndex < 0 || _keyboardItemIndex < 0) return;
        var menu = _menus[_openMenuIndex];
        int idx = _keyboardItemIndex;
        CloseMenu();
        menu.FireItemSelected(idx);
    }

    internal void OpenMenu(int index)
    {
        if (index < 0 || index >= _menus.Count) return;
        _openMenuIndex = index;
        _keyboardItemIndex = -1;
        _hoveredItemIndex = -1;
    }

    internal void CloseMenu()
    {
        _openMenuIndex = -1;
        _keyboardItemIndex = -1;
        _hoveredItemIndex = -1;
        _hoveredTitleIndex = -1;
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    private float MeasureTitleWidth(IRenderer renderer, string title)
    {
        var opts = new TextRenderOptions { Font = Font, LineSpacing = 1.0f };
        return renderer.MeasureText(title, opts).X + TitlePadding * 2f;
    }

    private int HitTestTitle(Vector2 pos)
    {
        if (pos.Y < Position.Y || pos.Y > Position.Y + BarHeight) return -1;
        float x = Position.X;
        for (int i = 0; i < _menus.Count; i++)
        {
            float w = i < _titleWidths.Length ? _titleWidths[i] : _menus[i].Title.Length * 8f + TitlePadding * 2f;
            if (pos.X >= x && pos.X <= x + w) return i;
            x += w;
        }
        return -1;
    }

    private Vector2 GetSubmenuPosition(int menuIndex, UIMenuBarMenu menu)
    {
        float x = Position.X;
        for (int i = 0; i < menuIndex && i < _titleWidths.Length; i++) x += _titleWidths[i];

        float subH = ComputeSubmenuHeight(menu);
        float y = Position.Y + BarHeight;

        // Flip above the bar if the submenu would overflow below.
        if (y + subH > ScreenHeight)
            y = Position.Y - subH;

        return new Vector2(x, y);
    }

    private float ComputeSubmenuHeight(UIMenuBarMenu menu)
    {
        float h = 0f;
        foreach (var item in menu.Items)
            h += item.IsSeparator ? SeparatorHeight : ItemHeight;
        return h;
    }

    private int HitTestItem(Vector2 pos)
    {
        if (_openMenuIndex < 0) return -1;
        var menu = _menus[_openMenuIndex];
        var subPos = GetSubmenuPosition(_openMenuIndex, menu);
        float subH = ComputeSubmenuHeight(menu);

        if (pos.X < subPos.X || pos.X > subPos.X + SubmenuWidth ||
            pos.Y < subPos.Y || pos.Y > subPos.Y + subH)
            return -1;

        float y = subPos.Y;
        for (int i = 0; i < menu.Items.Count; i++)
        {
            float rowH = menu.Items[i].IsSeparator ? SeparatorHeight : ItemHeight;
            if (pos.Y < y + rowH) return i;
            y += rowH;
        }
        return -1;
    }
}

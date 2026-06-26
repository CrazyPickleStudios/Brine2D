using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Modal dialog box with customizable buttons.
/// </summary>
public class UIDialog : IUIComponent
{
    private Vector2 _position;

    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public UITooltip? Tooltip { get; set; }
    public int TabIndex { get; set; } = int.MaxValue;
    public int ZOrder { get; set; } = 0;
    public string? Name { get; set; }

    /// <summary>
    /// Dialog title text.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Dialog message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Title bar height.
    /// </summary>
    public float TitleBarHeight { get; set; } = 40f;

    /// <summary>
    /// Message area padding.
    /// </summary>
    public float Padding { get; set; } = 20f;

    /// <summary>
    /// Maximum width for message text before wrapping (0 = full content width).
    /// </summary>
    public float MessageMaxWidth { get; set; } = 0f;

    /// <summary>
    /// Button area height.
    /// </summary>
    public float ButtonAreaHeight { get; set; } = 60f;

    /// <summary>
    /// Width of buttons added via <see cref="AddButton"/>. Defaults to 100.
    /// </summary>
    public float ButtonWidth { get; set; } = 100f;

    /// <summary>
    /// Height of buttons added via <see cref="AddButton"/>. Defaults to 35.
    /// </summary>
    public float ButtonHeight { get; set; } = 35f;

    /// <summary>
    /// Horizontal spacing between buttons added via <see cref="AddButton"/>. Defaults to 10.
    /// </summary>
    public float ButtonSpacing { get; set; } = 10f;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(50, 50, 50);

    /// <summary>
    /// Title bar color.
    /// </summary>
    public Color TitleBarColor { get; set; } = new Color(70, 70, 70);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Overlay color (darkens background behind dialog).
    /// </summary>
    public Color OverlayColor { get; set; } = new Color(0, 0, 0, 180);

    /// <summary>
    /// Whether to show overlay behind dialog.
    /// </summary>
    public bool ShowOverlay { get; set; } = true;

    /// <summary>
    /// Optional nine-slice texture for the dialog body background.
    /// When set, replaces the solid <see cref="BackgroundColor"/> fill.
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

    /// <summary>
    /// Optional nine-slice texture for the title bar. When set, replaces the solid
    /// <see cref="TitleBarColor"/> fill.
    /// </summary>
    public ITexture? TitleBarTexture { get; set; }

    /// <summary>
    /// Nine-slice border insets (texels) for <see cref="TitleBarTexture"/>.
    /// </summary>
    public NineSliceBorder TitleBarTextureBorder { get; set; }

    /// <summary>
    /// Tint color applied to <see cref="TitleBarTexture"/>. Defaults to white.
    /// </summary>
    public Color TitleBarTextureTint { get; set; } = Color.White;

    /// <summary>
    /// Screen size for overlay rendering. Set via <see cref="CenterOnScreen"/>.
    /// </summary>
    internal Vector2 ScreenSize { get; set; } = new Vector2(1280, 720);

    /// <summary>
    /// When <c>true</c>, the overlay is suppressed during rendering. Set by <see cref="UICanvas"/>
    /// so only the topmost dialog draws the darkening overlay.
    /// </summary>
    internal bool SuppressOverlay { get; set; } = false;

    /// <summary>
    /// Pressing Escape fires <see cref="OnEscapeDismissed"/> when <c>true</c>. Defaults to <c>true</c>.
    /// </summary>
    public bool AllowEscapeClose { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the dialog can be repositioned by clicking and dragging the title bar.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IsDraggable { get; set; } = false;

    /// <summary>
    /// Shows an X button in the top-right of the title bar that fires
    /// <see cref="OnEscapeDismissed"/> on click. Defaults to <c>false</c>.
    /// </summary>
    public bool ShowCloseButton { get; set; } = false;

    /// <summary>
    /// Background color of the close button. Defaults to a slightly lighter title bar shade.
    /// </summary>
    public Color CloseButtonColor { get; set; } = new Color(90, 90, 90);

    /// <summary>
    /// Background color of the close button when hovered.
    /// </summary>
    public Color CloseButtonHoverColor { get; set; } = new Color(180, 60, 60);

    /// <summary>
    /// Text color of the X glyph on the close button.
    /// </summary>
    public Color CloseButtonTextColor { get; set; } = Color.White;

    /// <summary>
    /// Fired when Escape is pressed and <see cref="AllowEscapeClose"/> is <c>true</c>,
    /// or when the close button is clicked. Typical handler: <c>dialog.Visible = false</c>.
    /// </summary>
    public event Action? OnEscapeDismissed;

    private readonly List<UIButton> _buttons = new();
    private readonly List<IUIComponent> _children = new();
    private UIButton? _pressedButton;
    private bool _closeButtonHovered;

    /// <summary>
    /// Creates a dialog. The dialog is centered using <see cref="ScreenSize"/> (defaults to
    /// 1280×720). When added to a <see cref="UICanvas"/> it will be re-centered to match
    /// the canvas's actual <see cref="UICanvas.ScreenSize"/> automatically.
    /// </summary>
    public UIDialog(string title, string message, Vector2 size)
    {
        Title = title;
        Message = message;
        Size = size;

        _position = new Vector2(
            (ScreenSize.X - size.X) / 2f,
            (ScreenSize.Y - size.Y) / 2f);
    }

    public void Update(float deltaTime)
    {
        foreach (var button in _buttons)
        {
            if (button.Enabled)
                button.Update(deltaTime);
        }

        foreach (var child in _children)
        {
            if (child.Enabled)
                child.Update(deltaTime);
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        if (ShowOverlay && !SuppressOverlay)
            renderer.DrawRectangleFilled(0, 0, ScreenSize.X, ScreenSize.Y, OverlayColor);

        const float borderThickness = 2f;

        if (BackgroundTexture != null)
            renderer.DrawNineSlice(BackgroundTexture, new Rectangle(_position.X, _position.Y, Size.X, Size.Y), BackgroundTextureBorder, BackgroundTextureTint);
        else
            renderer.DrawRectangleFilled(_position.X, _position.Y, Size.X, Size.Y, BackgroundColor);

        if (TitleBarTexture != null)
            renderer.DrawNineSlice(TitleBarTexture, new Rectangle(_position.X, _position.Y, Size.X, TitleBarHeight), TitleBarTextureBorder, TitleBarTextureTint);
        else
            renderer.DrawRectangleFilled(_position.X, _position.Y, Size.X, TitleBarHeight, TitleBarColor);

        renderer.DrawRectangleFilled(_position.X, _position.Y, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(_position.X, _position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(_position.X, _position.Y, borderThickness, Size.Y, BorderColor);
        renderer.DrawRectangleFilled(_position.X + Size.X - borderThickness, _position.Y, borderThickness, Size.Y, BorderColor);

        var titleX = _position.X + Padding;
        var titleHeight = renderer.MeasureText(Title).Y;
        var titleY = _position.Y + (TitleBarHeight - titleHeight) / 2f;

        if (ShowCloseButton)
        {
            float btnSize = TitleBarHeight;
            float btnX = _position.X + Size.X - btnSize;
            float btnY = _position.Y;
            var btnColor = _closeButtonHovered ? CloseButtonHoverColor : CloseButtonColor;
            renderer.DrawRectangleFilled(btnX, btnY, btnSize, btnSize, btnColor);

            var xStr = "×";
            var xOpts = new TextRenderOptions { Color = CloseButtonTextColor };
            var xSize = renderer.MeasureText(xStr, xOpts);
            renderer.DrawText(xStr, btnX + (btnSize - xSize.X) / 2f, btnY + (btnSize - xSize.Y) / 2f, xOpts);

            float maxTitleWidth = Size.X - Padding * 2f - btnSize;
            renderer.DrawText(Title, titleX, titleY, new TextRenderOptions { Color = TextColor, MaxWidth = maxTitleWidth });
        }
        else
        {
            renderer.DrawText(Title, titleX, titleY, TextColor);
        }

        var messageX = _position.X + Padding;
        var messageY = _position.Y + TitleBarHeight + Padding;
        float maxMsgWidth = MessageMaxWidth > 0f ? MessageMaxWidth : Size.X - Padding * 2f;
        float messageAreaHeight = Size.Y - TitleBarHeight - ButtonAreaHeight - Padding;
        var msgClip = new Core.Rectangle(messageX, messageY, maxMsgWidth, Math.Max(0f, messageAreaHeight));
        renderer.PushScissorRect(msgClip);
        renderer.DrawText(Message, messageX, messageY, new TextRenderOptions { Color = TextColor, MaxWidth = maxMsgWidth });
        renderer.PopScissorRect();

        foreach (var button in _buttons)
        {
            if (!button.Visible) continue;

            var saved = button.Position;
            button.Position = _position + saved;
            button.Render(renderer);
            button.Position = saved;
        }

        foreach (var child in _children)
        {
            if (!child.Visible) continue;

            if (child is IAnchoredUIComponent anchoredChild &&
                (anchoredChild.Anchor != UIAnchor.TopLeft || anchoredChild.AnchorOffset != Vector2.Zero))
            {
                var saved = anchoredChild.Position;
                var anchorOrigin = UIAnchorResolver.Resolve(anchoredChild.Anchor, Size.X, Size.Y);
                anchoredChild.Position = _position + anchorOrigin + anchoredChild.AnchorOffset;
                child.Render(renderer);
                anchoredChild.Position = saved;
            }
            else
            {
                var saved = child.Position;
                child.Position = _position + saved;
                child.Render(renderer);
                child.Position = saved;
            }
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= _position.X &&
               screenPosition.X <= _position.X + Size.X &&
               screenPosition.Y >= _position.Y &&
               screenPosition.Y <= _position.Y + Size.Y;
    }

    /// <summary>
    /// Adds a button to the dialog's button area. Buttons are laid out left-to-right in add
    /// order, centered horizontally as a group. Call <see cref="RepositionButtons"/> after
    /// adding all buttons if you need positions to be correct before the first render.
    /// </summary>
    public UIButton AddButton(string text, Action onClick)
    {
        float buttonY = Size.Y - Padding - ButtonHeight - 10f;
        var button = new UIButton(text, Vector2.Zero, new Vector2(ButtonWidth, ButtonHeight));
        button.OnClick += onClick;
        _buttons.Add(button);
        RepositionButtons();
        return button;
    }

    /// <summary>
    /// Recomputes button positions so they are centered horizontally across the bottom of the
    /// dialog in the order they were added. Called automatically by <see cref="AddButton"/>.
    /// </summary>
    public void RepositionButtons()
    {
        if (_buttons.Count == 0) return;

        float totalWidth = _buttons.Count * ButtonWidth + (_buttons.Count - 1) * ButtonSpacing;
        float startX = (Size.X - totalWidth) / 2f;
        float buttonY = Size.Y - Padding - ButtonHeight - 10f;

        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].Position = new Vector2(startX + i * (ButtonWidth + ButtonSpacing), buttonY);
        }
    }

    /// <summary>
    /// Removes all buttons.
    /// </summary>
    public void ClearButtons()
    {
        _pressedButton = null;
        _buttons.Clear();
    }

    /// <summary>
    /// Gets all buttons.
    /// </summary>
    public IReadOnlyList<UIButton> GetButtons() => _buttons.AsReadOnly();

    /// <summary>
    /// Adds an arbitrary UI component to the dialog's content area. Child positions are
    /// dialog-relative: <c>(0, 0)</c> is the top-left corner of the dialog. The component
    /// is offset by the dialog's current <see cref="Position"/> during rendering and input
    /// dispatch, so it moves with the dialog when it is dragged or re-centered.
    /// </summary>
    public void AddChild(IUIComponent child) => _children.Add(child);

    /// <summary>
    /// Removes a child component previously added via <see cref="AddChild"/>.
    /// </summary>
    public void RemoveChild(IUIComponent child) => _children.Remove(child);

    /// <summary>
    /// Removes all child components added via <see cref="AddChild"/>.
    /// Does not affect buttons added via <see cref="AddButton"/>.
    /// </summary>
    public void ClearChildren() => _children.Clear();

    /// <summary>
    /// Gets all child components added via <see cref="AddChild"/>.
    /// </summary>
    public IReadOnlyList<IUIComponent> GetChildren() => _children.AsReadOnly();

    /// <summary>
    /// Moves a child component to the end of the render/input order so it renders
    /// on top of all other children inside the dialog. No-op if the child is not present
    /// or is already at the front.
    /// </summary>
    internal void BringChildToFront(IUIComponent child)
    {
        int index = _children.IndexOf(child);
        if (index >= 0 && index != _children.Count - 1)
        {
            _children.RemoveAt(index);
            _children.Add(child);
        }
    }

    /// <summary>
    /// Centers the dialog on screen and updates the screen size used for overlay rendering.
    /// Called automatically by <see cref="UICanvas"/> on <see cref="UICanvas.Add"/> and
    /// whenever <see cref="UICanvas.ScreenSize"/> changes.
    /// </summary>
    public void CenterOnScreen(Vector2 screenSize)
    {
        ScreenSize = screenSize;
        Position = new Vector2(
            (screenSize.X - Size.X) / 2f,
            (screenSize.Y - Size.Y) / 2f);
    }

    /// <summary>
    /// Called by UICanvas to handle button input.
    /// </summary>
    internal bool ProcessButtonInput(Vector2 mousePosition, bool isPressed, bool isReleased)
    {
        // Translate to dialog-relative space so Contains works against stored relative positions.
        var relativePos = mousePosition - _position;

        foreach (var button in _buttons)
        {
            if (button.Contains(relativePos))
            {
                button.SetHovered(true);

                if (isPressed)
                {
                    button.SetPressed(true);
                    _pressedButton = button;
                }

                if (isReleased)
                {
                    button.SetPressed(false);
                    if (_pressedButton == button)
                        button.Click();
                    _pressedButton = null;
                }

                return true;
            }
            else
            {
                button.SetHovered(false);
                if (!isReleased)
                    button.SetPressed(false);
            }
        }

        if (isReleased)
            _pressedButton = null;

        return false;
    }

    /// <summary>
    /// Called by <see cref="UICanvas"/> when Escape is pressed while this is the active modal.
    /// Fires <see cref="OnEscapeDismissed"/> if <see cref="AllowEscapeClose"/> is <c>true</c>.
    /// </summary>
    internal void EscapeDismiss()
    {
        if (AllowEscapeClose)
            OnEscapeDismissed?.Invoke();
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="screenPosition"/> is within the title bar.
    /// Used by <see cref="UICanvas"/> to initiate a drag when <see cref="IsDraggable"/> is enabled.
    /// </summary>
    internal bool IsOverTitleBar(Vector2 screenPosition)
    {
        return screenPosition.X >= _position.X &&
               screenPosition.X <= _position.X + Size.X &&
               screenPosition.Y >= _position.Y &&
               screenPosition.Y <= _position.Y + TitleBarHeight;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="screenPosition"/> is over the close button.
    /// </summary>
    internal bool IsOverCloseButton(Vector2 screenPosition)
    {
        if (!ShowCloseButton) return false;
        float btnSize = TitleBarHeight;
        float btnX = _position.X + Size.X - btnSize;
        return screenPosition.X >= btnX &&
               screenPosition.X <= btnX + btnSize &&
               screenPosition.Y >= _position.Y &&
               screenPosition.Y <= _position.Y + btnSize;
    }

    /// <summary>
    /// Updates the close button hover highlight. Called each frame by <see cref="UICanvas"/>.
    /// </summary>
    internal void SetCloseButtonHovered(bool hovered)
    {
        _closeButtonHovered = hovered && ShowCloseButton;
    }
}

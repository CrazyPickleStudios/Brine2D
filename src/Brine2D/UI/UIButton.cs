using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Interactive button UI component.
/// </summary>
public class UIButton : IUIComponent, IAnchoredUIComponent
{
    public UITooltip? Tooltip { get; set; }
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
    /// Button text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Optional font for rendering text (null = renderer default).
        /// </summary>
        public IFont? Font { get; set; }

    /// <summary>
    /// Normal state color.
    /// </summary>
    public Color NormalColor { get; set; } = new Color(70, 70, 70, 255);

    /// <summary>
    /// Hover state color.
    /// </summary>
    public Color HoverColor { get; set; } = new Color(90, 90, 90, 255);

    /// <summary>
    /// Pressed state color.
    /// </summary>
    public Color PressedColor { get; set; } = new Color(50, 50, 50, 255);

    /// <summary>
    /// Color used when the button is disabled.
    /// </summary>
    public Color DisabledColor { get; set; } = new Color(60, 60, 60, 180);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Horizontal text alignment. Defaults to <see cref="TextAlignment.Center"/>.
    /// </summary>
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Center;

    /// <summary>
    /// Pixels between the text and the button edge when alignment is
    /// <see cref="TextAlignment.Left"/> or <see cref="TextAlignment.Right"/>.
    /// Ignored for <see cref="TextAlignment.Center"/>.
    /// </summary>
    public float TextPadding { get; set; } = 8f;

    /// <summary>
    /// Event fired when button is clicked.
    /// </summary>
    public event Action? OnClick;

    /// <summary>
    /// Event fired when the button is right-clicked.
    /// </summary>
    public event Action? OnRightClick;

    /// <summary>
    /// Event fired when the mouse cursor enters the button bounds.
    /// </summary>
    public event Action? OnHoverEnter;

    /// <summary>
    /// Event fired when the mouse cursor leaves the button bounds.
    /// </summary>
    public event Action? OnHoverExit;

    /// <summary>
    /// Event fired when this button gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Event fired when this button loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    private bool _isHovered;
    private bool _isPressed;
    private bool _isFocused;

    /// <summary>
    /// Whether this button currently has keyboard focus.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Color of the focus ring drawn when the button has keyboard focus.
    /// </summary>
    public Color FocusColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Optional nine-slice texture for the normal (idle) state. Replaces the solid
    /// <see cref="NormalColor"/> fill when set.
    /// </summary>
    public ITexture? NormalTexture { get; set; }

    /// <summary>
    /// Optional nine-slice texture for the hover state. Falls back to
    /// <see cref="NormalTexture"/> when <c>null</c>.
    /// </summary>
    public ITexture? HoverTexture { get; set; }

    /// <summary>
    /// Optional nine-slice texture for the pressed state. Falls back to
    /// <see cref="NormalTexture"/> when <c>null</c>.
    /// </summary>
    public ITexture? PressedTexture { get; set; }

    /// <summary>
    /// Optional nine-slice texture for the disabled state. Falls back to
    /// <see cref="NormalTexture"/> when <c>null</c>.
    /// </summary>
    public ITexture? DisabledTexture { get; set; }

    /// <summary>
    /// Nine-slice border insets (texels) shared by all state textures.
    /// </summary>
    public NineSliceBorder TextureBorder { get; set; }

    /// <summary>
    /// Tint color applied to the active state texture.
    /// </summary>
    public Color TextureTint { get; set; } = Color.White;

    public UIButton(string text, Vector2 position, Vector2 size)
    {
        Text = text;
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        var currentColor = !Enabled ? DisabledColor :
                          _isPressed ? PressedColor :
                          _isHovered ? HoverColor :
                          NormalColor;

        var activeTexture = !Enabled ? (DisabledTexture ?? NormalTexture) :
                            _isPressed ? (PressedTexture ?? NormalTexture) :
                            _isHovered ? (HoverTexture ?? NormalTexture) :
                            NormalTexture;

        if (activeTexture != null)
            renderer.DrawNineSlice(activeTexture, new Rectangle(Position.X, Position.Y, Size.X, Size.Y), TextureBorder, TextureTint);
        else
            renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, currentColor);

        var borderColor = _isFocused ? FocusColor :
                          Enabled ? new Color(150, 150, 150) : new Color(100, 100, 100);
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, 2f, borderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - 2f, Size.X, 2f, borderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, 2f, Size.Y, borderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - 2f, Position.Y, 2f, Size.Y, borderColor);

        if (!string.IsNullOrEmpty(Text))
        {
            var textColor = Enabled ? TextColor : new Color(100, 100, 100);
            var options = Font == null
                ? new TextRenderOptions { Color = textColor, LineSpacing = 1.0f }
                : new TextRenderOptions { Color = textColor, Font = Font, LineSpacing = 1.0f };
            var textSize = renderer.MeasureText(Text, options);

            float textX = TextAlignment switch
            {
                TextAlignment.Left  => Position.X + TextPadding,
                TextAlignment.Right => Position.X + Size.X - textSize.X - TextPadding,
                _                   => Position.X + (Size.X - textSize.X) / 2f
            };
            float textX2 = MathF.Round(textX);
            float textY = MathF.Round(Position.Y + (Size.Y - textSize.Y) / 2f);

            renderer.DrawText(Text, textX2, textY, options);
        }
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    internal void SetHovered(bool hovered)
    {
        bool newHovered = hovered && Enabled;
        if (newHovered != _isHovered)
        {
            _isHovered = newHovered;
            if (_isHovered) OnHoverEnter?.Invoke();
            else OnHoverExit?.Invoke();
        }
    }

    internal void SetPressed(bool pressed)
    {
        _isPressed = pressed && Enabled;
    }

    internal void Click()
    {
        if (Enabled)
            OnClick?.Invoke();
    }

    internal void RightClick()
    {
        if (Enabled)
            OnRightClick?.Invoke();
    }

    internal void SetFocused(bool focused)
    {
        bool newFocused = focused && Enabled;
        if (newFocused == _isFocused) return;
        _isFocused = newFocused;
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }
}
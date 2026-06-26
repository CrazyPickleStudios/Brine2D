using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;
using System.Globalization;
using System.Numerics;

namespace Brine2D.UI;

/// <summary>
/// Numeric spin-box with increment/decrement buttons and optional inline text entry.
/// </summary>
public class UISpinBox : IUIComponent, IAnchoredUIComponent
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

    /// <summary>
    /// Current numeric value, clamped between <see cref="MinValue"/> and <see cref="MaxValue"/>.
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, MinValue, MaxValue);
            if (Math.Abs(_value - clamped) > 0.00001f)
            {
                _value = clamped;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Minimum allowed value. Defaults to 0.
    /// </summary>
    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            if (_minValue <= _maxValue)
                _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    /// Maximum allowed value. Defaults to 100.
    /// </summary>
    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            if (_minValue <= _maxValue)
                _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    /// Amount to increment or decrement per button click or arrow key press. Defaults to 1.
    /// </summary>
    public float Step { get; set; } = 1f;

    /// <summary>
    /// Display format string for the numeric value (e.g. "0", "0.00"). Defaults to "0".
    /// </summary>
    public string ValueFormat { get; set; } = "0";

    /// <summary>
    /// Optional font (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Background color of the value field.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40);

    /// <summary>
    /// Background color when focused.
    /// </summary>
    public Color FocusedBackgroundColor { get; set; } = new Color(50, 50, 50);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Border color when focused.
    /// </summary>
    public Color FocusedBorderColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Text color for the displayed value.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Background color of the increment/decrement buttons.
    /// </summary>
    public Color ButtonColor { get; set; } = new Color(70, 70, 70);

    /// <summary>
    /// Button background color when hovered.
    /// </summary>
    public Color ButtonHoverColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Button background color when pressed.
    /// </summary>
    public Color ButtonPressedColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Arrow/symbol color on the buttons.
    /// </summary>
    public Color ButtonSymbolColor { get; set; } = Color.White;

    /// <summary>
    /// Whether this spin box is currently focused.
    /// </summary>
    public bool IsFocused => _isFocused;

    /// <summary>
    /// Whether the value text is currently being edited directly via keyboard.
    /// </summary>
    public bool IsEditing => _isEditing;

    /// <summary>
    /// Fired when the value changes.
    /// </summary>
    public event Action<float>? OnValueChanged;

    /// <summary>
    /// Fired when this spin box gains keyboard focus.
    /// </summary>
    public event Action? OnFocusGained;

    /// <summary>
    /// Fired when this spin box loses keyboard focus.
    /// </summary>
    public event Action? OnFocusLost;

    private float _value;
    private float _minValue = 0f;
    private float _maxValue = 100f;
    private bool _isFocused;
    private bool _isHoveredUp;
    private bool _isHoveredDown;
    private bool _isPressedUp;
    private bool _isPressedDown;

    // Inline text-editing state.
    private bool _isEditing;
    private string _editBuffer = string.Empty;
    private float _cursorBlinkTime;
    private bool _cursorVisible = true;
    private const float CursorBlinkInterval = 0.5f;

    /// <summary>
    /// Width of each increment/decrement button. Derived from <see cref="Size"/>.
    /// </summary>
    private float ButtonWidth => Size.Y;

    public UISpinBox(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        _value = _minValue;
    }

    public void Update(float deltaTime)
    {
        if (_isEditing)
        {
            _cursorBlinkTime += deltaTime;
            if (_cursorBlinkTime >= CursorBlinkInterval)
            {
                _cursorBlinkTime -= CursorBlinkInterval;
                _cursorVisible = !_cursorVisible;
            }
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        var bg = _isFocused ? FocusedBackgroundColor : BackgroundColor;
        var border = _isFocused ? FocusedBorderColor : BorderColor;
        var bw = ButtonWidth;
        float fieldWidth = Size.X - bw * 2;

        // Background field
        renderer.DrawRectangleFilled(Position.X + bw, Position.Y, fieldWidth, Size.Y, bg);
        renderer.DrawRectangleOutline(Position.X + bw, Position.Y, fieldWidth, Size.Y, border);

        // Value text (or edit buffer)
        var displayText = _isEditing ? _editBuffer : _value.ToString(ValueFormat, CultureInfo.InvariantCulture);
        var opts = new TextRenderOptions { Color = TextColor, Font = Font, LineSpacing = 1.0f };
        var textSize = renderer.MeasureText(displayText, opts);
        float textX = MathF.Round(Position.X + bw + (fieldWidth - textSize.X) / 2f);
        float textY = MathF.Round(Position.Y + (Size.Y - textSize.Y) / 2f);
        renderer.DrawText(displayText, textX, textY, opts);

        // Cursor while editing
        if (_isEditing && _cursorVisible)
        {
            float cursorX = MathF.Round(textX + textSize.X + 1f);
            renderer.DrawRectangleFilled(cursorX, textY, 1f, textSize.Y, TextColor);
        }

        // Decrement button (left)
        var downBg = _isPressedDown ? ButtonPressedColor : (_isHoveredDown ? ButtonHoverColor : ButtonColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, bw, Size.Y, downBg);
        renderer.DrawRectangleOutline(Position.X, Position.Y, bw, Size.Y, border);
        DrawArrow(renderer, Position.X, Position.Y, bw, Size.Y, isUp: false);

        // Increment button (right)
        var upBg = _isPressedUp ? ButtonPressedColor : (_isHoveredUp ? ButtonHoverColor : ButtonColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - bw, Position.Y, bw, Size.Y, upBg);
        renderer.DrawRectangleOutline(Position.X + Size.X - bw, Position.Y, bw, Size.Y, border);
        DrawArrow(renderer, Position.X + Size.X - bw, Position.Y, bw, Size.Y, isUp: true);
    }

    private void DrawArrow(IRenderer renderer, float bx, float by, float bw, float bh, bool isUp)
    {
        float cx = bx + bw / 2f;
        float cy = by + bh / 2f;
        float halfW = bw * 0.2f;
        float halfH = bh * 0.15f;

        if (isUp)
        {
            renderer.DrawRectangleFilled(cx - 1f, cy - halfH, 2f, halfH * 2f, ButtonSymbolColor);
            renderer.DrawRectangleFilled(cx - halfW, cy - halfH, halfW * 2f, 2f, ButtonSymbolColor);
        }
        else
        {
            renderer.DrawRectangleFilled(cx - 1f, cy - halfH, 2f, halfH * 2f, ButtonSymbolColor);
            renderer.DrawRectangleFilled(cx - halfW, cy + halfH - 2f, halfW * 2f, 2f, ButtonSymbolColor);
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
    /// Returns whether the given screen position is within the decrement (left) button.
    /// </summary>
    internal bool ContainsDecrement(Vector2 pos) =>
        pos.X >= Position.X && pos.X <= Position.X + ButtonWidth &&
        pos.Y >= Position.Y && pos.Y <= Position.Y + Size.Y;

    /// <summary>
    /// Returns whether the given screen position is within the increment (right) button.
    /// </summary>
    internal bool ContainsIncrement(Vector2 pos) =>
        pos.X >= Position.X + Size.X - ButtonWidth && pos.X <= Position.X + Size.X &&
        pos.Y >= Position.Y && pos.Y <= Position.Y + Size.Y;

    /// <summary>
    /// Returns whether the given screen position is within the text field (not the buttons).
    /// </summary>
    internal bool ContainsField(Vector2 pos)
    {
        float bw = ButtonWidth;
        return pos.X >= Position.X + bw && pos.X <= Position.X + Size.X - bw &&
               pos.Y >= Position.Y && pos.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Called by UICanvas to set keyboard focus.
    /// </summary>
    internal void SetFocused(bool focused)
    {
        bool newFocused = focused && Enabled;
        if (newFocused == _isFocused) return;
        _isFocused = newFocused;
        if (!_isFocused)
            CommitEdit();
        if (_isFocused) OnFocusGained?.Invoke();
        else OnFocusLost?.Invoke();
    }

    /// <summary>
    /// Called by UICanvas to set hover state on the increment button.
    /// </summary>
    internal void SetHoveredIncrement(bool hovered) => _isHoveredUp = hovered && Enabled;

    /// <summary>
    /// Called by UICanvas to set hover state on the decrement button.
    /// </summary>
    internal void SetHoveredDecrement(bool hovered) => _isHoveredDown = hovered && Enabled;

    /// <summary>
    /// Called by UICanvas when the increment button is pressed.
    /// </summary>
    internal void SetPressedIncrement(bool pressed) => _isPressedUp = pressed && Enabled;

    /// <summary>
    /// Called by UICanvas when the decrement button is pressed.
    /// </summary>
    internal void SetPressedDecrement(bool pressed) => _isPressedDown = pressed && Enabled;

    /// <summary>
    /// Increments the value by <see cref="Step"/>.
    /// </summary>
    internal void Increment()
    {
        if (!Enabled) return;
        Value = Math.Clamp(_value + Step, MinValue, MaxValue);
    }

    /// <summary>
    /// Decrements the value by <see cref="Step"/>.
    /// </summary>
    internal void Decrement()
    {
        if (!Enabled) return;
        Value = Math.Clamp(_value - Step, MinValue, MaxValue);
    }

    /// <summary>
    /// Nudges the value in a given direction (positive = increment, negative = decrement).
    /// Called by UICanvas arrow-key handling.
    /// </summary>
    internal void NudgeValue(float direction)
    {
        if (!Enabled) return;
        Value = Math.Clamp(_value + direction * Step, MinValue, MaxValue);
    }

    /// <summary>
    /// Begins direct text editing of the value field.
    /// </summary>
    internal void BeginEdit()
    {
        if (!Enabled || _isEditing) return;
        _isEditing = true;
        _editBuffer = _value.ToString(ValueFormat, CultureInfo.InvariantCulture);
        _cursorBlinkTime = 0f;
        _cursorVisible = true;
    }

    /// <summary>
    /// Handles a character typed while editing. Called by UICanvas keyboard routing.
    /// </summary>
    internal void HandleEditChar(char c)
    {
        if (!_isEditing) return;
        if (char.IsDigit(c) || c == '-' || c == '.')
            _editBuffer += c;
    }

    /// <summary>
    /// Handles Backspace while editing.
    /// </summary>
    internal void HandleEditBackspace()
    {
        if (!_isEditing || _editBuffer.Length == 0) return;
        _editBuffer = _editBuffer[..^1];
    }

    /// <summary>
    /// Commits the edit buffer to <see cref="Value"/> and exits editing mode.
    /// If the buffer cannot be parsed the value is left unchanged.
    /// </summary>
    internal void CommitEdit()
    {
        if (!_isEditing) return;
        _isEditing = false;
        if (float.TryParse(_editBuffer, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            Value = parsed;
        _editBuffer = string.Empty;
    }

    /// <summary>
    /// Cancels the current edit without applying the buffer.
    /// </summary>
    internal void CancelEdit()
    {
        if (!_isEditing) return;
        _isEditing = false;
        _editBuffer = string.Empty;
    }
}

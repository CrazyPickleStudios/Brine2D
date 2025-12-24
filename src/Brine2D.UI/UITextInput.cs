using System.Numerics;
using System.Text;
using Brine2D.Input;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Text input field UI component with cursor support.
/// </summary>
public class UITextInput : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    public UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Current text value.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Current cursor position (character index, 0 = before first character).
    /// </summary>
    public int CursorPosition 
    { 
        get => _cursorPosition;
        set => _cursorPosition = Math.Clamp(value, 0, Text.Length);
    }

    /// <summary>
    /// Placeholder text shown when empty.
    /// </summary>
    public string Placeholder { get; set; } = "Enter text...";

    /// <summary>
    /// Maximum character length (0 = unlimited).
    /// </summary>
    public int MaxLength { get; set; } = 0;

    /// <summary>
    /// Whether this input is currently focused.
    /// </summary>
    public bool IsFocused { get; private set; }

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Placeholder text color.
    /// </summary>
    public Color PlaceholderColor { get; set; } = new Color(150, 150, 150);

    /// <summary>
    /// Background color when unfocused.
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
    /// Focused border color.
    /// </summary>
    public Color FocusedBorderColor { get; set; } = new Color(120, 180, 255);

    /// <summary>
    /// Event fired when text changes.
    /// </summary>
    public event Action<string>? OnTextChanged;

    /// <summary>
    /// Event fired when Enter key is pressed.
    /// </summary>
    public event Action<string>? OnSubmit;

    private int _cursorPosition;
    private float _cursorBlinkTime;
    private bool _cursorVisible = true;
    private const float CursorBlinkInterval = 0.5f;

    public UITextInput(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        if (!IsFocused) return;

        // Cursor blink animation
        _cursorBlinkTime += deltaTime;
        if (_cursorBlinkTime >= CursorBlinkInterval)
        {
            _cursorBlinkTime = 0;
            _cursorVisible = !_cursorVisible;
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Draw background
        var bgColor = IsFocused ? FocusedBackgroundColor : BackgroundColor;
        renderer.DrawRectangle(Position.X, Position.Y, Size.X, Size.Y, bgColor);

        // Draw border
        var borderColor = IsFocused ? FocusedBorderColor : BorderColor;
        float borderThickness = 2f;
        renderer.DrawRectangle(Position.X, Position.Y, Size.X, borderThickness, borderColor); // Top
        renderer.DrawRectangle(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, borderColor); // Bottom
        renderer.DrawRectangle(Position.X, Position.Y, borderThickness, Size.Y, borderColor); // Left
        renderer.DrawRectangle(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, borderColor); // Right

        // Draw text or placeholder
        var textX = Position.X + 10;
        var textY = Position.Y + (Size.Y / 2) - 8;

        if (string.IsNullOrEmpty(Text))
        {
            renderer.DrawText(Placeholder, textX, textY, PlaceholderColor);
        }
        else
        {
            renderer.DrawText(Text, textX, textY, TextColor);

            // Draw cursor if focused (at cursor position)
            if (IsFocused && _cursorVisible)
            {
                // Calculate cursor X position based on cursor position in text
                var cursorX = textX + (_cursorPosition * 8); // Rough character width
                renderer.DrawRectangle(cursorX, textY, 2, 16, TextColor);
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
    /// Called by UICanvas when input is focused.
    /// </summary>
    internal void SetFocused(bool focused, IInputService input)
    {
        if (IsFocused == focused) return;
        
        IsFocused = focused;
        _cursorBlinkTime = 0;
        _cursorVisible = true;

        if (focused)
        {
            // Move cursor to end when focused
            _cursorPosition = Text.Length;
            input.StartTextInput();
        }
        else
        {
            input.StopTextInput();
        }
    }

    /// <summary>
    /// Called by UICanvas to handle text input.
    /// </summary>
    internal void HandleTextInput(IInputService input)
    {
        if (!IsFocused || !Enabled) return;

        // Reset cursor blink on any input
        _cursorBlinkTime = 0;
        _cursorVisible = true;

        // Handle backspace - delete character BEFORE cursor (with key repeat)
        if (input.IsBackspacePressed() && _cursorPosition > 0)
        {
            Text = Text.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
            OnTextChanged?.Invoke(Text);
        }

        // Handle delete - delete character AT cursor (with key repeat)
        if (input.IsDeletePressed() && _cursorPosition < Text.Length)
        {
            Text = Text.Remove(_cursorPosition, 1);
            OnTextChanged?.Invoke(Text);
        }

        // Arrow keys for cursor movement
        if (input.IsKeyPressed(Keys.Left) && _cursorPosition > 0)
        {
            _cursorPosition--;
        }

        if (input.IsKeyPressed(Keys.Right) && _cursorPosition < Text.Length)
        {
            _cursorPosition++;
        }

        if (input.IsKeyPressed(Keys.Home))
        {
            _cursorPosition = 0;
        }

        if (input.IsKeyPressed(Keys.End))
        {
            _cursorPosition = Text.Length;
        }

        if (input.IsReturnPressed())
        {
            OnSubmit?.Invoke(Text);
        }

        // Text input from SDL
        var textInput = input.GetTextInput();
        if (!string.IsNullOrEmpty(textInput))
        {
            foreach (char c in textInput)
            {
                if (MaxLength == 0 || Text.Length < MaxLength)
                {
                    Text = Text.Insert(_cursorPosition, c.ToString());
                    _cursorPosition++;
                }
            }
            OnTextChanged?.Invoke(Text);
        }
    }
}
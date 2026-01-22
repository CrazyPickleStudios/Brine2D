using System.Drawing;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Modal dialog box with customizable buttons.
/// </summary>
public class UIDialog : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public UITooltip? Tooltip { get; set; }

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
    /// Button area height.
    /// </summary>
    public float ButtonAreaHeight { get; set; } = 60f;

    /// <summary>
    /// Background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.FromArgb(50, 50, 50);

    /// <summary>
    /// Title bar color.
    /// </summary>
    public Color TitleBarColor { get; set; } = Color.FromArgb(70, 70, 70);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = Color.FromArgb(100, 100, 100);

    /// <summary>
    /// Text color.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Overlay color (darkens background behind dialog).
    /// </summary>
    public Color OverlayColor { get; set; } = Color.FromArgb(180, 0, 0, 0);

    /// <summary>
    /// Whether to show overlay behind dialog.
    /// </summary>
    public bool ShowOverlay { get; set; } = true;

    /// <summary>
    /// Screen size for overlay (set by UICanvas).
    /// </summary>
    internal Vector2 ScreenSize { get; set; } = new Vector2(1280, 720);

    private readonly List<UIButton> _buttons = new();

    public UIDialog(string title, string message, Vector2 size)
    {
        Title = title;
        Message = message;
        Size = size;
        
        // Center on screen (default)
        Position = new Vector2(
            (1280 - size.X) / 2,
            (720 - size.Y) / 2);
    }

    public void Update(float deltaTime)
    {
        foreach (var button in _buttons)
        {
            if (button.Enabled)
            {
                button.Update(deltaTime);
            }
        }
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Draw overlay
        if (ShowOverlay)
        {
            renderer.DrawRectangleFilled(0, 0, ScreenSize.X, ScreenSize.Y, OverlayColor);
        }

        float borderThickness = 2f;

        // Draw dialog background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Draw title bar
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, TitleBarHeight, TitleBarColor);

        // Draw border
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, BorderColor);

        // Draw title text
        var titleX = Position.X + Padding;
        var titleY = Position.Y + (TitleBarHeight / 2) - 8;
        renderer.DrawText(Title, titleX, titleY, TextColor);

        // Draw message text (word wrap not implemented, so keep messages short)
        var messageX = Position.X + Padding;
        var messageY = Position.Y + TitleBarHeight + Padding;
        renderer.DrawText(Message, messageX, messageY, TextColor);

        // Render buttons
        foreach (var button in _buttons)
        {
            if (button.Visible)
            {
                button.Render(renderer);
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
    /// Adds a button to the dialog.
    /// </summary>
    public UIButton AddButton(string text, Action onClick)
    {
        float buttonWidth = 100f;
        float buttonHeight = 35f;
        float buttonSpacing = 10f;

        // Calculate button position (right-aligned at bottom)
        float buttonX = Position.X + Size.X - Padding - ((_buttons.Count + 1) * (buttonWidth + buttonSpacing));
        float buttonY = Position.Y + Size.Y - Padding - buttonHeight - 10f;

        var button = new UIButton(text, new Vector2(buttonX, buttonY), new Vector2(buttonWidth, buttonHeight));
        button.OnClick += onClick;

        _buttons.Add(button);
        return button;
    }

    /// <summary>
    /// Removes all buttons.
    /// </summary>
    public void ClearButtons()
    {
        _buttons.Clear();
    }

    /// <summary>
    /// Gets all buttons.
    /// </summary>
    public IReadOnlyList<UIButton> GetButtons() => _buttons.AsReadOnly();

    /// <summary>
    /// Centers the dialog on screen.
    /// </summary>
    public void CenterOnScreen(Vector2 screenSize)
    {
        ScreenSize = screenSize;
        Position = new Vector2(
            (screenSize.X - Size.X) / 2,
            (screenSize.Y - Size.Y) / 2);
    }

    /// <summary>
    /// Called by UICanvas to handle button input.
    /// </summary>
    internal bool ProcessButtonInput(Vector2 mousePosition, bool isPressed, bool isReleased)
    {
        foreach (var button in _buttons)
        {
            if (button.Contains(mousePosition))
            {
                button.SetHovered(true);

                if (isPressed)
                {
                    button.SetPressed(true);
                }

                if (isReleased)
                {
                    button.SetPressed(false);
                    button.Click();
                }

                return true; // Consumed input
            }
            else
            {
                button.SetHovered(false);
                button.SetPressed(false);
            }
        }

        return false;
    }
}
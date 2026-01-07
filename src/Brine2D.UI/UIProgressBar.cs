using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.UI;

/// <summary>
/// Progress bar UI component for displaying progress/percentage.
/// </summary>
public class UIProgressBar : IUIComponent
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Current value (0.0 to 1.0, representing 0% to 100%).
    /// </summary>
    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Background color (empty portion).
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40);

    public UITooltip? Tooltip { get; set; }

    /// <summary>
    /// Fill color (filled portion).
    /// </summary>
    public Color FillColor { get; set; } = new Color(0, 200, 0);

    /// <summary>
    /// Border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(100, 100, 100);

    /// <summary>
    /// Whether to show percentage text.
    /// </summary>
    public bool ShowPercentage { get; set; } = true;

    /// <summary>
    /// Text color for percentage display.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Optional label text (e.g., "Health", "Loading").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Label text color.
    /// </summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>
    /// Direction the bar fills.
    /// </summary>
    public ProgressBarDirection Direction { get; set; } = ProgressBarDirection.LeftToRight;

    /// <summary>
    /// Event fired when value changes.
    /// </summary>
    public event Action<float>? OnValueChanged;

    private float _value;

    public UIProgressBar(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void Update(float deltaTime)
    {
        // Progress bar is typically updated by game logic, not by input
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible) return;

        // Draw background
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, Size.Y, BackgroundColor);

        // Draw fill based on direction
        float fillAmount = _value;
        switch (Direction)
        {
            case ProgressBarDirection.LeftToRight:
                {
                    float fillWidth = Size.X * fillAmount;
                    if (fillWidth > 0)
                    {
                        renderer.DrawRectangleFilled(Position.X, Position.Y, fillWidth, Size.Y, FillColor);
                    }
                    break;
                }
            case ProgressBarDirection.RightToLeft:
                {
                    float fillWidth = Size.X * fillAmount;
                    if (fillWidth > 0)
                    {
                        renderer.DrawRectangleFilled(Position.X + Size.X - fillWidth, Position.Y, fillWidth, Size.Y, FillColor);
                    }
                    break;
                }
            case ProgressBarDirection.BottomToTop:
                {
                    float fillHeight = Size.Y * fillAmount;
                    if (fillHeight > 0)
                    {
                        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - fillHeight, Size.X, fillHeight, FillColor);
                    }
                    break;
                }
            case ProgressBarDirection.TopToBottom:
                {
                    float fillHeight = Size.Y * fillAmount;
                    if (fillHeight > 0)
                    {
                        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, fillHeight, FillColor);
                    }
                    break;
                }
        }

        // Draw border
        float borderThickness = 2f;
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, borderThickness, BorderColor); // Top
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - borderThickness, Size.X, borderThickness, BorderColor); // Bottom
        renderer.DrawRectangleFilled(Position.X, Position.Y, borderThickness, Size.Y, BorderColor); // Left
        renderer.DrawRectangleFilled(Position.X + Size.X - borderThickness, Position.Y, borderThickness, Size.Y, BorderColor); // Right

        // Draw percentage text if enabled
        if (ShowPercentage)
        {
            var percentageText = $"{(_value * 100):0}%";
            var textX = Position.X + (Size.X / 2) - (percentageText.Length * 4);
            var textY = Position.Y + (Size.Y / 2) - 8;
            renderer.DrawText(percentageText, textX, textY, TextColor);
        }

        // Draw label if provided
        if (!string.IsNullOrEmpty(Label))
        {
            var labelX = Position.X;
            var labelY = Position.Y - 20; // Above the bar
            renderer.DrawText(Label, labelX, labelY, LabelColor);
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
    /// Sets the value and fires the OnValueChanged event.
    /// </summary>
    public void SetValue(float newValue)
    {
        var clampedValue = Math.Clamp(newValue, 0f, 1f);
        if (Math.Abs(_value - clampedValue) > 0.0001f)
        {
            _value = clampedValue;
            OnValueChanged?.Invoke(_value);
        }
    }

    /// <summary>
    /// Sets the value from a percentage (0-100).
    /// </summary>
    public void SetPercentage(float percentage)
    {
        SetValue(percentage / 100f);
    }
}

/// <summary>
/// Direction the progress bar fills.
/// </summary>
public enum ProgressBarDirection
{
    LeftToRight,
    RightToLeft,
    BottomToTop,
    TopToBottom
}
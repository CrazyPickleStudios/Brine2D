using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Progress bar UI component for displaying progress/percentage.
/// </summary>
public class UIProgressBar : IUIComponent, IAnchoredUIComponent
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
    /// Current value, clamped to [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// Setting this property fires <see cref="OnValueChanged"/> when the clamped value differs.
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, MinValue, MaxValue);
            if (Math.Abs(_value - clamped) > 0.0001f)
            {
                _value = clamped;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Minimum value of the range. Defaults to 0.
    /// </summary>
    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    /// Maximum value of the range. Defaults to 1.
    /// Setting this below <see cref="MinValue"/> is not supported.
    /// </summary>
    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            _value = Math.Clamp(_value, _minValue, _maxValue);
        }
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
    /// Format string for the progress text. The token <c>{0}</c> is replaced with the
    /// current percentage (0–100). Defaults to <c>"{0:0}%"</c>.
    /// Ignored when <see cref="ProgressTextProvider"/> is set.
    /// </summary>
    public string PercentageFormat { get; set; } = "{0:0}%";

    /// <summary>
    /// Custom text provider for the progress label. Receives <c>(currentValue, maxValue)</c>
    /// and returns the display string. Overrides <see cref="PercentageFormat"/> when set.
    /// </summary>
    public Func<float, float, string>? ProgressTextProvider { get; set; }

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
    /// Optional font for rendering text (null = renderer default).
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Direction the bar fills.
    /// </summary>
    public ProgressBarDirection Direction { get; set; } = ProgressBarDirection.LeftToRight;

    /// <summary>
    /// Border thickness in pixels. Defaults to 2.
    /// </summary>
    public float BorderThickness { get; set; } = 2f;

    /// <summary>
    /// Event fired when value changes.
    /// </summary>
    public event Action<float>? OnValueChanged;

    private float _value;
    private float _minValue = 0f;
    private float _maxValue = 1f;

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
        float range = Math.Max(0.0001f, MaxValue - MinValue);
        float fillAmount = Math.Clamp((_value - MinValue) / range, 0f, 1f);
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
        renderer.DrawRectangleFilled(Position.X, Position.Y, Size.X, BorderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y + Size.Y - BorderThickness, Size.X, BorderThickness, BorderColor);
        renderer.DrawRectangleFilled(Position.X, Position.Y, BorderThickness, Size.Y, BorderColor);
        renderer.DrawRectangleFilled(Position.X + Size.X - BorderThickness, Position.Y, BorderThickness, Size.Y, BorderColor);

        // Draw percentage text if enabled
        if (ShowPercentage)
        {
            var percentageText = ProgressTextProvider != null
                ? ProgressTextProvider(_value, MaxValue)
                : string.Format(PercentageFormat, fillAmount * 100f);
            Vector2 textSize;
            float textX, textY;
            {
                var opts = new TextRenderOptions { Color = TextColor, Font = Font, LineSpacing = 1.0f };
                textSize = renderer.MeasureText(percentageText, opts);
                textX = MathF.Round(Position.X + (Size.X - textSize.X) / 2f);
                textY = MathF.Round(Position.Y + (Size.Y - textSize.Y) / 2f);
                renderer.DrawText(percentageText, textX, textY, opts);
            }
        }

        if (!string.IsNullOrEmpty(Label))
        {
            var labelOpts = new TextRenderOptions { Color = LabelColor, Font = Font };
            var labelSize = renderer.MeasureText(Label, labelOpts);
            var labelX = Position.X;
            var labelY = Position.Y - labelSize.Y - 4f;
            renderer.DrawText(Label, labelX, labelY, labelOpts);
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
    /// Sets the value and fires <see cref="OnValueChanged"/> when the value differs.
    /// Equivalent to setting the <see cref="Value"/> property directly.
    /// </summary>
    public void SetValue(float newValue)
    {
        Value = newValue;
    }

    /// <summary>
    /// Sets the value from a percentage (0–100), mapped onto [<see cref="MinValue"/>, <see cref="MaxValue"/>].
    /// </summary>
    public void SetPercentage(float percentage)
    {
        Value = MinValue + (percentage / 100f) * (MaxValue - MinValue);
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
using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Corner of the screen where <see cref="UICanvas"/> stacks toast notifications.
/// </summary>
public enum ToastAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

/// <summary>
/// Short-lived notification managed by <see cref="UICanvas"/>.
/// Show via <see cref="UICanvas.ShowToast"/>; it fades in, stays visible, then fades out
/// and removes itself. Use <see cref="UICanvas.DismissToast"/> to remove it early.
/// </summary>
public sealed class UIToast
{
    /// <summary>
    /// Message text to display.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// How long the toast stays fully visible, in seconds.
    /// </summary>
    public float Duration { get; set; } = 3f;

    /// <summary>
    /// Time in seconds to fade from transparent to fully opaque. 0 = instant.
    /// </summary>
    public float FadeInTime { get; set; } = 0.2f;

    /// <summary>
    /// Time in seconds to fade from fully opaque to transparent before dismissal. 0 = instant.
    /// </summary>
    public float FadeOutTime { get; set; } = 0.4f;

    /// <summary>
    /// Toast panel background colour (alpha is further multiplied by <see cref="Alpha"/>).
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(30, 30, 30, 220);

    /// <summary>
    /// Border colour.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(160, 160, 160, 255);

    /// <summary>
    /// Text colour.
    /// </summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Width of the toast panel in pixels. Text wraps inside the padding.
    /// </summary>
    public float Width { get; set; } = 260f;

    /// <summary>
    /// Padding between the panel edge and the text.
    /// </summary>
    public float Padding { get; set; } = 10f;

    /// <summary>
    /// Optional font for the toast text. Null = renderer default.
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Current opacity driven by the canvas (0 = transparent, 1 = opaque).
    /// </summary>
    public float Alpha { get; internal set; } = 0f;

    /// <summary>
    /// Computed panel height after the last <see cref="Render"/> call. Zero until first render.
    /// </summary>
    public float Height { get; internal set; } = 0f;

    /// <summary>
    /// Fired once when the toast is fully removed (either naturally or via
    /// <see cref="UICanvas.DismissToast"/>).
    /// </summary>
    public event Action? OnDismissed;

    private float _elapsed = 0f;
    private bool _dismissRequested = false;

    internal float TotalLifetime => FadeInTime + Duration + FadeOutTime;

    internal bool IsExpired => _elapsed >= TotalLifetime || (_dismissRequested && Alpha <= 0f);

    /// <summary>
    /// Advances the toast lifetime and updates <see cref="Alpha"/>.
    /// </summary>
    internal void Update(float deltaTime)
    {
        if (_dismissRequested)
        {
            _elapsed = Math.Max(_elapsed, FadeInTime + Duration);
        }

        _elapsed += deltaTime;

        float fadeOutStart = FadeInTime + Duration;

        if (_elapsed < FadeInTime)
        {
            Alpha = FadeInTime > 0f ? _elapsed / FadeInTime : 1f;
        }
        else if (_elapsed < fadeOutStart)
        {
            Alpha = 1f;
        }
        else
        {
            float fadeOutElapsed = _elapsed - fadeOutStart;
            Alpha = FadeOutTime > 0f ? Math.Max(0f, 1f - fadeOutElapsed / FadeOutTime) : 0f;
        }
    }

    internal void RequestDismiss() => _dismissRequested = true;

    internal void FireDismissed() => OnDismissed?.Invoke();

    internal void Render(IRenderer renderer, Vector2 position)
    {
        if (Alpha <= 0f) return;

        var textOpts = new TextRenderOptions
        {
            Color = ApplyAlpha(TextColor, Alpha),
            Font = Font,
            MaxWidth = Width - Padding * 2f
        };

        var textSize = renderer.MeasureText(Text, textOpts);
        Height = textSize.Y + Padding * 2f;

        var bg = ApplyAlpha(BackgroundColor, Alpha);
        var border = ApplyAlpha(BorderColor, Alpha);

        renderer.DrawRectangleFilled(position.X, position.Y, Width, Height, bg);
        renderer.DrawRectangleOutline(position.X, position.Y, Width, Height, border);
        renderer.DrawText(Text, position.X + Padding, position.Y + Padding, textOpts);
    }

    private static Color ApplyAlpha(Color c, float alpha) =>
        new Color(c.R, c.G, c.B, (byte)(c.A * alpha));
}

using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// World-space text label projected to screen space by <see cref="UICanvas"/> each frame.
/// Typical uses: floating name tags, damage numbers, waypoint markers.
/// </summary>
/// <remarks>
/// Add via <see cref="UICanvas.AddWorldComponent"/> and set <see cref="WorldPosition"/>
/// to the world-space anchor point (e.g. the entity's position). The canvas converts
/// this to screen coordinates using <see cref="UICanvas.WorldCamera"/> before rendering.
/// Use <see cref="ScreenOffset"/> to nudge the label relative to its projected point
/// (e.g. <c>new Vector2(-Size.X / 2, -40)</c> to centre it 40 px above).
/// </remarks>
public class UIWorldLabel : IUIWorldComponent
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
    public Vector2 WorldPosition { get; set; }

    /// <inheritdoc />
    public Vector2 ScreenOffset { get; set; }

    /// <inheritdoc />
    public bool CullWhenOffScreen { get; set; } = true;

    // ── Appearance ────────────────────────────────────────────────────────────

    /// <summary>Text to display.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Text color. Defaults to white.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Font. <c>null</c> uses the renderer default.</summary>
    public IFont? Font { get; set; }

    /// <summary>Maximum text width before wrapping. 0 = no wrap.</summary>
    public float MaxWidth { get; set; } = 0f;

    /// <summary>
    /// Optional background padding around the text. When &gt; 0, a filled
    /// rectangle is drawn behind the text using <see cref="BackgroundColor"/>.
    /// </summary>
    public float BackgroundPadding { get; set; } = 0f;

    /// <summary>Background fill color (only drawn when <see cref="BackgroundPadding"/> &gt; 0).</summary>
    public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 160);

    /// <summary>Optional border color drawn around the background (requires <see cref="BackgroundPadding"/> &gt; 0).</summary>
    public Color? BorderColor { get; set; }

    /// <summary>Border thickness. Only used when <see cref="BorderColor"/> is set.</summary>
    public float BorderThickness { get; set; } = 1f;

    // ── IUIComponent ──────────────────────────────────────────────────────────

    public void Update(float deltaTime) { }

    public void Render(IRenderer renderer)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        var options = new TextRenderOptions
        {
            Color = TextColor,
            Font = Font,
            MaxWidth = MaxWidth > 0f ? MaxWidth : (float?)null
        };

        Size = renderer.MeasureText(Text, options);

        if (BackgroundPadding > 0f)
        {
            var bgX = Position.X - BackgroundPadding;
            var bgY = Position.Y - BackgroundPadding;
            var bgW = Size.X + BackgroundPadding * 2f;
            var bgH = Size.Y + BackgroundPadding * 2f;
            renderer.DrawRectangleFilled(bgX, bgY, bgW, bgH, BackgroundColor);

            if (BorderColor.HasValue)
                renderer.DrawRectangleOutline(bgX, bgY, bgW, bgH, BorderColor.Value, BorderThickness);
        }

        renderer.DrawText(Text, Position.X, Position.Y, options);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X && screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y && screenPosition.Y <= Position.Y + Size.Y;
    }
}

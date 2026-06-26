using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// A text label that renders markup through <see cref="TextRenderOptions.ParseMarkup"/>.
/// Use instead of <see cref="UILabel"/> when you need inline colour, bold/italic,
/// word-wrap, or alignment.
/// </summary>
public class UIRichTextLabel : IUIComponent, IAnchoredUIComponent
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
    /// Markup text to display. Parsed according to <see cref="MarkupParser"/>.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Default text colour (applied where no inline colour tag overrides it).
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Font to use for rendering. When null, the renderer's default font is used.
    /// </summary>
    public IFont? Font { get; set; }

    /// <summary>
    /// Maximum pixel width before text wraps. 0 = no wrapping.
    /// </summary>
    public float MaxWidth { get; set; } = 0f;

    /// <summary>
    /// Maximum pixel height for vertical alignment. 0 = no constraint.
    /// </summary>
    public float MaxHeight { get; set; } = 0f;

    /// <summary>
    /// Horizontal text alignment within <see cref="MaxWidth"/>. Only meaningful when
    /// <see cref="MaxWidth"/> is non-zero.
    /// </summary>
    public TextAlignment HorizontalAlign { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Vertical alignment within <see cref="MaxHeight"/>. Only meaningful when
    /// <see cref="MaxHeight"/> is non-zero.
    /// </summary>
    public VerticalAlignment VerticalAlign { get; set; } = VerticalAlignment.Top;

    /// <summary>
    /// Line spacing multiplier (1.0 = normal, 1.2 = 120% spacing).
    /// </summary>
    public float LineSpacing { get; set; } = 1.2f;

    /// <summary>
    /// Drop-shadow offset in pixels. Null disables the shadow.
    /// </summary>
    public Vector2? ShadowOffset { get; set; }

    /// <summary>
    /// Drop-shadow colour. Only used when <see cref="ShadowOffset"/> is non-null.
    /// </summary>
    public Color ShadowColor { get; set; } = new Color(0, 0, 0, 128);

    /// <summary>
    /// The markup parser to use. When null, the renderer falls back to its default
    /// BBCode parser.
    /// </summary>
    public IMarkupParser? MarkupParser { get; set; }

    /// <summary>
    /// Fired when the label is clicked.
    /// </summary>
    public event Action? OnClick;

    /// <summary>
    /// Creates a rich text label at <paramref name="position"/>.
    /// </summary>
    public UIRichTextLabel(string text, Vector2 position)
    {
        Text = text;
        Position = position;
        Size = new Vector2(text.Length * 8, 16);
    }

    public void Update(float deltaTime)
    {
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible || string.IsNullOrEmpty(Text)) return;

        var options = new TextRenderOptions
        {
            Color = Color,
            Font = Font,
            MaxWidth = MaxWidth > 0f ? MaxWidth : (float?)null,
            MaxHeight = MaxHeight > 0f ? MaxHeight : (float?)null,
            HorizontalAlign = HorizontalAlign,
            VerticalAlign = VerticalAlign,
            LineSpacing = LineSpacing,
            ShadowOffset = ShadowOffset,
            ShadowColor = ShadowColor,
            ParseMarkup = true,
            MarkupParser = MarkupParser
        };

        Size = renderer.MeasureText(Text, options);
        renderer.DrawText(Text, Position.X, Position.Y, options);
    }

    public bool Contains(Vector2 screenPosition)
    {
        return screenPosition.X >= Position.X &&
               screenPosition.X <= Position.X + Size.X &&
               screenPosition.Y >= Position.Y &&
               screenPosition.Y <= Position.Y + Size.Y;
    }

    internal bool HasOnClick => OnClick != null;

    internal void Click()
    {
        if (Enabled)
            OnClick?.Invoke();
    }
}

using Brine2D.Core;
using System.Numerics;
using Brine2D.Rendering;
using Brine2D.Rendering.Text;

namespace Brine2D.UI;

/// <summary>
/// Simple text label UI component.
/// </summary>
public class UILabel : IUIComponent, IAnchoredUIComponent
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
    /// Text to display.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text color.
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
    /// Fired when the label is clicked.
    /// </summary>
    public event Action? OnClick;

    /// <summary>
    /// Creates a label at <paramref name="position"/> with the given <paramref name="text"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IUIComponent.Size"/> is an approximation until the first <see cref="Render"/>
    /// call, so hit-testing before the first render may be slightly off.
    /// </remarks>
    public UILabel(string text, Vector2 position)
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
            MaxWidth = MaxWidth > 0f ? MaxWidth : (float?)null
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

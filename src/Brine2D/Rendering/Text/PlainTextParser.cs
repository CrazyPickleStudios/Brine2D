namespace Brine2D.Rendering.Text;

/// <summary>
/// A no-op parser that treats all text as plain text (no markup processing).
/// Useful when you want to guarantee that special characters are rendered literally.
/// </summary>
public sealed class PlainTextParser : IMarkupParser
{
    public string FormatName => "PlainText";
    
    /// <inheritdoc/>
    public IReadOnlyList<TextRun> Parse(string markup, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(markup))
            return Array.Empty<TextRun>();
        
        return new[]
        {
            new TextRun
            {
                Text = markup,
                Color = options.Color,
                Font = options.Font,
                FontSize = options.FontSize,
                Style = TextStyle.Normal,
                SourceIndex = 0
            }
        };
    }
}
using System.Text;
using Brine2D.Core;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.Text;

/// <summary>
/// Parses BBCode-style markup into styled text runs.
/// Supported tags: [color=#RRGGBB], [size=N], [b], [i], [u], [s]
/// </summary>
/// <example>
/// <code>
/// var parser = new BBCodeParser();
/// var runs = parser.Parse(
///     "Hello [color=#FF0000]World[/color]!", 
///     new TextRenderOptions());
/// </code>
/// </example>
public sealed class BBCodeParser : IMarkupParser
{
    private readonly ILogger? _logger;
    
    public string FormatName => "BBCode";
    
    public BBCodeParser(ILogger? logger = null)
    {
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public IReadOnlyList<TextRun> Parse(string markup, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(markup))
            return Array.Empty<TextRun>();
        
        if (!options.ParseMarkup)
        {
            // Treat as plain text
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
        
        var runs = new List<TextRun>();
        var styleStack = new Stack<StyleState>();
        var currentState = new StyleState
        {
            Color = options.Color,
            Font = options.Font,
            FontSize = options.FontSize,
            Style = TextStyle.Normal
        };
        
        var textBuilder = new StringBuilder();
        int index = 0;
        int sourceIndex = 0;
        
        while (index < markup.Length)
        {
            if (markup[index] == '[')
            {
                // Flush current text before processing tag
                if (textBuilder.Length > 0)
                {
                    runs.Add(CreateRun(textBuilder.ToString(), currentState, sourceIndex));
                    textBuilder.Clear();
                }
                
                sourceIndex = index;
                
                // Try to parse tag
                if (TryParseTag(markup, ref index, ref currentState, styleStack))
                {
                    continue;
                }
            }
            
            // Regular character or unparseable '['
            textBuilder.Append(markup[index]);
            index++;
        }
        
        // Flush remaining text
        if (textBuilder.Length > 0)
        {
            runs.Add(CreateRun(textBuilder.ToString(), currentState, sourceIndex));
        }
        
        return runs;
    }
    
    private bool TryParseTag(string markup, ref int index, ref StyleState state, Stack<StyleState> stack)
    {
        int tagStart = index;
        int tagEnd = markup.IndexOf(']', tagStart);
        
        if (tagEnd == -1)
        {
            // No closing bracket - treat as literal '['
            return false;
        }
        
        string tag = markup.Substring(tagStart + 1, tagEnd - tagStart - 1);
        index = tagEnd + 1;
        
        // Closing tags
        if (tag.StartsWith('/'))
        {
            string tagName = tag.Substring(1).ToLowerInvariant();
            
            if (stack.Count > 0 && ShouldPopStack(tagName))
            {
                state = stack.Pop();
                return true;
            }
            
            _logger?.LogWarning("Unmatched closing tag: [{Tag}]", tag);
            return true;
        }
        
        // Opening tags
        if (TryApplyTag(tag, ref state, stack))
        {
            return true;
        }
        
        _logger?.LogWarning("Unknown markup tag: [{Tag}]", tag);
        return true;
    }
    
    private bool TryApplyTag(string tag, ref StyleState state, Stack<StyleState> stack)
    {
        string tagLower = tag.ToLowerInvariant();
        
        // Color tag: [color=#RRGGBB] or [color=#RRGGBBAA]
        if (tagLower.StartsWith("color="))
        {
            string colorValue = tag.Substring(6);
            if (TryParseColor(colorValue, out Color color))
            {
                stack.Push(state);
                state = state with { Color = color };
                return true;
            }
        }
        
        // Size tag: [size=24] (absolute font size in points)
        if (tagLower.StartsWith("size="))
        {
            string sizeValue = tag.Substring(5);
            if (float.TryParse(sizeValue, out float fontSize) && fontSize > 0)
            {
                stack.Push(state);
                state = state with { FontSize = fontSize };
                return true;
            }
        }
        
        // Style tags
        switch (tagLower)
        {
            case "b":
                stack.Push(state);
                state = state with { Style = state.Style | TextStyle.Bold };
                return true;
                
            case "i":
                stack.Push(state);
                state = state with { Style = state.Style | TextStyle.Italic };
                return true;
                
            case "u":
                stack.Push(state);
                state = state with { Style = state.Style | TextStyle.Underline };
                return true;
                
            case "s":
                stack.Push(state);
                state = state with { Style = state.Style | TextStyle.Strikethrough };
                return true;
        }
        
        return false;
    }
    
    private static bool ShouldPopStack(string tagName)
    {
        return tagName is "color" or "size" or "b" or "i" or "u" or "s";
    }
    
    private static bool TryParseColor(string value, out Color color)
    {
        color = Color.White;
        
        if (!value.StartsWith('#'))
            return false;
        
        // Support #RGB, #RRGGBB, #RRGGBBAA
        string hex = value.Substring(1);
        
        try
        {
            if (hex.Length == 3)
            {
                // #RGB -> #RRGGBB
                byte r = Convert.ToByte(new string(hex[0], 2), 16);
                byte g = Convert.ToByte(new string(hex[1], 2), 16);
                byte b = Convert.ToByte(new string(hex[2], 2), 16);
                color = new Color(r, g, b, 255);
                return true;
            }
            else if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                color = new Color(r, g, b, 255);
                return true;
            }
            else if (hex.Length == 8)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                byte a = Convert.ToByte(hex.Substring(6, 2), 16);
                color = new Color(r, g, b, a);
                return true;
            }
        }
        catch
        {
            return false;
        }
        
        return false;
    }
    
    private static TextRun CreateRun(string text, StyleState state, int sourceIndex)
    {
        return new TextRun
        {
            Text = text,
            Color = state.Color,
            Font = state.Font,
            FontSize = state.FontSize,
            Style = state.Style,
            SourceIndex = sourceIndex
        };
    }
    
    private record struct StyleState
    {
        public Color Color;
        public IFont? Font;
        public float FontSize;
        public TextStyle Style;
    }
}
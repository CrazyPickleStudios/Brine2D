using System.Globalization;
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
        var styleStack = new Stack<StackEntry>();
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
                if (textBuilder.Length > 0)
                {
                    runs.Add(CreateRun(textBuilder.ToString(), currentState, sourceIndex));
                    textBuilder.Clear();
                }
                
                sourceIndex = index;
                
                if (TryParseTag(markup, ref index, ref currentState, styleStack))
                {
                    sourceIndex = index;
                    continue;
                }
            }
            
            textBuilder.Append(markup[index]);
            index++;
        }
        
        if (textBuilder.Length > 0)
        {
            runs.Add(CreateRun(textBuilder.ToString(), currentState, sourceIndex));
        }
        
        return runs;
    }
    
    private bool TryParseTag(string markup, ref int index, ref StyleState state, Stack<StackEntry> stack)
    {
        int tagStart = index;
        int tagEnd = markup.IndexOf(']', tagStart);
        
        if (tagEnd == -1)
            return false;
        
        string tag = markup.Substring(tagStart + 1, tagEnd - tagStart - 1);
        
        if (tag.StartsWith('/'))
        {
            string tagName = tag[1..].ToLowerInvariant();
            
            if (!IsKnownTag(tagName))
                return false;
            
            index = tagEnd + 1;
            
            var entries = stack.ToArray();
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].TagName == tagName)
                {
                    StyleState restored = state;
                    for (int j = 0; j <= i; j++)
                        restored = stack.Pop().PreviousState;
                    state = restored;
                    return true;
                }
            }
            
            _logger?.LogWarning("Unmatched closing tag: [{Tag}]", tag);
            return true;
        }
        
        if (TryApplyTag(tag, ref state, stack))
        {
            index = tagEnd + 1;
            return true;
        }
        
        return false;
    }
    
    private static bool TryApplyTag(string tag, ref StyleState state, Stack<StackEntry> stack)
    {
        string tagLower = tag.ToLowerInvariant();
        
        if (tagLower.StartsWith("color="))
        {
            string colorValue = tag[6..];
            if (TryParseColor(colorValue, out Color color))
            {
                stack.Push(new StackEntry("color", state));
                state = state with { Color = color };
                return true;
            }
            return false;
        }
        
        if (tagLower.StartsWith("size="))
        {
            string sizeValue = tag[5..];
                if (float.TryParse(sizeValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float fontSize) && fontSize > 0)
            {
                stack.Push(new StackEntry("size", state));
                state = state with { FontSize = fontSize };
                return true;
            }
            return false;
        }
        
        switch (tagLower)
        {
            case "b":
                stack.Push(new StackEntry("b", state));
                state = state with { Style = state.Style | TextStyle.Bold };
                return true;
                
            case "i":
                stack.Push(new StackEntry("i", state));
                state = state with { Style = state.Style | TextStyle.Italic };
                return true;
                
            case "u":
                stack.Push(new StackEntry("u", state));
                state = state with { Style = state.Style | TextStyle.Underline };
                return true;
                
            case "s":
                stack.Push(new StackEntry("s", state));
                state = state with { Style = state.Style | TextStyle.Strikethrough };
                return true;
        }
        
        return false;
    }
    
    private static bool IsKnownTag(string tagName)
    {
        return tagName is "color" or "size" or "b" or "i" or "u" or "s";
    }
    
    private static bool TryParseColor(string value, out Color color)
    {
        color = Color.White;
        
        if (!value.StartsWith('#'))
            return false;
        
        string hex = value[1..];
        
        try
        {
            if (hex.Length == 3)
            {
                byte r = Convert.ToByte(new string(hex[0], 2), 16);
                byte g = Convert.ToByte(new string(hex[1], 2), 16);
                byte b = Convert.ToByte(new string(hex[2], 2), 16);
                color = new Color(r, g, b, 255);
                return true;
            }
            else if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex[..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                color = new Color(r, g, b, 255);
                return true;
            }
            else if (hex.Length == 8)
            {
                byte r = Convert.ToByte(hex[..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                byte a = Convert.ToByte(hex[6..8], 16);
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
    
    private record struct StackEntry(string TagName, StyleState PreviousState);
    
    private record struct StyleState
    {
        public Color Color;
        public IFont? Font;
        public float FontSize;
        public TextStyle Style;
    }
}
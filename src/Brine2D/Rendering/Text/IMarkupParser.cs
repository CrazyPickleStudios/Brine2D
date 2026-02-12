namespace Brine2D.Rendering.Text;

/// <summary>
/// Interface for parsing markup text into styled text runs.
/// Implement this to support custom markup formats (Markdown, HTML, etc.).
/// </summary>
public interface IMarkupParser
{
    /// <summary>
    /// Parse markup text into a collection of styled text runs.
    /// </summary>
    /// <param name="markup">Raw text with markup tags</param>
    /// <param name="options">Rendering options (provides default styling)</param>
    /// <returns>Collection of text runs with applied styles</returns>
    IReadOnlyList<TextRun> Parse(string markup, TextRenderOptions options);
    
    /// <summary>
    /// Gets the name of the markup format this parser supports.
    /// </summary>
    /// <example>"BBCode", "Markdown", "HTML", "Custom"</example>
    string FormatName { get; }
}
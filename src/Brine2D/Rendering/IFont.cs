namespace Brine2D.Rendering;

/// <summary>
/// Represents a loaded font.
/// </summary>
public interface IFont : IDisposable
{
    /// <summary>
    /// Font name or path.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Font size in points.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Whether the font is loaded.
    /// </summary>
    bool IsLoaded { get; }
}
namespace Brine2D.Rendering;

/// <summary>
/// Represents a loaded font that can be used for text rendering.
/// </summary>
public interface IFont : IDisposable
{
    /// <summary>Gets the name or path of the font.</summary>
    string Name { get; }

    /// <summary>Gets the font size in points.</summary>
    int Size { get; }

    /// <summary>Gets whether the font is loaded and ready to use.</summary>
    bool IsLoaded { get; }
}
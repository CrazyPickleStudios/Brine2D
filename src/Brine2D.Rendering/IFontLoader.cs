namespace Brine2D.Rendering;

/// <summary>
/// Service for loading and managing fonts.
/// </summary>
public interface IFontLoader : IDisposable
{
    /// <summary>
    /// Loads a font from file.
    /// </summary>
    /// <param name="path">Path to the font file (.ttf, .otf).</param>
    /// <param name="size">Font size in points.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded font.</returns>
    Task<IFont> LoadFontAsync(string path, int size, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a font and frees resources.
    /// </summary>
    void UnloadFont(IFont font);
}
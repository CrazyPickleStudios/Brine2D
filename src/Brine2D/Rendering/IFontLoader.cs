namespace Brine2D.Rendering;

/// <summary>
/// Low-level font loading interface.
/// <para><strong>For most use cases, use <see cref="IAssetLoader"/> instead.</strong></para>
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
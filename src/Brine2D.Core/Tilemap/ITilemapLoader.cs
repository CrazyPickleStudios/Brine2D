namespace Brine2D.Core.Tilemap;

/// <summary>
/// Interface for loading tilemaps from various formats.
/// </summary>
public interface ITilemapLoader
{
    /// <summary>
    /// Loads a tilemap from a file.
    /// </summary>
    /// <param name="path">Path to the tilemap file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded tilemap.</returns>
    Task<Tilemap> LoadAsync(string path, CancellationToken cancellationToken = default);
}
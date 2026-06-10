namespace Brine2D.Tilemap;

public interface ITilemapLoader
{
    Task<Tilemap> LoadAsync(string path, CancellationToken cancellationToken = default);
}
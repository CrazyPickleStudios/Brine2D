namespace Brine2D.Content;

public interface IAssetLoader<TAsset>
    where TAsset : class, IDisposable
{
    Task<TAsset> LoadAsync(string path, CancellationToken ct = default);
    Task<TAsset> LoadAsync(Stream stream, CancellationToken ct = default);
}
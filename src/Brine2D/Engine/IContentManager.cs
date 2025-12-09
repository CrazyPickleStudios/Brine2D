namespace Brine2D.Engine;

public interface IContentManager : IDisposable
{
    void Clear();

    Task<T> LoadAsync<T>(string key, string path, CancellationToken ct = default) where T : class, IDisposable;
    Task<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : class, IDisposable;
    Task<T?> TryLoadAsync<T>(string key, string path, CancellationToken ct = default) where T : class, IDisposable;
    Task<T?> TryLoadAsync<T>(string path, CancellationToken ct = default) where T : class, IDisposable;

    bool TryGet<T>(string key, out T? asset) where T : class, IDisposable;
    void Unload(string key);
}
namespace Brine2D.Rendering;

/// <summary>
/// No-op font loader for headless mode (servers, testing).
/// Returns stub fonts; all operations are silently ignored.
/// </summary>
internal sealed class HeadlessFontLoader : IFontLoader
{
    public Task<IFont> LoadFontAsync(string path, int size, CancellationToken cancellationToken = default)
        => Task.FromResult<IFont>(new HeadlessFont(path, size));

    public void UnloadFont(IFont font) => font?.Dispose();

    public void Dispose() { }

    private sealed class HeadlessFont(string name, int size) : IFont
    {
        public string Name { get; } = name;
        public int Size { get; } = size;
        public bool IsLoaded => true;
        public void Dispose() { }
    }
}
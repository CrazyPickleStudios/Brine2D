namespace Brine2D.Rendering;

/// <summary>
/// No-op texture loader for headless mode (servers, testing).
/// Returns zero-size stub textures; all operations are silently ignored.
/// </summary>
internal sealed class HeadlessTextureLoader : ITextureLoader
{
    public Task<ITexture> LoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ITexture>(new HeadlessTexture(path, scaleMode));

    public ITexture LoadTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear)
        => new HeadlessTexture(path, scaleMode);

    public ITexture CreateTexture(int width, int height, TextureScaleMode scaleMode = TextureScaleMode.Linear)
        => new HeadlessTexture($"created:{width}x{height}", scaleMode);

    public void UnloadTexture(ITexture texture) { }

    public void Dispose() { }

    private sealed class HeadlessTexture(string source, TextureScaleMode scaleMode) : ITexture
    {
        public int Width => 0;
        public int Height => 0;
        public string Source { get; } = source;
        public bool IsLoaded => true;
        public TextureScaleMode ScaleMode { get; } = scaleMode;
        public void Dispose() { }
    }
}
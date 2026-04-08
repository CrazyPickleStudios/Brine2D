namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Lightweight texture view for render targets.
/// Does not own the GPU handle - parent RenderTarget handles disposal.
/// Reads the handle indirectly so it sees <see cref="nint.Zero"/> after the parent is disposed.
/// </summary>
internal sealed class RenderTargetTextureView : ITexture
{
    private readonly Func<nint> _handleAccessor;

    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsLoaded => _handleAccessor() != nint.Zero;
    public TextureScaleMode ScaleMode { get; }
    public int SortKey { get; }

    internal nint Handle => _handleAccessor();

    internal RenderTargetTextureView(string name, Func<nint> handleAccessor, int width, int height, TextureScaleMode scaleMode)
    {
        Name = name;
        _handleAccessor = handleAccessor;
        Width = width;
        Height = height;
        ScaleMode = scaleMode;
        SortKey = ITexture.NextSortKey();
    }

    public void Dispose()
    {
    }
}
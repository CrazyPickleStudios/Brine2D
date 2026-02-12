namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Lightweight texture view for render targets.
/// Does not own the GPU handle - parent RenderTarget handles disposal.
/// </summary>
internal sealed class RenderTargetTextureView : ITexture
{
    private readonly nint _handle;
    
    public string Source { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsLoaded => true;
    public TextureScaleMode ScaleMode { get; }
    
    internal nint Handle => _handle;
    
    internal RenderTargetTextureView(string source, nint handle, int width, int height, TextureScaleMode scaleMode)
    {
        Source = source;
        _handle = handle;
        Width = width;
        Height = height;
        ScaleMode = scaleMode;
    }
    
    public void Dispose()
    {
        // No-op: RenderTarget owns the GPU handle
    }
}
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 GPU API texture implementation.
/// </summary>
public class SDL3GPUTexture : ITexture
{
    private readonly ILogger<SDL3GPUTexture> _logger;
    private nint _textureHandle;
    private bool _disposed;

    public string Source { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsLoaded => _textureHandle != nint.Zero && !_disposed;
    public TextureScaleMode ScaleMode { get; set; }
    
    internal nint Handle => _textureHandle;

    public SDL3GPUTexture(
        string source,
        nint textureHandle,
        int width,
        int height,
        TextureScaleMode scaleMode,
        ILogger<SDL3GPUTexture> logger)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _textureHandle = textureHandle;
        Width = width;
        Height = height;
        ScaleMode = scaleMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_textureHandle != nint.Zero)
        {
            // Note: GPU textures need to be released with SDL_ReleaseGPUTexture
            // which requires the device handle. This is handled in the texture loader's Dispose
            _logger.LogDebug("Marking GPU texture for disposal: {Source}", Source);
            _textureHandle = nint.Zero;
        }

        _disposed = true;
    }
}
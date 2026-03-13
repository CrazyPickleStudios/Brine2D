using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering;

/// <summary>
/// SDL3 GPU API texture implementation.
/// </summary>
public class SDL3Texture : ITexture
{
    private readonly ILogger<SDL3Texture> _logger;
    private readonly nint _device;
    private nint _textureHandle;
    private bool _disposed;

    public string Source { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsLoaded => _textureHandle != nint.Zero && !_disposed;
    public TextureScaleMode ScaleMode { get; set; }

    internal nint Handle => _textureHandle;

    public SDL3Texture(
        string source,
        nint device,
        nint textureHandle,
        int width,
        int height,
        TextureScaleMode scaleMode,
        ILogger<SDL3Texture> logger)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _device = device;
        _textureHandle = textureHandle;
        Width = width;
        Height = height;
        ScaleMode = scaleMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_textureHandle != nint.Zero && _device != nint.Zero)
        {
            SDL3.SDL.ReleaseGPUTexture(_device, _textureHandle);
            _logger.LogDebug("Released GPU texture: {Source}", Source);
            _textureHandle = nint.Zero;
        }

        _disposed = true;
    }
}
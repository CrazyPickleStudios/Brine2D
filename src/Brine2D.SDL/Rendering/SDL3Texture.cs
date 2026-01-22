using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Rendering;

/// <summary>
/// SDL3 implementation of a texture.
/// </summary>
public class SDL3Texture : ITexture
{
    private readonly ILogger<SDL3Texture> _logger;
    private nint _texture;
    private bool _disposed;
    private TextureScaleMode _scaleMode;

    public int Width { get; }
    public int Height { get; }
    public string Source { get; }
    public bool IsLoaded => _texture != IntPtr.Zero && !_disposed;

    public TextureScaleMode ScaleMode
    {
        get => _scaleMode;
        set
        {
            if (_scaleMode != value && _texture != IntPtr.Zero)
            {
                _scaleMode = value;
                ApplyScaleMode();
            }
        }
    }

    internal nint Handle => _texture;

    public SDL3Texture(string source, nint texture, int width, int height, TextureScaleMode scaleMode, ILogger<SDL3Texture> logger)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _texture = texture;
        Width = width;
        Height = height;
        _scaleMode = scaleMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ApplyScaleMode();
        _logger.LogDebug("Texture created: {Source} ({Width}x{Height}), ScaleMode: {ScaleMode}", source, width, height, scaleMode);
    }

    private void ApplyScaleMode()
    {
        if (_texture == IntPtr.Zero) return;

        var sdlScaleMode = _scaleMode == TextureScaleMode.Nearest
            ? SDL3.SDL.ScaleMode.Nearest
            : SDL3.SDL.ScaleMode.Linear;

        SDL3.SDL.SetTextureScaleMode(_texture, sdlScaleMode);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_texture != IntPtr.Zero)
        {
            SDL3.SDL.DestroyTexture(_texture);
            _texture = IntPtr.Zero;
            _logger.LogDebug("Texture disposed: {Source}", Source);
        }

        _disposed = true;
    }
}
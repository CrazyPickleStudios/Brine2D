using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 implementation of a texture.
/// </summary>
public class SDL3Texture : ITexture
{
    private readonly ILogger<SDL3Texture> _logger;
    private nint _texture;
    private bool _disposed;

    public int Width { get; }
    public int Height { get; }
    public string Source { get; }
    public bool IsLoaded => _texture != IntPtr.Zero && !_disposed;

    internal nint Handle => _texture;

    public SDL3Texture(string source, nint texture, int width, int height, ILogger<SDL3Texture> logger)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        _texture = texture;
        Width = width;
        Height = height;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Texture created: {Source} ({Width}x{Height})", source, width, height);
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
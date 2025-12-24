using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL_ttf implementation of a font.
/// </summary>
public class SDL3Font : IFont
{
    private readonly ILogger<SDL3Font> _logger;
    private nint _fontHandle;
    private bool _disposed;

    public string Name { get; }
    public int Size { get; }
    public bool IsLoaded => _fontHandle != nint.Zero;

    /// <summary>
    /// Internal SDL font handle.
    /// </summary>
    internal nint Handle => _fontHandle;

    public SDL3Font(string name, int size, nint fontHandle, ILogger<SDL3Font> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Size = size;
        _fontHandle = fontHandle;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_fontHandle != nint.Zero)
        {
            SDL3.TTF.CloseFont(_fontHandle);
            _fontHandle = nint.Zero;
            _logger.LogDebug("Font unloaded: {Name} ({Size}pt)", Name, Size);
        }

        _disposed = true;
    }
}
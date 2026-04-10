using Microsoft.Extensions.Logging;
using System.Threading;

namespace Brine2D.Rendering;

/// <summary>
/// SDL_ttf implementation of a font.
/// </summary>
public class SDL3Font : IFont
{
    private readonly ILogger<SDL3Font> _logger;
    private nint _fontHandle;
    private int _disposed;

    public string Name { get; }
    public int Size { get; }
    public bool IsLoaded => Volatile.Read(ref _fontHandle) != nint.Zero && _disposed == 0;

    /// <summary>
    /// Internal SDL font handle.
    /// </summary>
    internal nint Handle => Volatile.Read(ref _fontHandle);

    internal SDL3Font(string name, int size, nint fontHandle, ILogger<SDL3Font> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Size = size;
        _fontHandle = fontHandle;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var handle = Interlocked.Exchange(ref _fontHandle, nint.Zero);
        if (handle != nint.Zero)
        {
            SDL3.TTF.CloseFont(handle);
            _logger.LogDebug("Font unloaded: {Name} ({Size}pt)", Name, Size);
        }
    }
}
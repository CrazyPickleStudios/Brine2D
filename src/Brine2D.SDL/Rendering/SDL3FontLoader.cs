using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Rendering;

/// <summary>
/// SDL_ttf implementation of font loader.
/// </summary>
public class SDL3FontLoader : IFontLoader, IDisposable
{
    private readonly ILogger<SDL3FontLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<SDL3Font> _loadedFonts = new();
    private bool _initialized;
    private bool _disposed;

    public SDL3FontLoader(ILogger<SDL3FontLoader> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;

        _logger.LogInformation("Initializing SDL_ttf");

        if (!SDL3.TTF.Init())
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL_ttf: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL_ttf: {error}");
        }

        _initialized = true;
        _logger.LogInformation("SDL_ttf initialized successfully");
    }

    public async Task<IFont> LoadFontAsync(string path, int size, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadFont(path, size), cancellationToken);
    }

    private IFont LoadFont(string path, int size)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Font file not found: {path}", path);

        if (size <= 0)
            throw new ArgumentException("Font size must be positive", nameof(size));

        _logger.LogInformation("Loading font: {Path} at {Size}pt", path, size);

        var fontHandle = SDL3.TTF.OpenFont(path, size);

        if (fontHandle == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load font {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load font: {error}");
        }

        var font = new SDL3Font(path, size, fontHandle, _loggerFactory.CreateLogger<SDL3Font>());
        _loadedFonts.Add(font);

        _logger.LogDebug("Font loaded: {Path} at {Size}pt", path, size);
        return font;
    }

    public void UnloadFont(IFont font)
    {
        if (font is SDL3Font sdlFont)
        {
            _loadedFonts.Remove(sdlFont);
            sdlFont.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var font in _loadedFonts.ToList())
        {
            font.Dispose();
        }
        _loadedFonts.Clear();

        if (_initialized)
        {
            SDL3.TTF.Quit();
            _initialized = false;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~SDL3FontLoader()
    {
        Dispose();
    }
}
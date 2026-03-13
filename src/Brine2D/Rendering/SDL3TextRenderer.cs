using Brine2D.Core;
using Brine2D.Rendering.Text;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Brine2D.Rendering;

namespace Brine2D.Rendering;

/// <summary>
/// Handles text rendering using font atlases and markup parsing.
/// </summary>
internal sealed class SDL3TextRenderer : IDisposable
{
    private readonly ILogger<SDL3TextRenderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFontLoader? _fontLoader;
    private readonly MarkupParser _markupParser;
    private readonly IMarkupParser _defaultMarkupParser;
    
    private Font? _defaultFont;
    private FontAtlas? _defaultFontAtlas;
    
    private bool _disposed;
    
    public Font? DefaultFont => _defaultFont;
    public FontAtlas? DefaultFontAtlas => _defaultFontAtlas;
    
    public SDL3TextRenderer(
        ILogger<SDL3TextRenderer> logger,
        ILoggerFactory loggerFactory,
        IFontLoader? fontLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _fontLoader = fontLoader;
        
        _markupParser = new MarkupParser(_logger);
        _defaultMarkupParser = new BBCodeParser(_logger);
    }
    
    public async Task LoadDefaultFontAsync(CancellationToken cancellationToken = default)
    {
        if (_fontLoader == null)
        {
            _logger.LogInformation("No font loader available, text rendering will require SetDefaultFont()");
            return;
        }

        try
        {
            var assembly = typeof(SDL3Renderer).Assembly;
            var resourceName = "Brine2D.SDL.Fonts.Roboto.ttf";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Default font not found in embedded resources");
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), "Brine2D", "Roboto.ttf");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogDebug("Font extracted to: {TempPath}", tempPath);

            var loadedFont = await _fontLoader.LoadFontAsync(tempPath, 16, cancellationToken);

            if (loadedFont is Font sdlFont)
            {
                _defaultFont = sdlFont;
                _logger.LogInformation("Default font loaded from embedded resource at 16pt");
            }
            else
            {
                _logger.LogWarning("Loaded font is not an SDL3Font");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load default font");
        }
    }
    
    public void SetDefaultFont(Font? font)
    {
        if (font != null && font is not Font)
        {
            _logger.LogWarning("Font must be an SDL3Font for GPU renderer");
            return;
        }

        _defaultFont = font as Font;

        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

        if (_defaultFont != null)
        {
            _logger.LogInformation("Default font set to {Font}, atlas will be generated on first use", _defaultFont.Name);
        }
    }
    
    public void EnsureFontAtlasGenerated(ITextureContext textureContext)
    {
        if (_defaultFont == null || _defaultFontAtlas != null)
            return;

        if (_defaultFont is not Font sdlFont)
        {
            _logger.LogWarning("Default font is not an SDL3Font, cannot generate atlas");
            return;
        }

        _logger.LogInformation("Generating font atlas for {Font}", sdlFont.Name);
        _defaultFontAtlas = new FontAtlas(_loggerFactory.CreateLogger<FontAtlas>());

        if (!_defaultFontAtlas.Generate(sdlFont, textureContext, TextureScaleMode.Nearest))
        {
            _logger.LogError("Failed to generate font atlas");
            _defaultFontAtlas?.Dispose();
            _defaultFontAtlas = null;
        }
    }
    
    public IReadOnlyList<TextRun> ParseText(string text, TextRenderOptions options)
    {
        var parser = options.ParseMarkup 
            ? (options.MarkupParser ?? _defaultMarkupParser)
            : new PlainTextParser();

        return parser.Parse(text, options);
    }
    
    public Vector2 MeasureText(string text, float? fontSize = null)
    {
        if (string.IsNullOrEmpty(text) || _defaultFont == null)
            return Vector2.Zero;

        if (_defaultFont is not Font sdlFont)
            return Vector2.Zero;

        // Simple SDL measurement for plain text
        if (SDL3.TTF.GetStringSize(sdlFont.Handle, text, 0, out int w, out int h))
        {
            float scale = fontSize.HasValue ? (fontSize.Value / _defaultFont.Size) : 1.0f;
            return new Vector2(w * scale, h * scale);
        }

        return Vector2.Zero;
    }
    
    public Vector2 MeasureTextWithOptions(string text, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        var runs = options.ParseMarkup
            ? _markupParser.Parse(text, options)
            : new[] { new TextRun { Text = text, FontSize = options.FontSize } };

        return MeasureTextRuns(runs);
    }
    
    public Vector2 MeasureTextRuns(IReadOnlyList<TextRun> runs)
    {
        float totalWidth = 0;
        float totalHeight = _defaultFontAtlas?.LineHeight ?? 16;

        foreach (var run in runs)
        {
            var size = MeasureText(run.Text, run.FontSize);
            totalWidth += size.X;
            totalHeight = MathF.Max(totalHeight, size.Y);
        }

        return new Vector2(totalWidth, totalHeight);
    }

    public Vector2 MeasureGlyphSpan(ReadOnlySpan<char> text)
    {
        if (_defaultFontAtlas == null) return Vector2.Zero;
        float width = 0;
        foreach (char c in text)
        {
            if (_defaultFontAtlas.TryGetGlyph(c, out var glyph))
                width += glyph.Advance;
        }
        return new Vector2(width, _defaultFontAtlas.LineHeight);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;
        
        _disposed = true;
    }
}
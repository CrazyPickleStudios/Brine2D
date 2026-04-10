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
    private static readonly PlainTextParser _plainTextParser = new();

    private readonly ILogger<SDL3TextRenderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFontLoader? _fontLoader;
    private readonly IMarkupParser _defaultMarkupParser;
    
    private IFont? _defaultFont;
    private FontAtlas? _defaultFontAtlas;
    private bool _ownsDefaultFont;
    private bool _fontAtlasGenerationFailed;
    
    private bool _disposed;
    
    public IFont? DefaultFont => _defaultFont;
    public FontAtlas? DefaultFontAtlas => _defaultFontAtlas;
    
    public SDL3TextRenderer(
        ILogger<SDL3TextRenderer> logger,
        ILoggerFactory loggerFactory,
        IFontLoader? fontLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _fontLoader = fontLoader;
        
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

            DisposeOwnedFont();
            _defaultFontAtlas?.Dispose();
            _defaultFontAtlas = null;
            _fontAtlasGenerationFailed = false;
            _defaultFont = loadedFont;
            _ownsDefaultFont = true;
            _logger.LogInformation("Default font loaded from embedded resource at 16pt");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load default font");
        }
    }
    
    public void SetDefaultFont(IFont? font)
    {
        DisposeOwnedFont();

        _defaultFont = font;
        _ownsDefaultFont = false;
        _fontAtlasGenerationFailed = false;

        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

        if (_defaultFont != null)
        {
            if (_defaultFont is not SDL3Font)
            {
                _logger.LogWarning(
                    "Font {Font} is not an SDL3Font — atlas generation and text measurement will not function",
                    _defaultFont.Name);
            }

            _logger.LogInformation("Default font set to {Font}, atlas will be generated on first use", _defaultFont.Name);
        }
    }
    
    public void EnsureFontAtlasGenerated(ITextureContext textureContext)
    {
        if (_defaultFont == null || _defaultFontAtlas != null || _fontAtlasGenerationFailed)
            return;

        if (_defaultFont is not SDL3Font sdlFont)
        {
            _logger.LogWarning("Default font is not an SDL3Font, cannot generate atlas");
            _fontAtlasGenerationFailed = true;
            return;
        }

        _logger.LogInformation("Generating font atlas for {Font}", sdlFont.Name);
        _defaultFontAtlas = new FontAtlas(_loggerFactory.CreateLogger<FontAtlas>());

        if (!_defaultFontAtlas.Generate(sdlFont, textureContext, TextureScaleMode.Nearest))
        {
            _logger.LogError("Failed to generate font atlas");
            _defaultFontAtlas?.Dispose();
            _defaultFontAtlas = null;
            _fontAtlasGenerationFailed = true;
        }
    }
    
    public IReadOnlyList<TextRun> ParseText(string text, TextRenderOptions options)
    {
        var parser = options.ParseMarkup 
            ? (options.MarkupParser ?? _defaultMarkupParser)
            : _plainTextParser;

        return parser.Parse(text, options);
    }

    public Vector2 MeasureText(string text, float? fontSize = null, float lineSpacing = 1.2f)
    {
        if (string.IsNullOrEmpty(text) || _defaultFont == null)
            return Vector2.Zero;

        float scale = fontSize.HasValue ? (fontSize.Value / _defaultFont.Size) : 1.0f;

        if (_defaultFontAtlas != null)
            return MeasureGlyphSpan(text.AsSpan(), scale, lineSpacing);

        if (_defaultFont is not SDL3Font sdlFont)
            return Vector2.Zero;

        if (!text.Contains('\n'))
        {
            if (SDL3.TTF.GetStringSize(sdlFont.Handle, text, 0, out int w, out int h))
                return new Vector2(w * scale, h * scale * lineSpacing);

            return Vector2.Zero;
        }

        var lines = text.Split('\n');
        float maxWidth = 0;
        float singleLineHeight = 0;

        foreach (var line in lines)
        {
            if (line.Length > 0 && SDL3.TTF.GetStringSize(sdlFont.Handle, line, 0, out int w, out int h))
            {
                maxWidth = MathF.Max(maxWidth, w * scale);
                singleLineHeight = MathF.Max(singleLineHeight, h * scale);
            }
        }

        if (singleLineHeight == 0 && SDL3.TTF.GetStringSize(sdlFont.Handle, " ", 0, out _, out int fallbackH))
            singleLineHeight = fallbackH * scale;

        return new Vector2(maxWidth, singleLineHeight * lineSpacing * lines.Length);
    }

    public Vector2 MeasureTextWithOptions(string text, TextRenderOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        var runs = options.ParseMarkup
            ? (options.MarkupParser ?? _defaultMarkupParser).Parse(text, options)
            : new[] { new TextRun { Text = text, FontSize = options.FontSize } };

        return MeasureTextRuns(runs, options.LineSpacing);
    }
    
    public Vector2 MeasureTextRuns(IReadOnlyList<TextRun> runs, float lineSpacing = 1.2f)
    {
        if (_defaultFontAtlas != null)
            return MeasureTextRunsGlyph(runs, lineSpacing);

        float cursorX = 0;
        float maxWidth = 0;
        float maxLineHeight = 0;
        int lineCount = 1;

        foreach (var run in runs)
        {
            if (string.IsNullOrEmpty(run.Text)) continue;

            var segments = run.Text.Split('\n');
            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0)
                {
                    maxWidth = MathF.Max(maxWidth, cursorX);
                    cursorX = 0;
                    lineCount++;
                }

                if (segments[i].Length > 0)
                {
                    var segSize = MeasureText(segments[i], run.FontSize, lineSpacing);
                    cursorX += segSize.X;
                    maxLineHeight = MathF.Max(maxLineHeight, segSize.Y);
                }
            }
        }

        maxWidth = MathF.Max(maxWidth, cursorX);
        if (maxLineHeight == 0)
            maxLineHeight = 16;

        return new Vector2(maxWidth, maxLineHeight * lineCount);
    }

    private Vector2 MeasureTextRunsGlyph(IReadOnlyList<TextRun> runs, float lineSpacing)
    {
        float cursorX = 0;
        float maxWidth = 0;
        float maxLineHeight = 0;
        int lineCount = 1;

        foreach (var run in runs)
        {
            if (string.IsNullOrEmpty(run.Text))
                continue;

            float scale = _defaultFont != null ? run.FontSize / _defaultFont.Size : 1.0f;

            foreach (char c in run.Text)
            {
                if (c == '\n')
                {
                    maxWidth = MathF.Max(maxWidth, cursorX);
                    cursorX = 0;
                    lineCount++;
                    continue;
                }
                if (_defaultFontAtlas!.TryGetGlyph(c, out var glyph))
                    cursorX += glyph.Advance * scale;
            }

            maxLineHeight = MathF.Max(maxLineHeight, _defaultFontAtlas!.LineHeight * scale);
        }

        maxWidth = MathF.Max(maxWidth, cursorX);
        if (maxLineHeight == 0)
            maxLineHeight = _defaultFontAtlas?.LineHeight ?? 16;

        return new Vector2(maxWidth, maxLineHeight * lineSpacing * lineCount);
    }

    public Vector2 MeasureGlyphSpan(ReadOnlySpan<char> text, float scale = 1.0f, float lineSpacing = 1.2f)
    {
        if (_defaultFontAtlas == null) return Vector2.Zero;
        float width = 0;
        float maxWidth = 0;
        int lineCount = 1;
        foreach (char c in text)
        {
            if (c == '\n')
            {
                maxWidth = MathF.Max(maxWidth, width);
                width = 0;
                lineCount++;
                continue;
            }
            if (_defaultFontAtlas.TryGetGlyph(c, out var glyph))
                width += glyph.Advance * scale;
        }
        maxWidth = MathF.Max(maxWidth, width);
        return new Vector2(maxWidth, _defaultFontAtlas.LineHeight * scale * lineSpacing * lineCount);
    }

    private void DisposeOwnedFont()
    {
        if (_ownsDefaultFont)
        {
            _defaultFont?.Dispose();
        }
        _defaultFont = null;
        _ownsDefaultFont = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

        DisposeOwnedFont();
        
        _disposed = true;
    }
}
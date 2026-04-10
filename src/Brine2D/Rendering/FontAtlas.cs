using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering;

/// <summary>
/// Represents a font atlas containing pre-rendered glyphs packed into a texture.
/// </summary>
public class FontAtlas : IDisposable
{
    private const int MaxAtlasSize = 8192;

    private readonly ILogger<FontAtlas> _logger;
    private readonly Dictionary<char, FontGlyph> _glyphs = new();
    private ITexture? _atlasTexture;
    private int _disposed;

    public ITexture? Texture => _atlasTexture;
    public int LineHeight { get; private set; }
    public int Ascent { get; private set; }

    public FontAtlas(ILogger<FontAtlas> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a font atlas for the given font with ASCII printable characters (32-126).
    /// </summary>
    public bool Generate(SDL3Font font, ITextureContext textureContext, TextureScaleMode scaleMode = TextureScaleMode.Nearest)
    {
        if (font == null || !font.IsLoaded)
        {
            _logger.LogError("Cannot generate atlas: font not loaded");
            return false;
        }

        _atlasTexture?.Dispose();
        _atlasTexture = null;
        _glyphs.Clear();

        const int firstChar = 32;  // Space
        const int lastChar = 126;  // Tilde
        const int padding = 2;     // Padding between glyphs

        _logger.LogInformation("Generating font atlas for {Font} {Size}pt with {ScaleMode} filtering",
            font.Name, font.Size, scaleMode);

        // First pass: measure all glyphs to determine atlas size
        var glyphSurfaces = new List<(char character, nint surface, int width, int height, int bearingX, int bearingY, int advance)>();
        int maxHeight = 0;
        int totalWidth = 0;

        for (int i = firstChar; i <= lastChar; i++)
        {
            char c = (char)i;

            if (!SDL3.TTF.GetGlyphMetrics(font.Handle, (ushort)c, out int minX, out int maxX, out int minY, out int maxY, out int advance))
            {
                _logger.LogWarning("Failed to get metrics for glyph '{Char}'", c);
                continue;
            }

            var surface = SDL3.TTF.RenderGlyphBlended(font.Handle, (ushort)c, new SDL3.SDL.Color { R = 255, G = 255, B = 255, A = 255 });

            if (surface == nint.Zero)
            {
                _glyphs[c] = new FontGlyph
                {
                    Character = c,
                    AtlasX = 0,
                    AtlasY = 0,
                    Width = 0,
                    Height = 0,
                    BearingX = minX,
                    BearingY = maxY,
                    Advance = advance
                };
                continue;
            }

            var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);

            int width = surfaceStruct.Width;
            int height = surfaceStruct.Height;
            int bearingX = minX;
            int bearingY = maxY;

            glyphSurfaces.Add((c, surface, width, height, bearingX, bearingY, advance));

            totalWidth += width + padding;
            if (height > maxHeight)
                maxHeight = height;
        }

        if (glyphSurfaces.Count == 0 && _glyphs.Count == 0)
        {
            _logger.LogError("No glyphs were rendered successfully");
            return false;
        }

        // Calculate atlas dimensions (prefer square-ish texture)
        int atlasWidth = (int)Math.Ceiling(Math.Sqrt(totalWidth * maxHeight));
        atlasWidth = NextPowerOfTwo(atlasWidth);
        int atlasHeight = atlasWidth;

        // Get font metrics for proper baseline calculation
        var ascent = SDL3.TTF.GetFontAscent(font.Handle);
        var lineHeight = SDL3.TTF.GetFontHeight(font.Handle);

        Ascent = ascent;
        LineHeight = lineHeight;

        // Try packing, doubling atlas size on failure
        var zeroSurfaceGlyphs = new Dictionary<char, FontGlyph>(_glyphs);

        try
        {
            while (atlasWidth <= MaxAtlasSize)
            {
                if (TryPackAndCreateAtlas(glyphSurfaces, padding, atlasWidth, atlasHeight, textureContext, scaleMode))
                {
                    foreach (var kvp in zeroSurfaceGlyphs)
                        _glyphs.TryAdd(kvp.Key, kvp.Value);

                    return true;
                }

                _glyphs.Clear();
                atlasWidth *= 2;
                atlasHeight = atlasWidth;
            }

            _logger.LogError("Atlas exceeded maximum size ({Max}x{Max})", MaxAtlasSize, MaxAtlasSize);
            return false;
        }
        finally
        {
            CleanupSurfaces(glyphSurfaces);
        }
    }

    private bool TryPackAndCreateAtlas(
        List<(char character, nint surface, int width, int height, int bearingX, int bearingY, int advance)> glyphSurfaces,
        int padding,
        int atlasWidth,
        int atlasHeight,
        ITextureContext textureContext,
        TextureScaleMode scaleMode)
    {
        _logger.LogDebug("Attempting atlas dimensions: {Width}x{Height}, glyphs: {Count}",
            atlasWidth, atlasHeight, glyphSurfaces.Count);

        var atlasSurface = SDL3.SDL.CreateSurface(atlasWidth, atlasHeight, SDL3.SDL.PixelFormat.ARGB8888);
        if (atlasSurface == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create atlas surface: {Error}", error);
            return false;
        }

        try
        {
            SDL3.SDL.ClearSurface(atlasSurface, 0, 0, 0, 0);

            int x = 0, y = 0;
            int rowHeight = 0;

            foreach (var (character, surface, width, height, bearingX, bearingY, advance) in glyphSurfaces)
            {
                if (x + width > atlasWidth)
                {
                    x = 0;
                    y += rowHeight + padding;
                    rowHeight = 0;
                }

                if (y + height > atlasHeight)
                    return false;

                var srcRect = new SDL3.SDL.Rect { X = 0, Y = 0, W = width, H = height };
                var dstRect = new SDL3.SDL.Rect { X = x, Y = y, W = width, H = height };

                if (!SDL3.SDL.BlitSurface(surface, ref srcRect, atlasSurface, ref dstRect))
                {
                    _logger.LogWarning("Failed to blit glyph '{Char}' to atlas", character);
                }

                _glyphs[character] = new FontGlyph
                {
                    Character = character,
                    AtlasX = x,
                    AtlasY = y,
                    Width = width,
                    Height = height,
                    BearingX = bearingX,
                    BearingY = bearingY,
                    Advance = advance
                };

                x += width + padding;
                if (height > rowHeight)
                    rowHeight = height;
            }

            ConvertToGrayscale(atlasSurface);

            _atlasTexture = textureContext.CreateTextureFromSurface(atlasSurface, atlasWidth, atlasHeight, scaleMode);

            _logger.LogInformation("Font atlas generated successfully: {Width}x{Height}, {Count} glyphs",
                atlasWidth, atlasHeight, _glyphs.Count);

            return true;
        }
        finally
        {
            SDL3.SDL.DestroySurface(atlasSurface);
        }
    }

    private void CleanupSurfaces(List<(char, nint, int, int, int, int, int)> surfaces)
    {
        foreach (var (_, surface, _, _, _, _, _) in surfaces)
        {
            if (surface != nint.Zero)
            {
                SDL3.SDL.DestroySurface(surface);
            }
        }
    }

    private static int NextPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    public bool TryGetGlyph(char character, out FontGlyph glyph)
    {
        return _glyphs.TryGetValue(character, out glyph);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        if (_atlasTexture != null)
        {
            _atlasTexture.Dispose();
            _atlasTexture = null;
        }

        _glyphs.Clear();
    }

    /// <summary>
    /// Converts SDL_ttf glyph surfaces to proper format for GPU rendering.
    /// SDL_ttf RenderGlyphBlended with white produces premultiplied ARGB where
    /// every channel equals alpha coverage. This converts to straight-alpha white.
    /// </summary>
    private unsafe void ConvertToGrayscale(nint surface)
    {
        var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);

        int width = surfaceStruct.Width;
        int height = surfaceStruct.Height;
        int pitch = surfaceStruct.Pitch;

        byte* pixels = (byte*)surfaceStruct.Pixels;

        for (int y = 0; y < height; y++)
        {
            uint* row = (uint*)(pixels + y * pitch);
            for (int x = 0; x < width; x++)
            {
                byte coverage = (byte)(row[x] >> 24);
                row[x] = ((uint)coverage << 24) | 0x00FFFFFFu;
            }
        }
    }
}
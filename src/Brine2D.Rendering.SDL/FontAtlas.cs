using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// Represents a font atlas containing pre-rendered glyphs packed into a texture.
/// </summary>
public class FontAtlas : IDisposable
{
    private readonly ILogger<FontAtlas> _logger;
    private readonly Dictionary<char, FontGlyph> _glyphs = new();
    private ITexture? _atlasTexture;
    private bool _disposed;

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

            // Use RenderGlyphBlended for high-quality antialiased text
            var surface = SDL3.TTF.RenderGlyphBlended(font.Handle, (ushort)c, new SDL3.SDL.Color { R = 255, G = 255, B = 255, A = 255 });

            if (surface == nint.Zero)
            {
                _logger.LogWarning("Failed to render glyph '{Char}' (code {Code})", c, i);
                continue;
            }

            var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);

            // Get glyph metrics
            if (!SDL3.TTF.GetGlyphMetrics(font.Handle, (ushort)c, out int minX, out int maxX, out int minY, out int maxY, out int advance))
            {
                _logger.LogWarning("Failed to get metrics for glyph '{Char}'", c);
                SDL3.SDL.DestroySurface(surface);
                continue;
            }

            int width = surfaceStruct.Width;
            int height = surfaceStruct.Height;
            int bearingX = minX;
            int bearingY = maxY;

            glyphSurfaces.Add((c, surface, width, height, bearingX, bearingY, advance));

            totalWidth += width + padding;
            if (height > maxHeight)
                maxHeight = height;
        }

        if (glyphSurfaces.Count == 0)
        {
            _logger.LogError("No glyphs were rendered successfully");
            return false;
        }

        // Calculate atlas dimensions (prefer square-ish texture)
        int atlasWidth = (int)Math.Ceiling(Math.Sqrt(totalWidth * maxHeight));
        atlasWidth = NextPowerOfTwo(atlasWidth); // Power of 2 for better GPU compatibility
        int atlasHeight = atlasWidth;

        // Get font metrics for proper baseline calculation
        var ascent = SDL3.TTF.GetFontAscent(font.Handle);
        var lineHeight = SDL3.TTF.GetFontHeight(font.Handle);

        Ascent = ascent;
        LineHeight = lineHeight;

        _logger.LogDebug("Atlas dimensions: {Width}x{Height}, glyphs: {Count}",
            atlasWidth, atlasHeight, glyphSurfaces.Count);

        // Create atlas surface with ARGB format (same as glyphs from SDL_ttf)
        var atlasSurface = SDL3.SDL.CreateSurface(atlasWidth, atlasHeight, SDL3.SDL.PixelFormat.ARGB8888);
        if (atlasSurface == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create atlas surface: {Error}", error);
            CleanupSurfaces(glyphSurfaces);
            return false;
        }

        try
        {
            // Clear atlas to fully transparent
            SDL3.SDL.ClearSurface(atlasSurface, 0, 0, 0, 0);

            // Pack glyphs into atlas
            int x = 0, y = 0;
            int rowHeight = 0;

            foreach (var (character, surface, width, height, bearingX, bearingY, advance) in glyphSurfaces)
            {
                // Check if we need to move to next row
                if (x + width > atlasWidth)
                {
                    x = 0;
                    y += rowHeight + padding;
                    rowHeight = 0;
                }

                if (y + height > atlasHeight)
                {
                    _logger.LogError("Atlas too small, glyphs don't fit");
                    SDL3.SDL.DestroySurface(atlasSurface);
                    CleanupSurfaces(glyphSurfaces);
                    return false;
                }

                // Blit glyph to atlas
                var srcRect = new SDL3.SDL.Rect { X = 0, Y = 0, W = width, H = height };
                var dstRect = new SDL3.SDL.Rect { X = x, Y = y, W = width, H = height };

                if (!SDL3.SDL.BlitSurface(surface, ref srcRect, atlasSurface, ref dstRect))
                {
                    _logger.LogWarning("Failed to blit glyph '{Char}' to atlas", character);
                }

                // Store glyph metadata
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

            // Convert to proper format for rendering (white RGB with alpha)
            ConvertToGrayscale(atlasSurface);

            // Create GPU texture from atlas
            _atlasTexture = textureContext.CreateTextureFromSurface(atlasSurface, atlasWidth, atlasHeight, scaleMode);

            _logger.LogInformation("Font atlas generated successfully: {Width}x{Height}, {Count} glyphs",
                atlasWidth, atlasHeight, _glyphs.Count);

            return true;
        }
        finally
        {
            SDL3.SDL.DestroySurface(atlasSurface);
            CleanupSurfaces(glyphSurfaces);
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
        if (_disposed) return;

        if (_atlasTexture != null)
        {
            _atlasTexture.Dispose();
            _atlasTexture = null;
        }

        _glyphs.Clear();
        _disposed = true;
    }

    /// <summary>
    /// Converts SDL_ttf glyph surfaces to proper format for GPU rendering.
    /// SDL_ttf stores alpha coverage in the R channel of ARGB8888 surfaces.
    /// This method converts it to white RGB with proper alpha channel.
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
            for (int x = 0; x < width; x++)
            {
                int offset = y * pitch + x * 4;

                // ARGB8888 little-endian memory layout: [B, G, R, A]
                // SDL_ttf stores alpha coverage in the R channel (offset+2)
                byte a = pixels[offset + 2];

                // Set RGB to white, alpha to coverage value
                pixels[offset + 0] = 255;    // B
                pixels[offset + 1] = 255;    // G
                pixels[offset + 2] = 255;    // R
                pixels[offset + 3] = a;      // A
            }
        }
    }
}
using System.Drawing;
using Brine2D.Animation;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Brine2D.Core;

namespace Brine2D.Rendering.SDL.TextureAtlas;

/// <summary>
/// Builds texture atlases by packing multiple textures into a single larger texture.
/// Uses a simple shelf-packing algorithm optimized for sprite-based games.
/// </summary>
public sealed class TextureAtlasBuilder : ITextureAtlasBuilder
{
    private readonly ITextureLoader _textureLoader;
    private readonly ITextureContext _textureContext;
    private readonly ILogger<TextureAtlasBuilder> _logger;
    private readonly ILogger<TextureAtlas> _atlasLogger;
    private readonly ILogger<TextureAtlasCollection> _collectionLogger;
    private readonly TextureAtlasOptions _options;

    private readonly List<TextureEntry> _textures = new();
    private string _atlasName = "Atlas";
    private int _maxWidth = 2048;
    private int _maxHeight = 2048;
    private int _padding = 2;
    private bool _usePowerOfTwo = true;
    private TextureScaleMode _scaleMode = TextureScaleMode.Nearest;

    public TextureAtlasBuilder(
        ITextureLoader textureLoader,
        ITextureContext textureContext,
        ILogger<TextureAtlasBuilder> logger,
        ILogger<TextureAtlas> atlasLogger,
        ILogger<TextureAtlasCollection> collectionLogger,
        TextureAtlasOptions options)
    {
        _textureLoader = textureLoader ?? throw new ArgumentNullException(nameof(textureLoader));
        _textureContext = textureContext ?? throw new ArgumentNullException(nameof(textureContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _atlasLogger = atlasLogger ?? throw new ArgumentNullException(nameof(atlasLogger));
        _collectionLogger = collectionLogger ?? throw new ArgumentNullException(nameof(collectionLogger));
        _options = options ?? new TextureAtlasOptions();

        // Apply defaults from options
        _maxWidth = _options.MaxAtlasWidth;
        _maxHeight = _options.MaxAtlasHeight;
        _padding = _options.Padding;
        _usePowerOfTwo = _options.UsePowerOfTwo;
        _scaleMode = _options.DefaultScaleMode;
    }

    public ITextureAtlasBuilder WithName(string name)
    {
        _atlasName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    public ITextureAtlasBuilder AddTexture(string path, string? name = null)
    {
        var entryName = name ?? Path.GetFileNameWithoutExtension(path);
        _textures.Add(new TextureEntry(path, entryName));
        return this;
    }

    public ITextureAtlasBuilder AddFolder(string folderPath, string pattern = "*.png", bool recursive = false)
    {
        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Folder '{FolderPath}' does not exist", folderPath);
            return this;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(folderPath, pattern, searchOption);

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            _textures.Add(new TextureEntry(file, name));
        }

        _logger.LogInformation("Added {Count} textures from folder '{FolderPath}'", files.Length, folderPath);
        return this;
    }

    public ITextureAtlasBuilder WithMaxSize(int maxWidth, int maxHeight)
    {
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
        return this;
    }

    public ITextureAtlasBuilder WithPadding(int padding)
    {
        _padding = Math.Max(0, padding);
        return this;
    }

    public ITextureAtlasBuilder WithPowerOfTwo(bool usePowerOfTwo = true)
    {
        _usePowerOfTwo = usePowerOfTwo;
        return this;
    }

    public ITextureAtlasBuilder WithScaleMode(TextureScaleMode scaleMode)
    {
        _scaleMode = scaleMode;
        return this;
    }

    public async Task<ITextureAtlasCollection> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (_textures.Count == 0)
        {
            throw new InvalidOperationException("Cannot build atlas: no textures added");
        }

        _logger.LogInformation("Building texture atlas collection '{AtlasName}' with {Count} textures", 
            _atlasName, _textures.Count);

        // Load all textures
        var loadedTextures = new List<LoadedTexture>();
        
        try
        {
            foreach (var entry in _textures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var surface = await LoadTextureToSurfaceAsync(entry.Path, cancellationToken);
                if (surface != nint.Zero)
                {
                    var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);
                    loadedTextures.Add(new LoadedTexture(
                        entry.Name,
                        surface,
                        surfaceStruct.Width,
                        surfaceStruct.Height));
                }
                else
                {
                    _logger.LogWarning("Failed to load texture '{Path}' for atlas", entry.Path);
                }
            }

            if (loadedTextures.Count == 0)
            {
                throw new InvalidOperationException("No textures were loaded successfully");
            }

            // Create collection
            var collection = new TextureAtlasCollection(_atlasName, _collectionLogger);

            // Pack textures into multiple atlases as needed
            var remainingTextures = loadedTextures.OrderByDescending(t => t.Height).ThenByDescending(t => t.Width).ToList();
            int atlasIndex = 0;

            while (remainingTextures.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var atlasName = loadedTextures.Count > _maxWidth * _maxHeight / 1024 
                    ? $"{_atlasName}_{atlasIndex}" 
                    : _atlasName;

                var (atlas, packedCount) = await BuildSingleAtlasAsync(
                    atlasName,
                    remainingTextures,
                    cancellationToken);

                collection.AddAtlas(atlas);
                remainingTextures = remainingTextures.Skip(packedCount).ToList();
                atlasIndex++;

                _logger.LogInformation(
                    "Built atlas {Index}: '{AtlasName}' with {RegionCount} regions. Remaining: {Remaining}",
                    atlasIndex, atlasName, atlas.RegionCount, remainingTextures.Count);
            }

            _logger.LogInformation(
                "Successfully built atlas collection '{CollectionName}': {AtlasCount} atlases, {TotalRegions} total regions",
                _atlasName, collection.Atlases.Count, collection.TotalRegionCount);

            return collection;
        }
        finally
        {
            // Clean up loaded surfaces
            foreach (var loaded in loadedTextures)
            {
                if (loaded.Surface != nint.Zero)
                {
                    SDL3.SDL.DestroySurface(loaded.Surface);
                }
            }
        }
    }

    private async Task<(ITextureAtlas atlas, int packedCount)> BuildSingleAtlasAsync(
        string atlasName,
        List<LoadedTexture> textures,
        CancellationToken cancellationToken)
    {
        // Pack as many textures as will fit
        var packedRegions = new List<PackedRegion>();
        int currentX = _padding;
        int currentY = _padding;
        int rowHeight = 0;
        int maxWidth = 0;
        int maxHeight = 0;
        int packedCount = 0;

        foreach (var texture in textures)
        {
            int width = texture.Width + _padding * 2;
            int height = texture.Height + _padding * 2;

            // Check if we need to move to the next row
            if (currentX + width > _maxWidth)
            {
                currentX = _padding;
                currentY += rowHeight;
                rowHeight = 0;

                // Check if we've exceeded max height - stop packing this atlas
                if (currentY + height > _maxHeight)
                {
                    break; // This texture and remaining ones go to next atlas
                }
            }

            // Place texture
            packedRegions.Add(new PackedRegion(
                texture.Name,
                currentX,
                currentY,
                texture.Width,
                texture.Height));

            // Update position
            currentX += width;
            rowHeight = Math.Max(rowHeight, height);
            maxWidth = Math.Max(maxWidth, currentX);
            maxHeight = Math.Max(maxHeight, currentY + height);
            packedCount++;
        }

        if (packedRegions.Count == 0)
        {
            throw new InvalidOperationException(
                $"Single texture too large for atlas (max size: {_maxWidth}x{_maxHeight})");
        }

        // Apply power-of-two if requested
        int atlasWidth = _usePowerOfTwo ? NextPowerOfTwo(maxWidth) : maxWidth;
        int atlasHeight = _usePowerOfTwo ? NextPowerOfTwo(maxHeight) : maxHeight;

        // Create the atlas surface
        var atlasSurface = SDL3.SDL.CreateSurface(atlasWidth, atlasHeight, SDL3.SDL.PixelFormat.ARGB8888);
        if (atlasSurface == nint.Zero)
        {
            throw new InvalidOperationException($"Failed to create atlas surface: {SDL3.SDL.GetError()}");
        }

        try
        {
            // Clear to transparent
            SDL3.SDL.ClearSurface(atlasSurface, 0, 0, 0, 0);

            // Blit textures
            foreach (var region in packedRegions)
            {
                var loaded = textures.First(t => t.Name == region.Name);
                
                var srcRect = new SDL3.SDL.Rect { X = 0, Y = 0, W = loaded.Width, H = loaded.Height };
                var dstRect = new SDL3.SDL.Rect { X = region.X, Y = region.Y, W = region.Width, H = region.Height };

                if (!SDL3.SDL.BlitSurface(loaded.Surface, ref srcRect, atlasSurface, ref dstRect))
                {
                    _logger.LogWarning("Failed to blit texture '{Name}' to atlas", region.Name);
                }
            }

            // Create GPU texture
            var atlasTexture = _textureContext.CreateTextureFromSurface(atlasSurface, atlasWidth, atlasHeight, _scaleMode);
            
            // Create atlas object
            var atlas = new TextureAtlas(atlasName, atlasTexture, _atlasLogger);

            // Add regions
            foreach (var region in packedRegions)
            {
                var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);
                var atlasRegion = new AtlasRegion(region.Name, rect, atlasTexture, region.Width, region.Height);
                atlas.AddRegion(atlasRegion);
            }

            return (atlas, packedCount);
        }
        finally
        {
            SDL3.SDL.DestroySurface(atlasSurface);
        }
    }

    public ITextureAtlasBuilder Clear()
    {
        _textures.Clear();
        return this;
    }

    /// <summary>
    /// Loads a texture file to an SDL surface (without creating GPU texture).
    /// </summary>
    private async Task<nint> LoadTextureToSurfaceAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var surface = SDL3.Image.Load(path);
            if (surface == nint.Zero)
            {
                _logger.LogError("Failed to load texture '{Path}': {Error}", path, SDL3.SDL.GetError());
            }
            return surface;
        }, cancellationToken);
    }

    /// <summary>
    /// Packs textures using a shelf-packing algorithm.
    /// Simple and efficient for sprite-based games.
    /// </summary>
    private List<PackedRegion> PackTextures(List<LoadedTexture> textures, out int atlasWidth, out int atlasHeight)
    {
        // Sort textures by height (descending) for better packing
        var sortedTextures = textures.OrderByDescending(t => t.Height).ThenByDescending(t => t.Width).ToList();

        var packed = new List<PackedRegion>();
        int currentX = _padding;
        int currentY = _padding;
        int rowHeight = 0;
        int maxWidth = 0;
        int maxHeight = 0;

        foreach (var texture in sortedTextures)
        {
            int width = texture.Width + _padding * 2;
            int height = texture.Height + _padding * 2;

            // Check if we need to move to the next row
            if (currentX + width > _maxWidth)
            {
                currentX = _padding;
                currentY += rowHeight;
                rowHeight = 0;

                // Check if we've exceeded max height
                if (currentY + height > _maxHeight)
                {
                    throw new InvalidOperationException(
                        $"Textures don't fit in atlas (max size: {_maxWidth}x{_maxHeight}). " +
                        "Consider increasing max size or splitting into multiple atlases.");
                }
            }

            // Place texture
            packed.Add(new PackedRegion(
                texture.Name,
                currentX,
                currentY,
                texture.Width,
                texture.Height));

            // Update position
            currentX += width;
            rowHeight = Math.Max(rowHeight, height);

            // Track actual used dimensions
            maxWidth = Math.Max(maxWidth, currentX);
            maxHeight = Math.Max(maxHeight, currentY + height);
        }

        // Apply power-of-two if requested
        if (_usePowerOfTwo)
        {
            atlasWidth = NextPowerOfTwo(maxWidth);
            atlasHeight = NextPowerOfTwo(maxHeight);
        }
        else
        {
            atlasWidth = maxWidth;
            atlasHeight = maxHeight;
        }

        _logger.LogDebug("Packed {Count} textures into {Width}x{Height} atlas", textures.Count, atlasWidth, atlasHeight);

        return packed;
    }

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 0) return 1;
        
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    private record TextureEntry(string Path, string Name);
    private record LoadedTexture(string Name, nint Surface, int Width, int Height);
    private record PackedRegion(string Name, int X, int Y, int Width, int Height);
}
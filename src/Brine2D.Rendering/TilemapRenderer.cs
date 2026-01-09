using Brine2D.Core.Tilemap;
using System.Numerics;
using Brine2D.Core.Animation;

namespace Brine2D.Rendering;

/// <summary>
/// Handles rendering of tilemaps with sprite batching.
/// Separates rendering from core tilemap data.
/// Uses SpriteBatcher for optimal performance.
/// </summary>
public class TilemapRenderer
{
    private readonly Dictionary<string, ITexture> _tilesetTextures = new();
    private readonly SpriteBatcher _batcher = new();

    /// <summary>
    /// Loads the tileset texture for a tilemap.
    /// </summary>
    public async Task LoadTilesetAsync(Tilemap tilemap, ITextureLoader textureLoader, CancellationToken cancellationToken = default)
    {
        if (tilemap.Tileset == null || string.IsNullOrEmpty(tilemap.Tileset.ImagePath))
            return;

        if (_tilesetTextures.ContainsKey(tilemap.Tileset.ImagePath))
            return; // Already loaded

        var texture = await textureLoader.LoadTextureAsync(
            tilemap.Tileset.ImagePath,
            TextureScaleMode.Nearest, // Pixel art default
            cancellationToken);

        _tilesetTextures[tilemap.Tileset.ImagePath] = texture;
    }

    /// <summary>
    /// Renders a tilemap using sprite batching and frustum culling.
    /// All tiles are batched together for maximum performance.
    /// </summary>
    public void Render(Tilemap tilemap, IRenderer renderer, ICamera? camera = null)
    {
        if (tilemap.Tileset == null || !_tilesetTextures.TryGetValue(tilemap.Tileset.ImagePath, out var texture))
            return;

        // Calculate visible tile range (frustum culling)
        var visibleRect = GetVisibleTileRange(tilemap, camera);

        // Queue all visible tiles to the batcher
        foreach (var layer in tilemap.Layers)
        {
            if (!layer.Visible) continue;

            // Use layer Z-order for proper depth sorting
            int layerDepth = layer.ZOrder;

            for (int y = visibleRect.minY; y <= visibleRect.maxY; y++)
            {
                for (int x = visibleRect.minX; x <= visibleRect.maxX; x++)
                {
                    var tile = layer.GetTile(x, y);
                    if (tile.IsEmpty) continue;

                    var sourceRect = tilemap.Tileset.GetTileSourceRect(tile.Id);

                    var worldX = x * tilemap.TileWidth;
                    var worldY = y * tilemap.TileHeight;

                    // Add tile to batch
                    _batcher.Draw(
                        texture: texture,
                        position: new Vector2(worldX, worldY),
                        sourceRect: new Rectangle(
                            sourceRect.x, 
                            sourceRect.y, 
                            sourceRect.width, 
                            sourceRect.height),
                        scale: Vector2.One,
                        rotation: 0f,
                        origin: Vector2.Zero, // Tiles render from top-left
                        tint: Color.White,
                        layer: layerDepth);
                }
            }
        }

        // Flush all batched tiles (renders them sorted and grouped by texture)
        _batcher.Flush(renderer, camera);
    }

    /// <summary>
    /// Calculates which tiles are visible based on camera position.
    /// Uses frustum culling to skip off-screen tiles.
    /// </summary>
    private (int minX, int minY, int maxX, int maxY) GetVisibleTileRange(Tilemap tilemap, ICamera? camera)
    {
        if (camera == null)
        {
            return (0, 0, tilemap.Width - 1, tilemap.Height - 1);
        }

        var cameraLeft = camera.Position.X - (camera.ViewportWidth / 2f / camera.Zoom);
        var cameraTop = camera.Position.Y - (camera.ViewportHeight / 2f / camera.Zoom);
        var cameraRight = camera.Position.X + (camera.ViewportWidth / 2f / camera.Zoom);
        var cameraBottom = camera.Position.Y + (camera.ViewportHeight / 2f / camera.Zoom);

        var minX = Math.Max(0, (int)(cameraLeft / tilemap.TileWidth) - 1);
        var minY = Math.Max(0, (int)(cameraTop / tilemap.TileHeight) - 1);
        var maxX = Math.Min(tilemap.Width - 1, (int)(cameraRight / tilemap.TileWidth) + 1);
        var maxY = Math.Min(tilemap.Height - 1, (int)(cameraBottom / tilemap.TileHeight) + 1);

        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Gets batching statistics for tilemaps.
    /// </summary>
    public (int TileCount, int DrawCalls) GetBatchStats()
    {
        return (_batcher.Count, _batcher.EstimatedDrawCalls);
    }

    /// <summary>
    /// Unloads all loaded tileset textures.
    /// </summary>
    public void UnloadAll(ITextureLoader textureLoader)
    {
        foreach (var texture in _tilesetTextures.Values)
        {
            textureLoader.UnloadTexture(texture);
        }
        _tilesetTextures.Clear();
    }
}
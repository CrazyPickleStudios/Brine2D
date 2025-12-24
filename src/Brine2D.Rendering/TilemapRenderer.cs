using Brine2D.Core.Tilemap;

namespace Brine2D.Rendering;

/// <summary>
/// Handles rendering of tilemaps. Separates rendering from core tilemap data.
/// </summary>
public class TilemapRenderer
{
    private readonly Dictionary<string, ITexture> _tilesetTextures = new();

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
    /// Renders a tilemap using the specified renderer and camera.
    /// </summary>
    public void Render(Tilemap tilemap, IRenderer renderer, ICamera? camera = null)
    {
        if (tilemap.Tileset == null || !_tilesetTextures.TryGetValue(tilemap.Tileset.ImagePath, out var texture))
            return;

        var visibleRect = GetVisibleTileRange(tilemap, camera);

        foreach (var layer in tilemap.Layers)
        {
            if (!layer.Visible) continue;

            for (int y = visibleRect.minY; y <= visibleRect.maxY; y++)
            {
                for (int x = visibleRect.minX; x <= visibleRect.maxX; x++)
                {
                    var tile = layer.GetTile(x, y);
                    if (tile.IsEmpty) continue;

                    var sourceRect = tilemap.Tileset.GetTileSourceRect(tile.Id);

                    var worldX = x * tilemap.TileWidth;
                    var worldY = y * tilemap.TileHeight;

                    renderer.DrawTexture(
                        texture,
                        sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height,
                        worldX, worldY, tilemap.TileWidth, tilemap.TileHeight);
                }
            }
        }
    }

    /// <summary>
    /// Calculates which tiles are visible based on camera position.
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
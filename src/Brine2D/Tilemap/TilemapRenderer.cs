using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Tilemap;
using System.Numerics;

namespace Brine2D.Rendering;

public class TilemapRenderer : IDisposable
{
    private bool _disposed;
    private readonly Dictionary<string, ITexture> _tilesetTextures = new();
    private readonly SpriteBatcher _batcher = new();

    public async Task LoadTilesetAsync(Tilemap.Tilemap tilemap, ITextureLoader textureLoader, CancellationToken cancellationToken = default)
    {
        foreach (var tileset in tilemap.Tilesets)
        {
            if (string.IsNullOrEmpty(tileset.ImagePath) || _tilesetTextures.ContainsKey(tileset.ImagePath))
                continue;

            var texture = await textureLoader.LoadTextureAsync(
                tileset.ImagePath,
                TextureScaleMode.Nearest,
                cancellationToken);

            _tilesetTextures[tileset.ImagePath] = texture;
        }
    }

    public void Render(Tilemap.Tilemap tilemap, IRenderer renderer, ICamera? camera = null, TilemapAnimator? animator = null, Vector2 positionOffset = default)
    {
        if (tilemap.Tilesets.Count == 0) return;

        foreach (var layer in tilemap.Layers)
        {
            if (!layer.Visible) continue;

            var visibleRect = GetVisibleTileRange(tilemap, camera, layer.OffsetX + positionOffset.X, layer.OffsetY + positionOffset.Y, layer.ParallaxX, layer.ParallaxY);
            var layerTint = layer.TintColor.WithAlpha((byte)(layer.TintColor.A / 255f * layer.Opacity * 255f));
            byte layerDepth = layer.ZOrder;

            // Parallax shifts the layer's world origin relative to the camera.
            var parallaxShiftX = camera != null ? camera.Position.X * (1f - layer.ParallaxX) : 0f;
            var parallaxShiftY = camera != null ? camera.Position.Y * (1f - layer.ParallaxY) : 0f;

            for (int y = visibleRect.minY; y <= visibleRect.maxY; y++)
            {
                for (int x = visibleRect.minX; x <= visibleRect.maxX; x++)
                {
                    var tile = layer.GetTile(x, y);
                    if (tile.IsEmpty) continue;

                    var tileset = tilemap.ResolveTileset(tile.Id);
                    if (tileset == null) continue;

                    if (!_tilesetTextures.TryGetValue(tileset.ImagePath, out var texture))
                        continue;

                    var displayGid = animator != null ? animator.ResolveGid(tile.Id) : tile.Id;
                    var sourceRect = tileset.GetTileSourceRect(displayGid);

                    var worldX = x * tilemap.TileWidth + layer.OffsetX + positionOffset.X + parallaxShiftX;
                    var worldY = y * tilemap.TileHeight + layer.OffsetY + positionOffset.Y + parallaxShiftY;

                    // Tiled diagonal-flip combos in Y-down screen space (CCW-positive angles):
                    //   H+D = 90° CW,  V+D = 90° CCW,  H+V+D = 90° CW + H flip,  D alone = transpose
                    float rotation = 0f;
                    var flip = SpriteFlip.None;

                    bool h = tile.FlipHorizontal;
                    bool v = tile.FlipVertical;
                    bool d = tile.FlipDiagonal;

                    if (d)
                    {
                        if (h && !v) { rotation = -MathF.PI / 2f; flip = SpriteFlip.None; }
                        else if (!h && v) { rotation = MathF.PI / 2f; flip = SpriteFlip.None; }
                        else if (h && v) { rotation = MathF.PI / 2f; flip = SpriteFlip.Horizontal; }
                        else { rotation = -MathF.PI / 2f; flip = SpriteFlip.Vertical; }
                    }
                    else
                    {
                        if (h) flip |= SpriteFlip.Horizontal;
                        if (v) flip |= SpriteFlip.Vertical;
                    }

                    var origin = d
                        ? new Vector2(tilemap.TileWidth / 2f, tilemap.TileHeight / 2f)
                        : Vector2.Zero;

                    _batcher.Draw(
                        texture: texture,
                        position: new Vector2(worldX, worldY),
                        sourceRect: new Rectangle(sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height),
                        scale: Vector2.One,
                        rotation: rotation,
                        origin: origin,
                        tint: layerTint,
                        layer: layerDepth,
                        flip: flip);
                }
            }
        }

        _batcher.Flush(renderer);
    }

    /// <summary>Returns the inclusive tile index range visible to the camera, accounting for layer offset and parallax.</summary>
    internal (int minX, int minY, int maxX, int maxY) GetVisibleTileRange(
        Tilemap.Tilemap tilemap,
        ICamera? camera,
        float layerOffsetX = 0f,
        float layerOffsetY = 0f,
        float parallaxX = 1f,
        float parallaxY = 1f)
    {
        if (camera == null)
            return (0, 0, tilemap.Width - 1, tilemap.Height - 1);

        // Scale camera position by parallax to get this layer's effective view origin.
        var effectiveCamX = camera.Position.X * parallaxX;
        var effectiveCamY = camera.Position.Y * parallaxY;

        var halfW = camera.ViewportWidth / 2f / camera.Zoom;
        var halfH = camera.ViewportHeight / 2f / camera.Zoom;

        var cameraLeft = effectiveCamX - halfW;
        var cameraTop = effectiveCamY - halfH;
        var cameraRight = effectiveCamX + halfW;
        var cameraBottom = effectiveCamY + halfH;

        var effectiveLeft = cameraLeft - layerOffsetX;
        var effectiveTop = cameraTop - layerOffsetY;
        var effectiveRight = cameraRight - layerOffsetX;
        var effectiveBottom = cameraBottom - layerOffsetY;

        var minX = Math.Max(0, (int)(effectiveLeft / tilemap.TileWidth) - 1);
        var minY = Math.Max(0, (int)(effectiveTop / tilemap.TileHeight) - 1);
        var maxX = Math.Min(tilemap.Width - 1, (int)(effectiveRight / tilemap.TileWidth) + 1);
        var maxY = Math.Min(tilemap.Height - 1, (int)(effectiveBottom / tilemap.TileHeight) + 1);

        return (minX, minY, maxX, maxY);
    }

    public (int TileCount, int DrawCalls) GetBatchStats() =>
        (_batcher.Count, _batcher.EstimatedDrawCalls);

    public void UnloadAll(ITextureLoader textureLoader)
    {
        foreach (var texture in _tilesetTextures.Values)
            textureLoader.UnloadTexture(texture);

        _tilesetTextures.Clear();
    }

    /// <summary>Disposes the sprite batcher. Call <see cref="UnloadAll"/> first to avoid GPU texture leaks.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _batcher.Dispose();
        _tilesetTextures.Clear();
    }
}
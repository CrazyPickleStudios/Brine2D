using System.Drawing;

namespace Brine2D.Graphics.Tilemaps;

public sealed class TilemapRenderer
{
    private const uint FlipD = 0x20000000;
    private const uint FlipH = 0x80000000;
    private const uint FlipV = 0x40000000;

    public void Draw(IRenderContext ctx, Tilemap map, RectangleF? viewport = null)
    {
        var vw = viewport?.Width ?? map.MapWidthTiles * map.TileWidth;
        var vh = viewport?.Height ?? map.MapHeightTiles * map.TileHeight;
        var vx = viewport?.X ?? 0;
        var vy = viewport?.Y ?? 0;

        var startTileX = Math.Clamp((int)Math.Floor(vx / map.TileWidth), 0, map.MapWidthTiles - 1);
        var startTileY = Math.Clamp((int)Math.Floor(vy / map.TileHeight), 0, map.MapHeightTiles - 1);

        const float epsilon = 0.0001f;

        var endTileX = Math.Clamp((int)Math.Floor((vx + vw - epsilon) / map.TileWidth), 0, map.MapWidthTiles - 1);
        var endTileY = Math.Clamp((int)Math.Floor((vy + vh - epsilon) / map.TileHeight), 0, map.MapHeightTiles - 1);

        foreach (var layer in map.Layers)
        {
            if (!layer.Visible || layer.Opacity <= 0f)
            {
                continue;
            }

            var alpha = (byte)Math.Clamp((int)(layer.Opacity * 255), 0, 255);
            var tint = Color.FromArgb(alpha, 255, 255, 255);

            for (var ty = startTileY; ty <= endTileY; ty++)
            {
                for (var tx = startTileX; tx <= endTileX; tx++)
                {
                    var index = ty * map.MapWidthTiles + tx;
                    var gid = (uint)layer.GIds[index];

                    if (gid == 0)
                    {
                        continue;
                    }

                    var flags = gid & (FlipH | FlipV | FlipD);
                    var id = (int)(gid & 0x1FFFFFFF);

                    if (id == 0)
                    {
                        continue;
                    }

                    TilesetRef? tsRef = null;
                    for (var i = 0; i < map.Tilesets.Count; i++)
                    {
                        var ts = map.Tilesets[i];
                        var nextFirst = i + 1 < map.Tilesets.Count ? map.Tilesets[i + 1].FirstGid : int.MaxValue;

                        if (id >= ts.FirstGid && id < nextFirst)
                        {
                            tsRef = ts;
                            break;
                        }
                    }

                    if (tsRef is null)
                    {
                        continue;
                    }

                    var localIndex = id - tsRef.FirstGid;

                    if ((uint)localIndex >= (uint)tsRef.Tileset.TileRegions.Count)
                    {
                        continue;
                    }

                    var src = tsRef.Tileset.TileRegions[localIndex];
                    var dst = new RectangleF(tx * map.TileWidth, ty * map.TileHeight, map.TileWidth, map.TileHeight);

                    var rotation = 0f;
                    var flipMode = FlipMode.None;
                    var hasH = (flags & FlipH) != 0;
                    var hasV = (flags & FlipV) != 0;
                    var hasD = (flags & FlipD) != 0;

                    if (hasD)
                    {
                        rotation = 90f;
                        (hasH, hasV) = (hasV, hasH);
                    }

                    if (hasH && hasV)
                    {
                        flipMode = FlipMode.HorizontalVertical;
                    }
                    else if (hasH)
                    {
                        flipMode = FlipMode.Horizontal;
                    }
                    else if (hasV)
                    {
                        flipMode = FlipMode.Vertical;
                    }

                    ctx.DrawTexture(tsRef.Tileset.Texture, dst, src, tint, rotation, null, flipMode);
                }
            }
        }
    }
}
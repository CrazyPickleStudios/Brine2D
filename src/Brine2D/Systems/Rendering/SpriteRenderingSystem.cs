using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Brine2D.Rendering;
using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// System that renders all entities with SpriteComponent.
/// Uses batching for optimal performance on both Legacy and GPU renderers.
/// Includes frustum culling to only render visible sprites.
/// </summary>
public class SpriteRenderingSystem : RenderSystemBase
{
    public override int RenderOrder => SystemRenderOrder.Sprites;
    public string Name => "SpriteRenderingSystem";

    private readonly ITextureLoader _textureLoader;
    private readonly ICamera? _camera;
    private readonly Dictionary<string, ITexture> _textureCache = new();
    private readonly SpriteBatcher _batcher = new();

    private int _lastRenderedCount = 0;
    private int _lastTotalCount = 0;

    private CachedEntityQuery<SpriteComponent>? _spriteQuery;
    private List<(Entity Entity, SpriteComponent Sprite)> _cachedSprites = new();

    public SpriteRenderingSystem(
        ITextureLoader textureLoader,
        ICamera? camera = null)
    {
        _textureLoader = textureLoader;
        _camera = camera;
    }

    /// <summary>
    /// Loads textures for all sprites that need them.
    /// </summary>
    public async Task LoadTexturesAsync(IEntityWorld world, CancellationToken cancellationToken = default)
    {
        var sprites = world.GetEntitiesWithComponent<SpriteComponent>();

        foreach (var entity in sprites)
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            if (sprite == null || string.IsNullOrEmpty(sprite.TexturePath))
                continue;

            if (sprite.Texture == null && !_textureCache.ContainsKey(sprite.TexturePath))
            {
                var texture = await _textureLoader.LoadTextureAsync(
                    sprite.TexturePath,
                    sprite.TextureScaleMode,
                    cancellationToken);

                _textureCache[sprite.TexturePath] = texture;
                sprite.Texture = texture;
            }
            else if (sprite.Texture == null && _textureCache.ContainsKey(sprite.TexturePath))
            {
                sprite.Texture = _textureCache[sprite.TexturePath];
            }
        }
    }

    /// <summary>
    /// Renders all entities with SpriteComponent using batching.
    /// Automatically culls off-screen sprites when a camera is present.
    /// </summary>
    public override void Render(IEntityWorld world, IRenderer renderer, GameTime gameTime)
    {
        _spriteQuery ??= world.CreateCachedQuery<SpriteComponent>().Build();
        _cachedSprites.Clear();

        foreach (var (entity, sprite) in _spriteQuery)
        {
            if (sprite.Texture != null)
                _cachedSprites.Add((entity, sprite));
        }

        _lastTotalCount = _cachedSprites.Count;
        int culledCount = 0;

        // Queue all visible sprites to the batcher
        foreach (var item in _cachedSprites)
        {
            var sprite = item.Sprite;
            var transform = item.Entity.GetComponent<TransformComponent>();

            if (transform == null || !sprite.IsEnabled)
            {
                culledCount++;
                continue;
            }

            // Frustum culling: delegate to camera.IsVisible(Rectangle) so that camera rotation
            // and full transform scale are handled correctly.
            if (_camera != null)
            {
                var textureWidth = sprite.SourceRect?.Width ?? sprite.Texture?.Width ?? 0;
                var textureHeight = sprite.SourceRect?.Height ?? sprite.Texture?.Height ?? 0;
                var spriteScale = transform.Scale * sprite.Scale;
                var halfW = textureWidth * MathF.Abs(spriteScale.X) / 2f;
                var halfH = textureHeight * MathF.Abs(spriteScale.Y) / 2f;
                var pos = transform.Position + sprite.Offset;
                var spriteBounds = new Rectangle(pos.X - halfW, pos.Y - halfH, halfW * 2f, halfH * 2f);
                if (!_camera.IsVisible(spriteBounds))
                {
                    culledCount++;
                    continue;
                }
            }

            var position = transform.Position + sprite.Offset;
            var finalScale = transform.Scale * sprite.Scale;
            var flipX = sprite.FlipX;
            var flipY = sprite.FlipY;

            var flip = SpriteFlip.None;
            if (flipX) flip |= SpriteFlip.Horizontal;
            if (flipY) flip |= SpriteFlip.Vertical;

            // Ghost draws (outgoing frames of concurrent cross-fades) — rendered first so they appear behind.
            foreach (var ghost in sprite.CrossFadeGhosts)
            {
                if (ghost.Alpha <= 0f)
                    continue;

                var ghostTexture = ghost.Texture
                    ?? (ghost.TexturePath != null && _textureCache.TryGetValue(ghost.TexturePath, out var cached) ? cached : null);

                if (ghostTexture == null)
                    continue;

                var ghostFlip = SpriteFlip.None;
                if (ghost.FlipX) ghostFlip |= SpriteFlip.Horizontal;
                if (ghost.FlipY) ghostFlip |= SpriteFlip.Vertical;

                _batcher.Draw(
                    texture: ghostTexture,
                    position: transform.Position + ghost.DrawOffset,
                    sourceRect: ghost.SourceRect,
                    scale: transform.Scale * sprite.Scale,
                    rotation: transform.Rotation,
                    origin: ghost.Origin,
                    tint: ghost.Tint.WithAlpha(ghost.Alpha),
                    layer: sprite.Layer,
                    orderInLayer: sprite.OrderInLayer,
                    flip: ghostFlip,
                    blendMode: sprite.BlendMode);
            }

            _batcher.Draw(
                texture: sprite.Texture!,
                position: position,
                sourceRect: sprite.SourceRect,
                scale: finalScale,
                rotation: transform.Rotation,
                origin: sprite.Origin,
                tint: sprite.Tint,
                layer: sprite.Layer,
                orderInLayer: sprite.OrderInLayer,
                flip: flip,
                blendMode: sprite.BlendMode);
        }

        _lastRenderedCount = _lastTotalCount - culledCount;

        // Flush batch (sorts by layer/texture and renders)
        _batcher.Flush(renderer);
    }

    /// <summary>
    /// Gets batching statistics for performance monitoring.
    /// Returns (rendered sprites, batch/draw call count).
    /// </summary>
    public (int RenderedCount, int DrawCalls) GetBatchStats()
    {
        return (_lastRenderedCount, _batcher.EstimatedDrawCalls);
    }
    
    /// <summary>
    /// Gets the total sprite count (before culling).
    /// </summary>
    public int GetTotalSpriteCount()
    {
        return _lastTotalCount;
    }

    /// <summary>
    /// Disposes all cached textures.
    /// </summary>
    public void Dispose()
    {
        foreach (var texture in _textureCache.Values)
        {
            _textureLoader.UnloadTexture(texture);
        }
        _textureCache.Clear();
    }
}
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using System.Numerics;
using Brine2D.ECS.Systems;
using Brine2D.Engine;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// System that renders all entities with SpriteComponent.
/// Uses batching for optimal performance on both Legacy and GPU renderers.
/// Includes frustum culling to only render visible sprites.
/// </summary>
public class SpriteRenderingSystem : IRenderSystem
{
    public int RenderOrder => 0;
    public string Name => "SpriteRenderingSystem";

    private readonly ITextureLoader _textureLoader;
    private readonly ICamera? _camera;
    private readonly Dictionary<string, ITexture> _textureCache = new();
    private readonly SpriteBatcher _batcher = new();
    
    // Track stats for performance monitoring
    private int _lastRenderedCount = 0;
    private int _lastTotalCount = 0;

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
    /// <param name="world">The entity world to load textures for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadTexturesAsync(IEntityWorld world, CancellationToken cancellationToken = default)
    {
        var sprites = world.GetEntitiesWithComponent<SpriteComponent>();

        foreach (var entity in sprites)
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            if (sprite == null || string.IsNullOrEmpty(sprite.TexturePath))
                continue;

            // Load texture if not already loaded
            if (sprite.Texture == null && !_textureCache.ContainsKey(sprite.TexturePath))
            {
                var texture = await _textureLoader.LoadTextureAsync(
                    sprite.TexturePath,
                    TextureScaleMode.Nearest,
                    cancellationToken);

                _textureCache[sprite.TexturePath] = texture;
                sprite.Texture = texture;
            }
            else if (sprite.Texture == null && _textureCache.ContainsKey(sprite.TexturePath))
            {
                // Reuse cached texture
                sprite.Texture = _textureCache[sprite.TexturePath];
            }
        }
    }

    /// <summary>
    /// Renders all entities with SpriteComponent using batching.
    /// Automatically culls off-screen sprites when a camera is present.
    /// </summary>
    public void Render(IRenderer renderer, IEntityWorld world)
    {
        _cachedSprites.Clear();
        
        var sprites = world.GetEntitiesWithComponent<SpriteComponent>();
        
        foreach (var entity in sprites)
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            if (sprite != null && sprite.Texture != null)
            {
                _cachedSprites.Add((entity, sprite));
            }
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

            // Frustum culling (if camera exists)
            if (_camera != null && !IsVisible(transform.Position, sprite, _camera))
            {
                culledCount++;
                continue;
            }

            // Calculate final scale (transform scale * sprite scale)
            var finalScale = transform.Scale * sprite.Scale;
            
            // Apply flip by negating scale
            if (sprite.FlipX)
                finalScale.X *= -1;
            if (sprite.FlipY)
                finalScale.Y *= -1;

            // Add to batch
            _batcher.Draw(
                texture: sprite.Texture!,
                position: transform.Position + sprite.Offset,
                sourceRect: sprite.SourceRect,
                scale: finalScale,
                rotation: transform.Rotation,
                origin: new Vector2(0.5f, 0.5f), // Center origin
                tint: sprite.Tint,
                layer: sprite.Layer
            );
        }
        
        _lastRenderedCount = _lastTotalCount - culledCount;

        // Flush batch (sorts by layer/texture and renders)
        _batcher.Flush(renderer, _camera);
    }

    /// <summary>
    /// Checks if a sprite is visible within the camera frustum.
    /// Uses simple AABB (Axis-Aligned Bounding Box) culling.
    /// </summary>
    private bool IsVisible(Vector2 position, SpriteComponent sprite, ICamera camera)
    {
        // Calculate sprite bounds in world space
        var textureWidth = sprite.SourceRect?.Width ?? sprite.Texture?.Width ?? 0;
        var textureHeight = sprite.SourceRect?.Height ?? sprite.Texture?.Height ?? 0;
        
        var halfWidth = textureWidth * sprite.Scale / 2f;
        var halfHeight = textureHeight * sprite.Scale / 2f;

        // Calculate camera frustum bounds in world space
        // Camera position is CENTER of the view, not top-left!
        var cameraLeft = camera.Position.X - (camera.ViewportWidth / 2f / camera.Zoom);
        var cameraRight = camera.Position.X + (camera.ViewportWidth / 2f / camera.Zoom);
        var cameraTop = camera.Position.Y - (camera.ViewportHeight / 2f / camera.Zoom);
        var cameraBottom = camera.Position.Y + (camera.ViewportHeight / 2f / camera.Zoom);

        // AABB overlap test
        return position.X + halfWidth >= cameraLeft &&
               position.X - halfWidth <= cameraRight &&
               position.Y + halfHeight >= cameraTop &&
               position.Y - halfHeight <= cameraBottom;
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
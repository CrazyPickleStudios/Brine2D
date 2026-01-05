using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// System that renders all entities with SpriteComponent.
/// This is the bridge between ECS and Rendering.
/// </summary>
public class SpriteRenderingSystem: IRenderSystem
{
    public int RenderOrder => 0;

    private readonly IEntityWorld _world;
    private readonly ITextureLoader _textureLoader;
    private readonly Dictionary<string, ITexture> _textureCache = new();

    public SpriteRenderingSystem(IEntityWorld world, ITextureLoader textureLoader)
    {
        _world = world;
        _textureLoader = textureLoader;
    }

    /// <summary>
    /// Loads textures for all sprites that need them.
    /// </summary>
    public async Task LoadTexturesAsync(CancellationToken cancellationToken = default)
    {
        var sprites = _world.GetEntitiesWithComponent<SpriteComponent>();

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
                sprite.Texture = texture; // Direct assignment to ITexture
            }
            else if (sprite.Texture == null && _textureCache.ContainsKey(sprite.TexturePath))
            {
                // Reuse cached texture
                sprite.Texture = _textureCache[sprite.TexturePath];
            }
        }
    }

    /// <summary>
    /// Renders all entities with SpriteComponent.
    /// </summary>
    public void Render(IRenderer renderer)
    {
        var sprites = _world.GetEntitiesWithComponent<SpriteComponent>()
            .Select(e => new { Entity = e, Sprite = e.GetComponent<SpriteComponent>() })
            .Where(x => x.Sprite != null)
            .OrderBy(x => x.Sprite!.Layer);

        foreach (var item in sprites)
        {
            var sprite = item.Sprite!;
            var transform = item.Entity.GetComponent<TransformComponent>();

            // Use Texture property directly (not TextureHandle)
            if (sprite.Texture == null || transform == null)
                continue;

            var position = transform.WorldPosition + sprite.Offset;
            var scale = transform.Scale * sprite.Scale;

            if (sprite.SourceRect.HasValue)
            {
                var src = sprite.SourceRect.Value;
                var destWidth = src.Width * scale.X;
                var destHeight = src.Height * scale.Y;

                var drawX = position.X - destWidth / 2;
                var drawY = position.Y - destHeight / 2;

                renderer.DrawTexture(
                    sprite.Texture,
                    src.X, src.Y, src.Width, src.Height,
                    drawX, drawY, destWidth, destHeight
                );
            }
            else
            {
                var destWidth = sprite.Texture.Width * scale.X;
                var destHeight = sprite.Texture.Height * scale.Y;

                var drawX = position.X - destWidth / 2;
                var drawY = position.Y - destHeight / 2;

                renderer.DrawTexture(sprite.Texture, drawX, drawY, destWidth, destHeight);
            }
        }
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
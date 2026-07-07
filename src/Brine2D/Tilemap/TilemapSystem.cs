using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Tilemap;

/// <summary>
/// Updates tile animations and renders all <see cref="TilemapComponent"/> entities.
/// Textures are loaded asynchronously; a component is skipped by the renderer until <see cref="TilemapComponent.IsLoaded"/> is set.
/// </summary>
public sealed class TilemapSystem : IUpdateSystem, IRenderSystem, IDisposable
{
    public int UpdateOrder => SystemUpdateOrder.Animation;
    public int RenderOrder => SystemRenderOrder.Tilemap;
    public bool IsEnabled { get; set; } = true;

    private readonly ITextureLoader _textureLoader;
    private readonly ICamera? _camera;
    private readonly ILogger<TilemapSystem>? _logger;
    private readonly TilemapRenderer _renderer = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    private CachedEntityQuery<TilemapComponent>? _query;

    public TilemapSystem(ITextureLoader textureLoader, ICamera? camera = null, ILogger<TilemapSystem>? logger = null)
    {
        _textureLoader = textureLoader;
        _camera = camera;
        _logger = logger;
    }

    public void Update(IEntityWorld world, GameTime gameTime)
    {
        _query ??= world.CreateCachedQuery<TilemapComponent>().Build();

        foreach (var (_, component) in _query)
        {
            if (!component.IsEnabled || component.Tilemap == null)
                continue;

            if (component.Animator == null || !ReferenceEquals(component.Tilemap, component.InitializedTilemap))
            {
                component.Animator = null;
                component.IsLoaded = false;
                component.InitializedTilemap = null;

                var animator = new TilemapAnimator();
                animator.Initialize(component.Tilemap);
                component.Animator = animator;
                component.InitializedTilemap = component.Tilemap;

                var tilemap = component.Tilemap;
                _ = _renderer.LoadTilesetAsync(tilemap, _textureLoader, _cts.Token)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            _logger?.LogError(t.Exception, "Failed to load tileset textures for tilemap");
                        else if (ReferenceEquals(component.InitializedTilemap, tilemap))
                            component.IsLoaded = true;
                    }, TaskScheduler.Default);
            }

            component.Animator.Update((float)gameTime.DeltaTime);
        }
    }

    public void Render(IEntityWorld world, IRenderer renderer, GameTime gameTime)
    {
        if (_query == null) return;

        foreach (var (_, component) in _query)
        {
            if (!component.IsEnabled || component.Tilemap == null || !component.IsLoaded)
                continue;

            _renderer.Render(
                component.Tilemap,
                renderer,
                _camera,
                component.Animator,
                component.PositionOffset);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _renderer.UnloadAll(_textureLoader);
        _renderer.Dispose();
    }
}

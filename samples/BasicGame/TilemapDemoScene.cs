using Brine2D.Core;
using Brine2D.Core.Collision;
using Brine2D.Core.Tilemap;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace BasicGame;

/// <summary>
/// Demo scene showing tilemap rendering and collision.
/// </summary>
public class TilemapDemoScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly ITextureLoader _textureLoader;
    private readonly ITilemapLoader _tilemapLoader;
    private readonly TilemapRenderer _tilemapRenderer;
    private readonly CollisionSystem _collisionSystem;

    private Tilemap? _tilemap;
    private Camera2D? _camera;
    private Vector2 _cameraPosition = new Vector2(400, 300);

    public TilemapDemoScene(
        IRenderer renderer,
        IGameContext gameContext,
        IInputService input,
        ITextureLoader textureLoader,
        ITilemapLoader tilemapLoader,
        TilemapRenderer tilemapRenderer,
        CollisionSystem collisionSystem,
        ILogger<TilemapDemoScene> logger) : base(logger)
    {
        _renderer = renderer;
        _gameContext = gameContext;
        _input = input;
        _textureLoader = textureLoader;
        _tilemapLoader = tilemapLoader;
        _tilemapRenderer = tilemapRenderer;
        _collisionSystem = collisionSystem;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("Tilemap Demo initialized!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  Arrow Keys - Move camera");
        Logger.LogInformation("  Q/E - Zoom out/in");
        Logger.LogInformation("  ESC - Exit");

        // Camera is entity-like, created manually
        _camera = new Camera2D(1280, 720);
        _camera.Zoom = 1.0f;
        _renderer.Camera = _camera;
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Loading tilemap...");

        // Use injected loader (no manual construction!)
        _tilemap = await _tilemapLoader.LoadAsync("assets/maps/level1.tmj", cancellationToken);

        // Use injected renderer
        await _tilemapRenderer.LoadTilesetAsync(_tilemap, _textureLoader, cancellationToken);

        // Generate collision from tilemap (if it has a collision layer)
        var colliders = _tilemap.GenerateColliders("gameplay");
        foreach (var collider in colliders)
        {
            _collisionSystem.AddShape(collider);
        }

        Logger.LogInformation("Tilemap loaded successfully! ({ColliderCount} collision tiles)", colliders.Count);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }

        // Camera controls
        var speed = 300f;
        if (_input.IsKeyDown(Keys.Left)) _cameraPosition.X -= speed * deltaTime;
        if (_input.IsKeyDown(Keys.Right)) _cameraPosition.X += speed * deltaTime;
        if (_input.IsKeyDown(Keys.Up)) _cameraPosition.Y -= speed * deltaTime;
        if (_input.IsKeyDown(Keys.Down)) _cameraPosition.Y += speed * deltaTime;

        if (_camera != null)
        {
            _camera.Position = _cameraPosition;

            if (_input.IsKeyDown(Keys.Q))
                _camera.Zoom = Math.Max(0.5f, _camera.Zoom - 1.0f * deltaTime);
            if (_input.IsKeyDown(Keys.E))
                _camera.Zoom = Math.Min(3.0f, _camera.Zoom + 1.0f * deltaTime);
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(20, 20, 30));
        _renderer.BeginFrame();

        // Render tilemap using injected renderer
        if (_tilemap != null)
        {
            _tilemapRenderer.Render(_tilemap, _renderer, _camera);
        }

        _renderer.EndFrame();
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        _tilemapRenderer.UnloadAll(_textureLoader);
        return Task.CompletedTask;
    }
}
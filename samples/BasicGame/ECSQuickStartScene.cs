using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Serialization;
using Brine2D.ECS.Systems;
using Brine2D.Rendering.ECS;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace BasicGame;

/// <summary>
/// Minimal ECS example showing the basics:
/// - Creating entities
/// - Using prefabs
/// - Saving/loading
/// - Events
/// </summary>
public class ECSQuickStartScene : Scene
{
    private readonly IEntityWorld _world;
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;
    private readonly PrefabLibrary _prefabLibrary;
    private readonly EntitySerializer _serializer;
    private readonly EventBus _eventBus;
    private readonly UpdatePipeline _updatePipeline;
    private readonly RenderPipeline _renderPipeline;

    private Entity? _player;

    public ECSQuickStartScene(
        IEntityWorld world,
        UpdatePipeline updatePipeline, 
        RenderPipeline renderPipeline,
        IRenderer renderer,
        IInputService input,
        IGameContext gameContext,
        PrefabLibrary prefabLibrary,
        EntitySerializer serializer,
        EventBus eventBus,
        ILogger<ECSQuickStartScene> logger) : base(logger)
    {
        _world = world;
        _updatePipeline = updatePipeline;
        _renderPipeline = renderPipeline;
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
        _prefabLibrary = prefabLibrary;
        _serializer = serializer;
        _eventBus = eventBus;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("=== ECS Quick Start Demo ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move entities");
        Logger.LogInformation("  SPACE - Create bouncing coin");
        Logger.LogInformation("  F5 - Save game");
        Logger.LogInformation("  F9 - Load game");
        Logger.LogInformation("  ESC - Exit");

        // === 1. Create a simple entity manually ===
        _player = _world.CreateEntity("Player");
        _player.Tags.Add("Player");
        
        var transform = _player.AddComponent<TransformComponent>();
        transform.Position = new Vector2(400, 300);
        
        var velocity = _player.AddComponent<VelocityComponent>();
        velocity.MaxSpeed = 200f;

        Logger.LogInformation("Created player entity manually");

        // === 2. Create a prefab ===
        var coinPrefab = new EntityPrefab("Coin");
        coinPrefab.Tags.Add("Collectible");
        
        coinPrefab.AddComponent<TransformComponent>();
        
        coinPrefab.AddComponent<TweenComponent>(t =>
        {
            t.Type = TweenType.Position;
            t.StartPosition = Vector2.Zero;
            t.EndPosition = new Vector2(0, -50);
            t.Duration = 0.5f;
            t.Loop = true;
            t.PingPong = true;
            t.Easing = EasingType.EaseInOutQuad;
        });
        
        coinPrefab.AddComponent<LifetimeComponent>(l =>
        {
            l.Lifetime = 5f;
            l.AutoDestroy = true;
        });

        _prefabLibrary.Register(coinPrefab);

        Logger.LogInformation("Created coin prefab");

        // === 3. Subscribe to events ===
        _world.OnEntityCreated += (entity) =>
        {
            Logger.LogInformation("Entity created: {Name}", entity.Name);
        };

        _world.OnEntityDestroyed += (entity) =>
        {
            Logger.LogInformation("Entity destroyed: {Name}", entity.Name);
        };

        // Custom event example
        _eventBus.Subscribe<CoinCollectedEvent>(e =>
        {
            Logger.LogInformation("Coin collected! Total: {Count}", e.TotalCoins);
        });

        Logger.LogInformation("Event subscriptions setup");
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Handle input
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }

        // === Manual entity movement (since no PlayerControllerSystem in this demo) ===
        var movement = Vector2.Zero;
        if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
        if (_input.IsKeyDown(Keys.S)) movement.Y += 1;
        if (_input.IsKeyDown(Keys.A)) movement.X -= 1;
        if (_input.IsKeyDown(Keys.D)) movement.X += 1;

        if (movement != Vector2.Zero && _player != null)
        {
            var velocity = _player.GetComponent<VelocityComponent>();
            if (velocity != null)
            {
                velocity.Velocity = Vector2.Normalize(movement) * velocity.MaxSpeed;
            }
        }

        // === Spawn coin from prefab ===
        if (_input.IsKeyPressed(Keys.Space))
        {
            var coinPrefab = _prefabLibrary.Get("Coin");
            if (coinPrefab != null && _player != null)
            {
                var playerTransform = _player.GetComponent<TransformComponent>();
                var coin = coinPrefab.Instantiate(_world, playerTransform?.Position ?? Vector2.Zero);
                
                Logger.LogInformation("Spawned coin at player position");
            }
        }

        // === Save game ===
        if (_input.IsKeyPressed(Keys.F5))
        {
            _ = Task.Run(async () =>
            {
                await _serializer.SaveWorldAsync(_world, "saves/quickstart.json");
                Logger.LogInformation("Game saved!");
            });
        }

        // === Load game ===
        if (_input.IsKeyPressed(Keys.F9))
        {
            _ = Task.Run(async () =>
            {
                await _serializer.LoadAndRestoreWorldAsync(_world, "saves/quickstart.json");
                Logger.LogInformation("Game loaded!");
            });
        }

        // Execute update pipeline (runs VelocitySystem, etc.)
        _updatePipeline.Execute(gameTime);
        
        // Entity lifecycle updates (component OnUpdate)
        _world.Update(gameTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(30, 30, 50));
        _renderer.BeginFrame();

        // Execute render pipeline (if any systems registered)
        _renderPipeline.Execute(_renderer);

        // Manual rendering for this simple demo
        foreach (var entity in _world.Entities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                var color = entity.Tags.Contains("Player") ? Color.Green : Color.Yellow;
                _renderer.DrawCircle(transform.Position.X, transform.Position.Y, 10, color);
                _renderer.DrawText(entity.Name, transform.Position.X - 20, transform.Position.Y - 30, Color.White);
            }
        }

        _renderer.EndFrame();
    }
}

/// <summary>
/// Custom event example.
/// </summary>
public class CoinCollectedEvent
{
    public Entity? Coin { get; set; }
    public int TotalCoins { get; set; }
}
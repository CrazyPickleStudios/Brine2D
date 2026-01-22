using Brine2D.Core;
using Brine2D.Animation;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using Brine2D.Engine;
using Brine2D.Performance;

namespace FeatureDemos.Scenes.ECS;

/// <summary>
/// Demo scene showcasing all advanced query features:
/// - Spatial queries (WithinRadius, WithinBounds)
/// - Component filtering (With<T>(filter))
/// - Multi-tag queries (WithAllTags, WithAnyTag)
/// - Sorting (OrderBy, ThenBy)
/// - Pagination (Take, Skip)
/// - Random selection
/// - Query cloning
/// </summary>
public class QueryDemoScene : DemoSceneBase
{
    private readonly IEntityWorld _world;
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;

    private Entity? _player;
    private readonly List<Entity> _entities = new();
    private string _currentDemo = "Spatial";
    private int _demoIndex = 0;
    private readonly string[] _demos = 
    {
        "Spatial",      // WithinRadius
        "Filtering",    // With<T>(filter)
        "Tags",         // WithAllTags, WithAnyTag
        "Sorting",      // OrderBy, ThenBy
        "Pagination",   // Take, Skip
        "Random",       // Random()
        "Cloning"       // Clone()
    };

    public QueryDemoScene(
        IEntityWorld world,
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<QueryDemoScene> logger,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, logger, renderer, world, perfOverlay)
    {
        _world = world;
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("=== Query Demo Scene ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move player");
        Logger.LogInformation("  TAB - Cycle demos");
        Logger.LogInformation("  SPACE - Refresh query");
        Logger.LogInformation("  ESC - Exit");
        Logger.LogInformation("");
        Logger.LogInformation("Current Demo: {Demo}", _currentDemo);

        _renderer.ClearColor = Color.FromArgb(20, 20, 30);

        // Create player
        _player = _world.CreateEntity("Player");
        _player.Tags.Add("Player");
        
        var playerTransform = _player.AddComponent<TransformComponent>();
        playerTransform.Position = new Vector2(640, 360);

        // Create demo entities with various properties
        var random = new Random(42); // Fixed seed for consistency

        // Create 20 entities with random positions and "health"
        for (int i = 0; i < 20; i++)
        {
            var entity = _world.CreateEntity($"Entity_{i}");
            
            var transform = entity.AddComponent<TransformComponent>();
            transform.Position = new Vector2(
                random.Next(100, 1180),
                random.Next(100, 620));

            // Add a simple "health" component for filtering demos
            var healthComp = entity.AddComponent<HealthDemoComponent>();
            healthComp.Health = random.Next(10, 100);

            // Add random tags
            if (i % 3 == 0) entity.Tags.Add("Enemy");
            if (i % 5 == 0) entity.Tags.Add("Boss");
            if (i % 7 == 0) entity.Tags.Add("Elite");
            if (i % 2 == 0) entity.Tags.Add("Active");

            _entities.Add(entity);
        }

        Logger.LogInformation("Created {Count} demo entities", _entities.Count);
        RunCurrentDemo();

        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();
        var deltaTime = (float)gameTime.DeltaTime;

        if (CheckReturnToMenu()) return;

        // Cycle demos
        if (_input.IsKeyPressed(Keys.Tab))
        {
            _demoIndex = (_demoIndex + 1) % _demos.Length;
            _currentDemo = _demos[_demoIndex];
            Logger.LogInformation("\n=== Switched to Demo: {Demo} ===", _currentDemo);
            RunCurrentDemo();
        }

        // Refresh current demo
        if (_input.IsKeyPressed(Keys.Space))
        {
            Logger.LogInformation("\n=== Refreshing Demo: {Demo} ===", _currentDemo);
            RunCurrentDemo();
        }

        // Move player
        if (_player != null)
        {
            var transform = _player.GetComponent<TransformComponent>();
            if (transform != null)
            {
                var movement = Vector2.Zero;
                var speed = 300f;

                if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
                if (_input.IsKeyDown(Keys.S)) movement.Y += 1;
                if (_input.IsKeyDown(Keys.A)) movement.X -= 1;
                if (_input.IsKeyDown(Keys.D)) movement.X += 1;

                if (movement != Vector2.Zero)
                {
                    movement = Vector2.Normalize(movement);
                    transform.Position += movement * speed * deltaTime;
                }
            }
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Draw all entities (gray)
        foreach (var entity in _entities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                _renderer.DrawCircleFilled(
                    transform.Position.X, 
                    transform.Position.Y, 
                    8,
                    Color.FromArgb(100, 100, 100));
            }
        }

        // Highlight query results (green)
        var results = GetCurrentQueryResults();
        foreach (var entity in results)
        {
            var transform = entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                _renderer.DrawCircleFilled(
                    transform.Position.X, 
                    transform.Position.Y, 
                    12, 
                    Color.Green);
            }
        }

        // Draw player (blue)
        if (_player != null)
        {
            var transform = _player.GetComponent<TransformComponent>();
            if (transform != null)
            {
                _renderer.DrawCircleFilled(
                    transform.Position.X, 
                    transform.Position.Y, 
                    15, 
                    Color.Blue);

                // Draw radius for spatial demo (use outline)
                if (_currentDemo == "Spatial")
                {
                    _renderer.DrawCircleOutline(
                        transform.Position.X, 
                        transform.Position.Y, 
                        200, 
                        Color.Yellow);
                }
            }
        }

        // Draw UI
        _renderer.DrawText($"Demo: {_currentDemo} (TAB to cycle)", 10, 10, Color.White);
        _renderer.DrawText($"Results: {results.Count()} entities", 10, 35, Color.Yellow);
        _renderer.DrawText("SPACE: Refresh | WASD: Move Player", 10, 60, Color.Gray);

        RenderPerformanceOverlay();
    }

    private void RunCurrentDemo()
    {
        var results = GetCurrentQueryResults();

        switch (_currentDemo)
        {
            case "Spatial":
                DemoSpatialQueries();
                break;
            case "Filtering":
                DemoComponentFiltering();
                break;
            case "Tags":
                DemoTagQueries();
                break;
            case "Sorting":
                DemoSorting();
                break;
            case "Pagination":
                DemoPagination();
                break;
            case "Random":
                DemoRandom();
                break;
            case "Cloning":
                DemoCloning();
                break;
        }
    }

    private IEnumerable<Entity> GetCurrentQueryResults()
    {
        if (_player == null) return Enumerable.Empty<Entity>();
        
        var playerPos = _player.GetComponent<TransformComponent>()?.Position ?? Vector2.Zero;

        return _currentDemo switch
        {
            "Spatial" => _world.Query()
                .WithinRadius(playerPos, 200f)
                .Execute(),
            
            "Filtering" => _world.Query()
                .With<HealthDemoComponent>(h => h.Health < 50)
                .Execute(),
            
            "Tags" => _world.Query()
                .WithAllTags("Enemy", "Boss")
                .Execute(),
            
            "Sorting" => _world.Query()
                .With<HealthDemoComponent>()
                .OrderBy(e => e.GetComponent<HealthDemoComponent>()!.Health)
                .Take(5)
                .Execute(),
            
            "Pagination" => _world.Query()
                .With<TransformComponent>()
                .Skip(5)
                .Take(10)
                .Execute(),
            
            "Random" => new[] { _world.Query()
                .With<TransformComponent>()
                .Random() }
                .Where(e => e != null)
                .Cast<Entity>(),
            
            "Cloning" => GetCloningDemoResults(),
            
            _ => Enumerable.Empty<Entity>()
        };
    }

    private void DemoSpatialQueries()
    {
        Logger.LogInformation("--- Spatial Queries Demo ---");
        
        var playerPos = _player?.GetComponent<TransformComponent>()?.Position ?? Vector2.Zero;

        // WithinRadius
        var nearbyEntities = _world.Query()
            .WithinRadius(playerPos, 200f)
            .Execute();

        Logger.LogInformation("WithinRadius(playerPos, 200f): {Count} entities", 
            nearbyEntities.Count());

        // WithinBounds
        var boundsEntities = _world.Query()
            .WithinBounds(new Rectangle(300, 200, 400, 300))
            .Execute();

        Logger.LogInformation("WithinBounds(300, 200, 400, 300): {Count} entities", 
            boundsEntities.Count());

        // Combined
        var combined = _world.Query()
            .WithinRadius(playerPos, 200f)
            .WithTag("Enemy")
            .Execute();

        Logger.LogInformation("Within 200 units + Enemy tag: {Count} entities", 
            combined.Count());
    }

    private void DemoComponentFiltering()
    {
        Logger.LogInformation("--- Component Filtering Demo ---");

        // Filter by component property
        var lowHealth = _world.Query()
            .With<HealthDemoComponent>(h => h.Health < 50)
            .Execute();

        Logger.LogInformation("With<Health>(h => h.Health < 50): {Count} entities", 
            lowHealth.Count());

        // Multiple filters
        var mediumHealth = _world.Query()
            .With<HealthDemoComponent>(h => h.Health >= 30 && h.Health <= 70)
            .Execute();

        Logger.LogInformation("Health between 30-70: {Count} entities", 
            mediumHealth.Count());

        // Log some results
        foreach (var entity in lowHealth.Take(3))
        {
            var health = entity.GetComponent<HealthDemoComponent>();
            Logger.LogInformation("  - {Name}: Health={Health}", 
                entity.Name, health?.Health);
        }
    }

    private void DemoTagQueries()
    {
        Logger.LogInformation("--- Tag Queries Demo ---");

        // WithAllTags
        var bosses = _world.Query()
            .WithAllTags("Enemy", "Boss")
            .Execute();

        Logger.LogInformation("WithAllTags('Enemy', 'Boss'): {Count} entities", 
            bosses.Count());

        // WithAnyTag
        var targets = _world.Query()
            .WithAnyTag("Enemy", "Elite")
            .Execute();

        Logger.LogInformation("WithAnyTag('Enemy', 'Elite'): {Count} entities", 
            targets.Count());

        // WithoutTag
        var nonEnemies = _world.Query()
            .With<TransformComponent>()
            .WithoutTag("Enemy")
            .WithoutTag("Player")
            .Execute();

        Logger.LogInformation("Without 'Enemy' or 'Player' tag: {Count} entities", 
            nonEnemies.Count());
    }

    private void DemoSorting()
    {
        Logger.LogInformation("--- Sorting Demo ---");

        var playerPos = _player?.GetComponent<TransformComponent>()?.Position ?? Vector2.Zero;

        // OrderBy health
        var weakest = _world.Query()
            .With<HealthDemoComponent>()
            .OrderBy(e => e.GetComponent<HealthDemoComponent>()!.Health)
            .Take(5)
            .Execute();

        Logger.LogInformation("OrderBy(health) - Top 5 weakest:");
        foreach (var entity in weakest)
        {
            var health = entity.GetComponent<HealthDemoComponent>();
            Logger.LogInformation("  - {Name}: Health={Health}", 
                entity.Name, health?.Health);
        }

        // OrderBy with ThenBy
        var sorted = _world.Query()
            .WithTag("Enemy")
            .With<HealthDemoComponent>()
            .OrderBy(e => e.GetComponent<HealthDemoComponent>()!.Health)
            .ThenBy(e => Vector2.Distance(
                e.GetComponent<TransformComponent>()!.Position, 
                playerPos))
            .Take(3)
            .Execute();

        Logger.LogInformation("\nOrderBy(health).ThenBy(distance) - Top 3:");
        foreach (var entity in sorted)
        {
            var health = entity.GetComponent<HealthDemoComponent>();
            var distance = Vector2.Distance(
                entity.GetComponent<TransformComponent>()!.Position, 
                playerPos);
            Logger.LogInformation("  - {Name}: Health={Health}, Distance={Distance:F1}", 
                entity.Name, health?.Health, distance);
        }
    }

    private void DemoPagination()
    {
        Logger.LogInformation("--- Pagination Demo ---");

        var total = _world.Query()
            .With<TransformComponent>()
            .Count();

        Logger.LogInformation("Total entities: {Count}", total);

        // Page 1
        var page1 = _world.Query()
            .With<TransformComponent>()
            .Take(5)
            .Execute();

        Logger.LogInformation("Page 1 (Take 5): {Count} entities", page1.Count());

        // Page 2
        var page2 = _world.Query()
            .With<TransformComponent>()
            .Skip(5)
            .Take(5)
            .Execute();

        Logger.LogInformation("Page 2 (Skip 5, Take 5): {Count} entities", page2.Count());

        // Page 3
        var page3 = _world.Query()
            .With<TransformComponent>()
            .Skip(10)
            .Take(5)
            .Execute();

        Logger.LogInformation("Page 3 (Skip 10, Take 5): {Count} entities", page3.Count());
    }

    private void DemoRandom()
    {
        Logger.LogInformation("--- Random Selection Demo ---");

        // Random single
        var randomEntity = _world.Query()
            .With<TransformComponent>()
            .Random();

        if (randomEntity != null)
        {
            Logger.LogInformation("Random(): {Name}", randomEntity.Name);
        }

        // Random multiple
        var random3 = _world.Query()
            .WithTag("Enemy")
            .Random(3)
            .Execute();

        Logger.LogInformation("Random(3) enemies:");
        foreach (var entity in random3)
        {
            Logger.LogInformation("  - {Name}", entity.Name);
        }
    }

    private void DemoCloning()
    {
        Logger.LogInformation("--- Query Cloning Demo ---");

        // Base query
        var baseQuery = _world.Query()
            .With<HealthDemoComponent>()
            .WithTag("Enemy");

        Logger.LogInformation("Base query (Enemy + Health): {Count} entities", 
            baseQuery.Count());

        // Clone and modify
        var weakEnemies = baseQuery.Clone()
            .With<HealthDemoComponent>(h => h.Health < 50)
            .Execute();

        Logger.LogInformation("Clone + low health filter: {Count} entities", 
            weakEnemies.Count());

        var strongEnemies = baseQuery.Clone()
            .With<HealthDemoComponent>(h => h.Health > 70)
            .OrderByDescending(e => e.GetComponent<HealthDemoComponent>()!.Health)
            .Execute();

        Logger.LogInformation("Clone + high health + sort: {Count} entities", 
            strongEnemies.Count());

        // Original query unchanged
        Logger.LogInformation("Original query still: {Count} entities", 
            baseQuery.Count());
    }

    private IEnumerable<Entity> GetCloningDemoResults()
    {
        var baseQuery = _world.Query()
            .With<HealthDemoComponent>()
            .WithTag("Enemy");

        return baseQuery.Clone()
            .With<HealthDemoComponent>(h => h.Health < 50)
            .Execute();
    }
}

/// <summary>
/// Simple health component for demo purposes.
/// </summary>
public class HealthDemoComponent : Component
{
    public int Health { get; set; } = 100;
}

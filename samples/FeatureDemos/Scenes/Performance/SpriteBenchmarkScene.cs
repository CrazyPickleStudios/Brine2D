using System.Drawing;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Brine2D.ECS.Systems;
using Brine2D.Systems.Rendering;
using Brine2D.Engine;
using Brine2D.Engine.Systems;
using Brine2D.Performance;
using Brine2D.Systems.Physics;

namespace FeatureDemos.Scenes.Performance;

/// <summary>
/// Benchmark scene for testing sprite rendering performance.
/// Spawns increasing numbers of sprites to measure batching, culling, and multi-threaded query performance.
/// Use UP/DOWN to add/remove sprites and observe FPS impact.
/// Use WASD to move camera and see culling in action!
/// </summary>
public class SpriteBenchmarkScene : DemoSceneBase
{
    private readonly ITextureLoader _textureLoader;
    private readonly DebugRenderer? _debugRenderer;
    private readonly ICamera? _camera;
    private ITexture? _sharedTexture;
    private int _spriteCount = 100;
    private bool _showCullingVisualization = false;
    private bool _enableAnimation = true;
    private System.Diagnostics.Stopwatch _cpuWorkStopwatch = new();

    public SpriteBenchmarkScene(
        ITextureLoader textureLoader,
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay,
        DebugRenderer? debugRenderer,
        ICamera? camera)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
        _textureLoader = textureLoader;
        _debugRenderer = debugRenderer;
        _camera = camera;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("=== Sprite Rendering Benchmark ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  UP/DOWN - Add/Remove 100 sprites");
        Logger.LogInformation("  LEFT/RIGHT - Add/Remove 1000 sprites");
        Logger.LogInformation("  WASD - Move camera (see culling in action!)");
        Logger.LogInformation("  Q/E - Zoom camera");
        Logger.LogInformation("  C - Toggle culling visualization");
        Logger.LogInformation("  SPACE - Toggle sprite animation (tests multi-threading!)");
        Logger.LogInformation("  R - Reset to 100 sprites");
        Logger.LogInformation("  F3 - Toggle detailed stats");
        Logger.LogInformation("  F4 - Toggle system profiling");
        Logger.LogInformation("  F1 - Toggle overlay");
        Logger.LogInformation("  ESC - Return to menu");
        
        Renderer.ClearColor = Color.FromArgb(20, 20, 30);
        
        // Setup camera for culling
        if (_camera != null)
        {
            _camera.Position = new Vector2(640, 360); // Center of 1280x720
            _camera.Zoom = 1.0f;
            Renderer.Camera = _camera;
            Logger.LogInformation("Camera initialized for frustum culling");
        }
        
        // Show performance overlay by default with detailed stats
        if (PerfOverlay != null)
        {
            PerfOverlay.IsVisible = true;
            PerfOverlay.ShowDetailedStats = true;
        }

        // Disable debug rendering for cleaner benchmark
        if (_debugRenderer != null)
        {
            _debugRenderer.ShowEntityNames = false;
            _debugRenderer.ShowColliders = false;
            _debugRenderer.ShowVelocities = false;
            _debugRenderer.ShowAIDebug = false;
            Logger.LogInformation("Debug rendering disabled for benchmark");
        }

        // Spawn initial sprites
        SpawnSprites(_spriteCount);

        return Task.CompletedTask;
    }
    
    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Loading shared sprite texture...");
            _sharedTexture = await _textureLoader.LoadTextureAsync(
                "assets/images/logo.png", 
                TextureScaleMode.Nearest, 
                cancellationToken);
            
            Logger.LogInformation("Texture loaded: {Width}x{Height}", 
                _sharedTexture.Width, _sharedTexture.Height);
            
            ApplyTextureToAllSprites();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load texture! Sprites will not render.");
        }
    }
    
    private void ApplyTextureToAllSprites()
    {
        if (World == null || _sharedTexture == null) return;
        
        var sprites = World.GetEntitiesWithComponent<SpriteComponent>();
        int count = 0;
        
        foreach (var entity in sprites)
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            if (sprite != null && sprite.Texture == null)
            {
                sprite.Texture = _sharedTexture;
                count++;
            }
        }
        
        Logger.LogDebug("Applied texture to {Count} sprites", count);
    }
    
    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();
        
        if (CheckReturnToMenu()) return;
        
        var deltaTime = (float)gameTime.DeltaTime;
        
        // Camera movement (WASD)
        if (_camera != null)
        {
            const float cameraSpeed = 500f;
            
            if (Input.IsKeyDown(Key.W))
                _camera.Position += new Vector2(0, -cameraSpeed * deltaTime);
            if (Input.IsKeyDown(Key.S))
                _camera.Position += new Vector2(0, cameraSpeed * deltaTime);
            if (Input.IsKeyDown(Key.A))
                _camera.Position += new Vector2(-cameraSpeed * deltaTime, 0);
            if (Input.IsKeyDown(Key.D))
                _camera.Position += new Vector2(cameraSpeed * deltaTime, 0);
            
            // Camera zoom (Q/E)
            if (Input.IsKeyDown(Key.Q))
                _camera.Zoom = Math.Max(0.1f, _camera.Zoom - deltaTime);
            if (Input.IsKeyDown(Key.E))
                _camera.Zoom = Math.Min(3.0f, _camera.Zoom + deltaTime);
        }
        
        // Toggle culling visualization
        if (Input.IsKeyPressed(Key.C))
        {
            _showCullingVisualization = !_showCullingVisualization;
            Logger.LogInformation("Culling visualization: {State}", 
                _showCullingVisualization ? "ON" : "OFF");
        }
        
        // Toggle animation (tests multi-threaded performance)
        if (Input.IsKeyPressed(Key.Space))
        {
            _enableAnimation = !_enableAnimation;
            Logger.LogInformation("Animation: {State} (Press F4 to see system timings!)", 
                _enableAnimation ? "ON" : "OFF");
        }
        
        // Add 100 sprites
        if (Input.IsKeyPressed(Key.Up))
        {
            SpawnSprites(100);
            _spriteCount += 100;
            Logger.LogInformation("Sprites: {Count}", _spriteCount);
        }
        
        // Remove 100 sprites
        if (Input.IsKeyPressed(Key.Down) && _spriteCount > 100)
        {
            RemoveSprites(100);
            _spriteCount -= 100;
            Logger.LogInformation("Sprites: {Count}", _spriteCount);
        }
        
        // Add 1000 sprites (stress test)
        if (Input.IsKeyPressed(Key.Right))
        {
            SpawnSprites(1000);
            _spriteCount += 1000;
            Logger.LogInformation("Sprites: {Count} (Stress Test!)", _spriteCount);
        }
        
        // Remove 1000 sprites
        if (Input.IsKeyPressed(Key.Left) && _spriteCount > 1000)
        {
            RemoveSprites(1000);
            _spriteCount -= 1000;
            Logger.LogInformation("Sprites: {Count}", _spriteCount);
        }
        
        // Reset to 100
        if (Input.IsKeyPressed(Key.R))
        {
            RemoveAllSprites();
            SpawnSprites(100);
            _spriteCount = 100;
            Logger.LogInformation("Reset to {Count} sprites", _spriteCount);
        }
        
        if (_enableAnimation && World != null)
        {
            AnimateSprites(gameTime);
        }
    }
    
    /// <summary>
    /// Animates sprites using the new component-based ForEach.
    /// This automatically parallelizes when entity count > 100!
    /// Press F4 to see system timings.
    /// </summary>
    private void AnimateSprites(GameTime gameTime)
    {
        var time = gameTime.TotalTime;
        var deltaTime = (float)gameTime.DeltaTime;
        
        World!.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .ForEach((Entity entity, TransformComponent transform, VelocityComponent velocity) =>
            {
                // Update position (bounces off boundaries)
                transform.Position += velocity.Velocity * deltaTime;
                
                // Bounce off edges
                if (transform.Position.X < -2000 || transform.Position.X > 3280)
                    velocity.Velocity = new Vector2(-velocity.Velocity.X, velocity.Velocity.Y);
                
                if (transform.Position.Y < -2000 || transform.Position.Y > 2720)
                    velocity.Velocity = new Vector2(velocity.Velocity.X, -velocity.Velocity.Y);
                
                // Subtle rotation based on velocity
                transform.Rotation += velocity.Velocity.Length() * deltaTime * 0.01f;
            });
        
        // Also animate sprite colors (demonstrates 3-component ForEach)
        World!.Query()
            .With<SpriteComponent>()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .ForEach((Entity entity, SpriteComponent sprite, TransformComponent transform, VelocityComponent velocity) =>
            {
                // Pulse color based on speed
                var speed = velocity.Velocity.Length();
                var intensity = (byte)(100 + (speed / 10f) * 155);
                
                sprite.Tint = Color.FromArgb(
                    intensity,
                    (byte)(255 - intensity / 2),
                    (byte)(200 - intensity / 3));
            });
    }

    /// <summary>
    /// Heavy CPU workload to test multi-threading.
    /// Simulates expensive per-entity logic (e.g., pathfinding, complex AI).
    /// </summary>
    private void HeavyCPUWorkload(GameTime gameTime)
    {
        _cpuWorkStopwatch.Restart();
        
        var deltaTime = (float)gameTime.DeltaTime;
        
        World!.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .ForEach((Entity entity, TransformComponent transform, VelocityComponent velocity) =>
            {
                // Heavy workload
                var result = 0.0;
                for (int i = 0; i < 1000; i++)
                {
                    result += Math.Sin(transform.Position.X + i) * Math.Cos(transform.Position.Y + i);
                }
                
                transform.Position += velocity.Velocity * deltaTime * (float)result * 0.0001f;
                
                if (transform.Position.X < -2000 || transform.Position.X > 3280)
                    velocity.Velocity = new Vector2(-velocity.Velocity.X, velocity.Velocity.Y);
                
                if (transform.Position.Y < -2000 || transform.Position.Y > 2720)
                    velocity.Velocity = new Vector2(velocity.Velocity.X, -velocity.Velocity.Y);
            });
        
        _cpuWorkStopwatch.Stop();
        Logger.LogInformation("CPU Work Time: {Time:F2}ms", _cpuWorkStopwatch.Elapsed.TotalMilliseconds);
    }

    protected override void OnRender(GameTime gameTime)
    {
        if (_showCullingVisualization && _camera != null)
        {
            DrawCullingVisualization();
        }
        
        RenderPerformanceOverlay();
        
        // Draw instructions
        var instructionY = 300;
        Renderer.DrawText($"Sprites: {_spriteCount}", 10, instructionY, Color.White);
        instructionY += 25;
        
        if (_camera != null)
        {
            Renderer.DrawText($"Camera: ({_camera.Position.X:F0}, {_camera.Position.Y:F0}) Zoom: {_camera.Zoom:F2}x", 
                10, instructionY, Color.FromArgb(100, 200, 255));
            instructionY += 25;
        }
        
        // Show animation status
        Renderer.DrawText($"Animation: {(_enableAnimation ? "ON" : "OFF")} (SPACE)", 
            10, instructionY, _enableAnimation ? Color.FromArgb(0, 255, 100) : Color.Gray);
        instructionY += 25;
        
        Renderer.DrawText("Controls:", 10, instructionY, Color.FromArgb(255, 255, 100));
        instructionY += 20;
        
        Renderer.DrawText("UP/DOWN: +/- 100 sprites", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        Renderer.DrawText("LEFT/RIGHT: +/- 1000 sprites", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        Renderer.DrawText("WASD: Move camera | Q/E: Zoom", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        Renderer.DrawText($"C: Culling Viz ({(_showCullingVisualization ? "ON" : "OFF")})", 
            10, instructionY, Color.Gray);
        instructionY += 20;
        
        Renderer.DrawText("SPACE: Toggle Animation", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        Renderer.DrawText("F4: System Profiling", 10, instructionY, Color.FromArgb(255, 200, 0));
        instructionY += 20;
        
        Renderer.DrawText("R: Reset", 10, instructionY, Color.Gray);
    }
    
    private void DrawCullingVisualization()
    {
        if (_camera == null) return;
        
        var halfWidth = _camera.ViewportWidth / 2f / _camera.Zoom;
        var halfHeight = _camera.ViewportHeight / 2f / _camera.Zoom;
        
        var left = _camera.Position.X - halfWidth;
        var top = _camera.Position.Y - halfHeight;
        var width = halfWidth * 2;
        var height = halfHeight * 2;
        
        Renderer.DrawRectangleOutline(left, top, width, height,
            Color.FromArgb(150, 0, 255, 0), 4f);
        
        Renderer.DrawText("Frustum (everything inside is rendered)", 
            left + 10, top + 10, Color.FromArgb(0, 255, 0));
    }
    
    private void SpawnSprites(int count)
    {
        if (World == null) return;
        
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var sprite = World.CreateEntity($"BenchSprite_{_spriteCount + i}");
            
            // Add transform - spawn in large area
            var transform = sprite.AddComponent<TransformComponent>();
            transform.Position = new Vector2(
                random.Next(-2000, 3280),
                random.Next(-2000, 2720));
            transform.Scale = new Vector2(
                0.5f + (float)random.NextDouble() * 1.0f, 
                0.5f + (float)random.NextDouble() * 1.0f);
            
            var velocity = sprite.AddComponent<VelocityComponent>();
            velocity.Velocity = new Vector2(
                (float)(random.NextDouble() - 0.5) * 200f,
                (float)(random.NextDouble() - 0.5) * 200f);
            
            // Add sprite component with random color tint
            var spriteComp = sprite.AddComponent<SpriteComponent>();
            spriteComp.Tint = Color.FromArgb(
                (byte)random.Next(100, 255),
                (byte)random.Next(100, 255),
                (byte)random.Next(100, 255));
            spriteComp.Layer = random.Next(0, 3);
            
            if (_sharedTexture != null)
            {
                spriteComp.Texture = _sharedTexture;
            }
        }
    }
    
    private void RemoveSprites(int count)
    {
        if (World == null) return;
        
        var sprites = World.GetEntitiesWithComponent<SpriteComponent>()
            .Take(count)
            .ToList();
        
        foreach (var sprite in sprites)
        {
            sprite.Destroy();
        }
    }
    
    private void RemoveAllSprites()
    {
        if (World == null) return;
        
        var sprites = World.GetEntitiesWithComponent<SpriteComponent>().ToList();
        
        foreach (var sprite in sprites)
        {
            sprite.Destroy();
        }
        
        _spriteCount = 0;
    }

    protected override void ConfigureSystems(ISystemConfigurator systems)
    {
        // Add benchmark system ONLY for this scene
        systems.AddUpdateSystem<BenchmarkSystem>();
        
        // Optional: Disable VelocitySystem since BenchmarkSystem replaces it
        // systems.DisableSystem<VelocitySystem>();
    }
}
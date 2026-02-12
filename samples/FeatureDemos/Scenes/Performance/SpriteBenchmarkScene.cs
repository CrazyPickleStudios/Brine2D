using Brine2D.Assets;
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
    private readonly IAssetLoader _assetLoader;
    private readonly DebugRenderer? _debugRenderer;
    private readonly ICamera? _camera;
    private ITexture? _sharedTexture;
    private int _spriteCount = 100;
    private bool _showCullingVisualization = false;
    private bool _enableAnimation = true;
    private System.Diagnostics.Stopwatch _cpuWorkStopwatch = new();

    public SpriteBenchmarkScene(
        IAssetLoader assetLoader,
        IInputContext input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay,
        DebugRenderer? debugRenderer,
        ICamera? camera)
        : base(input, sceneManager, gameContext, perfOverlay)
    {
        _assetLoader = assetLoader;
        _debugRenderer = debugRenderer;
        _camera = camera;
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
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
        
        Renderer.ClearColor = new Color(20, 20, 30);
        
        // Setup camera
        if (_camera != null)
        {
            _camera.Position = new Vector2(640, 360);
            _camera.Zoom = 1.0f;
            Renderer.Camera = _camera;
            Logger.LogInformation("Camera initialized for frustum culling");
        }
        
        // Show performance overlay
        if (PerfOverlay != null)
        {
            PerfOverlay.IsVisible = true;
            PerfOverlay.ShowDetailedStats = true;
        }

        // Disable debug rendering
        if (_debugRenderer != null)
        {
            _debugRenderer.ShowEntityNames = false;
            _debugRenderer.ShowColliders = false;
            _debugRenderer.ShowVelocities = false;
            _debugRenderer.ShowAIDebug = false;
            Logger.LogInformation("Debug rendering disabled for benchmark");
        }

        try
        {
            Logger.LogInformation("Loading shared sprite texture...");
            _sharedTexture = await _assetLoader.LoadTextureAsync(
                "assets/images/logo.png",
                TextureScaleMode.Nearest,
                cancellationToken: cancellationToken);
            Logger.LogInformation("Texture loaded: {Width}x{Height}", _sharedTexture.Width, _sharedTexture.Height);
            ApplyTextureToAllSprites();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load texture!");
        }

        // Spawn sprites
        SpawnSprites(_spriteCount);
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
                
                sprite.Tint = new Color(
                    intensity,
                    (byte)(255 - intensity / 2),
                    (byte)(200 - intensity / 3));
            });
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
                10, instructionY, new Color(100, 200, 255));
            instructionY += 25;
        }
        
        // Show animation status
        Renderer.DrawText($"Animation: {(_enableAnimation ? "ON" : "OFF")} (SPACE)", 
            10, instructionY, _enableAnimation ? new Color(0, 255, 100) : Color.Gray);
        instructionY += 25;
        
        Renderer.DrawText("Controls:", 10, instructionY, new Color(255, 255, 100));
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
        
        Renderer.DrawText("F4: System Profiling", 10, instructionY, new Color(255, 200, 0));
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
            new Color(0, 255, 0, 150), 4f);
        
        Renderer.DrawText("Frustum (everything inside is rendered)", 
            left + 10, top + 10, new Color(0, 255, 0));
    }
    
    private void SpawnSprites(int count)
    {
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var entity = World.CreateEntity($"Sprite_{i}");
            
            entity.AddComponent<TransformComponent>();
            entity.AddComponent<SpriteComponent>();
            entity.AddComponent<VelocityComponent>();
            
            var transform = entity.GetComponent<TransformComponent>()!;
            var sprite = entity.GetComponent<SpriteComponent>()!;
            var velocity = entity.GetComponent<VelocityComponent>()!;
            
            // Configure transform
            transform.Position = new Vector2(
                random.Next(-2000, 3280),
                random.Next(-2000, 2720));
            transform.Scale = new Vector2(
                0.5f + (float)random.NextDouble() * 0.5f,
                0.5f + (float)random.NextDouble() * 0.5f);
            transform.Rotation = (float)(random.NextDouble() * Math.PI * 2);
            
            // Configure sprite
            if (_sharedTexture != null)
            {
                sprite.Texture = _sharedTexture;
            }
            sprite.Tint = new Color(
                (byte)random.Next(150, 255),
                (byte)random.Next(150, 255),
                (byte)random.Next(150, 255));
            
            // Configure velocity
            velocity.Velocity = new Vector2(
                (float)(random.NextDouble() - 0.5) * 200,
                (float)(random.NextDouble() - 0.5) * 200);
        }
        
        Logger.LogInformation("Spawned {Count} sprites", count);
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
        // TEMPORARILY DISABLE to test
        // systems.AddUpdateSystem<BenchmarkSystem>();
    }
}
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.ECS;
using Brine2D.Rendering.Performance;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace FeatureDemos.Scenes.Performance;

/// <summary>
/// Benchmark scene for testing sprite rendering performance.
/// Spawns increasing numbers of sprites to measure batching and culling efficiency.
/// Use UP/DOWN to add/remove sprites and observe FPS impact.
/// Use WASD to move camera and see culling in action!
/// </summary>
public class SpriteBenchmarkScene : DemoSceneBase
{
    private readonly IRenderer _renderer;
    private readonly ITextureLoader _textureLoader;
    private readonly DebugRenderer? _debugRenderer;
    private readonly ICamera? _camera; // Add camera for culling
    private ITexture? _sharedTexture;
    private int _spriteCount = 100;
    private bool _showCullingVisualization = false; // Toggle culling visualization
    
    public SpriteBenchmarkScene(
        IEntityWorld world,
        IRenderer renderer,
        ITextureLoader textureLoader,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        PerformanceOverlay? perfOverlay,
        DebugRenderer? debugRenderer,
        ICamera? camera, // Add camera parameter
        ILogger<SpriteBenchmarkScene> logger)
        : base(input, sceneManager, gameContext, logger, renderer, world, perfOverlay)
    {
        _renderer = renderer;
        _textureLoader = textureLoader;
        _debugRenderer = debugRenderer;
        _camera = camera;
    }
    
    protected override void OnInitialize()
    {
        Logger.LogInformation("=== Sprite Rendering Benchmark ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  UP/DOWN - Add/Remove 100 sprites");
        Logger.LogInformation("  LEFT/RIGHT - Add/Remove 1000 sprites");
        Logger.LogInformation("  WASD - Move camera (see culling in action!)");
        Logger.LogInformation("  Q/E - Zoom camera");
        Logger.LogInformation("  C - Toggle culling visualization");
        Logger.LogInformation("  R - Reset to 100 sprites");
        Logger.LogInformation("  F3 - Toggle detailed stats");
        Logger.LogInformation("  F1 - Toggle overlay");
        Logger.LogInformation("  ESC - Return to menu");
        
        _renderer.ClearColor = new Color(20, 20, 30);
        
        // Setup camera for culling
        if (_camera != null)
        {
            _camera.Position = new Vector2(640, 360); // Center of 1280x720
            _camera.Zoom = 1.0f;
            _renderer.Camera = _camera; // Assign to renderer
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

        // Spawn initial sprites across a large area
        SpawnSprites(_spriteCount);
    }
    
    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // Load the shared texture once
        try
        {
            Logger.LogInformation("Loading shared sprite texture...");
            _sharedTexture = await _textureLoader.LoadTextureAsync(
                "assets/images/logo.png", 
                TextureScaleMode.Nearest, 
                cancellationToken);
            
            Logger.LogInformation("Texture loaded: {Width}x{Height}", 
                _sharedTexture.Width, _sharedTexture.Height);
            
            // Apply to all existing sprites
            ApplyTextureToAllSprites();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load texture! Sprites will not render.");
        }
    }
    
    /// <summary>
    /// Applies the shared texture to all sprites in the world.
    /// </summary>
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
            
            if (Input.IsKeyDown(Keys.W))
                _camera.Position += new Vector2(0, -cameraSpeed * deltaTime);
            if (Input.IsKeyDown(Keys.S))
                _camera.Position += new Vector2(0, cameraSpeed * deltaTime);
            if (Input.IsKeyDown(Keys.A))
                _camera.Position += new Vector2(-cameraSpeed * deltaTime, 0);
            if (Input.IsKeyDown(Keys.D))
                _camera.Position += new Vector2(cameraSpeed * deltaTime, 0);
            
            // Camera zoom (Q/E)
            if (Input.IsKeyDown(Keys.Q))
                _camera.Zoom = Math.Max(0.1f, _camera.Zoom - deltaTime);
            if (Input.IsKeyDown(Keys.E))
                _camera.Zoom = Math.Min(3.0f, _camera.Zoom + deltaTime);
        }
        
        // Toggle culling visualization
        if (Input.IsKeyPressed(Keys.C))
        {
            _showCullingVisualization = !_showCullingVisualization;
            Logger.LogInformation("Culling visualization: {State}", 
                _showCullingVisualization ? "ON" : "OFF");
        }
        
        // Add 100 sprites
        if (Input.IsKeyPressed(Keys.Up))
        {
            SpawnSprites(100);
            _spriteCount += 100;
            Logger.LogInformation("Sprites: {Count}", _spriteCount);
        }
        
        // Remove 100 sprites
        if (Input.IsKeyPressed(Keys.Down) && _spriteCount > 100)
        {
            RemoveSprites(100);
            _spriteCount -= 100;
            Logger.LogInformation("Sprites: {Count}", _spriteCount);
        }
        
        // Add 1000 sprites (stress test)
        if (Input.IsKeyPressed(Keys.Right))
        {
            SpawnSprites(1000);
            _spriteCount += 1000;
            Logger.LogInformation("Sprites: {Count} (Stress Test!)", _spriteCount);
        }
        
        // Remove 1000 sprites
        if (Input.IsKeyPressed(Keys.Left) && _spriteCount > 1000)
        {
            RemoveSprites(1000);
            _spriteCount -= 1000;
            Logger.LogInformation("Sprites: {Count}", _spriteCount);
        }
        
        // Reset to 100
        if (Input.IsKeyPressed(Keys.R))
        {
            RemoveAllSprites();
            SpawnSprites(100);
            _spriteCount = 100;
            Logger.LogInformation("Reset to {Count} sprites", _spriteCount);
        }
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        // Draw culling visualization if enabled
        if (_showCullingVisualization && _camera != null)
        {
            DrawCullingVisualization();
        }
        
        RenderPerformanceOverlay();
        
        // Draw instructions (positioned to not overlap with performance overlay)
        var instructionY = 300;
        _renderer.DrawText($"Sprites: {_spriteCount}", 10, instructionY, Color.White);
        instructionY += 25;
        
        if (_camera != null)
        {
            _renderer.DrawText($"Camera: ({_camera.Position.X:F0}, {_camera.Position.Y:F0}) Zoom: {_camera.Zoom:F2}x", 
                10, instructionY, new Color(100, 200, 255));
            instructionY += 25;
        }
        
        _renderer.DrawText("Controls:", 10, instructionY, new Color(255, 255, 100));
        instructionY += 20;
        
        _renderer.DrawText("UP/DOWN: +/- 100 sprites", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        _renderer.DrawText("LEFT/RIGHT: +/- 1000 sprites", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        _renderer.DrawText("WASD: Move camera | Q/E: Zoom", 10, instructionY, Color.Gray);
        instructionY += 20;
        
        _renderer.DrawText($"C: Culling Viz ({(_showCullingVisualization ? "ON" : "OFF")})", 
            10, instructionY, Color.Gray);
        instructionY += 20;
        
        _renderer.DrawText("R: Reset", 10, instructionY, Color.Gray);
    }
    
    /// <summary>
    /// Draws a visualization of the camera frustum (what's being rendered).
    /// </summary>
    private void DrawCullingVisualization()
    {
        if (_camera == null) return;
        
        // Calculate camera frustum bounds in world space
        var halfWidth = _camera.ViewportWidth / 2f / _camera.Zoom;
        var halfHeight = _camera.ViewportHeight / 2f / _camera.Zoom;
        
        var left = _camera.Position.X - halfWidth;
        var top = _camera.Position.Y - halfHeight;
        var width = halfWidth * 2;
        var height = halfHeight * 2;
        
        // Draw frustum boundary (green outline)
        _renderer.DrawRectangleOutline(left, top, width, height, 
            new Color(0, 255, 0, 150), 4f);
        
        // Draw label
        _renderer.DrawText("Frustum (everything inside is rendered)", 
            left + 10, top + 10, new Color(0, 255, 0));
    }
    
    private void SpawnSprites(int count)
    {
        if (World == null) return;
        
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var sprite = World.CreateEntity($"BenchSprite_{_spriteCount + i}");
            
            // Add transform - spawn in a MUCH larger area to see culling
            var transform = sprite.AddComponent<TransformComponent>();
            transform.Position = new Vector2(
                random.Next(-2000, 3280), // Much larger area (5280 wide)
                random.Next(-2000, 2720)); // Much larger area (4720 tall)
            transform.Scale = new Vector2(
                0.5f + (float)random.NextDouble() * 1.0f, 
                0.5f + (float)random.NextDouble() * 1.0f);
            
            // Add sprite component with random color tint
            var spriteComp = sprite.AddComponent<SpriteComponent>();
            spriteComp.Tint = new Color(
                (byte)random.Next(100, 255),
                (byte)random.Next(100, 255),
                (byte)random.Next(100, 255));
            spriteComp.Layer = random.Next(0, 3);
            
            // Assign shared texture immediately if already loaded
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
}
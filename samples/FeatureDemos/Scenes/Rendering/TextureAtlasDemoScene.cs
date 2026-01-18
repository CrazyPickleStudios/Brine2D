using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Rendering.Performance;
using Brine2D.Rendering.TextureAtlas;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace FeatureDemos.Scenes.Rendering;

/// <summary>
/// Demo scene showcasing the texture atlas system.
/// Demonstrates:
/// - Building atlases from folders and individual files
/// - Drawing sprites from atlases with SpriteBatcher
/// - Performance benefits of texture atlasing
/// - Different atlas configurations
/// </summary>
public class TextureAtlasDemoScene : DemoSceneBase
{
    private readonly ITextureAtlasBuilder _atlasBuilder;
    private readonly ITextureLoader _textureLoader;
    private readonly SpriteBatcher _spriteBatcher;

    private ITextureAtlasCollection? _testAtlas;
    private ITexture? _standaloneTexture;

    private readonly List<SpriteInstance> _atlasSprites = new();
    private readonly List<SpriteInstance> _standaloneSprites = new();

    private bool _useAtlas = true;
    private int _spriteCount = 100;
    private float _rotation = 0f;

    private class SpriteInstance
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Scale { get; set; }
        public Color Tint { get; set; }
        public string RegionName { get; set; } = string.Empty;
    }

    public TextureAtlasDemoScene(
        ITextureAtlasBuilder atlasBuilder,
        ITextureLoader textureLoader,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        IRenderer renderer,
        ILogger<TextureAtlasDemoScene> logger,
        PerformanceOverlay? perfOverlay = null)
        : base(input, sceneManager, gameContext, logger, renderer, null, perfOverlay)
    {
        _atlasBuilder = atlasBuilder;
        _textureLoader = textureLoader;
        _spriteBatcher = new SpriteBatcher();
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("=== Texture Atlas Demo Scene ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  SPACE - Toggle atlas vs standalone textures");
        Logger.LogInformation("  UP/DOWN - Increase/decrease sprite count");
        Logger.LogInformation("  R - Rotate sprites");
        Logger.LogInformation("  F1 - Toggle performance overlay");
        Logger.LogInformation("  F3 - Toggle detailed stats");
        Logger.LogInformation("  ESC - Return to menu");
        Logger.LogInformation("");

        Renderer.ClearColor = new Color(20, 20, 30);

        // Build a test atlas
        await BuildTestAtlasAsync(cancellationToken);

        // Initialize sprite instances
        InitializeSprites();

        Logger.LogInformation("Atlas collection built with {RegionCount} total regions", _testAtlas?.TotalRegionCount ?? 0);
        Logger.LogInformation("Estimated draw calls - Atlas: 1, Standalone: {Count}", _spriteCount);
    }

    private async Task<ITextureAtlasCollection?> BuildTestAtlasAsync(CancellationToken cancellationToken)
    {
        try
        {
            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "sprites");
            
            if (Directory.Exists(assetsPath) && Directory.GetFiles(assetsPath, "*.png").Length > 0)
            {
                Logger.LogInformation("Building atlas from assets folder: {Path}", assetsPath);
                
                var collection = await _atlasBuilder
                    .WithName("SpriteAtlas")
                    .AddFolder(assetsPath, "*.png", recursive: false)
                    .WithMaxSize(2048, 2048)
                    .WithPadding(2)
                    .WithPowerOfTwo(true)
                    .WithScaleMode(TextureScaleMode.Nearest)
                    .BuildAsync(cancellationToken);
                
                Logger.LogInformation("Atlas collection created with {AtlasCount} atlases, {RegionCount} total regions", 
                    collection.Atlases.Count, collection.TotalRegionCount);
                
                return collection;
            }
            else
            {
                Logger.LogWarning("=== No sprite assets found ===");
                Logger.LogWarning("Assets folder: {Path}", assetsPath);
                Logger.LogWarning("Add PNG files to the above folder to see real atlas functionality.");
                Logger.LogWarning("Demo will continue with placeholder visualization.");
                Logger.LogWarning("");
                
                Directory.CreateDirectory(assetsPath);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to build atlas - continuing with placeholder mode");
            return null;
        }
    }

    private void InitializeSprites()
    {
        var random = new Random(42); // Fixed seed for consistency
        var screenWidth = 1280f;
        var screenHeight = 720f;

        for (int i = 0; i < _spriteCount; i++)
        {
            var sprite = new SpriteInstance
            {
                Position = new Vector2(
                    random.Next(0, (int)screenWidth),
                    random.Next(0, (int)screenHeight)),
                Velocity = new Vector2(
                    random.Next(-100, 100),
                    random.Next(-100, 100)),
                Scale = 0.5f + (float)random.NextDouble() * 0.5f,
                Tint = new Color(
                    (byte)random.Next(200, 255),
                    (byte)random.Next(200, 255),
                    (byte)random.Next(200, 255),
                    255),
                RegionName = "sprite" // Would match actual asset names
            };

            _atlasSprites.Add(sprite);

            // Clone for standalone comparison
            _standaloneSprites.Add(new SpriteInstance
            {
                Position = sprite.Position,
                Velocity = sprite.Velocity,
                Scale = sprite.Scale,
                Tint = sprite.Tint,
                RegionName = sprite.RegionName
            });
        }
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        HandlePerformanceHotkeys();
        if (CheckReturnToMenu()) return;

        var deltaTime = (float)gameTime.DeltaTime;

        // Toggle atlas mode
        if (Input.IsKeyPressed(Keys.Space))
        {
            _useAtlas = !_useAtlas;
            Logger.LogInformation("Using {Mode}", _useAtlas ? "ATLAS" : "STANDALONE TEXTURES");
            Logger.LogInformation("Estimated draw calls: {Count}", _useAtlas ? 1 : _spriteCount);
        }

        // Adjust sprite count
        if (Input.IsKeyPressed(Keys.Up))
        {
            _spriteCount = Math.Min(_spriteCount + 100, 10000);
            Logger.LogInformation("Sprite count: {Count}", _spriteCount);
            InitializeSprites();
        }

        if (Input.IsKeyPressed(Keys.Down))
        {
            _spriteCount = Math.Max(_spriteCount - 100, 10);
            Logger.LogInformation("Sprite count: {Count}", _spriteCount);
            InitializeSprites();
        }

        // Toggle rotation
        if (Input.IsKeyDown(Keys.R))
        {
            _rotation += deltaTime * 2f;
        }

        // Update sprite positions
        UpdateSpritePositions(deltaTime);
    }

    private void UpdateSpritePositions(float deltaTime)
    {
        const float screenWidth = 1280f;
        const float screenHeight = 720f;

        var sprites = _useAtlas ? _atlasSprites : _standaloneSprites;

        foreach (var sprite in sprites)
        {
            sprite.Position += sprite.Velocity * deltaTime;

            // Bounce off screen edges
            if (sprite.Position.X < 0 || sprite.Position.X > screenWidth)
            {
                sprite.Velocity = new Vector2(-sprite.Velocity.X, sprite.Velocity.Y);
                sprite.Position = new Vector2(
                    Math.Clamp(sprite.Position.X, 0, screenWidth),
                    sprite.Position.Y);
            }

            if (sprite.Position.Y < 0 || sprite.Position.Y > screenHeight)
            {
                sprite.Velocity = new Vector2(sprite.Velocity.X, -sprite.Velocity.Y);
                sprite.Position = new Vector2(
                    sprite.Position.X,
                    Math.Clamp(sprite.Position.Y, 0, screenHeight));
            }
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Draw sprites based on current mode
        if (_testAtlas != null && _useAtlas)
        {
            DrawWithAtlas();
        }
        else
        {
            DrawWithStandaloneTextures();
        }

        // Flush the sprite batcher
        _spriteBatcher.Flush(Renderer);

        // Draw UI
        DrawUI();

        // Render performance overlay
        RenderPerformanceOverlay();
    }

    private void DrawWithAtlas()
    {
        if (_testAtlas == null) return;

        var sprites = _atlasSprites.Take(_spriteCount);

        foreach (var sprite in sprites)
        {
            _spriteBatcher.DrawFromAtlas(
                _testAtlas,
                sprite.RegionName,
                sprite.Position,
                scale: new Vector2(sprite.Scale),
                rotation: _rotation,
                origin: new Vector2(0.5f),
                tint: sprite.Tint,
                layer: 0);
        }
    }

    private void DrawWithStandaloneTextures()
    {
        // In a real scenario, you'd load standalone textures
        // For demo purposes, we'll draw rectangles to show the concept
        var sprites = _standaloneSprites.Take(_spriteCount);

        foreach (var sprite in sprites)
        {
            // Simulate drawing with individual textures (lots of draw calls)
            Renderer.DrawRectangleFilled(
                sprite.Position.X - 16 * sprite.Scale,
                sprite.Position.Y - 16 * sprite.Scale,
                32 * sprite.Scale,
                32 * sprite.Scale,
                sprite.Tint);
        }
    }

    private void DrawUI()
    {
        var y = 10f;
        var lineHeight = 20f;

        Renderer.DrawText($"Texture Atlas Demo", 10, y, Color.White);
        y += lineHeight * 2;

        Renderer.DrawText($"Mode: {(_useAtlas ? "ATLAS (1 draw call)" : $"STANDALONE ({_spriteCount} draw calls)")}", 
            10, y, _useAtlas ? Color.Green : Color.Yellow);
        y += lineHeight;

        Renderer.DrawText($"Sprites: {_spriteCount}", 10, y, Color.White);
        y += lineHeight;

        Renderer.DrawText($"Rotation: {(_rotation > 0 ? "ON" : "OFF")}", 10, y, Color.White);
        y += lineHeight;

        if (_testAtlas != null)
        {
            y += lineHeight;
            Renderer.DrawText($"Atlas Collection: {_testAtlas.Name}", 10, y, Color.Cyan);
            y += lineHeight;
            
            // Show info about each atlas in the collection
            for (int i = 0; i < _testAtlas.Atlases.Count; i++)
            {
                var atlas = _testAtlas.Atlases[i];
                Renderer.DrawText($"  Atlas {i}: {atlas.Texture.Width}x{atlas.Texture.Height}, {atlas.RegionCount} regions", 
                    10, y, Color.Cyan);
                y += lineHeight;
            }
            
            Renderer.DrawText($"  Total Regions: {_testAtlas.TotalRegionCount}", 10, y, Color.Cyan);
            y += lineHeight;
            Renderer.DrawText($"  Estimated Batches: {_spriteBatcher.EstimatedDrawCalls}", 10, y, Color.Cyan);
        }
        else
        {
            y += lineHeight;
            Renderer.DrawText("No atlas loaded - add sprites to assets/sprites/", 10, y, Color.Red);
        }

        // Controls
        y += lineHeight * 2;
        Renderer.DrawText("Controls:", 10, y, Color.Gray);
        y += lineHeight;
        Renderer.DrawText("  SPACE - Toggle mode", 10, y, Color.Gray);
        y += lineHeight;
        Renderer.DrawText("  UP/DOWN - Sprite count", 10, y, Color.Gray);
        y += lineHeight;
        Renderer.DrawText("  R - Rotate", 10, y, Color.Gray);
        y += lineHeight;
        Renderer.DrawText("  F1/F3 - Performance", 10, y, Color.Gray);
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        _testAtlas?.Dispose();
        _standaloneTexture?.Dispose();

        Logger.LogInformation("Texture atlas demo scene unloaded");

        return Task.CompletedTask;
    }
}
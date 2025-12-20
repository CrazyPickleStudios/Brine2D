using Brine2D.Core;
using Brine2D.Core.Input;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Brine2D.Input.SDL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// Create the game application builder (ASP.NET style!)
var builder = GameApplication.CreateBuilder(args);

// Add SDL3 Input
builder.Services.AddSDL3Input();

// Configure rendering with SDL3 - BIND configuration to options
builder.Services.AddSDL3Rendering(options =>
{
    // Bind from configuration first
    builder.Configuration.GetSection("Rendering").Bind(options);

    // Then override specific values if needed
    options.WindowTitle = "Brine2D - Texture Demo";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    options.VSync = true;
});

// Register our game scene
builder.Services.AddScene<TextureTestScene>();

// Build and run
var game = builder.Build();
await game.RunAsync<TextureTestScene>();

// ===== TEXTURE TEST SCENE =====

public class TextureTestScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly ITextureLoader _textureLoader;

    private ITexture? _testTexture;
    private float _spriteX = 400;
    private float _spriteY = 300;
    private float _speed = 300;
    private float _scale = 1.0f;
    private float _rotation = 0f;

    public TextureTestScene(
        IRenderer renderer,
        IGameContext gameContext,
        IInputService input,
        ITextureLoader textureLoader,
        ILogger<TextureTestScene> logger) : base(logger)
    {
        _renderer = renderer;
        _gameContext = gameContext;
        _input = input;
        _textureLoader = textureLoader;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("TextureTestScene initialized!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD or Arrow Keys - Move sprite");
        Logger.LogInformation("  Q/E - Scale down/up");
        Logger.LogInformation("  Left Mouse Click - Move sprite to cursor");
        Logger.LogInformation("  Escape - Exit");
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Loading textures...");

        try
        {
            // Try to load a test image
            // You can replace this with any PNG/JPG file path
            var testImagePath = "assets/test.png";

            if (File.Exists(testImagePath))
            {
                _testTexture = await _textureLoader.LoadTextureAsync(testImagePath, cancellationToken);
                Logger.LogInformation("Test texture loaded successfully!");
            }
            else
            {
                Logger.LogWarning("Test image not found at {Path}, creating placeholder", testImagePath);
                Logger.LogInformation("To test with a real image, place a PNG file at: {Path}",
                    Path.GetFullPath(testImagePath));

                // Create a placeholder colored texture
                _testTexture = _textureLoader.CreateTexture(128, 128);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load texture");
            throw;
        }
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        // === Input ===
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }

        // Movement
        if (_input.IsKeyDown(Keys.W) || _input.IsKeyDown(Keys.Up))
            _spriteY -= _speed * deltaTime;

        if (_input.IsKeyDown(Keys.S) || _input.IsKeyDown(Keys.Down))
            _spriteY += _speed * deltaTime;

        if (_input.IsKeyDown(Keys.A) || _input.IsKeyDown(Keys.Left))
            _spriteX -= _speed * deltaTime;

        if (_input.IsKeyDown(Keys.D) || _input.IsKeyDown(Keys.Right))
            _spriteX += _speed * deltaTime;

        // Scaling
        if (_input.IsKeyDown(Keys.Q))
        {
            _scale -= 1.0f * deltaTime;
            _scale = Math.Max(0.1f, _scale);
        }

        if (_input.IsKeyDown(Keys.E))
        {
            _scale += 1.0f * deltaTime;
            _scale = Math.Min(5.0f, _scale);
        }

        // Mouse teleport
        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            var mousePos = _input.MousePosition;
            _spriteX = mousePos.X;
            _spriteY = mousePos.Y;
        }

        // Gamepad support
        if (_input.IsGamepadConnected())
        {
            var leftStick = _input.GetGamepadLeftStick();
            if (leftStick.LengthSquared() > 0.01f)
            {
                _spriteX += leftStick.X * _speed * deltaTime;
                _spriteY += leftStick.Y * _speed * deltaTime;
            }
        }

        // Auto-rotate for fun
        _rotation += 45f * deltaTime; // 45 degrees per second
        if (_rotation > 360f)
            _rotation -= 360f;
    }

    protected override void OnRender(GameTime gameTime)
    {
        // Clear with dark blue
        _renderer.Clear(new Color(20, 30, 50));

        _renderer.BeginFrame();

        // Draw instructions
        _renderer.DrawText($"Scale: {_scale:F2}x (Q/E to change)", 10, 10, Color.White);
        _renderer.DrawText($"Position: ({_spriteX:F0}, {_spriteY:F0})", 10, 40, Color.White);

        if (_testTexture != null)
        {
            // Draw the texture with scaling
            var drawWidth = _testTexture.Width * _scale;
            var drawHeight = _testTexture.Height * _scale;

            // Center the texture on the sprite position
            _renderer.DrawTexture(
                _testTexture,
                _spriteX - drawWidth / 2,
                _spriteY - drawHeight / 2,
                drawWidth,
                drawHeight);

            // Draw texture info
            _renderer.DrawText(
                $"Texture: {_testTexture.Source} ({_testTexture.Width}x{_testTexture.Height})",
                10, 70, Color.White);
        }
        else
        {
            // Fallback: draw a rectangle if no texture
            _renderer.DrawRectangle(_spriteX - 32, _spriteY - 32, 64, 64, Color.Red);
            _renderer.DrawText("No texture loaded - showing placeholder", 10, 70, Color.Red);
        }

        _renderer.EndFrame();
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Unloading textures...");

        if (_testTexture != null)
        {
            _textureLoader.UnloadTexture(_testTexture);
            _testTexture = null;
        }

        return Task.CompletedTask;
    }
}
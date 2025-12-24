using Brine2D.Core;
using Brine2D.Core.Animation;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace BasicGame;

/// <summary>
/// Demo scene showing camera movement with sprite animation.
/// </summary>
public class CameraDemoScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly ITextureLoader _textureLoader;
    private readonly ILoggerFactory _loggerFactory;

    private ITexture? _spriteSheet;
    private SpriteAnimator? _animator;
    private Camera2D? _camera;
    private CameraBounds? _worldBounds;
    
    private Vector2 _playerPosition = new Vector2(400, 300);
    private float _speed = 200f;

    public CameraDemoScene(
        IRenderer renderer,
        IGameContext gameContext,
        IInputService input,
        ITextureLoader textureLoader,
        ILoggerFactory loggerFactory,
        ILogger<CameraDemoScene> logger) : base(logger)
    {
        _renderer = renderer;
        _gameContext = gameContext;
        _input = input;
        _textureLoader = textureLoader;
        _loggerFactory = loggerFactory;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("Camera Demo initialized!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move player");
        Logger.LogInformation("  Q/E - Zoom out/in");
        Logger.LogInformation("  R - Reset camera");
        Logger.LogInformation("  1-6 - Change animations");
        Logger.LogInformation("  ESC - Exit");

        // Create camera
        _camera = new Camera2D(1280, 720);
        _camera.Zoom = 1.0f;
        _renderer.Camera = _camera;

        // Set world bounds (example: 2000x2000 world)
        _worldBounds = new CameraBounds(0, 0, 2000, 2000);
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Loading sprite sheet...");

        var spriteSheetPath = "assets/sprites/character.png";

        if (File.Exists(spriteSheetPath))
        {
            _spriteSheet = await _textureLoader.LoadTextureAsync(
                spriteSheetPath, 
                TextureScaleMode.Nearest,
                cancellationToken);
        }
        else
        {
            _spriteSheet = _textureLoader.CreateTexture(576, 24, TextureScaleMode.Nearest);
        }

        _animator = new SpriteAnimator(_loggerFactory.CreateLogger<SpriteAnimator>());

        const int frameWidth = 24;
        const int frameHeight = 24;
        const int columns = 24;

        var walkAnim = AnimationClip.FromSpriteSheet("walk", frameWidth, frameHeight, 4, columns, 0.15f, true);
        var moveAnim = new AnimationClip("move") { Loop = true };
        for (int i = 4; i < 10; i++)
        {
            moveAnim.Frames.Add(new SpriteFrame(new Rectangle(i * frameWidth, 0, frameWidth, frameHeight), 0.1f));
        }

        _animator.AddAnimation(walkAnim);
        _animator.AddAnimation(moveAnim);
        _animator.Play("walk");

        Logger.LogInformation("Loaded animations with camera support");
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }

        // Camera zoom controls
        if (_input.IsKeyDown(Keys.Q) && _camera != null)
        {
            _camera.Zoom = Math.Max(0.5f, _camera.Zoom - 1.0f * deltaTime);
        }
        if (_input.IsKeyDown(Keys.E) && _camera != null)
        {
            _camera.Zoom = Math.Min(3.0f, _camera.Zoom + 1.0f * deltaTime);
        }

        // Reset camera
        if (_input.IsKeyPressed(Keys.R) && _camera != null)
        {
            _camera.Zoom = 1.0f;
            _camera.Rotation = 0f;
        }

        // Animation controls
        if (_input.IsKeyPressed(Keys.D1)) _animator?.Play("walk");
        if (_input.IsKeyPressed(Keys.D2)) _animator?.Play("move");

        // Player movement
        var movement = Vector2.Zero;
        
        if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
        if (_input.IsKeyDown(Keys.S)) movement.Y += 1;
        if (_input.IsKeyDown(Keys.A)) movement.X -= 1;
        if (_input.IsKeyDown(Keys.D)) movement.X += 1;

        if (movement != Vector2.Zero)
        {
            movement = Vector2.Normalize(movement);
            _playerPosition += movement * _speed * deltaTime;

            // Keep player in world bounds
            _playerPosition = new Vector2(
                Math.Clamp(_playerPosition.X, 0, 2000),
                Math.Clamp(_playerPosition.Y, 0, 2000));
        }

        // Camera follows player smoothly
        if (_camera != null && _worldBounds != null)
        {
            _camera.LerpTo(_playerPosition, 5f * deltaTime);
            
            // Constrain camera to world bounds
            _camera.Position = _worldBounds.ClampPosition(_camera.Position, _camera);
        }

        _animator?.Update(deltaTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(40, 40, 40));
        _renderer.BeginFrame();

        // Draw world grid (to show camera movement)
        DrawGrid();

        // Draw animated sprite at world position
        if (_spriteSheet != null && _animator?.CurrentFrame != null)
        {
            var frame = _animator.CurrentFrame;
            var rect = frame.SourceRect;

            var scale = 4.0f;
            var destWidth = rect.Width * scale;
            var destHeight = rect.Height * scale;

            var drawX = _playerPosition.X - destWidth / 2;
            var drawY = _playerPosition.Y - destHeight / 2;

            _renderer.DrawTexture(
                _spriteSheet,
                rect.X, rect.Y, rect.Width, rect.Height,
                drawX, drawY, destWidth, destHeight);
        }

        _renderer.EndFrame();
    }

    private void DrawGrid()
    {
        // Draw a simple grid to visualize camera movement
        var gridSize = 100;
        var gridColor = new Color(60, 60, 60);

        for (int x = 0; x <= 2000; x += gridSize)
        {
            _renderer.DrawRectangle(x, 0, 2, 2000, gridColor);
        }

        for (int y = 0; y <= 2000; y += gridSize)
        {
            _renderer.DrawRectangle(0, y, 2000, 2, gridColor);
        }
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        if (_spriteSheet != null)
        {
            _textureLoader.UnloadTexture(_spriteSheet);
        }

        return Task.CompletedTask;
    }
}
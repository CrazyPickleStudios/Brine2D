using System.Numerics;
using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace MyGame;

public class MovingSpriteScene : Scene
{
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly IRenderer _renderer;
    private readonly ITextureLoader _textureLoader;
    private Vector2 _playerPosition = new(640, 360); // Center of 1280x720

    private ITexture? _playerTexture;
    private readonly float _speed = 200f;

    public MovingSpriteScene(
        IRenderer renderer,
        IInputService input,
        ITextureLoader textureLoader,
        IGameContext gameContext,
        ILogger<MovingSpriteScene> logger
    ) : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _textureLoader = textureLoader;
        _gameContext = gameContext;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("Moving Sprite Scene initialized!");
        Logger.LogInformation("Controls: Arrow Keys to move, Escape to exit");
    }

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Loading player sprite...");

        var spritePath = "assets/sprites/player.png";

        if (File.Exists(spritePath))
        {
            _playerTexture = await _textureLoader.LoadTextureAsync(
                spritePath,
                TextureScaleMode.Nearest,
                cancellationToken
            );

            Logger.LogInformation("Sprite loaded: {Width}x{Height}",
                _playerTexture.Width, _playerTexture.Height);
        }
        else
        {
            Logger.LogWarning("Sprite not found, using placeholder");
            _playerTexture = _textureLoader.CreateTexture(32, 32, TextureScaleMode.Nearest);
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(40, 40, 40));
        _renderer.BeginFrame();

        if (_playerTexture != null)
        {
            // Draw centered on position
            var drawX = _playerPosition.X - _playerTexture.Width / 2f;
            var drawY = _playerPosition.Y - _playerTexture.Height / 2f;

            _renderer.DrawTexture(_playerTexture, drawX, drawY);
        }

        _renderer.EndFrame();
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        // Clean up the texture when scene unloads
        if (_playerTexture != null)
        {
            _textureLoader.UnloadTexture(_playerTexture);
        }

        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }

        // Calculate movement direction
        var movement = Vector2.Zero;
        if (_input.IsKeyDown(Keys.Left))
        {
            movement.X -= 1;
        }

        if (_input.IsKeyDown(Keys.Right))
        {
            movement.X += 1;
        }

        if (_input.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1;
        }

        if (_input.IsKeyDown(Keys.Down))
        {
            movement.Y += 1;
        }

        // Apply movement
        if (movement != Vector2.Zero)
        {
            movement = Vector2.Normalize(movement);
            _playerPosition += movement * _speed * deltaTime;
        }

        // Keep player on screen
        const float screenWidth = 1280f;
        const float screenHeight = 720f;

        var spriteWidth = _playerTexture?.Width ?? 32;
        var spriteHeight = _playerTexture?.Height ?? 32;

        _playerPosition.X = Math.Clamp(_playerPosition.X, spriteWidth / 2f, screenWidth - spriteWidth / 2f);
        _playerPosition.Y = Math.Clamp(_playerPosition.Y, spriteHeight / 2f, screenHeight - spriteHeight / 2f);
    }
}
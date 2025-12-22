using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.Core.Animation;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace BasicGame;

/// <summary>
/// Demo scene showing sprite animation.
/// </summary>
public class AnimationDemoScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly ITextureLoader _textureLoader;
    private readonly ILoggerFactory _loggerFactory;

    private ITexture? _spriteSheet;
    private SpriteAnimator? _animator;
    
    private Vector2 _position = new Vector2(400, 300);
    private float _speed = 200f;

    public AnimationDemoScene(
        IRenderer renderer,
        IGameContext gameContext,
        IInputService input,
        ITextureLoader textureLoader,
        ILoggerFactory loggerFactory,
        ILogger<AnimationDemoScene> logger) : base(logger)
    {
        _renderer = renderer;
        _gameContext = gameContext;
        _input = input;
        _textureLoader = textureLoader;
        _loggerFactory = loggerFactory;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("Animation Demo initialized!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move sprite");
        Logger.LogInformation("  1 - Walk animation");
        Logger.LogInformation("  2 - Move/Run animation");
        Logger.LogInformation("  3 - Kick animation");
        Logger.LogInformation("  4 - Hurt animation");
        Logger.LogInformation("  5 - Crouch");
        Logger.LogInformation("  6 - Sneak animation");
        Logger.LogInformation("  SPACE - Pause/Resume animation");
        Logger.LogInformation("  ESC - Exit");
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
            
            Logger.LogInformation("Sprite sheet loaded! Size: {Width}x{Height}",
                _spriteSheet.Width, _spriteSheet.Height);
        }
        else
        {
            Logger.LogWarning("Sprite sheet not found at: {Path}", Path.GetFullPath(spriteSheetPath));
            Logger.LogInformation("Expected: 576x24 sprite sheet with 24x24 frames");

            _spriteSheet = _textureLoader.CreateTexture(576, 24, TextureScaleMode.Nearest);
        }

        _animator = new SpriteAnimator(_loggerFactory.CreateLogger<SpriteAnimator>());

        // 576x24 sprite sheet = 24 frames at 24x24 each
        const int frameWidth = 24;
        const int frameHeight = 24;
        const int columns = 24;

        // Walk: frames 1-4
        var walkAnim = AnimationClip.FromSpriteSheet(
            name: "walk",
            frameWidth: frameWidth,
            frameHeight: frameHeight,
            frameCount: 4,
            columns: columns,
            frameDuration: 0.15f,
            loop: true);

        // Move/Run: frames 5-10
        var moveAnim = new AnimationClip("move") { Loop = true };
        for (int i = 4; i < 10; i++)
        {
            moveAnim.Frames.Add(new SpriteFrame(
                new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                0.1f));
        }

        // Kick: frames 11-13
        var kickAnim = new AnimationClip("kick") { Loop = false };
        for (int i = 10; i < 13; i++)
        {
            kickAnim.Frames.Add(new SpriteFrame(
                new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                0.1f));
        }

        // Hurt: frames 14-17
        var hurtAnim = new AnimationClip("hurt") { Loop = false };
        for (int i = 13; i < 17; i++)
        {
            hurtAnim.Frames.Add(new SpriteFrame(
                new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                0.15f));
        }

        // Crouch: frame 18
        var crouchAnim = new AnimationClip("crouch") { Loop = true };
        crouchAnim.Frames.Add(new SpriteFrame(
            new Rectangle(17 * frameWidth, 0, frameWidth, frameHeight),
            1.0f));

        // Sneak: frames 19-24
        var sneakAnim = new AnimationClip("sneak") { Loop = true };
        for (int i = 18; i < 24; i++)
        {
            sneakAnim.Frames.Add(new SpriteFrame(
                new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                0.12f));
        }

        _animator.AddAnimation(walkAnim);
        _animator.AddAnimation(moveAnim);
        _animator.AddAnimation(kickAnim);
        _animator.AddAnimation(hurtAnim);
        _animator.AddAnimation(crouchAnim);
        _animator.AddAnimation(sneakAnim);

        _animator.Play("walk");

        Logger.LogInformation("Loaded {Count} animations", 6);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }

        // Animation controls
        if (_input.IsKeyPressed(Keys.D1))
        {
            _animator?.Play("walk");
            Logger.LogInformation("Playing: walk");
        }
        if (_input.IsKeyPressed(Keys.D2))
        {
            _animator?.Play("move");
            Logger.LogInformation("Playing: move/run");
        }
        if (_input.IsKeyPressed(Keys.D3))
        {
            _animator?.Play("kick");
            Logger.LogInformation("Playing: kick");
        }
        if (_input.IsKeyPressed(Keys.D4))
        {
            _animator?.Play("hurt");
            Logger.LogInformation("Playing: hurt");
        }
        if (_input.IsKeyPressed(Keys.D5))
        {
            _animator?.Play("crouch");
            Logger.LogInformation("Playing: crouch");
        }
        if (_input.IsKeyPressed(Keys.D6))
        {
            _animator?.Play("sneak");
            Logger.LogInformation("Playing: sneak");
        }

        if (_input.IsKeyPressed(Keys.Space))
        {
            if (_animator?.IsPlaying == true)
            {
                _animator.Pause();
                Logger.LogInformation("Animation paused");
            }
            else
            {
                _animator?.Resume();
                Logger.LogInformation("Animation resumed");
            }
        }

        // Movement
        var movement = Vector2.Zero;

        if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
        if (_input.IsKeyDown(Keys.S)) movement.Y += 1;
        if (_input.IsKeyDown(Keys.A)) movement.X -= 1;
        if (_input.IsKeyDown(Keys.D)) movement.X += 1;

        if (movement != Vector2.Zero)
        {
            movement = Vector2.Normalize(movement);
            _position += movement * _speed * deltaTime;
        }

        _animator?.Update(deltaTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(40, 40, 40));
        _renderer.BeginFrame();

        if (_spriteSheet != null && _animator?.CurrentFrame != null)
        {
            var frame = _animator.CurrentFrame;
            var rect = frame.SourceRect;

            var scale = 4.0f;
            var destWidth = rect.Width * scale;
            var destHeight = rect.Height * scale;

            var drawX = _position.X - destWidth / 2;
            var drawY = _position.Y - destHeight / 2;

            _renderer.DrawTexture(
                _spriteSheet,
                rect.X, rect.Y, rect.Width, rect.Height,
                drawX, drawY, destWidth, destHeight);
        }
        else if (_spriteSheet != null)
        {
            _renderer.DrawTexture(_spriteSheet,
                0, 0, 24, 24,
                _position.X - 48, _position.Y - 48, 96, 96);
        }

        _renderer.EndFrame();
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
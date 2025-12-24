using Brine2D.Core;
using Brine2D.Core.Animation;
using Brine2D.Core.Collision;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.UI;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Brine2D.Rendering.SDL;

namespace BasicGame;

/// <summary>
/// Demo scene showing collision detection with camera and animation.
/// </summary>
public class CollisionDemoScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly ITextureLoader _textureLoader;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CollisionSystem _collisionSystem; // Update constructor to inject CollisionSystem
    private readonly UICanvas _uiCanvas;
    private readonly InputLayerManager _inputLayerManager;

    private ITexture? _spriteSheet;
    private SpriteAnimator? _animator;
    private Camera2D? _camera;
    private CameraBounds? _worldBounds;
    
    private Vector2 _playerPosition = new Vector2(400, 300);
    private BoxCollider? _playerCollider;
    private float _speed = 200f;

    private readonly List<BoxCollider> _walls = new();
    private readonly List<CircleCollider> _coins = new();

    private Vector2 _ballPosition = new Vector2(500, 200);
    private Vector2 _ballVelocity = new Vector2(150, 100);
    private CircleCollider _ballCollider = new CircleCollider(15);

    private UILabel? _coinsLabel;
    private UILabel? _fpsLabel;

    private readonly IFontLoader _fontLoader;
    private IFont? _defaultFont;

    private UITextInput? _nameInput;
    private UISlider? _speedSlider;
    private UIButton? _resetButton;
    private UIProgressBar? _healthBar;

    private ITexture? _coinIcon;

    private UIDropdown? _qualityDropdown;
    private UIRadioButtonGroup? _difficultyGroup;
    private UITabContainer? _settingsTab;

    public CollisionDemoScene(
        IRenderer renderer,
        IGameContext gameContext,
        IInputService input,
        ITextureLoader textureLoader,
        ILoggerFactory loggerFactory,
        CollisionSystem collisionSystem, 
        UICanvas uiCanvas,
        InputLayerManager inputLayerManager,
        IFontLoader fontLoader, 
        ILogger<CollisionDemoScene> logger) : base(logger)
    {
        _renderer = renderer;
        _gameContext = gameContext;
        _input = input;
        _textureLoader = textureLoader;
        _loggerFactory = loggerFactory;
        _collisionSystem = collisionSystem; 
        _uiCanvas = uiCanvas;
        _inputLayerManager = inputLayerManager;
        _fontLoader = fontLoader;
    }

    protected override void OnInitialize()
    {
        Logger.LogInformation("Collision Demo initialized!");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD - Move player");
        Logger.LogInformation("  Q/E - Zoom out/in");
        Logger.LogInformation("  R - Reset camera");
        Logger.LogInformation("  ESC - Exit");

        _camera = new Camera2D(1280, 720);
        _camera.Zoom = 1.0f;
        _renderer.Camera = _camera;

        _worldBounds = new CameraBounds(0, 0, 2000, 2000);

        // Create player collider
        _playerCollider = new BoxCollider(80, 80, new Vector2(-40, -40))
        {
            Position = _playerPosition
        };

        // Create some walls
        _walls.Add(new BoxCollider(400, 50) { Position = new Vector2(300, 500) });
        _walls.Add(new BoxCollider(50, 400) { Position = new Vector2(800, 300) });
        _walls.Add(new BoxCollider(300, 50) { Position = new Vector2(1000, 800) });
        _walls.Add(new BoxCollider(50, 300) { Position = new Vector2(500, 1000) });

        // Create some coins to collect
        _coins.Add(new CircleCollider(20) { Position = new Vector2(600, 400) });
        _coins.Add(new CircleCollider(20) { Position = new Vector2(700, 600) });
        _coins.Add(new CircleCollider(20) { Position = new Vector2(900, 500) });
        _coins.Add(new CircleCollider(20) { Position = new Vector2(1200, 700) });

        // Register walls with collision system
        foreach (var wall in _walls)
        {
            _collisionSystem.AddShape(wall);
        }

        // Register coins with collision system
        foreach (var coin in _coins)
        {
            _collisionSystem.AddShape(coin);
        }

        // Register the ball with the collision system
        _collisionSystem.AddShape(_ballCollider);
        _collisionSystem.AddShape(_playerCollider);

        // Setup UI
        var panel = new UIPanel(new Vector2(10, 10), new Vector2(250, 120))
        {
            BackgroundColor = new Color(30, 30, 30, 200),
            BorderColor = new Color(100, 100, 100)
        };
        _uiCanvas.Add(panel);

        _coinsLabel = new UILabel($"Coins: {_coins.Count}", new Vector2(20, 20));
        _uiCanvas.Add(_coinsLabel);

        _fpsLabel = new UILabel("FPS: 0", new Vector2(20, 50));
        _uiCanvas.Add(_fpsLabel);

        var controlsLabel = new UILabel("WASD: Move | Q/E: Zoom", new Vector2(20, 80));
        _uiCanvas.Add(controlsLabel);
        _inputLayerManager.RegisterLayer(_uiCanvas);

        // Add TextInput for player name
        _nameInput = new UITextInput(new Vector2(20, 110), new Vector2(200, 30))
        {
            Placeholder = "Enter player name...",
            MaxLength = 20
        };
        _nameInput.OnTextChanged += (text) =>
        {
            Logger.LogInformation("Player name: {Name}", text);
        };
        _nameInput.OnSubmit += (text) =>
        {
            Logger.LogInformation("Player name submitted: {Name}", text);
        };
        _uiCanvas.Add(_nameInput);

        // Add Slider for player speed
        _speedSlider = new UISlider(new Vector2(20, 150), new Vector2(180, 20))
        {
            MinValue = 100f,
            MaxValue = 500f,
            Value = _speed,
            ShowValue = true,
            ValueFormat = "0"
        };
        _speedSlider.OnValueChanged += (value) =>
        {
            _speed = value;
            Logger.LogDebug("Speed changed: {Speed}", _speed);
        };
        _uiCanvas.Add(_speedSlider);

        // Add label for slider
        var speedLabel = new UILabel("Speed:", new Vector2(20, 135));
        _uiCanvas.Add(speedLabel);

        // Add reset button
        _resetButton = new UIButton("Reset", new Vector2(20, 180), new Vector2(100, 30));
        _resetButton.OnClick += () =>
        {
            _playerPosition = new Vector2(400, 300);
            _speed = 200f;
            if (_speedSlider != null)
                _speedSlider.Value = _speed;
            Logger.LogInformation("Game reset!");
        };
        _uiCanvas.Add(_resetButton);

        // Add Health bar
        _healthBar = new UIProgressBar(new Vector2(100, 50), new Vector2(200, 20))
        {
            Label = "Health",
            FillColor = new Color(0, 200, 0),
            Value = 0.75f // 75%
        };
        _uiCanvas.Add(_healthBar);

        // Add UIImage for coin icon
        if (_coinIcon != null)
        {
            var coinImage = new UIImage(_coinIcon, new Vector2(230, 15), new Vector2(32, 32))
            {
                MaintainAspectRatio = true,
                Tooltip = new UITooltip("Collect all coins!")
            };
            _uiCanvas.Add(coinImage);
        }

        // Add tooltips to existing components
        if (_resetButton != null)
        {
            _resetButton.Tooltip = new UITooltip("Reset player position and speed");
        }

        if (_speedSlider != null)
        {
            _speedSlider.Tooltip = new UITooltip("Adjust player movement speed");
        }

        if (_nameInput != null)
        {
            _nameInput.Tooltip = new UITooltip("Enter your player name (max 20 characters)");
        }

        // Add Dropdown for quality settings
        _qualityDropdown = new UIDropdown(new Vector2(20, 220), new Vector2(120, 30))
        {
            Tooltip = new UITooltip("Graphics quality setting")
        };
        _qualityDropdown.AddItem("Low");
        _qualityDropdown.AddItem("Medium");
        _qualityDropdown.AddItem("High");
        _qualityDropdown.AddItem("Ultra");
        _qualityDropdown.SelectedIndex = 2; // Default to High
        _qualityDropdown.OnSelectionChanged += (index, text) =>
        {
            Logger.LogInformation("Quality changed to: {Quality}", text);
        };
        _uiCanvas.Add(_qualityDropdown);

        // Add label for dropdown
        var qualityLabel = new UILabel("Quality:", new Vector2(20, 205));
        _uiCanvas.Add(qualityLabel);

        // Add Radio Buttons for difficulty
        _difficultyGroup = new UIRadioButtonGroup();
        _difficultyGroup.OnSelectionChanged += (button) =>
        {
            Logger.LogInformation("Difficulty changed to: {Difficulty}", button?.Label);
        };

        var easyRadio = new UIRadioButton("Easy", _difficultyGroup, new Vector2(20, 260));
        var mediumRadio = new UIRadioButton("Medium", _difficultyGroup, new Vector2(20, 290));
        var hardRadio = new UIRadioButton("Hard", _difficultyGroup, new Vector2(20, 320));

        _uiCanvas.Add(easyRadio);
        _uiCanvas.Add(mediumRadio);
        _uiCanvas.Add(hardRadio);

        // Select medium by default
        mediumRadio.Select();

        // Add Tab Container for organized settings
        _settingsTab = new UITabContainer(new Vector2(300, 200), new Vector2(300, 250))
        {
            Tooltip = new UITooltip("Settings panel")
        };

        // Add tabs
        _settingsTab.AddTab("Graphics");
        _settingsTab.AddTab("Audio");
        _settingsTab.AddTab("Controls");

        // Add components to Graphics tab
        var graphicsLabel = new UILabel("Graphics Settings", new Vector2(320, 240));
        _settingsTab.AddComponentToTab(0, graphicsLabel);

        var vsyncCheckbox = new UICheckbox("VSync", new Vector2(320, 270));
        vsyncCheckbox.IsChecked = true;
        _settingsTab.AddComponentToTab(0, vsyncCheckbox);

        // Add components to Audio tab
        var audioLabel = new UILabel("Audio Settings", new Vector2(320, 240));
        _settingsTab.AddComponentToTab(1, audioLabel);

        var volumeSlider = new UISlider(new Vector2(320, 270), new Vector2(150, 20))
        {
            MinValue = 0f,
            MaxValue = 100f,
            Value = 75f,
            ShowValue = true,
            ValueFormat = "0"
        };
        _settingsTab.AddComponentToTab(1, volumeSlider);

        // Add components to Controls tab
        var controlsTabLabel = new UILabel("Control Settings", new Vector2(320, 240));
        _settingsTab.AddComponentToTab(2, controlsTabLabel);

        _uiCanvas.Add(_settingsTab);

        _settingsTab.OnTabChanged += (index, title) =>
        {
            Logger.LogInformation("Switched to tab: {TabTitle}", title);
        };

        // Register UI canvas as an input layer
        _inputLayerManager.RegisterLayer(_uiCanvas);

        var scrollView = new UIScrollView(new Vector2(620, 10), new Vector2(250, 400))
        {
            ContentHeight = 800, // Content is taller than view
            ShowVerticalScrollbar = true
        };

        // Add children (positions relative to content area)
        for (int i = 0; i < 20; i++)
        {
            var item = new UILabel($"Item {i + 1}", new Vector2(10, i * 35));
            scrollView.AddChild(item);
        }

        _uiCanvas.Add(scrollView);
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
        _animator.AddAnimation(walkAnim);
        _animator.Play("walk");

        Logger.LogInformation("Loaded with collision system");

        var fontPath = "assets/fonts/arial.ttf";
        if (File.Exists(fontPath))
        {
            _defaultFont = await _fontLoader.LoadFontAsync(fontPath, 16, cancellationToken);
            
            // Set as renderer's default font
            if (_renderer is SDL3Renderer sdlRenderer)
            {
                sdlRenderer.SetDefaultFont(_defaultFont);
            }
            
            Logger.LogInformation("Default font loaded");
        }
        else
        {
            Logger.LogWarning("Font not found at {Path}, using fallback rendering", fontPath);
        }

        var coinIconPath = "assets/sprites/coin_icon.png";
        if (File.Exists(coinIconPath))
        {
            _coinIcon = await _textureLoader.LoadTextureAsync(
                coinIconPath,
                TextureScaleMode.Nearest,
                cancellationToken);
        }
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        if (_input.IsKeyPressed(Keys.Escape))
        {
            var dialog = new UIDialog(
                "Confirm Exit", 
                "Are you sure you want to quit?",
                new Vector2(400, 200));

            dialog.CenterOnScreen(new Vector2(1280, 720));

            dialog.AddButton("Yes", () =>
            {
                _gameContext.RequestExit();
                dialog.Visible = false;
            });

            dialog.AddButton("No", () =>
            {
                dialog.Visible = false;
            });

            _uiCanvas.Add(dialog);
        }

        // Only process game input if NOT consumed by UI
        if (!_inputLayerManager.KeyboardConsumed)
        {
            // Camera controls
            if (_input.IsKeyDown(Keys.Q) && _camera != null)
            {
                _camera.Zoom = Math.Max(0.5f, _camera.Zoom - 1.0f * deltaTime);
            }
            if (_input.IsKeyDown(Keys.E) && _camera != null)
            {
                _camera.Zoom = Math.Min(3.0f, _camera.Zoom + 1.0f * deltaTime);
            }

            if (_input.IsKeyPressed(Keys.R) && _camera != null)
            {
                _camera.Zoom = 1.0f;
                _camera.Rotation = 0f;
            }

            // Player movement
            var movement = Vector2.Zero;

            if (_input.IsKeyDown(Keys.W)) movement.Y -= 1;
            if (_input.IsKeyDown(Keys.S)) movement.Y += 1;
            if (_input.IsKeyDown(Keys.A)) movement.X -= 1;
            if (_input.IsKeyDown(Keys.D)) movement.X += 1;

            if (movement != Vector2.Zero && _playerCollider != null)
            {
                movement = Vector2.Normalize(movement);
                var moveVector = movement * _speed * deltaTime;
                
                // Try full movement first
                var newPosition = _playerPosition + moveVector;
                _playerCollider.Position = newPosition;
                
                var collisions = _collisionSystem.GetCollisions(_playerCollider);
                
                // Check for both walls AND ball collision
                var hitWall = collisions.Any(c => c is BoxCollider && _walls.Contains(c));
                var hitBall = collisions.Contains(_ballCollider);
                
                // If we hit the ball, kick it before stopping!
                if (hitBall)
                {
                    var direction = Vector2.Normalize(_ballPosition - _playerPosition);
                    var kickStrength = 500f; // Instant kick force
                    _ballVelocity += direction * kickStrength * deltaTime;
                    Logger.LogDebug("Player kicked ball!");
                }
                
                if (!hitWall && !hitBall)
                {
                    // Full movement succeeded
                    _playerPosition = newPosition;
                }
                else
                {
                    // Try sliding along X axis
                    var slideX = _playerPosition + new Vector2(moveVector.X, 0);
                    _playerCollider.Position = slideX;
                    collisions = _collisionSystem.GetCollisions(_playerCollider);
                    var hitWallX = collisions.Any(c => c is BoxCollider && _walls.Contains(c));
                    var hitBallX = collisions.Contains(_ballCollider);
                    
                    // Kick ball on X slide
                    if (hitBallX)
                    {
                        var direction = Vector2.Normalize(_ballPosition - _playerPosition);
                        _ballVelocity += direction * 500f * deltaTime;
                    }
                    
                    if (!hitWallX && !hitBallX)
                    {
                        _playerPosition = slideX;
                    }
                    else
                    {
                        // Try sliding along Y axis instead
                        var slideY = _playerPosition + new Vector2(0, moveVector.Y);
                        _playerCollider.Position = slideY;
                        collisions = _collisionSystem.GetCollisions(_playerCollider);
                        var hitWallY = collisions.Any(c => c is BoxCollider && _walls.Contains(c));
                        var hitBallY = collisions.Contains(_ballCollider);
                        
                        // Kick ball on Y slide
                        if (hitBallY)
                        {
                            var direction = Vector2.Normalize(_ballPosition - _playerPosition);
                            _ballVelocity += direction * 500f * deltaTime;
                        }
                        
                        if (!hitWallY && !hitBallY)
                        {
                            _playerPosition = slideY;
                        }
                        else
                        {
                            // Completely blocked, restore position
                            _playerCollider.Position = _playerPosition;
                        }
                    }
                }

                // Check for coin collection at final position
                _playerCollider.Position = _playerPosition;
                collisions = _collisionSystem.GetCollisions(_playerCollider);
                
                foreach (var collision in collisions)
                {
                    if (collision is CircleCollider coin && _coins.Contains(coin))
                    {
                        _coins.Remove(coin);
                        _collisionSystem.RemoveShape(coin);
                        Logger.LogInformation("Coin collected! Remaining: {Count}", _coins.Count);
                    }
                }

                // Keep player in world bounds
                _playerPosition = new Vector2(
                    Math.Clamp(_playerPosition.X, 0, 2000),
                    Math.Clamp(_playerPosition.Y, 0, 2000));
            
                _playerCollider.Position = _playerPosition;
            }
        }

        // Ball simulation (bouncing ball demo)
        const float gravity = 800f;
        const float groundLevel = 1950f; // Near bottom of world
        const float damping = 0.98f; // Slight energy loss over time
        
        // Apply gravity
        _ballVelocity.Y += gravity * deltaTime;
        
        // Apply damping (air resistance)
        _ballVelocity *= damping;

        // Calculate new position
        var newBallPos = _ballPosition + _ballVelocity * deltaTime;
        
        // Ground collision (world bounds)
        if (newBallPos.Y + _ballCollider.Radius >= groundLevel)
        {
            newBallPos.Y = groundLevel - _ballCollider.Radius;
            _ballVelocity.Y = -_ballVelocity.Y * 0.7f; // Bounce with energy loss
            
            // Stop bouncing if velocity is too low
            if (Math.Abs(_ballVelocity.Y) < 10f)
            {
                _ballVelocity.Y = 0;
            }
        }

        // Wall/ceiling collision (world bounds)
        if (newBallPos.X - _ballCollider.Radius <= 0)
        {
            newBallPos.X = _ballCollider.Radius;
            _ballVelocity.X = -_ballVelocity.X * 0.7f;
        }
        else if (newBallPos.X + _ballCollider.Radius >= 2000)
        {
            newBallPos.X = 2000 - _ballCollider.Radius;
            _ballVelocity.X = -_ballVelocity.X * 0.7f;
        }

        if (newBallPos.Y - _ballCollider.Radius <= 0)
        {
            newBallPos.Y = _ballCollider.Radius;
            _ballVelocity.Y = -_ballVelocity.Y * 0.7f;
        }

        // Check wall collisions AND player collision
        _ballCollider.Position = newBallPos;
        var ballCollisions = _collisionSystem.GetCollisions(_ballCollider);
        
        foreach (var collision in ballCollisions)
        {
            // Wall collision (existing code)
            if (collision is BoxCollider wall && _walls.Contains(wall))
            {
                var wallBounds = wall.GetBounds();
                var ballBounds = _ballCollider.GetBounds();
                var ballRect = new RectangleF(ballBounds.X, ballBounds.Y, ballBounds.Width, ballBounds.Height);
                var penetration = wallBounds.GetPenetration(ballRect);
                
                if (penetration != Vector2.Zero)
                {
                    newBallPos = CollisionResponse.Push(newBallPos, penetration);
                    _ballVelocity = CollisionResponse.Bounce(_ballVelocity, penetration, 0.6f);
                    _ballCollider.Position = newBallPos;
                }
            }
            
            if (collision == _playerCollider)
            {
                // Calculate direction from player to ball
                var direction = Vector2.Normalize(_ballPosition - _playerPosition);
                
                // Apply kick force
                var kickStrength = 300f;
                _ballVelocity += direction * kickStrength * deltaTime;
                
                // Push ball away from player to prevent sticking
                var pushDistance = _ballCollider.Radius + 5f;
                newBallPos = _playerPosition + direction * pushDistance;
                _ballCollider.Position = newBallPos;
                
                Logger.LogDebug("Ball kicked!");
            }
        }

        // Update final position
        _ballPosition = newBallPos;
        _ballCollider.Position = _ballPosition;

        // Camera follows player
        if (_camera != null && _worldBounds != null)
        {
            _camera.LerpTo(_playerPosition, 5f * deltaTime);
            _camera.Position = _worldBounds.ClampPosition(_camera.Position, _camera);
        }

        _animator?.Update(deltaTime);

        // Update UI
        if (_coinsLabel != null)
        {
            _coinsLabel.Text = $"Coins: {_coins.Count}";
        }

        if (_fpsLabel != null)
        {
            var fps = deltaTime > 0 ? (int)(1.0 / deltaTime) : 0;
            _fpsLabel.Text = $"FPS: {fps}";
        }

        // Update Health bar value
        if (_healthBar != null)
        {
            _healthBar.Value = 1.0f; // 100%, set dynamically based on player health
        }

        _uiCanvas.Update(deltaTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(new Color(40, 40, 40));
        _renderer.BeginFrame();

        DrawGrid();

        // Draw walls
        foreach (var wall in _walls)
        {
            var bounds = wall.GetBounds();
            _renderer.DrawRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, new Color(100, 100, 100));
        }

        // Draw coins
        foreach (var coin in _coins)
        {
            var bounds = coin.GetBounds();
            _renderer.DrawRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, new Color(255, 215, 0));
        }

        // Draw player
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

        // Draw player collision box (debug)
        if (_playerCollider != null)
        {
            var bounds = _playerCollider.GetBounds();
            _renderer.DrawRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, new Color(255, 0, 0, 100));
        }

        // Draw the ball
        {
            var bounds = _ballCollider.GetBounds();
            _renderer.DrawCircle(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, bounds.Width / 2, new Color(0, 0, 255));
        }

        // Render UI (after game world, before EndFrame)
        _uiCanvas.Render(_renderer);

        _renderer.EndFrame();
    }

    private void DrawGrid()
    {
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

        if (_defaultFont != null)
        {
            _fontLoader.UnloadFont(_defaultFont);
        }

        return Task.CompletedTask;
    }
}
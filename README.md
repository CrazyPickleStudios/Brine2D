# Brine2D

**The ASP.NET of game engines** - A modern .NET 10 game engine built on SDL3 for creating 2D games with C#.

Brine2D brings the familiar patterns and developer experience of ASP.NET to game development. If you've built web apps with ASP.NET, you'll feel right at home building games with Brine2D.

## Features

- 🎮 **Input System** - Comprehensive keyboard, mouse, and gamepad support
- 🎨 **Sprite Rendering** - Hardware-accelerated texture rendering with sprite sheet support
- 🎬 **Animation System** - Frame-based sprite animations with multiple clips
- 🔊 **Audio** - Sound effects and music playback with SDL3_mixer
- 🎯 **Scene Management** - Organize your game into reusable scenes
- ⚙️ **Configuration** - JSON-based settings with hot reload
- 🏗️ **Dependency Injection** - ASP.NET-style DI container
- 🎮 **Multiple Backends** - Choose between SDL3 GPU renderer or legacy renderer

## Why Brine2D?

### ASP.NET Developers Will Feel at Home

```csharp
// Looks familiar? That's the point!
var builder = GameApplication.CreateBuilder(args);

// Configure services just like ASP.NET
builder.Services.AddSDL3Input();
builder.Services.AddSDL3Audio();

builder.Services.AddSDL3Rendering(options =>
{
    options.WindowTitle = "My Game";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
});

// Register your scenes like controllers
builder.Services.AddScene<GameScene>();

var game = builder.Build();

await game.RunAsync<GameScene>();
```

### Key Similarities to ASP.NET

| ASP.NET | Brine2D |
|---------|---------|
| `WebApplicationBuilder` | `GameApplicationBuilder` |
| Controllers | Scenes |
| Middleware | Game Loop |
| `appsettings.json` | `gamesettings.json` |
| Dependency Injection | Dependency Injection |
| `ILogger<T>` | `ILogger<T>` |
| Configuration binding | Configuration binding |

## Quick Start

### Installation

Create a new .NET 10 console project and add Brine2D:

```bash
dotnet new console -n MyGame
cd MyGame
```

### Add NuGet packages
> [!NOTE]
> There are currently no NuGet packages, you will need to reference the projects directly for now.

### Basic Game Setup

```csharp
using Brine2D.Audio.SDL;
using Brine2D.Hosting;
using Brine2D.Input.SDL;
using Brine2D.Rendering.SDL;

var builder = GameApplication.CreateBuilder(args);

builder.Services.AddSDL3Input();
builder.Services.AddSDL3Audio();

builder.Services.AddSDL3Rendering(options =>
{
    options.WindowTitle = "My Game";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    options.VSync = true;
});

builder.Services.AddScene<GameScene>();

var game = builder.Build();

await game.RunAsync<GameScene>();
```

### Creating a Scene (Like an ASP.NET Controller)

```csharp
using Brine2D.Core;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

// Scenes are like Controllers, they get dependencies injected.
public class GameScene : Scene
{
    private readonly IGameContext _gameContext;
    private readonly IInputService _input;
    private readonly IRenderer _renderer;

    // Constructor injection just like ASP.NET
    public GameScene
    (
        IRenderer renderer,
        IInputService input,
        IGameContext gameContext,
        ILogger<GameScene> logger
    )
        : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(Color.CornflowerBlue);
        _renderer.BeginFrame();

        // Your rendering code here

        _renderer.EndFrame();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }
    }
}
```

## Examples

### Loading and Drawing Sprites

```csharp
using Brine2D.Core;
using Brine2D.Rendering;

public class SpriteScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly ITextureLoader _textureLoader;

    private ITexture? _playerTexture;

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // Load with nearest neighbor filtering for pixel art
        _playerTexture = await _textureLoader.LoadTextureAsync
        (
            "assets/player.png",
            TextureScaleMode.Nearest,
            cancellationToken
        );
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(Color.Black);
        _renderer.BeginFrame();

        if (_playerTexture != null)
        {
            _renderer.DrawTexture(_playerTexture, 100, 100);
        }

        _renderer.EndFrame();
    }
}
```

### Sprite Animation

```csharp
using Brine2D.Core;
using Brine2D.Core.Animation;
using Brine2D.Rendering;

public class AnimatedScene : Scene
{
    private SpriteAnimator? _animator;
    private ITexture? _spriteSheet;

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        _spriteSheet = await _textureLoader.LoadTextureAsync
        (
            "assets/character.png",
            TextureScaleMode.Nearest,
            cancellationToken
        );

        _animator = new SpriteAnimator();

        // Create walk animation from sprite sheet
        var walkAnim = AnimationClip.FromSpriteSheet
        (
            "walk",
            32,
            32,
            8,
            8
        );

        _animator.AddAnimation(walkAnim);
        _animator.Play("walk");
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(Color.Black);
        _renderer.BeginFrame();

        if (_spriteSheet != null && _animator?.CurrentFrame != null)
        {
            var frame = _animator.CurrentFrame;
            var rect = frame.SourceRect;

            _renderer.DrawTexture
            (
                _spriteSheet,
                rect.X, rect.Y, rect.Width, rect.Height,
                100, 100, 64, 64
            );
        }

        _renderer.EndFrame();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        _animator?.Update((float)gameTime.DeltaTime);
    }
}
```

### Playing Audio

```csharp
using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.Input;

public class AudioScene : Scene
{
    private readonly IAudioService _audio;

    private IMusic? _bgMusic;
    private ISoundEffect? _jumpSound;

    protected override async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        _jumpSound = await _audio.LoadSoundAsync("assets/jump.wav", cancellationToken);
        _bgMusic = await _audio.LoadMusicAsync("assets/music.mp3", cancellationToken);

        _audio.PlayMusic(_bgMusic);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_input.IsKeyPressed(Keys.Space) && _jumpSound != null)
        {
            _audio.PlaySound(_jumpSound);
        }
    }
}
```

### Input Handling

```csharp
using Brine2D.Core;
using Brine2D.Input;

protected override void OnUpdate(GameTime gameTime)
{
    // Keyboard
    if (_input.IsKeyDown(Keys.W))
    {
        /* Move up */
    }

    if (_input.IsKeyPressed(Keys.Space))
    {
        /* Jump */
    }

    // Mouse
    var mousePos = _input.MousePosition;

    if (_input.IsMouseButtonPressed(MouseButton.Left))
    {
        /* Click */
    }

    // Gamepad
    if (_input.IsGamepadConnected())
    {
        var leftStick = _input.GetGamepadLeftStick();

        if (_input.IsGamepadButtonPressed(GamepadButton.A))
        {
            /* Jump */
        }
    }
}
```

## Configuration

Create a `gamesettings.json` file in your project:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Brine2D": "Debug"
    }
  },
  "Rendering": {
    "WindowTitle": "My Game",
    "WindowWidth": 1280,
    "WindowHeight": 720,
    "VSync": true,
    "Fullscreen": false,
    "Backend": "GPU"
  }
}
```

## Architecture

Brine2D follows a modular architecture with clear separation of concerns:

- **Brine2D.Core** - Core abstractions and interfaces
- **Brine2D.Engine** - Game loop and scene management
- **Brine2D.Rendering** - Rendering abstractions
- **Brine2D.Rendering.SDL** - SDL3 rendering implementation
- **Brine2D.Input** - Input abstractions
- **Brine2D.Input.SDL** - SDL3 input implementation
- **Brine2D.Audio** - Audio abstractions
- **Brine2D.Audio.SDL** - SDL3_mixer audio implementation
- **Brine2D.Hosting** - ASP.NET-style application hosting

## Requirements

- .NET 10 SDK
- SDL3 (included via SDL3-CS NuGet package)
- SDL3_image (for texture loading)
- SDL3_mixer (for audio playback)

## Building from Source

```bash
git clone https://github.com/CrazyPickleStudios/Brine2D.git
cd Brine2D
dotnet build
```

## Samples

Check out the `samples/` directory for complete working examples:

- **BasicGame** - Animation and input demo
- **PlatformerGame** - Coming soon
- **AdvancedGame** - Coming soon

## License

MIT License - see LICENSE file for details

## Credits

Built with:
- [SDL3](https://github.com/libsdl-org/SDL) - Simple DirectMedia Layer
- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS) - C# bindings for SDL3

---

Made with ❤️ by CrazyPickleStudios
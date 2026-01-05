# Brine2D

**The ASP.NET of game engines** - A modern .NET 10 game engine built on SDL3 for creating 2D games with C#.

Brine2D brings the familiar patterns and developer experience of ASP.NET to game development. If you've built web apps with ASP.NET, you'll feel right at home building games with Brine2D.

## Features

- **Entity Component System (ECS)** - ASP.NET-style system pipelines with automatic ordering ✨ **NEW in 0.4.0**
- **Input System** - Keyboard, mouse, gamepad with polling and events
- **Sprite Rendering** - Hardware-accelerated with sprite sheets and animations
- **Animation System** - Frame-based with multiple clips and events
- **Audio System** - Sound effects and music via SDL3_mixer
- **Scene Management** - Async loading, transitions, and lifecycle hooks
- **Tilemap Support** - Tiled (.tmj) integration with auto-collision
- **Collision Detection** - AABB and circle colliders with spatial partitioning
- **Camera System** - 2D camera with zoom, rotation, and bounds
- **Particle System** - GPU-accelerated particle effects
- **UI Framework** - Immediate-mode UI with theming and tooltips
- **Configuration** - JSON-based settings with hot reload support
- **Dependency Injection** - ASP.NET Core-style DI container
- **Logging** - Structured logging with Microsoft.Extensions.Logging
- **Multiple Backends** - SDL3 GPU (alpha) and Legacy renderer

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

// Configure ECS systems like middleware (NEW!)
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    pipelines.AddSystem<PlayerControllerSystem>();
    pipelines.AddSystem<AISystem>();
    pipelines.AddSystem<VelocitySystem>();
    pipelines.AddSystem<PhysicsSystem>();
    pipelines.AddSystem<SpriteRenderingSystem>();
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
| Middleware | **ECS System Pipelines** ✨ **NEW** |
| `app.UseAuthentication()` | `pipelines.AddSystem<T>()` ✨ **NEW** |
| `appsettings.json` | `gamesettings.json` |
| Dependency Injection | Dependency Injection |
| `ILogger<T>` | `ILogger<T>` |
| Configuration binding | Configuration binding |

## Quick Start

### Installation

**Using NuGet (Recommended)**

Create a new .NET 10 console project and add Brine2D:

```sh
dotnet new console -n MyGame
cd MyGame
dotnet add package Brine2D.Desktop --version 0.4.0-alpha
```

That's it! `Brine2D.Desktop` includes everything you need to start building games.

### Package Options

For most users, install the meta-package:
```sh
dotnet add package Brine2D.Desktop
```

**Advanced:** Install only what you need:
```sh
# Core abstractions
dotnet add package Brine2D.Core
dotnet add package Brine2D.Engine
dotnet add package Brine2D.ECS  # NEW!

# Choose your implementations
dotnet add package Brine2D.Rendering.SDL
dotnet add package Brine2D.Input.SDL
dotnet add package Brine2D.Audio.SDL

# ECS bridges (optional)
dotnet add package Brine2D.Rendering.ECS  # NEW!
dotnet add package Brine2D.Input.ECS      # NEW!
dotnet add package Brine2D.Audio.ECS      # NEW!
```

---

### Your First Game

Create `Program.cs`:

```csharp
using Brine2D.Core;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Input.SDL;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL;
using Microsoft.Extensions.Logging;

// Create the game application builder
var builder = GameApplication.CreateBuilder(args);

// Configure SDL3 rendering
builder.Services.AddSDL3Rendering(options =>
{
    options.WindowTitle = "My First Brine2D Game";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
    options.VSync = true;
});

// Add SDL3 input
builder.Services.AddSDL3Input();

// Register your scene
builder.Services.AddScene<GameScene>();

// Build and run
var game = builder.Build();
await game.RunAsync<GameScene>();

// Define your game scene
public class GameScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;

    public GameScene(
        IRenderer renderer,
        IInputService input,
        IGameContext gameContext,
        ILogger<GameScene> logger) : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.Clear(Color.CornflowerBlue);
        _renderer.BeginFrame();
        
        _renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
        
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

Run your game:
```sh
dotnet run
```

---

### Alpha Release Notice

**⚠️ This is an alpha release (0.4.0-alpha)**

What works:
- ✅ **Entity Component System (ECS)** ✨ **NEW!**
- ✅ **System pipelines with automatic ordering** ✨ **NEW!**
- ✅ **Prefabs and serialization** ✨ **NEW!**
- ✅ **Transform hierarchy (parent/child)** ✨ **NEW!**
- ✅ **Utility components (Timer, Lifetime, Tween)** ✨ **NEW!**
- ✅ Legacy rendering (sprites, primitives, text)
- ✅ Input system (keyboard, mouse, gamepad)
- ✅ Audio system
- ✅ Animation system
- ✅ Collision detection
- ✅ Tilemap support
- ✅ UI framework
- ✅ Camera system
- ✅ Particle system

What doesn't work yet:
- ❌ GPU renderer (use `Backend = "LegacyRenderer"` in config)
- ⚠️ Scene graph (partially implemented via ECS hierarchy)

**Expect breaking changes before 1.0!**

---

## 🆕 Entity Component System (ECS)

Brine2D 0.4.0 introduces a powerful ECS framework with ASP.NET-style system pipelines.

### Creating Entities

```csharp
using Brine2D.ECS;
using Brine2D.ECS.Components;
using System.Numerics;

// Create an entity
var player = _world.CreateEntity("Player");
player.Tags.Add("Player");

// Add components
var transform = player.AddComponent<TransformComponent>();
transform.Position = new Vector2(400, 300);

var velocity = player.AddComponent<VelocityComponent>();
velocity.MaxSpeed = 200f;

var sprite = player.AddComponent<SpriteComponent>();
sprite.TexturePath = "assets/player.png";
```

### Using Prefabs (Reusable Templates)

```csharp
using Brine2D.ECS;

// Create a prefab
var enemyPrefab = new EntityPrefab("Enemy");
enemyPrefab.Tags.Add("Enemy");

enemyPrefab.AddComponent<TransformComponent>();
enemyPrefab.AddComponent<SpriteComponent>(s => 
{
    s.TexturePath = "assets/enemy.png";
    s.Tint = new Color(255, 100, 100);
});
enemyPrefab.AddComponent<VelocityComponent>(v => v.MaxSpeed = 150f);
enemyPrefab.AddComponent<AIControllerComponent>(ai => 
{
    ai.Behavior = AIBehavior.Chase;
    ai.TargetTag = "Player";
});

// Register and instantiate
_prefabLibrary.Register(enemyPrefab);
var enemy = enemyPrefab.Instantiate(_world, new Vector2(500, 300));
```

### Configuring System Pipelines (ASP.NET-style!)

```csharp
using Brine2D.ECS.Systems;
using Brine2D.Rendering.ECS;
using Brine2D.Input.ECS;
using Brine2D.Audio.ECS;

// Configure like ASP.NET middleware!
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    // Update systems (run every frame)
    pipelines.AddSystem<PlayerControllerSystem>();  // Order: 10 (input)
    pipelines.AddSystem<AISystem>();                // Order: 50 (AI)
    pipelines.AddSystem<VelocitySystem>();          // Order: 100 (movement)
    pipelines.AddSystem<PhysicsSystem>();           // Order: 200 (collision)
    pipelines.AddSystem<AudioSystem>();             // Order: 300 (audio)
    pipelines.AddSystem<CameraSystem>();            // Order: 400 (camera)
    
    // Render systems (run during render phase)
    pipelines.AddSystem<SpriteRenderingSystem>();   // Order: 0 (sprites)
    pipelines.AddSystem<ParticleSystem>();          // Update + Render
    pipelines.AddSystem<DebugRenderer>();           // Order: 1000 (debug overlay)
});
```

### Using System Pipelines in Scenes

```csharp
public class GameScene : Scene
{
    private readonly UpdatePipeline _updatePipeline;
    private readonly RenderPipeline _renderPipeline;
    private readonly IEntityWorld _world;

    public GameScene(
        UpdatePipeline updatePipeline,
        RenderPipeline renderPipeline,
        IEntityWorld world,
        ILogger<GameScene> logger) : base(logger)
    {
        _updatePipeline = updatePipeline;
        _renderPipeline = renderPipeline;
        _world = world;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Execute all update systems in order (ASP.NET-style!)
        _updatePipeline.Execute(gameTime);
        
        // Update entity lifecycle
        _world.Update(gameTime);
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.BeginFrame();
        
        // Execute all render systems in order
        _renderPipeline.Execute(_renderer);
        
        _renderer.EndFrame();
    }
}
```

### Save/Load System

```csharp
using Brine2D.ECS.Serialization;

// Save game state
await _serializer.SaveWorldAsync(_world, "saves/game.json");

// Load game state
await _serializer.LoadAndRestoreWorldAsync(_world, "saves/game.json");
```

### Utility Components

```csharp
// Timer - Countdown with events
var timer = entity.AddComponent<TimerComponent>();
timer.Duration = 3f;
timer.OnComplete += () => Logger.LogInfo("Timer finished!");

// Lifetime - Auto-destroy after time
var lifetime = projectile.AddComponent<LifetimeComponent>();
lifetime.Lifetime = 5f;

// Tween - Simple animations
var tween = entity.AddComponent<TweenComponent>();
tween.Type = TweenType.Position;
tween.StartPosition = new Vector2(0, 0);
tween.EndPosition = new Vector2(100, 100);
tween.Duration = 1f;
tween.Easing = EasingType.EaseInOutQuad;
```

### Transform Hierarchy (Parent/Child)

```csharp
// Create weapon as child of player
var weapon = _world.CreateEntity("Sword");
weapon.AddComponent<TransformComponent>();
weapon.AddComponent<SpriteComponent>();

// Attach weapon to player (transforms follow parent)
weapon.SetParent(player);

// When player moves/rotates, weapon follows automatically!
```

---

## Examples

### ECS Quick Start Example

See `samples/BasicGame/ECSQuickStartScene.cs` for a complete minimal example showing:
- Entity creation
- Prefabs
- System pipelines
- Save/load
- Events

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
    "Backend": "LegacyRenderer"
  }
}
```

## Architecture

Brine2D follows a modular architecture with clear separation of concerns:

### Core Packages
- **Brine2D.Core** - Core abstractions, animation, collision, tilemap
- **Brine2D.Engine** - Game loop and scene management
- **Brine2D.Hosting** - ASP.NET-style application hosting
- **Brine2D.ECS** - Entity Component System ✨ **NEW!**

### Abstraction Layers
- **Brine2D.Rendering** - Rendering abstractions (IRenderer, ITexture, ICamera)
- **Brine2D.Input** - Input abstractions (IInputService, keyboard, mouse, gamepad)
- **Brine2D.Audio** - Audio abstractions (IAudioService, music, sound effects)

### SDL3 Implementations
- **Brine2D.Rendering.SDL** - SDL3 GPU + Legacy renderer implementation
- **Brine2D.Input.SDL** - SDL3 input implementation
- **Brine2D.Audio.SDL** - SDL3_mixer audio implementation

### ECS Bridges ✨ **NEW!**
- **Brine2D.Rendering.ECS** - Sprite rendering, particles, camera systems
- **Brine2D.Input.ECS** - Player controller system
- **Brine2D.Audio.ECS** - Audio playback system

### Extensions
- **Brine2D.UI** - UI framework (buttons, inputs, dialogs, tabs)

### Meta-Package
- **Brine2D.Desktop** - All-in-one package (recommended for most users)

## Requirements

- .NET 10 SDK
- SDL3 (included via SDL3-CS NuGet package)
- SDL3_image (for texture loading)
- SDL3_mixer (for audio playback)

## Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows | ✅ Supported | Tested on Windows 10/11 |
| Linux | ⚠️ Untested | Should work via SDL3 |
| macOS | ⚠️ Untested | Should work via SDL3 |

SDL3 provides cross-platform support, but we've only tested on Windows so far. Community testing on other platforms is welcome!

## Building from Source

If you want to build from source or contribute:

```sh
git clone https://github.com/CrazyPickleStudios/Brine2D.git
cd Brine2D
dotnet build
```

Then reference the projects directly in your game:

```xml
<ItemGroup>
  <ProjectReference Include="..\Brine2D\src\Brine2D.Desktop\Brine2D.Desktop.csproj" />
</ItemGroup>
```

## Samples

Check out the `samples/` directory for complete working examples:

- **BasicGame** - ECS demo with entities, prefabs, systems ✨ **NEW!**
- **ECSQuickStartScene** - Minimal ECS example ✨ **NEW!**
- **PlatformerGame** - Coming soon
- **AdvancedGame** - Coming soon

## Community & Support

- [GitHub Discussions](https://github.com/CrazyPickleStudios/Brine2D/discussions) - Ask questions, share projects
- [Issue Tracker](https://github.com/CrazyPickleStudios/Brine2D/issues) - Report bugs, request features
- [Documentation](https://www.brine2d.com) - Full guides and API reference
- [Sample Projects](https://github.com/CrazyPickleStudios/Brine2D/tree/main/samples) - Learn by example

### Roadmap

**0.4.0-alpha** ✅ **CURRENT**
- ✅ Entity Component System (ECS)
- ✅ ASP.NET-style system pipelines
- ✅ Prefabs and serialization
- ✅ Transform hierarchy
- ✅ Utility components
- ✅ Working ECS samples

**0.5.0-beta** (Upcoming)
- Advanced ECS queries and filters
- Complete GPU renderer
- More polished samples
- Performance optimizations

**1.0.0** (Future)
- Stable API
- Complete documentation
- Production-ready
- Full platform testing

See the full [roadmap](https://github.com/CrazyPickleStudios/Brine2D/milestones).

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see LICENSE file for details

## Credits

Built with:
- [SDL3](https://github.com/libsdl-org/SDL) - Simple DirectMedia Layer
- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS) - C# bindings for SDL3

---

Made with ❤️ by CrazyPickle Studios
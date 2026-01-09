# Brine2D

**The ASP.NET of game engines** - A modern .NET 10 game engine built on SDL3 for creating 2D games with C#.

Brine2D brings the familiar patterns and developer experience of ASP.NET to game development. If you've built web apps with ASP.NET, you'll feel right at home building games with Brine2D.

## Features

- **Entity Component System (ECS)** - ASP.NET-style system pipelines with automatic ordering
- **Scene Management** - Async loading, transitions, loading screens, and lifecycle hooks
- **Advanced Queries** - Fluent API with spatial queries, filtering, sorting, and caching
- **Performance Monitoring** - Built-in FPS counter, frame time graphs, and rendering statistics
- **Object Pooling** - Zero-allocation systems using `ArrayPool<T>` and custom object pools
- **Sprite Batching** - Automatic batching with layer sorting and frustum culling
- **Input System** - Keyboard, mouse, gamepad with polling and events
- **Sprite Rendering** - Hardware-accelerated with sprite sheets and animations
- **Animation System** - Frame-based with multiple clips and events
- **Audio System** - Sound effects and music via SDL3_mixer
- **Tilemap Support** - Tiled (.tmj) integration with auto-collision
- **Collision Detection** - AABB and circle colliders with spatial partitioning
- **Camera System** - 2D camera with follow, zoom, rotation, and bounds
- **Particle System** - Pooled particle effects with customizable emitters
- **UI Framework** - Complete component library with tooltips, tabs, dialogs, and more
- **Configuration** - JSON-based settings with hot reload support
- **Dependency Injection** - ASP.NET Core-style DI container
- **Logging** - Structured logging with Microsoft.Extensions.Logging
- **Multiple Backends** - SDL3 GPU (alpha) and Legacy renderer

## Why Brine2D?

### ASP.NET Developers Will Feel at Home

~~~csharp
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

// Configure ECS systems like middleware
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
~~~

### Key Similarities to ASP.NET

| ASP.NET | Brine2D |
|---------|---------|
| `WebApplicationBuilder` | `GameApplicationBuilder` |
| Controllers | Scenes |
| Middleware | **ECS System Pipelines** |
| `app.UseAuthentication()` | `pipelines.AddSystem<T>()` |
| **Automatic execution** | **Lifecycle hooks** |
| `appsettings.json` | `gamesettings.json` |
| Dependency Injection | Dependency Injection |
| `ILogger<T>` | `ILogger<T>` |
| Configuration binding | Configuration binding |

## Quick Start

### Installation

**Using NuGet (Recommended)**

Create a new .NET 10 console project and add Brine2D:

~~~sh
dotnet new console -n MyGame
cd MyGame
dotnet add package Brine2D.Desktop --version 0.5.0-beta
~~~

That's it! `Brine2D.Desktop` includes everything you need to start building games.

### Package Options

For most users, install the meta-package:
~~~sh
dotnet add package Brine2D.Desktop
~~~

**Advanced:** Install only what you need:
~~~sh
# Core abstractions
dotnet add package Brine2D.Core
dotnet add package Brine2D.Engine
dotnet add package Brine2D.ECS

# Choose your implementations
dotnet add package Brine2D.Rendering.SDL
dotnet add package Brine2D.Input.SDL
dotnet add package Brine2D.Audio.SDL

# ECS bridges (optional)
dotnet add package Brine2D.Rendering.ECS
dotnet add package Brine2D.Input.ECS
dotnet add package Brine2D.Audio.ECS
~~~

---

### Your First Game

Create `Program.cs`:

~~~csharp
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
        // Systems and rendering happen automatically!
        _renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Systems run automatically via lifecycle hooks!
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _gameContext.RequestExit();
        }
    }
}
~~~

Run your game:
~~~sh
dotnet run
~~~

---

### Beta Release Notice

**⚠️ This is a beta release (0.5.0-beta)**

What works:
- ✅ **Entity Component System (ECS)**
- ✅ **System pipelines with automatic ordering**
- ✅ **Advanced query system with fluent API**
- ✅ **Performance monitoring and profiling**
- ✅ **Object pooling (ArrayPool, custom pools)**
- ✅ **Sprite batching with frustum culling**
- ✅ **Scene transitions and loading screens**
- ✅ **Lifecycle hooks with opt-out for power users**
- ✅ **Prefabs and serialization**
- ✅ **Transform hierarchy (parent/child)**
- ✅ **Utility components (Timer, Lifetime, Tween)**
- ✅ Legacy rendering (sprites, primitives, text, lines)
- ✅ Input system (keyboard, mouse, gamepad)
- ✅ Audio system
- ✅ Animation system
- ✅ Collision detection with physics response
- ✅ Tilemap support
- ✅ UI framework (complete component library)
- ✅ Camera system with follow behavior
- ✅ Particle system with pooling

What doesn't work yet:
- ❌ GPU renderer (use `Backend = "LegacyRenderer"` in config)

**Expect breaking changes before 1.0!**

---

## Performance & Optimization

Brine2D is built for performance with zero-allocation hot paths and efficient memory management.

### Performance Monitoring

Built-in performance overlay with real-time statistics:

~~~csharp
// Enable performance monitoring
builder.Services.AddPerformanceMonitoring(options =>
{
    options.EnableOverlay = true;
    options.ShowFPS = true;
    options.ShowFrameTime = true;
    options.ShowMemory = true;
});
~~~

**Features:**
- FPS counter with min/max/average tracking
- Frame time graph (60-frame history)
- Memory usage monitoring
- Rendering statistics (sprites, draw calls, batches)
- Per-system profiling

**Hotkeys (in scenes):**
- `F3` - Toggle performance overlay
- `F4` - Toggle frame time graph
- `F5` - Toggle memory stats

### Zero-Allocation Systems

Brine2D uses `ArrayPool<T>` and custom object pools to minimize GC pressure:

~~~csharp
// Entity updates use ArrayPool for safe iteration
protected internal virtual void OnUpdate(GameTime gameTime)
{
    var array = ArrayPool<Component>.Shared.Rent(count);
    try
    {
        _components.CopyTo(array, 0);
        // Process components without allocation
    }
    finally
    {
        ArrayPool<Component>.Shared.Return(array, clearArray: true);
    }
}

// Particle system uses object pooling
private readonly ObjectPool<Particle> _particlePool;

// Get from pool instead of 'new'
var particle = _particlePool.Get();
// ... use particle ...
_particlePool.Return(particle);
~~~

### Sprite Batching

Automatic batching with layer sorting and texture grouping:

~~~csharp
// SpriteRenderingSystem automatically batches sprites
// - Groups by texture to minimize draw calls
// - Sorts by layer for correct rendering order
// - Frustum culling for off-screen sprites

var sprite = entity.AddComponent<SpriteComponent>();
sprite.TexturePath = "assets/player.png";
sprite.Layer = 10; // Higher layers render on top
sprite.Tint = Color.White;

// Check batching stats
var (renderedCount, drawCalls) = spriteSystem.GetBatchStats();
Logger.LogInfo($"Rendered {renderedCount} sprites in {drawCalls} draw calls");
~~~

---

## Scene Management

Brine2D now includes powerful scene management with transitions and loading screens.

### Scene Transitions

~~~csharp
using Brine2D.Engine;
using Brine2D.Engine.Transitions;

// Load scene with fade transition
await _sceneManager.LoadSceneAsync<GameScene>(
    new FadeTransition(duration: 0.5f, color: Color.Black)
);
~~~

### Loading Screens

~~~csharp
public class CustomLoadingScreen : LoadingScene
{
    protected override void OnRender(GameTime gameTime)
    {
        _renderer.DrawText($"Loading... {Progress:P0}", 500, 300, Color.White);
    }
}

// Use loading screen during scene load
await _sceneManager.LoadSceneAsync<GameScene>(
    loadingScreen: new CustomLoadingScreen(),
    transition: new FadeTransition(0.5f, Color.Black)
);
~~~

### Automatic System Execution

Systems run automatically via lifecycle hooks - no manual calls needed!

~~~csharp
public class GameScene : Scene
{
    // No need to inject UpdatePipeline or RenderPipeline!
    
    protected override void OnUpdate(GameTime gameTime)
    {
        // Your scene-specific logic
        CheckWinCondition();
        
        // ECS systems run automatically!
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        // Frame management automatic!
        // ECS rendering happens automatically!
        
        // Just draw your UI/debug info
        _renderer.DrawText($"Score: {_score}", 10, 10, Color.White);
    }
}
~~~

### Manual Control (Power Users)

Need fine-grained control? Opt-out of automatic behavior:

~~~csharp
public class ManualControlScene : Scene
{
    public override bool EnableLifecycleHooks => false; // Disable automatic execution
    
    protected override void OnUpdate(GameTime gameTime)
    {
        // You control when systems run
        _updatePipeline.Execute(gameTime);
        _world.Update(gameTime);
    }
}
~~~

See the [Lifecycle Hooks Guide](docs/guides/scenes/lifecycle-hooks.md) for advanced usage.

---

## Advanced Entity Queries

Build complex queries with a fluent API:

~~~csharp
using Brine2D.ECS.Query;

// Find low-health enemies near the player
var weakEnemies = _world.Query()
    .With<EnemyComponent>()
    .With<HealthComponent>()
    .With<TransformComponent>()
    .Without<DeadComponent>()
    .WithTag("Boss")
    .Where(e => 
    {
        var health = e.GetComponent<HealthComponent>();
        var transform = e.GetComponent<TransformComponent>();
        var distance = Vector2.Distance(transform.Position, playerPosition);
        
        return health.CurrentHealth < 50 && distance < 200f;
    })
    .Execute();
~~~

### Spatial Queries

Query entities within a radius or bounds:

~~~csharp
// Find all entities within 200 units of player
var nearbyEntities = _world.Query()
    .WithinRadius(playerPosition, 200f)
    .With<EnemyComponent>()
    .Execute();

// Find entities within screen bounds
var visibleEntities = _world.Query()
    .WithinBounds(new Rectangle(0, 0, 1280, 720))
    .Execute();
~~~

### Sorting and Pagination

~~~csharp
// Get 5 nearest enemies
var nearestEnemies = _world.Query()
    .With<EnemyComponent>()
    .OrderBy(e => Vector2.Distance(
        e.GetComponent<TransformComponent>().Position, 
        playerPosition))
    .Take(5)
    .Execute();

// Get second page of results
var page2 = _world.Query()
    .With<ItemComponent>()
    .Skip(10)
    .Take(10)
    .Execute();
~~~

### Cached Queries for Performance

~~~csharp
// Create cached query (updates automatically)
var movingEntities = _world.CreateCachedQuery<TransformComponent, VelocityComponent>();

// Use in systems (no allocation!)
public void Update(GameTime gameTime)
{
    foreach (var entity in movingEntities.Execute())
    {
        var transform = entity.GetComponent<TransformComponent>();
        var velocity = entity.GetComponent<VelocityComponent>();
        
        transform.Position += velocity.Velocity * deltaTime;
    }
}
~~~

---

## Entity Component System (ECS)

Brine2D's ECS framework with ASP.NET-style system pipelines.

### Creating Entities

~~~csharp
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
~~~

### Using Prefabs (Reusable Templates)

~~~csharp
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
~~~

### Configuring System Pipelines (ASP.NET-style!)

~~~csharp
using Brine2D.ECS.Systems;
using Brine2D.Rendering.ECS;
using Brine2D.Input.ECS;
using Brine2D.Audio.ECS;

// Configure like ASP.NET middleware!
builder.Services.ConfigureSystemPipelines(pipelines =>
{
    // Update systems (run every frame, automatically!)
    pipelines.AddSystem<PlayerControllerSystem>();  // Order: 10 (input)
    pipelines.AddSystem<AISystem>();                // Order: 50 (AI)
    pipelines.AddSystem<VelocitySystem>();          // Order: 100 (movement)
    pipelines.AddSystem<PhysicsSystem>();           // Order: 200 (collision)
    pipelines.AddSystem<AudioSystem>();             // Order: 300 (audio)
    pipelines.AddSystem<CameraSystem>();            // Order: 400 (camera)
    
    // Render systems (run during render phase, automatically!)
    pipelines.AddSystem<SpriteRenderingSystem>();   // Order: 0 (sprites)
    pipelines.AddSystem<ParticleSystem>();          // Update + Render
    pipelines.AddSystem<DebugRenderer>();           // Order: 1000 (debug overlay)
});
~~~

### Camera System

Automatic camera following with smooth movement and constraints:

~~~csharp
// Make camera follow player
var cameraFollow = player.AddComponent<CameraFollowComponent>();
cameraFollow.CameraName = "Main";
cameraFollow.Smoothing = 5f; // Higher = slower follow
cameraFollow.Offset = new Vector2(0, -50); // Camera offset from target
cameraFollow.Deadzone = new Vector2(50, 30); // Don't move if within deadzone
cameraFollow.FollowX = true;
cameraFollow.FollowY = true;
cameraFollow.Priority = 10; // Higher priority targets take precedence
~~~

### Particle System

Pooled particle effects with customizable emitters:

~~~csharp
// Create particle emitter
var emitter = entity.AddComponent<ParticleEmitterComponent>();
emitter.IsEmitting = true;
emitter.EmissionRate = 50f; // Particles per second
emitter.MaxParticles = 200;
emitter.ParticleLifetime = 2f;

// Configure appearance
emitter.StartColor = new Color(255, 200, 0, 255);
emitter.EndColor = new Color(255, 50, 0, 0); // Fade to transparent
emitter.StartSize = 8f;
emitter.EndSize = 2f;

// Configure physics
emitter.InitialVelocity = new Vector2(0, -50);
emitter.VelocitySpread = 30f; // Random angle variance
emitter.Gravity = new Vector2(0, 100);
emitter.SpawnRadius = 10f; // Random spawn area

// Get stats
Logger.LogInfo($"Active particles: {emitter.ParticleCount}");
~~~

### Save/Load System

~~~csharp
using Brine2D.ECS.Serialization;

// Save game state
await _serializer.SaveWorldAsync(_world, "saves/game.json");

// Load game state
await _serializer.LoadAndRestoreWorldAsync(_world, "saves/game.json");
~~~

### Utility Components

~~~csharp
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
~~~

### Transform Hierarchy (Parent/Child)

~~~csharp
// Create weapon as child of player
var weapon = _world.CreateEntity("Sword");
weapon.AddComponent<TransformComponent>();
weapon.AddComponent<SpriteComponent>();

// Attach weapon to player (transforms follow parent)
weapon.SetParent(player);

// When player moves/rotates, weapon follows automatically!
~~~

---

## Examples

### Loading and Drawing Sprites

~~~csharp
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
        if (_playerTexture != null)
        {
            _renderer.DrawTexture(_playerTexture, 100, 100);
        }
    }
}
~~~

### Sprite Animation

~~~csharp
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
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        _animator?.Update((float)gameTime.DeltaTime);
    }
}
~~~

### Playing Audio

~~~csharp
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
~~~

### Input Handling

~~~csharp
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
~~~


## Configuration

Create a `gamesettings.json` file in your project:

~~~json
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
  },
  "Performance": {
    "EnableOverlay": true,
    "ShowFPS": true,
    "ShowFrameTime": true,
    "ShowMemory": true
  }
}
~~~

## Architecture

Brine2D follows a modular architecture with clear separation of concerns:

### Core Packages
- **Brine2D.Core** - Core abstractions, animation, collision, tilemap, pooling
- **Brine2D.Engine** - Game loop, scene management, transitions
- **Brine2D.Hosting** - ASP.NET-style application hosting
- **Brine2D.ECS** - Entity Component System

### Abstraction Layers
- **Brine2D.Rendering** - Rendering abstractions (IRenderer, ITexture, ICamera)
- **Brine2D.Input** - Input abstractions (IInputService, keyboard, mouse, gamepad)
- **Brine2D.Audio** - Audio abstractions (IAudioService, music, sound effects)

### SDL3 Implementations
- **Brine2D.Rendering.SDL** - SDL3 GPU + Legacy renderer implementation
- **Brine2D.Input.SDL** - SDL3 input implementation
- **Brine2D.Audio.SDL** - SDL3_mixer audio implementation

### ECS Bridges
- **Brine2D.Rendering.ECS** - Sprite rendering, particles, camera systems
- **Brine2D.Input.ECS** - Player controller system
- **Brine2D.Audio.ECS** - Audio playback system

### Extensions
- **Brine2D.UI** - UI framework (buttons, inputs, dialogs, tabs, scroll views)

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

~~~sh
git clone https://github.com/CrazyPickleStudios/Brine2D.git
cd Brine2D
dotnet build
~~~

Then reference the projects directly in your game:

~~~xml
<ItemGroup>
  <ProjectReference Include="..\Brine2D\src\Brine2D.Desktop\Brine2D.Desktop.csproj" />
</ItemGroup>
~~~

## Samples

Check out the `samples/` directory for complete working examples:

### FeatureDemos (0.5.0)

Interactive demo menu showcasing all major features:

- **Query System Demo** - Advanced entity queries with spatial filtering, sorting, and pagination
- **Particle System Demo** - Pooled particle effects with fire, explosions, smoke, and trails
- **Collision Demo** - AABB and circle colliders with physics response
- **Scene Transitions Demo** - Fade transitions and custom loading screens
- **UI Components Demo** - Complete UI framework showcase
- **Manual Control Demo** - Power user lifecycle hook examples
- **Performance Benchmark** - Sprite batching stress test with 10,000+ sprites

Run the demos:
~~~sh
cd samples/FeatureDemos
dotnet run
~~~

**Performance hotkeys (in any demo scene):**
- `F3` - Toggle performance overlay
- `F4` - Toggle frame time graph  
- `F5` - Toggle memory statistics

---

## Community & Support

- [GitHub Discussions](https://github.com/CrazyPickleStudios/Brine2D/discussions) - Ask questions, share projects
- [Issue Tracker](https://github.com/CrazyPickleStudios/Brine2D/issues) - Report bugs, request features
- [Documentation](https://www.brine2d.com) - Full guides and API reference
- [Sample Projects](https://github.com/CrazyPickleStudios/Brine2D/tree/main/samples) - Learn by example

### Roadmap

**0.4.0-alpha** ✅ **RELEASED**
- ✅ Entity Component System (ECS)
- ✅ ASP.NET-style system pipelines
- ✅ Prefabs and serialization
- ✅ Transform hierarchy
- ✅ Utility components (Timer, Lifetime, Tween)
- ✅ Event system (EventBus, component lifecycle)
- ✅ Working ECS samples

**0.5.0-beta** ✅ **RELEASED**
- ✅ Advanced ECS queries and filters
- ✅ Spatial queries (WithinRadius, WithinBounds)
- ✅ Query builder pattern with sorting and pagination
- ✅ Cached queries for performance
- ✅ Scene transitions (FadeTransition)
- ✅ Loading screens
- ✅ Lifecycle hooks with opt-out for power users
- ✅ Automatic system execution
- ✅ Performance monitoring and profiling
- ✅ Object pooling (ArrayPool, custom pools)
- ✅ Sprite batching with frustum culling
- ✅ Camera follow system
- ✅ Particle system with pooling
- ✅ 7 polished interactive demos
- ✅ Complete UI framework (dialogs, tabs, tooltips, scroll views)
- ✅ Collision detection with physics response
- ✅ Bug fixes and stability improvements

**0.6.0-beta** (Next Release)
- Complete GPU renderer with SDL3
- Advanced batching with texture atlases
- Post-processing effects
- Enhanced particle system (textures, rotation, trails)
- Spatial audio
- Multi-threaded ECS systems
- Comprehensive documentation

**1.0.0** (Stable Release)
- Stable, production-ready API
- Complete documentation and tutorials
- Full platform testing (Windows, Linux, macOS)
- Advanced ECS optimizations
- Comprehensive sample games
- Migration guides from alpha/beta

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
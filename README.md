# Brine2D

<p align="center">
  <img src=".github/images/logo.png" alt="Brine2D Logo" width="200">
</p>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Build Status](https://github.com/CrazyPickleStudios/Brine2D/workflows/CI/badge.svg)](https://github.com/CrazyPickleStudios/Brine2D/actions)
[![codecov](https://codecov.io/github/CrazyPickleStudios/Brine2D/graph/badge.svg?token=RIDC7GF0J4)](https://codecov.io/github/CrazyPickleStudios/Brine2D)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**A modern, opinionated 2D game engine for .NET 10**, built on SDL3 and designed for C# developers who want a great experience without an editor or a content pipeline.

If you've built web applications with ASP.NET Core, Brine2D will feel immediately familiar. If you've ever wanted a .NET game engine that feels like the rest of the modern .NET ecosystem, this is for you.

---

## Why Brine2D?

Brine2D is a **full engine**, not just a rendering library. Scene management, an entity system, audio, input, collision, particles, UI, and a DI container all work together out of the box. Everything you'd otherwise build yourself in the first few weeks is already there.

~~~csharp
var builder = GameApplication.CreateBuilder(args);

builder.Configure(options =>
{
    options.Window.Title = "My Game";
    options.Window.Width  = 1280;
    options.Window.Height = 720;
    options.Rendering.VSync = true;
});

builder.AddScene<MainMenuScene>();
builder.AddScene<GameScene>();

await using var game = builder.Build();
await game.RunAsync<MainMenuScene>();
~~~

That's a complete entry point. `Build()` validates that every scene's dependencies are registered before the window opens. A missing service means a clear error message at startup, not a `NullReferenceException` mid-game.

**No content pipeline. No editor. No special build steps.**  
Drop assets into a folder and load them. That's it.

~~~csharp
public class LevelAssets : AssetManifest
{
    public readonly AssetRef<ITexture>     Tileset = Texture("assets/images/tileset.png");
    public readonly AssetRef<ISoundEffect> Jump    = Sound("assets/audio/jump.wav");
    public readonly AssetRef<IMusic>       Theme   = Music("assets/audio/music/theme.ogg");
    public readonly AssetRef<Font>         HUD     = Font("assets/fonts/ui.ttf", size: 20);
}

public class GameScene : Scene
{
    private readonly IAssetLoader _assetLoader;
    private readonly LevelAssets _manifest = new();

    public GameScene(IAssetLoader assetLoader) => _assetLoader = assetLoader;

    protected override async Task OnLoadAsync(CancellationToken ct, IProgress<float>? progress = null)
        => await _assetLoader.PreloadAsync(_manifest, cancellationToken: ct);

    protected override void OnEnter()
    {
        _player.Sprite.Texture = _manifest.Tileset;
        Audio.PlayMusic(_manifest.Theme);
    }
}
~~~

---

## Quick Start

~~~bash
dotnet new console -n MyGame
cd MyGame
dotnet add package Brine2D
~~~

**Program.cs:**
~~~csharp
using Brine2D.Hosting;

var builder = GameApplication.CreateBuilder(args);

builder.Configure(options =>
{
    options.Window.Title  = "My First Game";
    options.Window.Width  = 1280;
    options.Window.Height = 720;
});

await using var game = builder.Build();
await game.RunAsync<GameScene>();
~~~

**GameScene.cs:**
~~~csharp
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;

public class GameScene : Scene
{
    protected override void OnEnter()
    {
        Renderer.ClearColor = Color.DarkSlateBlue;

        World.CreateEntity("Player")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(640, 360))
            .AddComponent<SpriteComponent>()
            .AddBehavior<PlayerMovementBehavior>();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (Input.IsKeyPressed(Key.Escape))
            Game.RequestExit();
    }

    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("Hello, Brine2D!", 10, 10, Color.White);
    }
}
~~~

~~~bash
dotnet run
~~~

---

## ASP.NET Patterns You Already Know

| ASP.NET Core | Brine2D |
|---|---|
| `WebApplication.CreateBuilder()` | `GameApplication.CreateBuilder()` |
| `builder.Services.AddDbContext<T>()` | `builder.Services.AddCollisionSystem()` |
| `ControllerBase` properties | `Scene` properties (`Input`, `Audio`, `Renderer`) |
| Request-scoped `DbContext` | Scene-scoped `IEntityWorld` (auto-disposed on exit) |
| `ILogger<T>` | `ILogger<T>` (same interface, same DI container) |
| Middleware pipeline | ECS systems (ordered, auto-added) |

---

## Core Concepts

### Scene Lifecycle

~~~csharp
public class GameScene : Scene
{
    private readonly IAssetLoader _assetLoader;
    private LevelAssets _assets = new();

    public GameScene(IAssetLoader assetLoader) => _assetLoader = assetLoader;

    // 1. OnLoadAsync: I/O only. Runs while loading screen is visible.
    protected override async Task OnLoadAsync(CancellationToken ct, IProgress<float>? progress = null)
    {
        await _assetLoader.PreloadAsync(_assets, cancellationToken: ct);
    }

    // 2. OnEnter: Scene logic. Assets are ready. Default systems already added.
    protected override void OnEnter()
    {
        Audio.PlayMusic(_assets.Theme);

        World.CreateEntity("Player")
            .AddComponent<TransformComponent>(t => t.Position = new Vector2(400, 300))
            .AddComponent<SpriteComponent>(s => s.Texture = _assets.Tileset)
            .AddBehavior<PlayerMovementBehavior>();

        // Disable systems you don't need
        World.GetSystem<ParticleSystem>()!.IsEnabled = false;
    }

    // 3. OnUpdate: Every frame
    protected override void OnUpdate(GameTime gameTime) { }

    // 4. OnRender: Every frame, after systems render
    protected override void OnRender(GameTime gameTime) { }

    // 5. OnExit: Before unload
    protected override void OnExit()
    {
        Audio.StopMusic();
    }

    // 6. OnUnloadAsync: Release resources
    protected override Task OnUnloadAsync(CancellationToken ct) => Task.CompletedTask;
}
~~~

**Framework properties (always available, no constructor needed):**

| Property | Type | Description |
|---|---|---|
| `World` | `IEntityWorld` | Scene-scoped entity world, auto-disposed |
| `Renderer` | `IRenderer` | Draw calls and render state |
| `Input` | `IInputContext` | Keyboard, mouse, gamepad |
| `Audio` | `IAudioService` | Music and sound effects |
| `Logger` | `ILogger` | Scoped to your scene type |
| `Game` | `IGameContext` | Frame time, frame count |

**Inject only what's yours:**
~~~csharp
public class GameScene : Scene
{
    private readonly IPlayerService _playerService;

    // Only inject YOUR services; framework properties handle the rest
    public GameScene(IPlayerService playerService)
    {
        _playerService = playerService;
    }
}
~~~

**Default systems (added automatically in execution order):**

| System | Pipeline | Order | Purpose |
|---|---|---|---|
| `SpriteRenderingSystem` | Render | 0 | Sprite batching and frustum culling |
| `AudioSystem` | Update | 0 | Spatial audio processing |
| `VelocitySystem` | Update | 100 | Position integration |
| `CollisionDetectionSystem` | Update | 200 | AABB and circle colliders |
| `ParticleSystem` | Both | 250 / 100 | Particle effects with object pooling |
| `CameraSystem` | Update | 500 | Camera follow and zoom |
| `DebugRenderer` | Render | 1000 | Debug visualization (disabled by default) |

---

### Scene Navigation

~~~csharp
// Simple load (inject ISceneManager via constructor)
_sceneManager.LoadScene<GameScene>();

// With a fade transition
_sceneManager.LoadScene<GameScene>(
    new FadeTransition(duration: 0.5f, color: Color.Black));

// With a loading screen (scene loads in background, window never freezes)
_sceneManager.LoadScene<GameScene, MyLoadingScreen>(
    new FadeTransition(duration: 1f));

// With a factory, for passing runtime data DI can't provide
_sceneManager.LoadScene(sp =>
    new LevelScene(sp.GetRequiredService<IRenderer>(), levelNumber: 3));
~~~

Calling `LoadScene` from inside `OnUpdate` is safe; the transition is deferred to the frame boundary automatically.

---

### Hybrid ECBS Architecture

Brine2D uses a **hybrid Entity–Component–Behavior–System** model. The distinction matters:

**Component** = pure data, no logic
~~~csharp
public class HealthComponent : Component
{
    public int HP    { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
}
~~~

**Behavior** = entity-specific logic, full DI support
~~~csharp
public class PlayerMovementBehavior : EntityBehavior
{
    private readonly IInputContext _input;
    private TransformComponent _transform = null!;

    public PlayerMovementBehavior(IInputContext input) => _input = input;

    protected override void OnAttached()
        => _transform = Entity.GetRequiredComponent<TransformComponent>();

    public override void Update(GameTime gameTime)
    {
        if (_input.IsKeyDown(Key.W))
            _transform.Position -= Vector2.UnitY * 200f * (float)gameTime.DeltaTime;
    }
}
~~~

**System** = batch processing across many entities
~~~csharp
public class GravitySystem : UpdateSystemBase
{
    public override int UpdateOrder => SystemUpdateOrder.Physics;

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        world.Query()
            .With<TransformComponent>()
            .With<RigidbodyComponent>()
            .ForEach((entity, transform, body) =>
            {
                body.Velocity += new Vector2(0, 980f) * (float)gameTime.DeltaTime;
                transform.Position += body.Velocity * (float)gameTime.DeltaTime;
            });
    }
}
~~~

**When to use what:**

| | Behavior | System |
|---|---|---|
| Scope | One entity | Many entities |
| DI | ✅ Full injection | ✅ Constructor injection |
| Examples | Player input, boss AI | Physics, rendering, audio |
| Runs every frame | ✅ Automatic | ✅ Automatic |

---

### Asset Loading

No content pipeline. No build step. Drop files into `assets/` and load them.

**Option 1: Typed manifest (recommended for scenes)**

Declare your assets once as a class. Load them all in parallel with one call.

~~~csharp
public class LevelAssets : AssetManifest
{
    public readonly AssetRef<ITexture>     Tileset  = Texture("assets/images/tileset.png", TextureScaleMode.Nearest);
    public readonly AssetRef<ITexture>     Player   = Texture("assets/images/player.png");
    public readonly AssetRef<ISoundEffect> Jump     = Sound("assets/audio/jump.wav");
    public readonly AssetRef<ISoundEffect> Hurt     = Sound("assets/audio/hurt.wav");
    public readonly AssetRef<IMusic>       Theme    = Music("assets/audio/music/level1.ogg");
    public readonly AssetRef<Font>         HUDFont  = Font("assets/fonts/ui.ttf", size: 20);
}
~~~

~~~csharp
private readonly IAssetLoader _assetLoader;
private readonly LevelAssets _assets = new();

public GameScene(IAssetLoader assetLoader) => _assetLoader = assetLoader;

protected override async Task OnLoadAsync(CancellationToken ct, IProgress<float>? progress = null)
{
    // All assets loaded in parallel
    await _assetLoader.PreloadAsync(_assets, cancellationToken: ct);
}

protected override void OnEnter()
{
    // Implicit conversion, no .Value needed
    _player.Sprite.Texture = _assets.Player;
    Audio.PlayMusic(_assets.Theme);
}
~~~

**Option 2: Direct loading (quick scripts, one-off assets)**

~~~csharp
var tex  = await _assetLoader.GetOrLoadTextureAsync("assets/images/logo.png");
var sfx  = await _assetLoader.GetOrLoadSoundAsync("assets/audio/click.wav");
var font = await _assetLoader.GetOrLoadFontAsync("assets/fonts/mono.ttf", size: 14);
~~~

All three share the same thread-safe cache, so loading the same path twice returns the cached instance.

**Asset types and their loader methods:**

| Type | Method | Cached? |
|---|---|---|
| `ITexture` | `GetOrLoadTextureAsync` | ✅ Yes |
| `ISoundEffect` | `GetOrLoadSoundAsync` | ✅ Yes |
| `IMusic` | `GetOrLoadMusicAsync` | ✅ Yes |
| `Font` | `GetOrLoadFontAsync(path, size)` | ✅ Yes |

---

### Queries

**Fluent one-shot query:**
~~~csharp
// Finds all active enemies within 200px of the player, ordered by distance
World.Query()
    .With<TransformComponent>()
    .With<EnemyComponent>()
    .Without<DeadComponent>()
    .WithTag("active")
    .WithinRadius(playerPos, 200f)
    .ForEach<TransformComponent, EnemyComponent>((entity, transform, enemy) =>
    {
        enemy.Alert();
    });
~~~

**Cached query (for systems that run every frame):**
~~~csharp
// Declare in OnEnter; cache rebuilds only when components change
private CachedEntityQuery<TransformComponent, EnemyComponent> _enemyQuery = null!;

protected override void OnEnter()
{
    _enemyQuery = World.CreateCachedQuery<TransformComponent, EnemyComponent>()
        .WithTag("active")
        .Build();
}

// Use in Update: zero allocation per frame
public override void Update(IEntityWorld world, GameTime gameTime)
{
    _enemyQuery.ForEach((entity, transform, enemy) =>
    {
        // Process...
    });
}
~~~

**Query factory (reusable template):**
~~~csharp
// Captures state once; each call returns a fresh independent clone
private Func<EntityQuery> _nearbyEnemies;

protected override void OnEnter()
{
    _nearbyEnemies = World.Query()
        .With<EnemyComponent>()
        .WithTag("active")
        .ToFactory();
}

protected override void OnUpdate(GameTime gt)
{
    // Modify per-frame without affecting the template
    _nearbyEnemies()
        .WithinRadius(PlayerPosition, 200f)
        .ForEach<EnemyComponent>((entity, enemy) => enemy.Alert());
}
~~~

**Supported filters:**

| Method | Description |
|---|---|
| `.With<T>(filter?)` | Must have component, optional value filter |
| `.Without<T>()` | Must not have component |
| `.WithTag(tag)` | Must have tag |
| `.WithoutTag(tag)` | Must not have tag |
| `.WithAllTags(...)` | Must have all tags |
| `.WithAnyTag(...)` | Must have at least one tag |
| `.WithinRadius(center, r)` | Spatial circle query |
| `.WithinBounds(rect)` | Spatial AABB query |
| `.Where(predicate)` | Custom predicate |
| `.OrderBy(selector)` | Sort results |
| `.Take(n)` / `.Skip(n)` | Pagination |
| `.Random(n)` | Random selection |
| `.OnlyActive()` | Skip inactive entities |

---

### Camera

~~~csharp
// Follow the player with smooth lag
player.AddComponent<CameraFollowComponent>(c =>
{
    c.CameraName  = "main";
    c.Smoothing   = 5f;      // 0 = instant snap, 2 = dreamy, 15 = tight
    c.Deadzone    = new Vector2(50, 30); // Won't move within this range
    c.Offset      = new Vector2(0, -50); // Look slightly ahead
});

// Zoom with smoothing
player.GetComponent<CameraFollowComponent>()!.TargetZoom     = 1.5f;
player.GetComponent<CameraFollowComponent>()!.ZoomSmoothing  = 3f;

// Control directly
_camera.Position = new Vector2(640, 360);
_camera.Zoom     = 2f;

// Camera shake (from any system or behavior)
_camera.Shake(duration: 0.3f, intensity: 8f);
~~~

---

### Configuration

~~~csharp
builder.Configure(options =>
{
    // Window
    options.Window.Title      = "My Game";
    options.Window.Width      = 1280;
    options.Window.Height     = 720;
    options.Window.Fullscreen = false;

    // Rendering
    options.Rendering.VSync              = true;
    options.Rendering.TargetFPS          = 60;       // 0 = unlimited
    options.Rendering.PreferredGPUDriver = GPUDriver.Vulkan; // D3D12, Metal, Auto

    // ECS
    options.ECS.EnableMultiThreading       = true;
    options.ECS.ParallelEntityThreshold    = 100;   // auto-parallel at 100+ entities
    options.ECS.WorkerThreadCount          = null;  // null = all CPU cores

    // Loading screens
    options.LoadingScreenMinimumDisplayMs  = 200;   // 0 = disable flash prevention

    // Headless mode: no window, no audio (for servers and testing)
    options.Headless = false;
});
~~~

Invalid configuration throws at `Build()` with a clear, specific error message, not at runtime.

---

### Custom Systems

~~~csharp
public class CameraShakeSystem : UpdateSystemBase
{
    // Execution phase constants (use these instead of magic numbers)
    public override int UpdateOrder => SystemUpdateOrder.LateUpdate; // 800

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        world.Query()
            .With<CameraShakeComponent>()
            .ForEach<CameraShakeComponent>((entity, shake) =>
            {
                shake.Remaining -= (float)gameTime.DeltaTime;
                if (shake.Remaining <= 0)
                    entity.RemoveComponent<CameraShakeComponent>();
            });
    }
}
~~~

**Ordering constants:**

| Constant | Value | Use for |
|---|---|---|
| `SystemUpdateOrder.Input` | -100 | Input processing |
| `SystemUpdateOrder.Update` | 0 | Main update logic |
| `SystemUpdateOrder.Physics` | 100 | Physics simulation |
| `SystemUpdateOrder.Collision` | 200 | Collision detection |
| `SystemUpdateOrder.Animation` | 400 | Animation updates |
| `SystemUpdateOrder.LateUpdate` | 800 | Post-physics cleanup |

~~~csharp
protected override void OnEnter()
{
    World.AddSystem<CameraShakeSystem>();

    // Remove a default system you don't need
    World.RemoveSystem<ParticleSystem>();

    // Configure a default system
    World.GetSystem<DebugRenderer>()!.IsEnabled = true;
    World.GetSystem<DebugRenderer>()!.ShowColliders = true;
}
~~~

---

### Project-Wide Scene Configuration

Apply settings to every scene's world without modifying each scene:

~~~csharp
// In Program.cs, runs after default systems are added to every scene
builder.ConfigureScene(world =>
{
    world.GetSystem<DebugRenderer>()!.IsEnabled = true;
    world.AddSystem<AnalyticsSystem>();
});
~~~

---

### Scene Registration

Optional, but catches missing DI dependencies at startup rather than at runtime:

~~~csharp
// Validated at Build() -- throws if a dependency isn't registered
builder.AddScene<MainMenuScene>();
builder.AddScene<GameScene>();

// Multi-constructor scenes: annotate the one DI should use
[ActivatorUtilitiesConstructor]
public GameScene(IPlayerService playerService, IInputContext input) { ... }
~~~

Unregistered scenes still load via `ActivatorUtilities`. You'll just get a warning in the log.

---

### Dependency Injection

~~~csharp
// Register your services
builder.Services.AddSingleton<IPlayerService, PlayerService>();
builder.Services.AddSingleton<ISaveSystem, LocalSaveSystem>();

// Optional features
builder.ConfigureBrine2D(b => b.UseInputLayers()); // context-sensitive input routing
builder.Services.AddPostProcessing();
builder.Services.AddTextureAtlasing();
builder.Services.AddTilemapServices();
builder.Services.AddCollisionSystem();
builder.Services.AddUICanvas();
builder.Services.AddPerformanceMonitoring();
~~~

---

### Testing with Headless Mode

~~~csharp
[Fact]
public async Task Player_TakingDamage_Dies_At_Zero_HP()
{
    var builder = GameApplication.CreateBuilder();
    builder.Configure(o => o.Headless = true); // No window, no SDL
    builder.Services.AddSingleton<IPlayerService, PlayerService>();

    await using var game = builder.Build();

    // Run your scene on a background thread; test thread stays free
    var runTask = game.RunAsync<GameScene>();

    // ... assert things ...

    game.Services.GetRequiredService<GameLoop>().Stop();
    await runTask;
}
~~~

~~~csharp
// Shutdown behaviour (useful for test environments)
options.ShutdownTimeoutSeconds           = 5;   // wait before forcing shutdown
options.ForceShutdownGracePeriodSeconds  = 2;   // grace period after forced stop
~~~

---

### Rich Text

~~~csharp
Renderer.DrawText(
    "[b]Score:[/b] [color=#FFD700]9,999[/color]\n[size=14][i]Personal best![/i][/size]",
    x: 10, y: 10,
    new TextRenderOptions
    {
        ParseMarkup  = true,
        Color        = Color.White,
        MaxWidth     = 300,
        ShadowOffset = new Vector2(2, 2),
        ShadowColor  = new Color(0, 0, 0, 128)
    });
~~~

**Supported tags:** `[color=#RRGGBB]`, `[size=n]`, `[b]`, `[i]`, `[u]`, `[s]`

---

### Advanced Rendering

~~~csharp
// Post-processing (register via builder.Services.AddPostProcessing() in Program.cs)

// Off-screen render target
using var minimap = Renderer.CreateRenderTarget(256, 256);
Renderer.PushRenderTarget(minimap);
RenderMinimapContent();
Renderer.PopRenderTarget();
Renderer.DrawTexture(minimap.Texture, x: 10, y: 10);

// Scissor rectangle (UI scroll views, clipping)
Renderer.PushScissorRect(new Rectangle(10, 10, 300, 200));
DrawScrollableContent();
Renderer.PopScissorRect();
~~~

---

## Performance

**Built-in diagnostics:** press `F3` in any scene:

~~~
FPS: 60 (16.67ms)    Draw Calls: 12    Entities: 1,247    Systems: 8
~~~

`F4` shows per-system frame timings. `F5` shows a rolling frame time graph.

**How zero-allocation queries work:**

`ForEach` iterates directly over `ComponentPool<T>` snapshots rented from `ArrayPool<T>`. The hot path touches only entities that have the queried components, not the full entity list. Cached queries (`CreateCachedQuery`) rebuild only when components are added or removed; on frames with no structural changes, they iterate a pre-built list with zero setup.

**Characteristics:**

| Entity count | Notes |
|---|---|
| < 1,000 | Single-threaded, negligible cost |
| 1,000–10,000 | Auto-parallelizes `ForEach` queries |
| 10,000–50,000 | Component pools and cached queries shine |
| 50,000+ | Achievable with cached queries; profiling recommended |

**Tips:**
- Use `CreateCachedQuery` for any query that runs every frame
- Use `.WithinRadius` or `.WithinBounds` to narrow spatial queries instead of filtering manually
- Disable default systems you don't use (`ParticleSystem`, `CollisionDetectionSystem`) in scenes that don't need them
- `options.ECS.EnableMultiThreading = true` for large scenes on multi-core hardware

---

## Features

### Core Engine
- Hybrid ECBS: Components (data), Behaviors (entity logic + DI), Systems (batch processing)
- Scene management: async loading, transitions, loading screens, frame-boundary deferral
- Fluent entity queries: spatial indexing, zero-allocation `ForEach`, cached queries
- Event bus: type-safe pub/sub
- Ordered system execution with named phase constants
- Headless mode: full engine without a window, for dedicated servers and unit tests
- Delta time clamping: frame spikes from debugger pauses can't corrupt simulation

### Rendering
- SDL3 GPU backend: Vulkan, Direct3D 12, Metal
- Sprite batching with automatic frustum culling
- Post-processing pipeline: Bloom, Blur, Grayscale, custom HLSL shaders
- Off-screen render targets
- Scissor rectangles
- Rich text with BBCode markup and shadow support
- Camera system: smooth follow, deadzone, zoom, shake

### Audio
- Spatial 2D audio via SDL3_mixer
- Music streaming
- Sound effect pooling
- Per-channel volume control

### Input
- Keyboard, mouse, gamepad
- Input layer manager for context-sensitive bindings
- Action mapping

### Gameplay
- AABB and circle collision detection
- Particle system with object pooling
- Frame-based sprite animation
- Tilemap support: Tiled (`.tmj`) integration
- UI framework: canvas, buttons, labels, scroll views

### Developer Experience
- ASP.NET Core DI container
- `Microsoft.Extensions.Logging` structured logging
- Engine options validated at `Build()` via `DataAnnotations`; bad config fails fast with a clear error
- Unified asset loader: one service, all types, thread-safe cache
- `AssetManifest`: typed, compile-time-safe asset declarations
- Startup-time dependency validation for registered scenes

---

## Samples

~~~bash
# Getting started -- step-by-step tutorials
cd samples/GettingStarted/01-HelloBrine && dotnet run

# Feature showcase -- interactive demos of every system
cd samples/FeatureDemos && dotnet run
~~~

**Getting Started tutorials:**
1. `01-HelloBrine`: Window and first render
2. `02-SceneBasics`: Lifecycle and scene transitions
3. `03-DependencyInjection`: Services, DI, and configuration
4. `04-InputAndText`: Input and rich text rendering

**Feature demos (interactive):**
- ECS query system: fluent queries, spatial indexing, caching
- Particles: GPU-accelerated effects
- Texture atlasing: runtime sprite packing
- Collision: AABB and circle demos
- Spatial audio: 2D positional sound
- Post-processing: real-time shader effects
- Scissor rectangles: UI clipping and scroll views
- Transitions: fade, slide, custom
- UI framework: complete component demos
- Sprite benchmark: **50,000+ sprite stress test** with performance overlay

---

## Architecture

~~~
src/
  Brine2D/         - core engine (published to NuGet as Brine2D)
  Brine2D.Build/   - optional MSBuild tooling (Brine2D.Build, coming in 1.0)
samples/
  GettingStarted/  - numbered tutorials
  FeatureDemos/    - interactive feature showcase
tests/
  Brine2D.Tests/              - unit tests
  Brine2D.Integration.Tests/  - integration tests
~~~

**Design principles:**

- *Scene-scoped worlds*: each scene gets its own `IEntityWorld`, auto-disposed on exit. No entity leaks between scenes.
- *Framework properties*: common services available on `Scene` without constructor injection, matching ASP.NET's `ControllerBase` pattern.
- *Lifecycle separation*: `OnLoadAsync` for I/O, `OnEnter` for logic. Default systems are in place by the time `OnEnter` runs.
- *Convention over configuration*: sensible defaults everywhere; power users can replace, remove, or reorder anything.
- *Fail fast*: `Build()` validates options and scene dependencies before any window opens.

---

## Platform Support

| Platform | GPU Backend | Status |
|---|---|---|
| Windows | Vulkan / Direct3D 12 | ✅ Tested |
| macOS | Metal | ⚠️ Untested |
| Linux | Vulkan | ⚠️ Untested |

SDL3 provides the cross-platform layer. macOS and Linux should work. Community testing welcome.

---

## Requirements

- .NET 10 SDK
- SDL3, SDL3_image, SDL3_mixer, SDL3_ttf (all included via NuGet as `SDL3-CS.*`)
- No other native dependencies to install manually

---

## Current Status

**Version 0.9.x-beta.** All core features working; API may change before 1.0.

✅ Working:
- Scene management, transitions, loading screens
- Hybrid ECBS with scene-scoped worlds
- Zero-allocation parallel queries
- Unified asset loader with `AssetManifest` support
- SDL3 GPU and legacy renderers
- Rich text with BBCode
- Post-processing, render targets, scissor rects
- Spatial audio
- Collision detection
- Particle system
- UI framework
- Tilemap support
- Headless mode
- Startup dependency validation

⚠️ Known limitations:
- macOS and Linux untested
- Documentation site in progress
- Test coverage ~20% (target: 80% for 1.0)
- API stability not guaranteed until 1.0

**Coming in 1.0:**
- [ ] Stable API
- [ ] Complete documentation at [brine2d.com](https://brine2d.com)
- [ ] macOS and Linux CI
- [ ] 80%+ test coverage
- [ ] `Brine2D.Build`: optional NuGet for auto-generated asset path constants

---

## Testing

~~~bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
dotnet test tests/Brine2D.Tests
~~~

---

## Contributing

Contributions welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

Most useful right now:
- Testing on macOS or Linux and reporting results
- Adding test coverage
- Building a sample game and documenting rough edges
- Trying the getting-started path as a new user and filing issues where it's unclear

---

## Community

- **Discussions:** [GitHub Discussions](https://github.com/CrazyPickleStudios/Brine2D/discussions)
- **Issues:** [Issue Tracker](https://github.com/CrazyPickleStudios/Brine2D/issues)
- **Docs:** [brine2d.com](https://brine2d.com)

---

## License

MIT - see [LICENSE](LICENSE).

---

## Credits

**Built on:**
- [SDL3](https://github.com/libsdl-org/SDL): cross-platform multimedia
- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS): C# bindings

*Brine2D is part of the .NET game development ecosystem and stands on the shoulders of the community that proved C# is a great language for games.*

---

*Made with ❤️ by CrazyPickle Studios. Modern .NET, no editor required.*
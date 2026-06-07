<div align="center">
   <img src=".github/images/logo.png" alt="Brine2D - 2D Game Engine for .NET" width="200">

  <br />
  <br />

  [![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
  [![Build Status](https://github.com/CrazyPickleStudios/Brine2D/workflows/CI/badge.svg)](https://github.com/CrazyPickleStudios/Brine2D/actions)
  [![codecov](https://codecov.io/github/CrazyPickleStudios/Brine2D/graph/badge.svg?token=RIDC7GF0J4)](https://codecov.io/github/CrazyPickleStudios/Brine2D)
  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
</div>

**A modern, opinionated 2D game engine for .NET 10**, built on SDL3 and designed for C# developers who want a great experience without an editor or a content pipeline.

If you've built web applications with ASP.NET Core, Brine2D will feel immediately familiar. If you've ever wanted a .NET game engine that feels like the rest of the modern .NET ecosystem, this is for you.

---

## Why Brine2D?

Brine2D is a **full engine**, not just a rendering library. Scene management, an entity system, audio, input, Box2D physics, particles, UI, and a DI container all work together out of the box. Everything you'd otherwise build yourself in the first few weeks is already there.

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
    public readonly AssetRef<IFont>        HUD     = Font("assets/fonts/ui.ttf", size: 20);
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
| `builder.Services.AddDbContext<T>()` | `builder.Services.AddPhysics()` |
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

    // 4. OnFixedUpdate: Fixed timestep (default 60 Hz). Zero or more times per frame.
    protected override void OnFixedUpdate(GameTime fixedTime) { }

    // 5. OnRender: Every frame, after systems render
    protected override void OnRender(GameTime gameTime) { }

    // 6. OnExit: Before unload
    protected override void OnExit()
    {
        Audio.StopMusic();
    }

    // 7. OnUnloadAsync: Release resources
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
| `ParticleSystem` | Both | 250 / 100 | GPU-instanced particles, trails, sub-emitters, turbulence |
| `AnimationSystem` | Update | 400 | Sprite animation, state machines, layers, blend trees |
| `CameraSystem` | Update | 500 | Camera follow and zoom |
| `DebugRenderer` | Render | 1000 | Debug visualization (disabled by default) |

> **Physics systems are opt-in.** Call `builder.Services.AddPhysics()` and then `World.AddSystem<Box2DPhysicsSystem>()` in your scene's `OnEnter`. See the [Physics](#physics) section below.

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
public class PlayerMovementBehavior : Behavior
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

    // Also supports FixedUpdate for deterministic physics/simulation logic
    public override void FixedUpdate(GameTime fixedTime) { }
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
    public readonly AssetRef<IFont>        HUDFont  = Font("assets/fonts/ui.ttf", size: 20);
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
| `IFont` | `GetOrLoadFontAsync(path, size)` | ✅ Yes |

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

### Animation

Add `AnimatorComponent` alongside `SpriteComponent`. `AnimationSystem` is a default system and runs automatically.

**Building a clip manually:**
~~~csharp
var clip = new AnimationClip { Name = "run", PlaybackMode = PlaybackMode.Loop };
clip.AddFrame(new SpriteFrame(new Rectangle(0,   0, 48, 48)));
clip.AddFrame(new SpriteFrame(new Rectangle(48,  0, 48, 48)));
clip.AddFrame(new SpriteFrame(new Rectangle(96,  0, 48, 48)));
clip.AddFrame(new SpriteFrame(new Rectangle(144, 0, 48, 48)));

var idle = new AnimationClip { Name = "idle", PlaybackMode = PlaybackMode.Loop };
idle.AddFrame(new SpriteFrame(new Rectangle(0, 48, 48, 48), duration: 0.5f));

var attack = new AnimationClip { Name = "attack", PlaybackMode = PlaybackMode.OnceHoldLast };
attack.AddFrame(new SpriteFrame(new Rectangle(0,  96, 48, 48)));
attack.AddFrame(new SpriteFrame(new Rectangle(48, 96, 48, 48)));
~~~

**Setting up the entity:**
~~~csharp
var entity = World.CreateEntity("Player")
    .AddComponent<TransformComponent>(t => t.Position = new Vector2(400, 300))
    .AddComponent<SpriteComponent>(s => s.TexturePath = "assets/images/player.png")
    .AddComponent<AnimatorComponent>();

var anim = entity.GetComponent<AnimatorComponent>()!;
anim.Animator.AddAnimation(idle);
anim.Animator.AddAnimation(clip);
anim.Animator.AddAnimation(attack);
~~~

**Playback modes:**

| `PlaybackMode` | Description |
|---|---|
| `Loop` | Loops indefinitely (default) |
| `OnceHoldLast` | Plays once, freezes on last frame |
| `OnceHoldFirst` | Plays once, freezes on first frame |
| `OnceStop` | Plays once, then clears the current frame (`CurrentFrame` → `null`) |
| `PingPong` | Loops forward→backward indefinitely |
| `PingPongOnce` | One full forward→backward cycle, then stops |

**Playback control:**
~~~csharp
anim.Animator.Play("run");
anim.Animator.Play("attack");                           // hard cut
anim.Animator.PlayWithCrossFade("run", crossFadeDuration: 0.1f);
anim.Animator.PlayFromFrame("run", startFrame: 2);
anim.Animator.PlayFromNormalizedTime("run", normalizedTime: 0.5f);
anim.Animator.PlayQueued("idle");                       // plays after current non-looping clip ends
anim.Animator.Pause();
anim.Animator.Resume();
anim.Animator.Stop();
anim.Animator.Speed    = 1.5f;                          // playback speed multiplier
anim.Animator.Reversed = true;                          // play in reverse
~~~

**Animator events:**
~~~csharp
anim.Animator.OnAnimationStart    += clip => { };
anim.Animator.OnAnimationComplete += clip => { };       // non-looping clips only
anim.Animator.OnLoopComplete      += clip => { };       // each wrap, ping-pong reverse, or RepeatCount pass
anim.Animator.OnFrameChanged      += frame => { };
~~~

**State machine (automatic transitions):**
~~~csharp
var sm = anim.StateMachine;

sm.SetDefaultState("idle");

// Condition-based transitions
sm.AddTransition("idle", "run",
    condition: () => _speed > 10f,
    canInterrupt: false,
    crossFadeDuration: 0.08f);

sm.AddTransition("run", "idle",
    condition: () => _speed <= 10f,
    crossFadeDuration: 0.08f);

// AnyState → attack when trigger is armed
sm.AddAnyTriggerTransition("attack", anim.Parameters, "AttackTrigger",
    canInterrupt: false, crossFadeDuration: 0.05f);

// Return to idle automatically when "attack" finishes
sm.AddOnCompleteTransition("attack", "idle");

// Arm a trigger from a behavior or system
anim.Parameters.SetTrigger("AttackTrigger");
~~~

**Per-state callbacks:**
~~~csharp
sm.OnStateEnter("run",    prev => StartFootstepAudio());
sm.OnStateExit("attack",  next => ResetHitbox());
sm.OnStateChanged += (prev, next) => Debug.WriteLine($"{prev} → {next}");
~~~

**Animation layers** (independent tracks — e.g., body + upper-body overlay):
~~~csharp
var upperBody = anim.AddLayer("upper-body", priority: 1);
upperBody.Mask      = AnimationLayerMask.SourceRect | AnimationLayerMask.FlipX;
upperBody.Weight    = 1f;
upperBody.BlendMode = AnimationLayerBlendMode.Override; // or Additive

upperBody.Animator.AddAnimation(aimClip);
upperBody.Animator.Play("aim");

// Tint-flash layer (additive, drives only Tint)
var tintLayer = anim.AddLayer("hit-flash", priority: 2);
tintLayer.Mask      = AnimationLayerMask.Tint;
tintLayer.BlendMode = AnimationLayerBlendMode.Additive;
tintLayer.Animator.AddAnimation(flashClip);
tintLayer.Animator.Play("flash");
~~~

**1D blend selector** (continuous parameter → clip selection):
~~~csharp
var tree = new AnimationBlendSelector1D(anim.Animator);
tree.AddNode(threshold: 0f,   clipName: "idle", speed: 0f);
tree.AddNode(threshold: 150f, clipName: "walk", speed: 1f);
tree.AddNode(threshold: 400f, clipName: "run",  speed: 1.5f);
tree.CrossFadeDuration      = 0.08f; // smooth clip transitions
tree.RespectNonLoopingClips = true;  // don't interrupt attack/hurt clips

anim.BlendSelector1D = tree;

// Set each frame from a behavior or system
anim.BlendSelector1D.Value = _velocity.Length();
~~~

**2D blend selector** (two-axis directional selection):
~~~csharp
var tree2d = new AnimationBlendSelector2D(anim.Animator);
tree2d.AddNode(new Vector2( 0,  1), "walk-up");
tree2d.AddNode(new Vector2( 0, -1), "walk-down");
tree2d.AddNode(new Vector2(-1,  0), "walk-left");
tree2d.AddNode(new Vector2( 1,  0), "walk-right");

anim.BlendSelector2D       = tree2d;
anim.BlendSelector2D.Value = new Vector2(_inputX, _inputY);
~~~

**Per-frame hit boxes:**
~~~csharp
// Set on a frame when building the clip
frame.HitBox = new Rectangle(8, 4, 32, 40);            // primary hit box
frame.SetHitBox("sword", new Rectangle(32, 8, 24, 8)); // named hit box

// Read back at runtime
var box   = anim.CurrentHitBox;
var sword = anim.GetCurrentHitBox("sword");
~~~

**Per-frame clip events:**
~~~csharp
clip.AddEvent(new ClipEvent { Time = 0.1f, Name = "footstep" });

// Events surface via OnFrameChanged; inspect clip.Events to match by Name / Time
anim.Animator.OnFrameChanged += frame => { };
~~~

---

### Particles

`ParticleSystem` is a default system. Add `ParticleEmitterComponent` to any entity with a `TransformComponent`.

~~~csharp
World.CreateEntity("Fire")
    .AddComponent<TransformComponent>(t => t.Position = new Vector2(400, 300))
    .AddComponent<ParticleEmitterComponent>(e =>
    {
        e.EmissionRate   = 40f;
        e.MaxParticles   = 200;
        e.ParticleLifetime   = 1.5f;
        e.LifetimeVariation  = 0.4f;
        e.StartSize      = 6f;
        e.EndSize        = 0f;
        e.StartColor     = new Color(255, 180, 60, 220);
        e.EndColor       = new Color(255, 80, 0, 0);
        e.InitialVelocity = new Vector2(0, -80f);
        e.VelocitySpread  = 35f;
        e.Gravity         = new Vector2(0, -20f);
        e.BlendMode       = BlendMode.Additive;
    });
~~~

**Emitter shapes:**

| `EmitterShape` | Properties used | Notes |
|---|---|---|
| `Point` | — | All particles spawn at the same position |
| `Circle` | `SpawnRadius`, `SpawnOnPerimeter` | Uniform disk fill, or ring perimeter |
| `Box` | `ShapeSize`, `BoxAngle` | Rotatable rectangular spawn area |
| `Line` | `LineLength` / `ShapeSize.X`, `LineAngle` | Particles spawn along a rotatable segment |
| `Cone` | `SpawnRadius`, `ConeAngle`, `SpawnOnPerimeter` | Directional cone; uses `InitialVelocity` as axis |

**Key emitter properties:**

| Property | Description |
|---|---|
| `EmissionRate` | Particles per second (continuous emitters) |
| `IsBurst` / `BurstCount` | Single-frame burst instead of continuous emission |
| `Duration` / `Loop` | Auto-stop after N seconds; re-arm with `Loop = true` |
| `Delay` | Seconds before first emission (re-applied each loop) |
| `WarmupDuration` | Pre-simulates N seconds on first activation; useful for ambient effects |
| `MaxParticles` | Hard cap on live particles for this emitter |
| `ColorGradient` | Multi-stop `Color[]` sampled over lifetime; overrides `StartColor`/`EndColor` |
| `StartColorVariation` / `EndColorVariation` | Per-channel random nudge at spawn |
| `SizeVariation` / `EndSizeVariation` | Per-particle random size range |
| `StartSpeedMultiplier` / `EndSpeedMultiplier` | Speed curve over lifetime (1/1 = no change) |
| `Damping` | Exponential drag: `velocity *= exp(-Damping * dt)` |
| `TurbulenceStrength` / `TurbulenceFrequency` | Coherent value-noise velocity perturbation |
| `SimulateInLocalSpace` | Particles move with the entity; good for exhaust and auras |
| `VelocityInheritance` | Fraction of the entity's velocity added to new particles at spawn |
| `RenderLayer` | Draw order relative to other emitters (lower = further back) |
| `BlendMode` | `Alpha`, `Additive`, etc. |
| `ParticleTexture` / `ParticleAtlasRegion` | Optional sprite; untextured particles use an SDF soft circle |
| `ParticleFrames` | `AtlasRegion[]` animation strip distributed evenly over lifetime |

**Burst emitter:**
~~~csharp
emitter.IsBurst   = true;
emitter.BurstCount = 60;
emitter.Loop      = false;                    // fires once; entity disables itself when particles expire
emitter.OnEmitterFinished += () => entity.Destroy();
~~~

**Playback control:**
~~~csharp
var e = entity.GetComponent<ParticleEmitterComponent>()!;

e.Play();    // start or restart from clean state
e.Pause();   // freeze aging, movement, and emission
e.Resume();  // unfreeze
e.Stop();    // clear all live particles on the next update

// Snapshot/restore configuration (e.g., after runtime tweaks)
e.CaptureDefaultState();
e.ResetToDefaultState();    // throws if CaptureDefaultState was never called
e.TryResetToDefaultState(); // safe version; returns false if no snapshot exists
~~~

**Trails:**
~~~csharp
emitter.EnableTrails     = true;
emitter.TrailLength      = 8;         // history slots
emitter.TrailHeadAlpha   = 0.9f;      // alpha of newest trail segment
emitter.TrailTailAlpha   = 0.0f;      // alpha of oldest trail segment
emitter.TrailHeadSizeRatio = 1.0f;
emitter.TrailTailSizeRatio = 0.3f;
emitter.TrailMode        = TrailMode.Sprites; // or TrailMode.Lines (untextured only)
~~~

> Trail particles fall back to the batch renderer; GPU instancing is used for non-trail particles.

**Sub-emitters (birth, death, lifetime-fraction):**
~~~csharp
var spark = new SubEmitterConfig
{
    BurstCount     = 8,
    ParticleLifetime = 0.3f,
    StartSize      = 3f,
    EndSize        = 0f,
    InitialVelocity = Vector2.Zero,
    VelocitySpread  = 360f,
    StartColor     = Color.White,
    EndColor       = new Color(255, 255, 255, 0),
    BlendMode      = BlendMode.Additive,
    MaxParticles   = 400,             // shared cap across all bursts of this config
};

emitter.DeathSubEmitters = [spark];   // burst at each particle's death position
emitter.BirthSubEmitters = [spark];   // burst at each particle's spawn position

// Trigger at 50% lifetime
emitter.LifetimeFractionSubEmitters =
[
    new LifetimeFractionSubEmitter { Fraction = 0.5f, Config = spark }
];
~~~

Sub-emitter particles are managed internally — no extra entities or components needed. Sub-emitters do not chain.

**Custom forces (`IParticleForce`):**
~~~csharp
public class VortexForce : IParticleForce
{
    public Vector2 Center { get; set; }
    public float   Strength { get; set; } = 200f;

    public Vector2 Evaluate(Vector2 particleWorldPosition, float deltaTime)
    {
        var diff = Center - particleWorldPosition;
        var perp = new Vector2(-diff.Y, diff.X);
        return Vector2.Normalize(perp) * Strength * deltaTime;
    }
}

emitter.Forces = [new VortexForce { Center = new Vector2(400, 300) }];
~~~

Forces are summed into `BaseVelocity` every frame and are subject to `Damping` and `StartSpeedMultiplier`/`EndSpeedMultiplier`.

**Fire-and-forget burst (no entity required):**
~~~csharp
// Inject ParticleSystem via constructor
_particleSystem.Burst(worldPosition, sparkConfig);
~~~

**Callbacks:**
~~~csharp
emitter.OnParticleSpawned  += p => { };  // called immediately after spawn
emitter.OnParticleDied     += p => { };  // called at natural expiry (not Stop())
emitter.OnEmitterFinished  += ()  => { };  // called when a duration/burst emitter finishes
~~~

**Budget monitoring:**
~~~csharp
int total = World.GetSystem<ParticleSystem>()!.TotalParticleCount;
int own   = emitter.ParticleCount;
var live  = emitter.ActiveParticles; // IReadOnlyList<Particle>
~~~

---

### Physics

Brine2D integrates [Box2D 3.x](https://box2d.org/) for rigid-body physics. Register physics services once at startup, then add the system to any scene that needs it.

**Registration (Program.cs):**
~~~csharp
builder.Services.AddPhysics(options =>
{
    options.Gravity        = new Vector2(0, 980); // pixels/s² — Y-down screen space
    options.PixelsPerMeter = 100f;                // process-wide; all AddPhysics calls must match
    options.SubStepCount   = 4;                   // higher = more accurate, more CPU
});

// Optional: named layers for readable collision filtering
builder.Services.AddPhysicsLayers(layers =>
{
    layers.Register("Default",  0);
    layers.Register("Player",   1);
    layers.Register("Enemies",  2);
    layers.Register("Terrain",  3);
    layers.Register("Triggers", 4);
});
~~~

**Scene setup:**
~~~csharp
protected override void OnEnter()
{
    World.AddSystem<Box2DPhysicsSystem>();

    // Optional: kinematic character controller (two instances required)
    World.AddSystem<PrePhysicsKinematicCharacterSystem>();
    World.AddSystem<PostPhysicsKinematicCharacterSystem>();

    // Optional: debug overlay (visualizes shapes, contacts, AABBs)
    World.AddSystem<Box2DDebugDrawSystem>();
}
~~~

**Adding a physics body to an entity:**
~~~csharp
World.CreateEntity("Crate")
    .AddComponent<TransformComponent>(t => t.Position = new Vector2(400, 100))
    .AddComponent<SpriteComponent>()
    .AddComponent<PhysicsBodyComponent>(b =>
    {
        b.Shape         = new BoxShape(48, 48);
        b.BodyType      = PhysicsBodyType.Dynamic;
        b.Mass          = 1f;
        b.SurfaceFriction = 0.5f;
        b.Restitution   = 0.2f;
        b.Layer         = 0;
        b.CollisionMask = ulong.MaxValue;
    });
~~~

**Body types:**

| Type | Description |
|---|---|
| `Dynamic` | Fully simulated; affected by gravity, forces, and collisions |
| `Static` | Never moves; other bodies push off it (terrain, walls) |
| `Kinematic` | Moved by code, not forces; pushes dynamic bodies out |

**Shape types:** `CircleShape`, `BoxShape`, `CapsuleShape`, `PolygonShape`, `ChainShape`, `SegmentShape`

**Collision events:**
~~~csharp
var body = entity.GetComponent<PhysicsBodyComponent>()!;

body.OnCollisionEnter += (other, contact) =>
{
    Debug.WriteLine($"Hit {other.Entity?.Name} at speed {contact.ImpactSpeed:F1}");
};

body.OnCollisionExit  += other => { };
body.OnCollisionStay  += (other, contact) => { };

// Trigger (sensor) events
body.IsTrigger        = true;
body.OnTriggerEnter   += other => { };
body.OnTriggerExit    += other => { };
~~~

**Applying forces and impulses (from FixedUpdate):**
~~~csharp
body.ApplyLinearImpulse(new Vector2(0, -500)); // jump
body.ApplyForce(new Vector2(200, 0));           // wind
body.ApplyTorque(50f);
~~~

**Queries (raycasts and shape overlaps):**
~~~csharp
// Inject PhysicsWorld via constructor
private readonly PhysicsWorld _physics;

// Raycast
var hit = _physics.RaycastClosest(origin, direction, maxDistance,
    new PhysicsQueryFilter { ExcludeSensors = true });

// Shape cast (sweep a circle)
var hit = _physics.ShapeCastClosest(origin, radius: 24f, direction, maxDistance);

// Overlap check
Span<OverlapHit> results = stackalloc OverlapHit[16];
int count = _physics.OverlapCircle(center, radius: 100f, results);

// Filter helpers
PhysicsQueryFilter.SolidOnly              // excludes sensors
PhysicsQueryFilter.ForLayer(layerIndex)   // single layer
PhysicsQueryFilter.SolidLayer(layerIndex) // solid shapes on one layer
~~~

**Kinematic character controller:**
~~~csharp
World.CreateEntity("Player")
    .AddComponent<TransformComponent>(t => t.Position = new Vector2(400, 300))
    .AddComponent<PhysicsBodyComponent>(b =>
    {
        b.Shape         = new CapsuleShape(center1: new Vector2(0, -16), center2: new Vector2(0, 16), radius: 16f);
        b.BodyType      = PhysicsBodyType.Kinematic;
        b.CollisionMask = ulong.MaxValue;
    })
    .AddComponent<KinematicCharacterBody>(c =>
    {
        c.FloorAngleLimit = 0.8f;    // ~46° — steeper slopes count as walls
        c.SnapDistance    = 8f;      // snap-to-floor on steps and slopes
        c.MaxSpeed        = 400f;
    })
    .AddBehavior<PlayerMovementBehavior>();
~~~

~~~csharp
public class PlayerMovementBehavior : Behavior
{
    private readonly IInputContext _input;
    private KinematicCharacterBody _character = null!;
    private const float Speed  = 300f;
    private const float JumpVY = -600f;

    public PlayerMovementBehavior(IInputContext input) => _input = input;

    protected override void OnAttached()
        => _character = Entity.GetRequiredComponent<KinematicCharacterBody>();

    public override void FixedUpdate(GameTime fixedTime)
    {
        var vel = _character.Velocity;

        vel.X = _input.IsKeyDown(Key.Right) ? Speed
              : _input.IsKeyDown(Key.Left)  ? -Speed
              : 0f;

        if (_input.IsKeyPressed(Key.Space) && _character.IsGrounded)
            vel.Y = JumpVY;
        else
            vel.Y += 980f * (float)fixedTime.DeltaTime; // manual gravity

        _character.MoveAndSlide(vel);
    }
}
~~~

**One-way platforms:**
~~~csharp
platform.AddComponent<PhysicsBodyComponent>(b =>
{
    b.Shape                 = new BoxShape(200, 16);
    b.BodyType              = PhysicsBodyType.Static;
    b.IsOneWayPlatform      = true;
    b.PlatformNormalDirection = new Vector2(0, -1); // solid from above
});
~~~

**Ignoring collisions between two bodies:**
~~~csharp
_physicsWorld.IgnoreCollision(bodyA, bodyB);
_physicsWorld.RestoreCollision(bodyA, bodyB);
~~~

**Teleporting a body without a velocity spike:**
~~~csharp
body.Teleport(new Vector2(100, 200));
body.Teleport(new Vector2(100, 200), rotation: 0f);
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
    options.ECS.FixedTimeStepMs            = 1000.0 / 60.0; // ~16.67ms = 60 Hz
    options.ECS.MaxFixedStepsPerFrame      = 8;     // caps catch-up after long frames

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

**Fixed update systems** run at a fixed timestep (deterministic physics, networking):

~~~csharp
public class PhysicsIntegrationSystem : FixedUpdateSystemBase
{
    public override int FixedUpdateOrder => SystemFixedUpdateOrder.Physics; // 0

    public override void FixedUpdate(IEntityWorld world, GameTime fixedTime)
    {
        world.Query()
            .With<TransformComponent>()
            .With<RigidbodyComponent>()
            .ForEach((entity, transform, body) =>
            {
                transform.Position += body.Velocity * (float)fixedTime.DeltaTime;
            });
    }
}
~~~

**Fixed update ordering constants:**

| Constant | Value | Use for |
|---|---|---|
| `SystemFixedUpdateOrder.EarlyFixedUpdate` | -100 | Force application, input-driven velocities |
| `SystemFixedUpdateOrder.PrePhysics` | -50 | Constraint setup |
| `SystemFixedUpdateOrder.Physics` | 0 | Position integration |
| `SystemFixedUpdateOrder.PostPhysics` | 50 | Physics cleanup |
| `SystemFixedUpdateOrder.Collision` | 100 | Collision detection and resolution |
| `SystemFixedUpdateOrder.LateFixedUpdate` | 200 | Post-collision cleanup |

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

// Add a custom system to every scene as a default
builder.AddDefaultSystem<FogOfWarSystem>();
builder.AddDefaultSystem<FogOfWarSystem>(s => s.Radius = 200f); // with configuration

// Permanently exclude a default system project-wide (avoids construction cost entirely)
builder.ExcludeDefaultSystem<ParticleSystem>();
builder.ExcludeDefaultSystem<CollisionDetectionSystem>();
~~~

`ExcludeDefaultSystem` removes the system from every scene. To conditionally disable a system at runtime instead, use `ConfigureScene` with `IsEnabled = false`.

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

**Fallback scene for load failures:**

~~~csharp
// Replace the built-in error scene with your own
builder.UseFallbackScene<MyErrorScene>();
~~~

~~~csharp
public class MyErrorScene : Scene
{
    private readonly ISceneLoadErrorInfo _error;

    public MyErrorScene(ISceneLoadErrorInfo error) => _error = error;

    protected override void OnEnter()
    {
        Logger.LogError(_error.Exception, "Failed to load {Scene}", _error.FailedSceneName);
    }
}
~~~

If a scene load fails and no `SceneLoadFailed` event handler queues a recovery transition, the fallback scene is shown automatically.

---

### Dependency Injection

~~~csharp
// Register your services
builder.Services.AddSingleton<IPlayerService, PlayerService>();
builder.Services.AddSingleton<ISaveSystem, LocalSaveSystem>();

// Optional features
builder.ConfigureBrine2D(b => b.UseInputLayers()); // context-sensitive input routing
builder.Services.AddPhysics();                      // Box2D rigid-body physics
builder.Services.AddPhysicsLayers(layers => { ... }); // named layer registry
builder.Services.AddPostProcessing();
builder.Services.AddTextureAtlasing();
builder.Services.AddTilemapServices();
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
- Disable default systems you don't use (`ParticleSystem`) in scenes that don't need them
- Don't add `Box2DPhysicsSystem` to scenes that have no physics bodies — it has near-zero overhead when idle, but the intent is clearer
- `options.ECS.EnableMultiThreading = true` for large scenes on multi-core hardware

---

## Features

### Core Engine
- Hybrid ECBS: Components (data), Behaviors (entity logic + DI), Systems (batch processing)
- Scene management: async loading, transitions, loading screens, frame-boundary deferral
- Fluent entity queries: spatial indexing, zero-allocation `ForEach`, cached queries
- Event bus: type-safe pub/sub
- Fixed timestep pipeline: `FixedUpdateSystemBase`, `OnFixedUpdate`, deterministic simulation
- Ordered system execution with named phase constants
- Headless mode: full engine without a window, for dedicated servers and unit tests
- Delta time clamping: frame spikes from debugger pauses can't corrupt simulation

### Rendering
- SDL3 GPU backend: Vulkan, Direct3D 12, Metal
- Sprite batching with automatic frustum culling
- Post-processing pipeline: Blur, Grayscale, custom effects via `ISDL3PostProcessEffect`
- Off-screen render targets
- Scissor rectangles
- Rich text with BBCode markup and shadow support
- Camera system: smooth follow, deadzone, zoom, shake

### Audio
- Spatial 2D audio via SDL3_mixer
- Music streaming with crossfade support
- Sound effect pooling with priority-based track eviction
- Per-track volume, pan, and pitch control
- Bus-based audio grouping (pause/stop entire buses)
- Master, music, and sound volume channels

### Input
- Keyboard, mouse, multi-gamepad with automatic slot management
- Input layer manager: priority-based consumption with cleanup pass for lower layers
- Action maps: named, toggleable action groups with runtime rebinding
- 10+ binding types: key, key-axis, composite (Ctrl+S), mouse button, scroll, mouse delta, gamepad button, axis, trigger, stick (radial deadzone)
- Built-in `PlayerControllerSystem`: WASD + gamepad movement, diagonal normalization, custom action maps
- Gamepad features: radial and per-axis deadzones, rumble (standard + trigger), multi-gamepad lobby support
- Text input mode with full Unicode/IME support

### Gameplay
- Box2D 3.x rigid-body physics: dynamic, static, and kinematic bodies
- Five shape types: circle, box, capsule, polygon, chain
- Collision and sensor events with sub-shape detail (`OnCollisionEnter`, `OnTriggerEnter`, etc.)
- Raycasts, shape casts, and overlap queries with layer filtering
- Kinematic character controller: `MoveAndSlide`, `MoveAndCollide`, grounded state, snap-to-floor, moving platforms
- One-way platforms, collision groups, per-body gravity overrides
- Joints: revolute, distance, weld, prismatic, motor, wheel, mouse
- Particle system: GPU-instanced rendering, SDF soft circles, trails (sprites and line ribbon), sub-emitters (birth / death / lifetime-fraction triggers), coherent turbulence, `IParticleForce`, animated sprite frames, multi-stop color gradients, local-space simulation, warmup pre-simulation, object pooling
- Sprite animation: `AnimatorComponent` with `SpriteAnimator`, code-driven state machine, animation layers (independent tracks with mask, weight, blend mode), 1D and 2D blend trees, cross-fade, ping-pong, per-frame hit boxes and clip events
- Tilemap support: Tiled (`.tmj`) integration
- UI framework: canvas, buttons, labels, scroll views

### Developer Experience
- ASP.NET Core DI container
- `Microsoft.Extensions.Logging` structured logging
- Engine options validated at `Build()` via `DataAnnotations`; bad config fails fast with a clear error
- Unified asset loader: one service, all types, thread-safe cache
- `AssetManifest`: typed, compile-time-safe asset declarations
- Startup-time dependency validation for registered scenes
- Fallback scenes for graceful error recovery on load failures

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
- Physics: Box2D rigid bodies, character controller, joints, raycasts
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
- Box2D 3.x physics (rigid bodies, character controller, joints, raycasts, sensors)
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
# Brine2D

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Build Status](https://github.com/CrazyPickleStudios/Brine2D/workflows/CI/badge.svg)](https://github.com/CrazyPickleStudios/Brine2D/actions)
[![codecov](https://codecov.io/github/CrazyPickleStudios/Brine2D/graph/badge.svg?token=RIDC7GF0J4)](https://codecov.io/github/CrazyPickleStudios/Brine2D)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**The ASP.NET of game engines** - A modern .NET 10 game engine built on SDL3 for creating 2D games with C#.

Brine2D brings the familiar patterns and developer experience of ASP.NET to game development. If you've built web apps with ASP.NET, you'll feel right at home building games with Brine2D.

## Why Brine2D?

### Built for ASP.NET Developers

~~~csharp
// Looks familiar? That's the point!
var builder = GameApplication.CreateBuilder(args);

// Add Brine2D with sensible defaults
builder.Services.AddBrine2D(options =>
{
    options.WindowTitle = "My Game";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
});

// Register your scenes (like controllers)
builder.Services.AddScene<GameScene>();

var game = builder.Build();
await game.RunAsync<GameScene>();
~~~

### Clean, ASP.NET-Style Scenes

Framework concerns are handled automatically - you only inject what YOU need:

~~~csharp
// ✅ Clean! Only YOUR dependencies
public class GameScene : Scene
{
    private readonly IMyService _myService;
    
    public GameScene(IMyService myService)
    {
        _myService = myService;
    }
    
    protected override async Task OnLoadAsync(CancellationToken ct)
    {
        // Framework properties already set automatically!
        Logger.LogInformation("Loading game scene");
        Renderer.ClearColor = Color.Navy;
        
        var player = World.CreateEntity("Player");
        player.AddComponent<TransformComponent>();
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("Hello World!", 100, 100, Color.White);
    }
}
~~~

**Framework properties (set automatically):**
- `Logger` - Typed logger for your scene
- `World` - Entity world (scoped per scene, auto-cleanup!)
- `Renderer` - Drawing + state management
- `LoggerFactory` - For creating child loggers

### Key Similarities to ASP.NET

| ASP.NET | Brine2D |
|---------|---------|
| `WebApplicationBuilder` | `GameApplicationBuilder` |
| Controllers + `HttpContext` | Scenes + Framework Properties |
| Request scope (DI) | **Scene scope (EntityWorld)** |
| Property injection | **Property injection** |
| Middleware pipeline | ECS System pipelines |
| `appsettings.json` | `appsettings.json` |
| Convention over configuration | Convention over configuration |

## Features

**Core Engine:**
- **Entity Component System (ECS)** - Scoped per scene with automatic cleanup
- **Scene Management** - Async loading, transitions, loading screens
- **Advanced Queries** - Fluent API with spatial queries, filtering, sorting
- **Performance Monitoring** - FPS counter, frame time graphs, system profiling
- **Object Pooling** - Zero-allocation systems with `ArrayPool<T>`
- **Sprite Batching** - Automatic batching with frustum culling
- **Texture Atlasing** - Runtime sprite packing
- **Event System** - Type-safe EventBus

**Rendering & Graphics:**
- **SDL3 GPU Backend** - Vulkan/D3D12/Metal support
- **Legacy Renderer** - Compatibility fallback
- **Post-Processing** - Bloom, blur, grayscale, custom shaders
- **Sprite Rendering** - Hardware-accelerated with animations
- **Camera System** - Follow, zoom, rotation, bounds

**Audio & Input:**
- **Spatial Audio** - 2D distance attenuation + stereo panning
- **Input System** - Keyboard, mouse, gamepad with layers
- **Audio Playback** - Sound effects and music via SDL3_mixer

**Gameplay Systems:**
- **Collision Detection** - AABB and circle colliders
- **Particle System** - Pooled particles with textures, rotation, trails
- **Animation System** - Frame-based with multiple clips
- **Tilemap Support** - Tiled (.tmj) integration
- **UI Framework** - Complete component library

**Developer Experience:**
- **Dependency Injection** - ASP.NET Core-style DI
- **Logging** - Structured logging with `ILogger<T>`
- **Configuration** - JSON-based with hot reload
- **System.Drawing.Primitives** - Cross-platform `Rectangle`, `Color`

## Quick Start

### Installation

~~~sh
dotnet new console -n MyGame
cd MyGame
dotnet add package Brine2D
dotnet add package Brine2D.SDL
~~~

### Your First Game

**Program.cs:**
~~~csharp
using Brine2D.Hosting;

var builder = GameApplication.CreateBuilder(args);

builder.Services.AddBrine2D(options =>
{
    options.WindowTitle = "My First Game";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
});

builder.Services.AddScene<GameScene>();

var game = builder.Build();
await game.RunAsync<GameScene>();
~~~

**GameScene.cs:**
~~~csharp
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;

public class GameScene : Scene
{
    private readonly IInputContext _input;
    
    // Only inject YOUR dependencies!
    public GameScene(IInputContext input)
    {
        _input = input;
    }
    
    protected override Task OnInitializeAsync(CancellationToken ct)
    {
        Renderer.ClearColor = Color.CornflowerBlue;
        return Task.CompletedTask;
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("Press ESC to exit", 10, 10, Color.White);
    }
    
    protected override void OnUpdate(GameTime gameTime)
    {
        if (_input.IsKeyPressed(Key.Escape))
        {
            // Request app exit
        }
    }
}
~~~

Run it:
~~~sh
dotnet run
~~~

**That's it!** You have a working game.

### Scene Transitions

~~~csharp
// Load scene with fade transition
await SceneManager.LoadSceneAsync<MainMenuScene>(
    transition: new FadeTransition(duration: 0.5f, color: Color.Black)
);

// Load with loading screen
await SceneManager.LoadSceneAsync<GameScene, CustomLoadingScreen>(
    transition: new FadeTransition(duration: 1f, color: Color.Black)
);
~~~

Loading screens use the same property injection pattern - framework handles construction!

### ECS Example

~~~csharp
protected override async Task OnLoadAsync(CancellationToken ct)
{
    // World is already set by framework!
    var player = World.CreateEntity("Player");
    player.AddComponent<TransformComponent>().Position = new Vector2(400, 300);
    player.AddComponent<SpriteComponent>().Texture = await LoadTexture("player.png");
    player.AddComponent<VelocityComponent>();
    
    Logger.LogInformation("Player created with {Count} components", 
        player.GetAllComponents().Count);
}
~~~

**Each scene gets its own isolated EntityWorld** - when the scene unloads, entities are automatically cleaned up. No manual cleanup needed!

## Architecture

### Package Structure

| Package | Purpose | Required? |
|---------|---------|-----------|
| `Brine2D` | Complete game engine | ✅ Yes |
| `Brine2D.SDL` | Platform implementation | ✅ Yes |
| `Brine2D.Tilemap` | Tilemap support | Optional |
| `Brine2D.UI` | UI framework | Optional |

**Just 4 packages** (down from 12+ in previous versions!)

### Key Architecture Decisions

**Scoped EntityWorld:**
- Each scene gets its own `IEntityWorld`
- Automatic cleanup when scene unloads
- No entity leaks between scenes
- Service-based persistence (`PlayerService`, not persistent entities)
- Matches ASP.NET request scope pattern

**Property Injection:**
- Framework properties set automatically by `SceneManager`
- Constructors only have YOUR application dependencies
- Matches ASP.NET's `ControllerBase` pattern

**Convention Over Configuration:**
- Sensible defaults ("batteries included")
- Simple setup API
- Power users can still opt-out

## Documentation

**Full guides and tutorials:** [brine2d.com](https://www.brine2d.com)

**Topics covered:**
- Getting Started tutorials
- ECS deep dive
- Scene management patterns
- Advanced queries
- Performance optimization
- Custom systems
- Rendering techniques

## Samples

### Getting Started Tutorials

Step-by-step for ASP.NET developers:
- **01-HelloBrine** - Minimal setup
- **02-SceneBasics** - Scene lifecycle and transitions
- **03-DependencyInjection** - Services, `ILogger<T>`, `IOptions<T>`
- **04-InputAndText** - Keyboard, mouse, rendering

### Feature Demos

Interactive showcase of all major features:
- Query System
- Particle Effects
- Texture Atlasing
- Spatial Audio
- Collision Detection
- Scene Transitions
- UI Components
- Performance Benchmark (10,000+ sprites!)

~~~sh
# Run the demos
cd samples/FeatureDemos
dotnet run

# Performance hotkeys
# F3 - Toggle FPS overlay
# F4 - Toggle system profiling
# F5 - Toggle frame time graph
~~~

## Beta Release

**⚠️ Version 0.9.1-beta**

This release includes major architecture improvements:
- ✅ **Scoped EntityWorld** - Per-scene isolation and automatic cleanup
- ✅ **Property injection** - ASP.NET-style framework properties
- ✅ **Merged IRenderer** - Unified state + operations interface
- ✅ **Improved scene transitions** - No more overlapping/race conditions
- ✅ **Cleaner API** - Constructor only for YOUR dependencies

**What works:** All core features, ECS, rendering, audio, input, UI, collision, particles, atlasing, profiling.

**What's coming in 1.0.0:**
- Stable, production-ready API
- Complete documentation
- Full platform testing (Windows, Linux, macOS)
- Migration guides from beta

**Expect breaking changes before 1.0!**

## Requirements

- .NET 10 SDK
- SDL3 (included via SDL3-CS NuGet)
- SDL3_image (texture loading)
- SDL3_mixer (audio playback)

## Platform Support

| Platform | Status |
|----------|--------|
| Windows | ✅ Tested |
| Linux | ⚠️ Untested (should work) |
| macOS | ⚠️ Untested (should work) |

SDL3 provides cross-platform support. Community testing welcome!

## Community & Support

- [GitHub Discussions](https://github.com/CrazyPickleStudios/Brine2D/discussions)
- [Issue Tracker](https://github.com/CrazyPickleStudios/Brine2D/issues)
- [Documentation](https://www.brine2d.com)
- [Sample Projects](https://github.com/CrazyPickleStudios/Brine2D/tree/main/samples)

## Roadmap

**1.0.0** (Next Release)
- Stable API contract
- Complete documentation
- Full platform testing
- Performance optimizations
- Migration guides

See the full [roadmap](https://github.com/CrazyPickleStudios/Brine2D/milestones).

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Testing

~~~sh
# Run tests
dotnet test tests/Brine2D.Tests

# With coverage
dotnet test tests/Brine2D.Tests --collect:"XPlat Code Coverage"
~~~

**Current coverage: ~20%** | **Target for 1.0: >80% core systems**

## License

MIT License - see LICENSE file for details

## Credits

Built with:
- [SDL3](https://github.com/libsdl-org/SDL) - Simple DirectMedia Layer
- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS) - C# bindings for SDL3

---

**Made with ❤️ by CrazyPickle Studios**

*The ASP.NET of game engines - familiar patterns, clean code, modern .NET*
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
- **Texture Atlasing** - Runtime sprite packing with intelligent bin packing
- **Event System** - Type-safe EventBus for decoupled component communication
- **Input System** - Keyboard, mouse, gamepad with polling and events
- **Sprite Rendering** - Hardware-accelerated with sprite sheets and animations
- **Animation System** - Frame-based with multiple clips and events
- **Audio System** - Sound effects, music, and 2D spatial audio with distance attenuation
- **Tilemap Support** - Tiled (.tmj) integration with auto-collision
- **Collision Detection** - AABB and circle colliders with spatial partitioning
- **Camera System** - 2D camera with follow, zoom, rotation, and bounds
- **Particle System** - Pooled particles with textures, rotation, trails, and blend modes
- **UI Framework** - Complete component library with tooltips, tabs, dialogs, and more
- **Configuration** - JSON-based settings with hot reload support
- **Dependency Injection** - ASP.NET Core-style DI container
- **Logging** - Structured logging with Microsoft.Extensions.Logging
- **Multiple Backends** - SDL3 GPU (modern, high-performance) and Legacy renderer (compatibility)

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
    
    // Choose your backend
    options.Backend = GraphicsBackend.GPU;              // Modern SDL3 GPU API (recommended)
    // options.Backend = GraphicsBackend.LegacyRenderer; // SDL_Renderer API (fallback)
    
    options.PreferredGPUDriver = "vulkan"; // or "d3d12", "metal"
    options.VSync = true;
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
dotnet add package Brine2D.Desktop
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

**⚠️ This is a beta release (0.8.0-beta)**

What works:
- ✅ **Entity Component System (ECS)**
- ✅ **System pipelines with automatic ordering**
- ✅ **Advanced query system with fluent API**
- ✅ **Performance monitoring and profiling**
- ✅ **Object pooling (ArrayPool, custom pools)**
- ✅ **Sprite batching with frustum culling**
- ✅ **Texture atlasing with runtime packing**
- ✅ **Scene transitions and loading screens**
- ✅ **Lifecycle hooks with opt-out for power users**
- ✅ **EventBus for component communication**
- ✅ **Prefabs and serialization**
- ✅ **Transform hierarchy (parent/child)**
- ✅ **Utility components (Timer, Lifetime, Tween)**
- ✅ **GPU rendering** (SDL3 GPU API with Vulkan/D3D12/Metal)
- ✅ **Legacy rendering** (SDL_Renderer API for compatibility)
- ✅ Sprites, primitives, text, lines
- ✅ Input system (keyboard, mouse, gamepad)
- ✅ **Spatial audio system** (2D distance attenuation + stereo panning)
- ✅ Animation system
- ✅ Collision detection with physics response
- ✅ Tilemap support
- ✅ UI framework (complete component library)
- ✅ Camera system with follow behavior
- ✅ **Advanced particle system** (textures, rotation, trails, blend modes, 7 emitter shapes)

What's coming next:
- 🔄 Post-processing effects
- 🔄 Multi-threaded ECS systems

**Expect breaking changes before 1.0!**

---

## Documentation

Full guides and API reference available at [brine2d.com](https://www.brine2d.com)

---

## Samples

Check out the `samples/` directory for complete working examples:

### FeatureDemos

Interactive demo menu showcasing all major features:

- **Query System Demo** - Advanced entity queries with spatial filtering, sorting, and pagination
- **Particle System Demo** - Pooled particle effects with fire, explosions, smoke, and trails
- **Texture Atlas Demo** - Runtime sprite packing with automatic batching
- **Spatial Audio Demo** - 2D positional audio with distance attenuation and panning
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

---

## Community & Support

- [GitHub Discussions](https://github.com/CrazyPickleStudios/Brine2D/discussions) - Ask questions, share projects
- [Issue Tracker](https://github.com/CrazyPickleStudios/Brine2D/issues) - Report bugs, request features
- [Documentation](https://www.brine2d.com) - Full guides and API reference
- [Sample Projects](https://github.com/CrazyPickleStudios/Brine2D/tree/main/samples) - Learn by example

### Roadmap

**0.8.0-beta** (Current Release)
- ✅ Texture atlasing with runtime packing
- ✅ 2D spatial audio system
- ✅ Advanced particle system (textures, rotation, trails, blend modes)
- ✅ SDL3 GPU and Legacy renderers stable

**0.9.0-beta** (Next Release)
- Post-processing effects (bloom, blur, etc.)
- Multi-threaded ECS systems
- Performance optimizations

**1.0.0** (Stable Release)
- Stable, production-ready API
- Complete documentation and tutorials
- Full platform testing (Windows, Linux, macOS)
- Comprehensive sample games
- Migration guides from beta

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
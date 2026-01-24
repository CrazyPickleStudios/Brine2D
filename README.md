# Brine2D

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Build Status](https://github.com/CrazyPickleStudios/Brine2D/workflows/CI/badge.svg)](https://github.com/CrazyPickleStudios/Brine2D/actions)
[![codecov](https://codecov.io/github/CrazyPickleStudios/Brine2D/graph/badge.svg?token=RIDC7GF0J4)](https://codecov.io/github/CrazyPickleStudios/Brine2D)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Modern 2D game development with .NET elegance** - A modern .NET 10 game engine built on SDL3 for creating 2D games with C#.

Brine2D brings the familiar patterns and developer experience of ASP.NET to game development. If you've built web apps with ASP.NET, you'll feel right at home building games with Brine2D.

## Features

- **Entity Component System (ECS)** - ASP.NET-style system pipelines with automatic ordering
- **Scene Management** - Async loading, transitions, loading screens, and lifecycle hooks
- **Advanced Queries** - Fluent API with spatial queries, filtering, sorting, and caching
- **Performance Monitoring** - Built-in FPS counter, frame time graphs, system profiling, and rendering statistics
- **Object Pooling** - Zero-allocation systems using `ArrayPool<T>` and custom object pools
- **Sprite Batching** - Automatic batching with layer sorting and frustum culling
- **Texture Atlasing** - Runtime sprite packing with intelligent bin packing
- **Event System** - Type-safe EventBus for decoupled component communication
- **Input System** - Keyboard, mouse, gamepad with polling and events
- **Sprite Rendering** - Hardware-accelerated with sprite sheets and animations
- **Animation System** - Frame-based with multiple clips and tween components
- **Audio System** - Sound effects, music, and 2D spatial audio with distance attenuation
- **Tilemap Support** - Tiled (.tmj) integration with auto-collision
- **Collision Detection** - AABB and circle colliders with spatial partitioning
- **Camera System** - 2D camera with follow, zoom, rotation, and bounds
- **Particle System** - Pooled particles with textures, rotation, trails, and blend modes
- **Post-Processing Effects** - Bloom, blur, grayscale, and custom shader effects pipeline
- **UI Framework** - Complete component library with tooltips, tabs, dialogs, and more
- **Configuration** - JSON-based settings with hot reload support
- **Dependency Injection** - ASP.NET Core-style DI container
- **Logging** - Structured logging with Microsoft.Extensions.Logging
- **Multiple Backends** - SDL3 GPU (modern, high-performance) and Legacy renderer (compatibility)

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
```

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

```sh
dotnet new console -n MyGame
cd MyGame
dotnet add package Brine2D
dotnet add package Brine2D.SDL
```

That's it! You're ready to build games.

### Package Options

**For most users:**
```sh
# Core engine (ECS-first, batteries included)
dotnet add package Brine2D

# Platform implementation
dotnet add package Brine2D.SDL

# Optional features
dotnet add package Brine2D.Tilemap
dotnet add package Brine2D.UI
```

---

### Your First Game

Create `Program.cs`:

```csharp
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.SDL;
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
        _renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
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

### Beta Release Notice

**⚠️ This is a beta release (0.9.0-beta)**

What works:
- ✅ **ECS-first architecture** - Systems integrated into core package
- ✅ **Entity Component System (ECS)** - Fully featured with advanced queries
- ✅ **System pipelines with automatic ordering**
- ✅ **Multi-threaded ECS systems** - Parallel system execution with job scheduling
- ✅ **Advanced query system** - Spatial queries, filtering, sorting, caching
- ✅ **Performance monitoring and profiling** - Frame time graphs, system profiling
- ✅ **Object pooling** - ArrayPool integration, custom pools
- ✅ **Sprite batching with frustum culling**
- ✅ **Texture atlasing with runtime packing**
- ✅ **Scene transitions and loading screens**
- ✅ **Lifecycle hooks with opt-out for power users**
- ✅ **EventBus for component communication**
- ✅ **Prefabs and serialization**
- ✅ **Transform hierarchy (parent/child)**
- ✅ **Built-in components** - Timer, Lifetime, Tween, Velocity, Transform
- ✅ **Built-in systems** - Physics, AI, Audio, Input, Rendering
- ✅ **GPU rendering** (SDL3 GPU API with Vulkan/D3D12/Metal)
- ✅ **Legacy rendering** (SDL_Renderer API for compatibility)
- ✅ **Post-processing effects pipeline** - Bloom, blur, grayscale, custom shaders
- ✅ Sprites, primitives, text, lines
- ✅ Input system (keyboard, mouse, gamepad with layers)
- ✅ **Spatial audio system** (2D distance attenuation + stereo panning)
- ✅ Animation system with frame-based clips
- ✅ Collision detection with physics response
- ✅ Tilemap support (Tiled .tmj format)
- ✅ **UI framework** (complete component library with tooltips, tabs, dialogs)
- ✅ Camera system with follow behavior
- ✅ **Advanced particle system** (textures, rotation, trails, blend modes, 7 emitter shapes)
- ✅ **System.Drawing.Primitives integration** - Rectangle, Color (cross-platform, no GDI+)

What's coming in 1.0.0:
- 🔄 Stable, production-ready API
- 🔄 Complete documentation and tutorials
- 🔄 Full platform testing (Windows, Linux, macOS)
- 🔄 Comprehensive sample games
- 🔄 Migration guides from beta

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
```sh
cd samples/FeatureDemos
dotnet run
```

**Performance hotkeys (in any demo scene):**
- `F3` - Toggle performance overlay
- `F4` - Toggle system profiling
- `F5` - Toggle frame time graph

---

## Architecture

Brine2D follows a modular architecture with clear separation of concerns:

### Core Package (ECS-First)
- **Brine2D** - Complete game engine (batteries included)
  - Core abstractions (GameTime, extensions)
  - ECS framework (Entity, Component, systems)
  - Engine (Scene, GameLoop, transitions)
  - Hosting (GameApplication, lifecycle)
  - Events (EventBus)
  - Input abstractions (IInputService)
  - Rendering abstractions (IRenderer, ITexture, ICamera)
  - Audio abstractions (IAudioService)
  - Built-in systems (Physics, AI, Audio, Input, Rendering)
  - Built-in components (Transform, Velocity, Timer, Lifetime, Tween)
  - Animation (SpriteAnimator, AnimationClip)
  - Collision (BoxCollider, CircleCollider, CollisionSystem)
  - Performance (PerformanceMonitor, ScopedProfiler, PerformanceOverlay)
  - Pooling (ArrayPool integration)

### Platform Implementation
- **Brine2D.SDL** - SDL3 platform layer
  - SDL3 GPU renderer (Vulkan/D3D12/Metal)
  - SDL3 Legacy renderer (SDL_Renderer compatibility)
  - SDL3 input implementation
  - SDL3_mixer audio implementation
  - Texture atlas builder
  - Post-processing effects (blur, bloom, grayscale)

### Optional Features
- **Brine2D.Tilemap** - Tiled (.tmj) tilemap support
- **Brine2D.UI** - UI framework (buttons, inputs, dialogs, tabs, scroll views)

### Package Count: 4 Core Packages

| Package | Purpose | Required? |
|---------|---------|-----------|
| `Brine2D` | Complete game engine | ✅ Yes |
| `Brine2D.SDL` | Platform implementation | ✅ Yes |
| `Brine2D.Tilemap` | Tilemap support | Optional |
| `Brine2D.UI` | UI framework | Optional |

**Down from 12+ packages in previous versions!**

---

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

**0.9.0-beta** (Current Release)
- ✅ **ECS-first architecture** - Systems integrated into core package
- ✅ **Package consolidation** - 4 core packages (down from 12+)
- ✅ **Namespace reorganization** - Clean architecture with proper separation
- ✅ **Multi-threaded ECS systems** - Parallel execution with job scheduling
- ✅ **Post-processing effects** - Blur, bloom, grayscale, custom shaders
- ✅ **System.Drawing.Primitives integration** - Cross-platform Rectangle, Color
- ✅ **Performance improvements** - Better batching, profiling, pooling
- ✅ **Advanced query system** - Spatial queries, caching
- ✅ **System profiling** - Per-system timing and performance metrics

**1.0.0** (Next Major Release)
- Stable, production-ready API
- Complete documentation and tutorials
- Full platform testing (Windows, Linux, macOS)
- Comprehensive sample games
- Migration guides from beta
- Performance optimizations and polish

See the full [roadmap](https://github.com/CrazyPickleStudios/Brine2D/milestones).

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## 🧪 Testing

Brine2D follows ASP.NET testing best practices with comprehensive unit and integration tests.

### Quick Start

```sh
# Run all unit tests
dotnet test tests/Brine2D.Tests

# Run with coverage
dotnet test tests/Brine2D.Tests --collect:"XPlat Code Coverage"

# Run integration tests (requires graphics context)
dotnet test tests/Brine2D.Integration.Tests
```

**Note:** Integration tests require a graphics context and are currently skipped in CI. They work fine locally on Windows with SDL3.

### Test Organization

```
tests/
├── Brine2D.Tests/              # Unit tests
│   ├── ECS/                    # Entity-Component-System tests
│   ├── Engine/                 # Scene management tests
│   ├── Systems/                # Collision, particles tests
│   ├── Animation/              # Tween system tests
│   └── Rendering/              # Camera, rendering tests
└── Brine2D.Integration.Tests/  # Integration tests
    └── Rendering/              # Full rendering pipeline tests
```

### Coverage Goals

We're actively building test coverage for core systems:

| System | Coverage | Status | Priority |
|--------|----------|--------|----------|
| **ECS Core** | 90% | ✅ | Core |
| **Collision** | 90% | ✅ | Core |
| **Scene Management** | 85% | ✅ | Core |
| **Particle System** | 85% | ✅ | Core |
| **Tween Animation** | 85% | ✅ | Core |
| **Camera** | 90% | ✅ | Core |
| **Entity World/Queries** | 80% | ✅ | Core |
| **Math Helpers** | 60% | 🟡 | Core |
| **Transform Hierarchy** | 30% | 🟡 | Important |
| **Rendering** | 20% | ❌ | Important |
| **Object Pooling** | 50% | 🟡 | Performance |
| **Input System** | 0% | ❌ | Next |
| **Audio System** | 0% | ❌ | Next |
| **UI Framework** | 0% | ❌ | Next |
| **Tilemap** | 0% | ❌ | Optional |

**Current overall coverage: ~40%**

**Target for 1.0.0: >80% for all core systems**

### Testing Philosophy

1. **Test behavior, not implementation** - Tests verify public APIs
2. **ASP.NET patterns** - Use `GetRequired*`, `Try*`, fluent APIs
3. **Edge cases matter** - Zero values, null checks, boundary conditions
4. **Integration tests** - Verify full system interactions

### Contributing Tests

When adding features:
1. Write tests first (TDD encouraged)
2. Follow existing test patterns (see `EntityTests.cs`)
3. Use FluentAssertions for readable assertions
4. Add edge case tests

Example test:
```csharp
[Fact]
public void ShouldAddComponentWhenEntityIsValid()
{
    // Arrange
    var world = new EntityWorld();
    var entity = world.CreateEntity();

    // Act
    var component = entity.AddComponent<TransformComponent>();

    // Assert
    component.Should().NotBeNull();
    entity.HasComponent<TransformComponent>().Should().BeTrue();
}
```

## License

MIT License - see LICENSE file for details

## Credits

Built with:
- [SDL3](https://github.com/libsdl-org/SDL) - Simple DirectMedia Layer
- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS) - C# bindings for SDL3

---

Made with ❤️ by CrazyPickle Studios
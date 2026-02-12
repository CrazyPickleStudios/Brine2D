# Brine2D

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Build Status](https://github.com/CrazyPickleStudios/Brine2D/workflows/CI/badge.svg)](https://github.com/CrazyPickleStudios/Brine2D/actions)
[![codecov](https://codecov.io/github/CrazyPickleStudios/Brine2D/graph/badge.svg?token=RIDC7GF0J4)](https://codecov.io/github/CrazyPickleStudios/Brine2D)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**The ASP.NET of game engines** - A modern .NET 10 game engine built on SDL3 for creating 2D games with C#.

Brine2D brings the familiar patterns and developer experience of ASP.NET to game development. If you've built web apps with ASP.NET Core, you'll feel right at home.

## Why Brine2D?

### ASP.NET-Style Application Builder

```csharp
var builder = GameApplication.CreateBuilder(args);

// Configure Brine2D with SDL backend
builder.Services
    .AddBrine2D(options =>
    {
        options.Window.Title = "My Game";
        options.Window.Width = 1280;
        options.Window.Height = 720;
    })
    .UseGPURenderer(gpu => gpu
        .WithVSync(true)
        .WithTargetFPS(60))
    .UseSDL();

// Register scenes using fluent builder
builder.AddScenes(scenes => scenes
    .Add<MainMenuScene>()
    .Add<GameScene>());

var app = builder.Build();
await app.RunAsync<MainMenuScene>();
```

### Clean Scene Architecture

Scenes get framework properties automatically - no injection needed for common services:

```csharp
public class GameScene : Scene
{
    private ITexture? _playerTexture;
    
    // No constructor needed for basic scenes!
    // Input, Audio, Renderer, Logger, World are all available as properties
    
    protected override async Task OnLoadAsync(CancellationToken ct)
    {
        // Load assets only - async I/O
        _playerTexture = await LoadTextureAsync("player.png", ct);
        Logger.LogInformation("Assets loaded");
    }
    
    protected override Task OnEnterAsync(CancellationToken ct)
    {
        // Initialize scene logic - spawn entities, start music, etc.
        Renderer.ClearColor = new Color(52, 78, 65, 255);
        
        var player = World.CreateEntity("Player");
        player.AddComponent(new TransformComponent 
        { 
            Position = new Vector2(400, 300) 
        });
        player.AddComponent(new SpriteComponent 
        { 
            Texture = _playerTexture 
        });
        
        return Task.CompletedTask;
    }
    
    protected override void OnUpdate(GameTime gameTime)
    {
        // Game logic - Input, Audio, Game are available as properties
        if (Input.IsKeyPressed(Key.Escape))
        {
            Environment.Exit(0);
        }
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        // Rich text with BBCode markup
        Renderer.DrawText(
            "Score: [color=#FFD700][b]9999[/b][/color]",
            10, 10,
            new TextRenderOptions 
            { 
                ParseMarkup = true,
                Color = Color.White 
            });
    }
}
```

**Framework properties (automatically available):**
- `Renderer` - Drawing and render state
- `Input` - Keyboard, mouse, gamepad input
- `Audio` - Music and sound effects
- `Logger` - Scoped logger for your scene
- `World` - Scene-scoped entity world (auto-cleanup!)
- `Game` - Game context and state

**Only inject custom services:**
```csharp
public class GameScene : Scene
{
    private readonly IPlayerService _playerService;
    
    // Constructor injection for YOUR services only
    public GameScene(IPlayerService playerService)
    {
        _playerService = playerService;
    }
}
```

### ASP.NET Patterns You Know

| ASP.NET Core | Brine2D |
|--------------|---------|
| `WebApplication.CreateBuilder()` | `GameApplication.CreateBuilder()` |
| `builder.Services.Add...()` | `builder.Services.AddBrine2D().UseSDL()` |
| Controllers with base properties | Scenes with framework properties |
| `appsettings.json` | `gamesettings.json` |
| `ILogger<T>`, `IOptions<T>` | `ILogger<T>`, `IOptions<T>` |
| Request-scoped DbContext | Scene-scoped `EntityWorld` |
| Middleware pipeline | ECS system pipeline |

## Features

### 🎮 Core Engine
- **Entity Component System (ECS)** - High-performance with scene-scoped worlds
- **Scene Management** - Async loading, transitions, and loading screens
- **Advanced Queries** - Fluent API with spatial indexing
- **Object Pooling** - Zero-allocation systems with `ArrayPool<T>`
- **Event System** - Type-safe `EventBus` for pub/sub

### 🎨 Rendering & Graphics
- **SDL3 GPU Backend** - Vulkan, Direct3D 12, Metal via SDL_GPU
- **Legacy Renderer** - SDL_Renderer fallback for compatibility
- **Rich Text Rendering** - BBCode markup (`[color=#FF0000]text[/color]`)
- **Post-Processing** - Bloom, blur, grayscale + custom shaders (GPU only)
- **Render Targets** - Off-screen rendering (GPU only)
- **Scissor Rectangles** - UI clipping and scroll views
- **Sprite Batching** - Automatic batching with frustum culling
- **Camera System** - Follow, zoom, shake effects

### 🔊 Audio & Input
- **Spatial Audio** - 2D positional audio with SDL3_mixer
- **Input System** - Keyboard, mouse, gamepad with action mapping
- **Audio Streaming** - Music and sound effects

### 💥 Gameplay Systems
- **Collision Detection** - AABB and circle colliders
- **Particle System** - GPU-accelerated with pooling
- **Sprite Animation** - Frame-based animation system
- **Tilemap Support** - Tiled (.tmj) integration
- **UI Framework** - Complete component library

### 🛠️ Developer Experience
- **Dependency Injection** - Full ASP.NET Core-style container
- **Structured Logging** - `Microsoft.Extensions.Logging` integration
- **Configuration System** - JSON with environment overrides
- **Hot Reload** - Asset reloading during development

## Quick Start

### Installation

```bash
# Create new console app
dotnet new console -n MyGame
cd MyGame

# Add Brine2D packages
dotnet add package Brine2D
dotnet add package Brine2D.SDL
```

### Your First Game

**Program.cs:**
```csharp
using Brine2D.Hosting;

var builder = GameApplication.CreateBuilder(args);

builder.Services
    .AddBrine2D(options =>
    {
        options.Window.Title = "My First Game";
        options.Window.Width = 1280;
        options.Window.Height = 720;
    })
    .UseGPURenderer()
    .UseSDL();

builder.AddScenes(scenes => scenes
    .Add<GameScene>());

var app = builder.Build();
await app.RunAsync<GameScene>();
```

**GameScene.cs:**
```csharp
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;

public class GameScene : Scene
{
    // No constructor needed - framework properties available automatically!
    
    protected override Task OnLoadAsync(CancellationToken ct)
    {
        Logger.LogInformation("Game scene loading...");
        Renderer.ClearColor = Color.CornflowerBlue;
        return Task.CompletedTask;
    }
    
    protected override void OnUpdate(GameTime gameTime)
    {
        if (Input.IsKeyPressed(Key.Escape))
        {
            Environment.Exit(0);
        }
    }
    
    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("Hello Brine2D!", 10, 10, Color.White);
        Renderer.DrawCircleFilled(640, 360, 50, Color.Tomato);
    }
}
```

**Run it:**
```bash
dotnet run
```

## Core Concepts

### Scene Lifecycle

Scenes follow this lifecycle:

```csharp
public class MyScene : Scene
{
    // 1. OnLoadAsync - Load assets (textures, audio, etc.)
    protected override async Task OnLoadAsync(CancellationToken ct)
    {
        // Async I/O only - load resources
        _texture = await LoadTextureAsync("player.png", ct);
        _music = await LoadMusicAsync("theme.ogg", ct);
    }
    
    // 2. OnEnterAsync - Initialize scene logic
    protected override Task OnEnterAsync(CancellationToken ct)
    {
        // Start music
        Audio.PlayMusic(_music);
        
        // Spawn entities
        var player = World.CreateEntity("Player");
        player.AddComponent<TransformComponent>();
        
        return Task.CompletedTask;
    }
    
    // 3. OnUpdate - Called every frame
    protected override void OnUpdate(GameTime gameTime)
    {
        // Game logic
    }
    
    // 4. OnRender - Called every frame
    protected override void OnRender(GameTime gameTime)
    {
        // Drawing
    }
    
    // 5. OnExitAsync - Cleanup before unload
    protected override Task OnExitAsync(CancellationToken ct)
    {
        // Stop music, save state, etc.
        Audio.StopMusic();
        return Task.CompletedTask;
    }
    
    // 6. OnUnloadAsync - Unload assets
    protected override Task OnUnloadAsync(CancellationToken ct)
    {
        // Dispose resources if needed
        return Task.CompletedTask;
    }
}
```

**Key distinction:**
- `OnLoadAsync` - **Assets only** (I/O operations)
- `OnEnterAsync` - **Scene logic** (entities, audio, initialization)

### Scene Transitions

Load scenes with smooth transitions:

```csharp
// Simple fade
await SceneManager.LoadSceneAsync<GameScene>(
    new FadeTransition(duration: 0.5f, color: Color.Black)
);

// With loading screen
await SceneManager.LoadSceneAsync<GameScene, MyLoadingScreen>(
    new FadeTransition(duration: 1f, color: Color.Black)
);
```

**Custom transitions:**
```csharp
public class SlideTransition : ISceneTransition
{
    public float Duration { get; }
    public bool IsComplete { get; private set; }
    public float Progress { get; private set; }
    
    public void Begin() { }
    public void Update(GameTime gameTime) { }
    public void Render(IRenderer? renderer) { }
}
```

### Entity Component System

Each scene has its own isolated `EntityWorld`:

```csharp
protected override Task OnEnterAsync(CancellationToken ct)
{
    // Create entity
    var player = World.CreateEntity("Player");
    
    // Add components
    player.AddComponent(new TransformComponent 
    { 
        Position = new Vector2(400, 300),
        Rotation = 0f,
        Scale = Vector2.One
    });
    
    player.AddComponent(new SpriteComponent 
    { 
        Texture = _playerTexture
    });
    
    // Query entities
    var enemies = World.Query()
        .WithAll<TransformComponent, EnemyComponent>()
        .WithNone<DeadComponent>()
        .Execute();
    
    Logger.LogInformation("Found {Count} enemies", enemies.Count);
    
    return Task.CompletedTask;
}
```

**When the scene unloads, all entities are automatically destroyed!**

### Rich Text Rendering

Brine2D supports BBCode-style markup:

```csharp
// Simple colored text
Renderer.DrawText(
    "Health: [color=#00FF00]100[/color]",
    10, 10,
    new TextRenderOptions { ParseMarkup = true });

// Multiple styles
Renderer.DrawText(
    "[b]Boss Fight![/b]\n[color=#FF0000][size=32]Dragon[/size][/color]",
    100, 100,
    new TextRenderOptions 
    { 
        ParseMarkup = true,
        Color = Color.White,
        MaxWidth = 300,
        HorizontalAlign = TextAlignment.Center,
        ShadowOffset = new Vector2(2, 2),
        ShadowColor = new Color(0, 0, 0, 128)
    });
```

**Supported BBCode tags:**
- `[color=#RRGGBB]` or `[color=#RRGGBBAA]` - Text color
- `[size=24]` - Font size in points
- `[b]` - Bold style flag
- `[i]` - Italic style flag
- `[u]` - Underline
- `[s]` - Strikethrough

**Extensible parsers:**
```csharp
// Implement custom markup
public class MarkdownParser : IMarkupParser
{
    public string FormatName => "Markdown";
    
    public IReadOnlyList<TextRun> Parse(string markup, TextRenderOptions options)
    {
        // Parse **bold**, *italic*, etc.
        // Return collection of TextRun
    }
}

// Use custom parser
Renderer.DrawText(
    "Hello **world**!",
    10, 10,
    new TextRenderOptions 
    { 
        ParseMarkup = true,
        MarkupParser = new MarkdownParser()
    });
```

### Advanced Rendering

**Post-Processing (GPU only):**
```csharp
builder.Services
    .AddBrine2D(options =>
    {
        options.Window.Title = "My Game";
    })
    .UseGPURenderer(gpu => gpu
        .WithPostProcessing(effects =>
        {
            effects.Add("Bloom");
            effects.Add("Grayscale");
            effects.Add("Blur");
        }))
    .UseSDL();
```

**Render Targets (GPU only):**
```csharp
// Create off-screen texture
using var minimap = Renderer.CreateRenderTarget(256, 256);

// Render to it
Renderer.PushRenderTarget(minimap);
RenderMinimapContent();
Renderer.PopRenderTarget();

// Draw the texture
Renderer.DrawTexture(minimap.Texture, 10, 10);
```

**Scissor Rectangles:**
```csharp
// Clip rendering to region (UI scroll views, etc.)
Renderer.PushScissorRect(new Rectangle(10, 10, 300, 200));
DrawScrollableContent();
Renderer.PopScissorRect();
```

### Configuration

**gamesettings.json:**
```json
{
  "Window": {
    "Title": "My Game",
    "Width": 1280,
    "Height": 720,
    "Fullscreen": false,
    "Resizable": true
  },
  "Rendering": {
    "Backend": "GPU",
    "VSync": true,
    "TargetFPS": 60,
    "PreferredGPUDriver": null
  },
  "PostProcessing": {
    "Enabled": true,
    "Effects": ["Bloom", "Grayscale"]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Brine2D": "Debug"
    }
  }
}
```

**Environment overrides:**
```json
// gamesettings.Development.json
{
  "Window": {
    "Width": 800,
    "Height": 600
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Architecture

### Package Structure

| Package | Purpose | Required? |
|---------|---------|-----------|
| `Brine2D` | Core engine (ECS, scenes, rendering APIs) | ✅ Yes |
| `Brine2D.SDL` | SDL3 platform implementation | ✅ Yes |
| `Brine2D.Tilemap` | Tiled tilemap support | Optional |
| `Brine2D.UI` | UI component library | Optional |

### Design Principles

**1. Scene-Scoped Resources**
- Each scene gets its own `IEntityWorld` instance
- Automatic cleanup when scene unloads
- No entity leaks between scenes
- Persistent state via services, not entities

**2. Framework Properties Pattern**
- Common services available as properties (`Input`, `Audio`, `Renderer`)
- No constructor bloat for simple scenes
- Constructor injection still available for custom services
- Matches ASP.NET's `ControllerBase` pattern

**3. Lifecycle Separation**
- `OnLoadAsync` for async I/O (loading assets)
- `OnEnterAsync` for scene logic (spawning entities, starting audio)
- Clear separation of concerns

**4. Convention Over Configuration**
- Sensible defaults ("batteries included")
- Minimal boilerplate for common scenarios
- Power users can customize everything

**5. Performance First**
- Object pooling throughout hot paths
- Automatic sprite batching
- Spatial indexing for queries
- GPU acceleration where available

## Performance

### Built-in Diagnostics

Press **F3** in any scene for the FPS overlay:

```
FPS: 60 (16.67ms)
Draw Calls: 12
Entities: 1,247
Systems: 8
Memory: 45.2 MB
```

**Additional hotkeys:**
- **F3** - Toggle FPS overlay
- **F4** - System profiling (per-system timings)
- **F5** - Frame time graph

### Benchmarks

The `SpriteBenchmarkScene` renders **10,000+ sprites at 60 FPS** with batching enabled.

**Optimization tips:**
- Use sprite batching for many sprites
- Enable frustum culling
- Pool frequently created/destroyed objects
- Minimize texture swaps
- Use spatial queries for collision detection

## Samples

### Getting Started

Progressive tutorials for beginners:

```bash
cd samples/GettingStarted/01-HelloBrine
dotnet run
```

**Included tutorials:**
1. **01-HelloBrine** - Minimal setup and window
2. **02-SceneBasics** - Lifecycle and transitions
3. **03-DependencyInjection** - Services and configuration
4. **04-InputAndText** - Input handling and rendering

### Feature Demos

Interactive showcase of all engine features:

```bash
cd samples/FeatureDemos
dotnet run
```

**Demo scenes:**
- **Query System** - Entity queries and spatial indexing
- **Particles** - GPU-accelerated particle effects
- **Texture Atlas** - Runtime sprite packing
- **Collision** - AABB and circle colliders
- **Spatial Audio** - 2D positional audio
- **Post-Processing** - Real-time shader effects
- **Scissor Rects** - UI clipping
- **Rich Text** - BBCode markup rendering
- **Sprite Benchmark** - 10,000+ sprite stress test

## Documentation

**Coming soon:** [brine2d.com](https://www.brine2d.com)

Topics:
- Complete API reference
- Architecture guides
- Best practices
- Performance optimization
- Shader authoring
- Custom systems

## Current Status

**⚠️ Version 0.9.x-beta**

This is a **beta release** with all core features working:

✅ **Working:**
- ECS with scene-scoped worlds
- SDL3 GPU and legacy renderers
- Rich text with BBCode markup
- Post-processing pipeline (GPU only)
- Render targets and scissor rects
- Scene management with transitions
- Spatial audio
- Collision detection
- Particle system
- UI framework

⚠️ **Known Limitations:**
- Documentation incomplete
- Linux/macOS untested (should work via SDL3)
- API may change before 1.0
- Test coverage ~20% (target: 80%+)

**Coming in 1.0.0:**
- [ ] Stable API guarantee
- [ ] Complete documentation site
- [ ] Full platform testing (Windows/Linux/macOS)
- [ ] 80%+ test coverage
- [ ] Performance benchmarks
- [ ] Migration guides from beta

## Requirements

- **.NET 10 SDK** or later
- **SDL3** (included via SDL3-CS NuGet)
- **SDL3_image** (texture loading)
- **SDL3_mixer** (audio playback)

## Platform Support

| Platform | GPU Backend | Legacy Backend | Status |
|----------|-------------|----------------|--------|
| Windows | Vulkan/D3D12 | ✅ SDL_Renderer | ✅ Fully tested |
| Linux | Vulkan | ✅ SDL_Renderer | ⚠️ Untested |
| macOS | Metal | ✅ SDL_Renderer | ⚠️ Untested |

SDL3 provides cross-platform support. **Community testing needed for Linux/macOS!**

## Community

- **Discussions:** [GitHub Discussions](https://github.com/CrazyPickleStudios/Brine2D/discussions)
- **Issues:** [Issue Tracker](https://github.com/CrazyPickleStudios/Brine2D/issues)
- **Samples:** [Sample Projects](https://github.com/CrazyPickleStudios/Brine2D/tree/main/samples)

## Roadmap

See the full [roadmap and milestones](https://github.com/CrazyPickleStudios/Brine2D/milestones).

**1.0.0 Goals:**
- Stable, production-ready API
- Complete documentation
- Multi-platform testing
- High test coverage
- Performance baselines

## Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md).

**Ways to help:**
- Test on Linux/macOS
- Report bugs
- Submit feature requests
- Write documentation
- Create tutorials
- Build sample projects

## Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific test project
dotnet test tests/Brine2D.Tests
```

**Current coverage:** ~20% | **Target for 1.0:** 80%+

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

**Built with:**
- [SDL3](https://github.com/libsdl-org/SDL) - Cross-platform multimedia
- [SDL3-CS](https://github.com/edwardgushchin/SDL3-CS) - C# bindings for SDL3
- [stb_image](https://github.com/nothings/stb) - Image loading

**Inspired by:**
- **ASP.NET Core** - Hosting model and DI patterns
- **Unity** - Component-based architecture
- **Godot** - Scene system design
- **MonoGame** - .NET game development philosophy

---

**Made with ❤️ by CrazyPickle Studios**

*The ASP.NET of game engines - familiar patterns, modern .NET, built for C# developers.*
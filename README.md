# Brine2D

Brine2D is a modern, minimal 2D game engine for .NET. It aims to feel familiar and productive: simple APIs, quick startup, and a clean separation between core engine concepts and platform backends.

Status: Early preview.

## What is Brine2D?
Brine2D is a lightweight library you add to your .NET app to build 2D games. It focuses on:
- Fast iteration with a small, approachable API surface.
- Clear separation of responsibilities (game logic vs. platform/rendering).
- Practical defaults that work out of the box.

## Highlights
- SDL3-backed rendering and windowing.
- A straightforward game loop with optional fixed-step updates.
- Simple input and drawing primitives you can extend.
- Works with standard .NET tooling and projects.
- Centralized exception logging and robust scene transitions.
- Unified asset loading for textures, sounds, and music.

## What’s new
- Centralized exception logging:
  - Scene initialization errors are logged by the `SceneManager` (non-blocking, promotion on completion).
  - High-level guards in the SDL game loop log unhandled exceptions in update/render.
- Non-blocking scene initialization:
  - Queue scene changes, show a loading scene, initialize asynchronously, and promote once complete.
  - Deterministic and robust scene transitions.
- Standardized asset loading:
  - `ContentManager` resolves loaders via `IAssetLoader<T>` for textures, sounds, and music.
  - SDL loaders implement `IAssetLoader<T>` for consistency.
- Audio improvements (SDL3 Mixer):
  - Track pooling to reduce per-play allocations, with on-demand expansion.
  - Per-sound stopping using tracked mixer tracks.
  - Master volume control and simple music play/pause/stop.
- Desktop host builder simplification:
  - `CreateWithGame` refactored to reuse the internal host setup.

## Quick Start
1) Clone the repo and open the solution.
2) Run the sample project `samples/EmptyApp`.
3) Edit `DemoGame` (or your scene) to add update and render code.

A minimal game:


```csharp
using System.Drawing;
using Brine2D.Engine;

internal sealed class MyGame : IGame
{
    private int _x;

    public async Task Initialize(IGameContext context)
    {
        context.Window.Show();
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(Color.FromArgb(255, 30, 30, 30));
        ctx.DrawRect(new Rectangle(_x, 100, 40, 40), Color.FromArgb(255, 0, 200, 255));
        ctx.Present();
    }

    public void Update(GameTime time)
    {
        _x += (int)(60 * time.DeltaSeconds);

        if (_x > 760)
        {
            _x = 0;
        }
    }
}
```

## Modern hosting (how it fits)
Brine2D uses the standard .NET hosting model so your game runs like any modern app:
- You build a host, register the engine and SDL backend, and provide your `IGame` implementation.
- The host manages lifetime, configuration, and logging; a hosted service starts the loop.
- This keeps setup simple, enables clean shutdown, and makes the engine easy to extend.

Example host setup with loading and initial scenes:

```csharp
using Brine2D.Desktop;
using EmptyApp;
using Microsoft.Extensions.Hosting;

var host = DesktopHostBuilder.CreateWithScene<LoadingScene, GameplayScene>(w =>
{
    w.Title = "Brine2D Sample";
    w.Width = 800;
    w.Height = 600;
}, loop =>
{
    loop.UseFixedStep = true;
    loop.FixedStepSeconds = 1.0 / 60.0;
}).Build();

host.Run();
```

## Assets and audio
Load assets via `IContentManager` with `IAssetLoader<T>`:

```csharp
var content = context.Services.GetRequiredService<IContentManager>();
var texture = await content.LoadAsync<ITexture>("Assets/player.png", cancellationToken);
var music = await content.LoadAsync<IMusic>("Assets/music.mp3", cancellationToken);
```

Play audio with per-sound control:

```csharp
var audio = context.Services.GetRequiredService<IAudio>();

audio.PlayMusic(music); 

// later audio.StopAll();
// or audio.Stop(sound);
```

## Tech stack
- SDL3 via SDL3-CS for windowing, input, audio, and rendering.
- .NET (C#) with a small, composable API surface.
- Microsoft.Extensions for a modern app experience:
  - Hosting: run the game as a hosted service (clean startup/shutdown).
  - Dependency Injection: compose engine and platform services.
  - Options: configure window and loop settings.
  - Logging: structured logs (Console provider by default).

## Why choose Brine2D?
- Minimal: start drawing and moving things in minutes.
- Familiar: plain C# with simple interfaces; no heavy editor required.
- Flexible: add systems (sprites, scenes, physics) as you need them.

## How it’s different
Brine2D stays small and focused. It does not try to be an all-in-one engine with complex editors or mandatory patterns. You control how features are composed, and you can swap or extend platform backends without changing your game code.

## Roadmap (in progress)
- Rendering: textures, sprites, batching.
- Input: mouse, controller, text.
- Assets: async loading and hot-reload hooks.
- Scenes/ECS: lightweight scene management.

## Try it
- Run `samples/EmptyApp` and start hacking.
- Replace `DemoGame` with your game class.

## Contributing
We welcome contributions. See CONTRIBUTING.md for guidelines.

## License
MIT License. See LICENSE file for details.
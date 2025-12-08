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

## Quick Start
1) Clone the repo and open the solution.
2) Run the sample project `samples/EmptyApp`.
3) Edit `DemoGame` to add your own update and render code.

A minimal game:

```csharp
using System.Drawing;
using Brine2D.Abstractions;
using Brine2D.Desktop;
using Brine2D.Engine;
using Microsoft.Extensions.Hosting;

internal sealed class MyGame : IGame
{
    private int _x;

    public void Initialize(IGameContext context)
    {
        context.Window.Show();
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(new(30, 30, 30, 255));
        ctx.DrawRect(new Rectangle(_x, 100, 40, 40), new(0, 200, 255, 255));
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

## Tech stack
- SDL3 via SDL3-CS for windowing, input, and rendering.
- .NET (C#) with a small, composable API surface.
- Microsoft.Extensions for a modern app experience:
  - Hosting: run the game as a hosted service (clean startup/shutdown).
  - Dependency Injection: compose engine and platform services.
  - Options: configure window and loop settings.
  - Logging: structured logs (Console provider by default).

## Modern hosting (how it fits)
Brine2D uses the standard .NET hosting model so your game runs like any modern app:
- You build a host, register the engine and SDL backend, and provide your `IGame` implementation.
- The host manages lifetime, configuration, and logging; a hosted service starts the loop.
- This keeps setup simple, enables clean shutdown, and makes the engine easy to extend.

Example host setup (simplified):

```csharp
using Brine2D.Desktop;
using Microsoft.Extensions.Hosting;

var host = DesktopHostBuilder.CreateDefault<MyGame>(opts =>
{
    opts.Title = "My Game";
    opts.Width = 1280;
    opts.Height = 720;
}, loop =>
{
    loop.UseFixedStep = true;
    loop.FixedStepSeconds = 1.0 / 60.0;
}).Build();

host.Run();
```

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
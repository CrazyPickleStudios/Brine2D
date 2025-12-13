# Runtime Overview

Brine2D’s runtime is built on a modern .NET hosting model with an async-first design. Apps configure a host, initialize scenes asynchronously, and run a game loop that you can tune for your needs.

## Host & Configuration

Use `DesktopHostBuilder` to configure window options, register services, and set up the game loop. The host manages lifetime and integrates logging, DI, and configuration.

~~~csharp
using Brine2D.Desktop;
using Microsoft.Extensions.Hosting;

var host = DesktopHostBuilder.CreateWithScene<LoadingScene, GameplayScene>(
    opts =>
    {
        opts.Title = "Brine2D";
        opts.Width = 1280;
        opts.Height = 720;
        opts.VSync = true;
    },
    loop =>
    {
        loop.UseFixedStep = true;
        loop.FixedStepSeconds = 1.0 / 60.0; // deterministic 60 Hz update
        loop.MaxFps = null;                 // uncapped render (VSync governs display)
    }
).Build();

await host.RunAsync();
~~~

Single-scene startup is also supported:

~~~csharp
var host = DesktopHostBuilder.CreateWithScene<GameplayScene>(
    opts => { /* window options */ },
    loop => { /* loop options */ }
).Build();

await host.RunAsync();
~~~

## Async Scene Initialization

Scenes perform asynchronous setup in `InitializeAsync(IGameContext, CancellationToken)` before the loop starts:
- Load content (textures, audio, tilemaps) with `LoadAsync(...)`
- Resolve services via `IGameContext.Services`
- Prepare scene state

The engine awaits `InitializeAsync` to ensure the scene is ready before entering the update/draw cycle.

~~~csharp
public sealed class GameplayScene : IScene
{
    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        var content = context.Services.GetRequiredService<IContentManager>();
        var map = await content.LoadAsync<Tilemap>("Assets/untitled.tmj", ct);
        // set up state using loaded assets
    }
}
~~~

## Game Loop

The loop runs updates and draws with tunable timing:
- Fixed-step update: consistent delta (e.g., 1/60 s) for deterministic logic/physics
- Render cadence: uncapped or capped via `MaxFps`; display sync via VSync
- Pausing or throttling: adjust loop settings or scene behavior

Loop configuration:

~~~csharp
loop =>
{
    loop.UseFixedStep = true;
    loop.FixedStepSeconds = 1.0 / 60.0;
    loop.MaxFps = null; // set to a number to cap rendering, e.g., 120
}
~~~

## Services & DI

Runtime uses .NET hosting patterns:
- Central service provider for engine and game services (content, audio, input, logging)
- Register and resolve through the host; access via `IGameContext.Services` from scenes
- Configuration and logging integrate with `Microsoft.Extensions.*`

## Diagnostics

Integrate logging and tooling to observe runtime behavior:
- Logging via `ILogger<T>` surfaces engine/backend messages (e.g., SDL audio warnings)
- Use Visual Studio’s __Debug > Windows > Diagnostics Tools__ to monitor performance
- For profiling guidance, see [Performance Guide](../performance/overview.md)

## Platform

Desktop runtime uses SDL3 for windowing, input, graphics, and audio on Windows/macOS/Linux. Check backend notes for platform-specific behavior.

## Next Steps
- Scene patterns and transitions: [Gameplay Overview](../gameplay/overview.md)
- Content pipeline: [Content](../content/overview.md)
- Input handling: [Input](../input/overview.md)
- Configuration details: [Engine Config](./configuration.md)
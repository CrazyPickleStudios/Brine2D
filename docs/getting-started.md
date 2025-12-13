# Getting Started

Spin up your first Brine2D desktop app using `DesktopHostBuilder`. This guide covers environment setup, creating a project, wiring the host, and where to go next.

## Prerequisites
- .NET 10 SDK installed
- Visual Studio 2026
- Windows, macOS, or Linux

For details and alternatives, see [Prerequisites](./prerequisites.md).

## Create a New Project
1. Open __File > New > Project__ in Visual Studio.
2. Choose a Console App template.
3. Set Target Framework to `.NET 10`.
4. Name your project and create it.

Alternatively via CLI:

```console
dotnet new console -n Brine2DGame -f net10.0
cd Brine2DGame
dotnet add package Brine2D.Desktop
```

## Minimal Bootstrapping (Desktop)
Use `DesktopHostBuilder` to configure the window, game loop, and starting scenes, then run the host.

```csharp
using Brine2D.Desktop;
using Microsoft.Extensions.Hosting;

namespace Brine2DGame;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = DesktopHostBuilder.CreateWithScene<LoadingScene, GameplayScene>(
            opts =>
            {
                opts.Title = "Brine2D Starter";
                opts.Width = 1280;
                opts.Height = 720;
                opts.VSync = true;
            },
            loop =>
            {
                loop.UseFixedStep = true;
                loop.FixedStepSeconds = 1.0 / 60.0; // 60 FPS fixed update
                loop.MaxFps = null; // uncapped render, vsync controls display
            }
        ).Build();

        await host.RunAsync();
    }
}
```

- `CreateWithScene<TLoading, TMain>` sets an initial loading scene and a main gameplay scene. Replace with your own scene types.
- Configure window options (`Title`, `Width`, `Height`, `VSync`) in the first lambda.
- Configure the game loop (`UseFixedStep`, `FixedStepSeconds`, `MaxFps`) in the second lambda.

## Scenes
Define your scenes to handle loading and gameplay. A loading scene is optional—start directly in your main gameplay scene if you don’t need preloading. The loading scene typically preloads assets and transitions to gameplay.

- Scene lifecycle and patterns: [Gameplay Overview](./gameplay/overview.md)
- Managing transitions: [Scenes](./gameplay/scenes.md)
- Asset pipelines: [Content](./content/overview.md)

## Run & Debug
- Press __Debug > Start Debugging__ or F5.
- Use breakpoints and the __Diagnostics Tools__ window.
- For runtime options and configuration, see [Configuration](./runtime/configuration.md).

## Next Steps
- Input handling: [Input Overview](./input/overview.md)
- Drawing sprites and animations: [Sprites](./graphics/sprites.md)
- Audio playback: [Audio](./audio/overview.md)
- Packaging & deployment: [Deployment](./deployment/overview.md)

## Troubleshooting
- Build failures: check __Build > Build Solution__ output and restore packages.
- Window doesn’t open: verify `Brine2D.Desktop` is installed and target framework is `.NET 10`.
- Stutter or timing issues: review loop settings or see [Performance Guide](./performance/overview.md).

---
Ready to build gameplay? Continue with [Scenes](./gameplay/scenes.md).
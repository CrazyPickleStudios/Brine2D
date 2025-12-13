# Gameplay Overview

Brine2D organizes gameplay around scenes. Each scene encapsulates state, input, updates, and rendering. Your app can start with a loading scene that transitions to gameplay, but a loading scene is optional—start directly in your main scene if you don’t need preloading.

## Scenes at a Glance
- Self-contained lifecycle: Load, Update, Draw, and Dispose/Unload.
- Own state and services (input, content, audio).
- Transition to other scenes to change modes (menu, gameplay, pause).

Typical startup with loading + gameplay:

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
        loop.FixedStepSeconds = 1.0 / 60.0;
        loop.MaxFps = null;
    }
).Build();

await host.RunAsync();
~~~

Single-scene start:

~~~csharp
var host = DesktopHostBuilder.CreateWithScene<GameplayScene>(
    opts => { /* window options */ },
    loop => { /* loop options */ }
).Build();

await host.RunAsync();
~~~

## Scene Lifecycle

A typical scene implements asynchronous asset/setup work in `InitializeAsync`, game logic in `Update`, and drawing in `Draw`. Note: `InitializeAsync` runs asynchronously and is awaited by the engine during scene startup.

~~~csharp
public sealed class GameplayScene : Scene
{
    private Player _player = default!;
    private SpriteBatch _sprites = default!;

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        _sprites = new SpriteBatch(GraphicsDevice);

        var content = context.Services.GetRequiredService<IContentManager>();
        var playerTexture = await content.LoadAsync<Texture2D>("sprites/player.png", ct);

        _player = new Player(playerTexture, startPosition: new Vector2(100, 100));
    }

    protected override void Update(GameTime time)
    {
        _player.Update(time, Input);

        if (Input.IsPressed(Keys.Escape))
        {
            // Example transition: open a pause menu
            SceneManager.Switch(new PauseScene());
        }
    }

    protected override void Draw(GameTime time)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _sprites.Begin();
        _player.Draw(_sprites);
        _sprites.End();
    }

    protected override void Unload()
    {
        _sprites.Dispose();
    }
}
~~~

## Entities & State

Keep scenes lean. Move behavior into small entities/components:
- Entities hold per-object state (position, velocity, animation).
- Scene coordinates update order and rendering.
- Prefer clear ownership: scene owns entities; entities don’t own the scene.

Learn patterns: [Scenes](./scenes.md)

## Input

Query input in `Update`. Use mappings and handle edge cases (focus loss, device disconnects).
- Overview: [Input](../input/overview.md)
- Devices: [Controller & Keyboard](../input/devices.md)

## Content & Assets

Load assets in `Load`, release them in `Unload`. Avoid blocking loads in `Update` to prevent hitches—use a loading scene or prefetch during transitions.
- Overview: [Content](../content/overview.md)

## Timing & Loop

Default setup uses a fixed-step update (e.g., 60 FPS) for deterministic logic. Rendering can be uncapped and synced via VSync.
- Runtime tuning: [Runtime Overview](../runtime/overview.md)
- Configuration: [Engine Config](../runtime/configuration.md)

## Transitions

Switch scenes atomically to change modes (menu → gameplay → pause). Ensure assets are ready before entering the next scene and cleanly dispose when exiting.
- Patterns and APIs: [Scenes](./scenes.md)

## Next Steps
- Draw sprites & animations: [Sprites](../graphics/sprites.md)
- Play audio: [Audio](../audio/overview.md)
- Performance & profiling: [Performance Guide](../performance/overview.md)
- Packaging & deployment: [Deployment](../deployment/overview.md)
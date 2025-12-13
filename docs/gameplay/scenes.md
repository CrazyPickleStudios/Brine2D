# Scenes

Scenes organize gameplay into focused modes (loading, menus, gameplay, pause). Each scene owns its state, handles input, updates logic, and draws. You can start directly in your main scene or use a loading scene to prepare assets asynchronously.

## Lifecycle

A typical scene:
- InitializeAsync: async setup (load assets, resolve services, initialize state)
- Update: game logic, input handling, scene transitions
- Draw: rendering
- Unload/Dispose: release resources

Example:
~~~csharp
using Brine2D.Content;
using Brine2D.Audio;

public sealed class GameplayScene : IScene
{
    private SpriteBatch _sprites = default!;
    private Texture2D _playerTex = default!;
    private ISound _jump = default!;
    private Vector2 _playerPos;
    private IAudio _audio = default!;
    private IContentManager _content = default!;

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        _audio = context.Services.GetRequiredService<IAudio>();
        _content = context.Services.GetRequiredService<IContentManager>();

        _sprites = new SpriteBatch(GraphicsDevice);
        _playerTex = await _content.LoadAsync<Texture2D>("Assets/Textures/player.png", ct);
        _jump = await _content.LoadAsync<ISound>("Assets/Audio/jump.wav", ct);

        _playerPos = new Vector2(100, 100);
    }

    protected override void Update(GameTime time)
    {
        // Input-driven movement
        var speed = 180f * (float)time.Elapsed.TotalSeconds;

        if (Input.IsDown(Keys.Left))  _playerPos.X -= speed;
        if (Input.IsDown(Keys.Right)) _playerPos.X += speed;
        if (Input.IsPressed(Keys.Space)) _audio.Play(_jump);

        // Transition example
        if (Input.IsPressed(Keys.Escape))
        {
            SceneManager.Switch(new PauseScene());
        }
    }

    protected override void Draw(GameTime time)
    {
        GraphicsDevice.Clear(Color.Black);

        _sprites.Begin();
        _sprites.Draw(_playerTex, _playerPos, Color.White);
        _sprites.End();
    }

    protected override void Unload()
    {
        _sprites.Dispose();
        _playerTex.Dispose();
        _jump.Dispose();
    }
}
~~~

## Starting Scenes

Configure scenes via the host:
- Two-scene start (loading → gameplay)
- Single-scene start (direct to gameplay)

~~~csharp
using Brine2D.Desktop;
using Microsoft.Extensions.Hosting;

// Loading + Gameplay
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

// Single-scene
var host2 = DesktopHostBuilder.CreateWithScene<GameplayScene>(
    opts => { /* window options */ },
    loop => { /* loop options */ }
).Build();

await host2.RunAsync();
~~~

## Transitions

Switch scenes to change modes, keeping transitions atomic and asset-safe:
- `Switch(new SceneType())`: replace the current scene
- `Push(new SceneType())` / `Pop()`: stack-based navigation (e.g., pause/menu) if supported
- Ensure the next scene’s assets and state are prepared before display

~~~csharp
// Replace current scene with main menu
SceneManager.Switch(new MainMenuScene());

// Pause overlay via stack (if available)
SceneManager.Push(new PauseScene());
// ...
SceneManager.Pop();
~~~

## Patterns

- Keep scenes cohesive; avoid global singletons.
- Preload frequently used assets in `InitializeAsync`.
- Avoid blocking I/O during `Update`/`Draw`.
- Use fixed-step updates for deterministic gameplay, render uncapped with VSync.

## Troubleshooting

- Black screen: confirm `InitializeAsync` completes successfully and drawing clears the backbuffer.
- Stutter on transitions: move asset loads to `InitializeAsync` or a loading scene.
- Input not responding: verify focus and device state; see Input Overview.

## Next Steps
- Gameplay overview: [Overview](./overview.md)
- Content pipeline: [Content](../content/overview.md)
- Runtime & loop: [Runtime Overview](../runtime/overview.md)
# Content Overview

Brine2D’s content system provides an async-first pipeline for loading assets like textures, tilemaps, audio, and custom data. Assets are resolved via `IContentManager` using loaders registered by the engine and platform backends.

## Key Concepts
- Content manager (`IContentManager`): central API to load and cache assets.
- Asset loaders (`IAssetLoader<T>`): type-specific loaders (e.g., textures, tilemaps, audio).
- Async loading: `LoadAsync<T>(path, ct)` and `LoadAsync<T>(Stream, ct)`; avoid blocking the game loop.

## Quick Start

Load a texture:
~~~csharp
using Brine2D.Content;

public sealed class GameplayScene : IScene
{
    private Texture2D _player = default!;
    private IContentManager _content = default!;

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        _content = context.Services.GetRequiredService<IContentManager>();
        _player = await _content.LoadAsync<Texture2D>("Assets/Textures/player.png", ct);
    }
}
~~~

Load audio (sound/music):
~~~csharp
private ISound _click = default!;
private IMusic _bgm = default!;

public async Task InitializeAsync(IGameContext context, CancellationToken ct)
{
    var content = context.Services.GetRequiredService<IContentManager>();

    _click = await content.LoadAsync<ISound>("Assets/Audio/ui_click.wav", ct);
    _bgm = await content.LoadAsync<IMusic>("Assets/Audio/theme.ogg", ct);
}
~~~

Load a tilemap:
~~~csharp
private Tilemap _map = default!;

public async Task InitializeAsync(IGameContext context, CancellationToken ct)
{
    var content = context.Services.GetRequiredService<IContentManager>();
    _map = await content.LoadAsync<Tilemap>("Assets/Maps/level1.tmj", ct);
}
~~~

## Paths & Organization

Organize assets by type and purpose, for clarity and tooling:
- `Assets/Textures/` for sprites, atlases, fonts, UI
- `Assets/Audio/` for SFX and BGM
- `Assets/Maps/` for Tiled maps and related data
- `Assets/Data/` for JSON and custom resources

Use consistent naming (lowercase, underscores) and keep paths stable to simplify references.

## Streams & Embedded Content

When assets come from archives or embedded resources, use the `Stream` overload:
~~~csharp
using var stream = File.OpenRead("path/to/packed.dat");
var texture = await _content.LoadAsync<Texture2D>(stream, ct);
~~~

## Caching & Lifetime

`IContentManager` may cache loaded assets to avoid redundant I/O. General guidance:
- Load frequently used assets once in `InitializeAsync`.
- Dispose assets when they’re no longer needed, or in scene unload.
- Avoid loading in `Update` unless necessary; prefer prefetch on transitions.

## Custom Loaders

Extend the pipeline by implementing `IAssetLoader<T>` for new asset types:
~~~csharp
public sealed class DialogueLoader : IAssetLoader<Dialogue>
{
    public async Task<Dialogue> LoadAsync(string path, CancellationToken ct = default)
    {
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(ct);
        return Dialogue.Parse(json);
    }

    public async Task<Dialogue> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(ct);
        return Dialogue.Parse(json);
    }
}
~~~

Register your loader with the host or content system, then load via `IContentManager.LoadAsync<Dialogue>(...)`.

## Best Practices
- Prefer async `LoadAsync` with a `CancellationToken` for responsiveness.
- Preload assets in a loading scene or during scene `InitializeAsync`.
- Validate paths and asset availability early; fail fast with clear errors.
- Keep asset sizes reasonable; compress large content (OGG/MP3 for music, PNG/WebP for textures as supported).

## Troubleshooting
- “Asset not found”: verify path, working directory, and build/package inclusion.
- Hitches: move loads out of `Update`/`Draw`; preload or stream incrementally.
- Format issues: confirm backend support (e.g., SDL for audio, graphics formats).

## Next Steps
- Gameplay patterns: [Gameplay Overview](../gameplay/overview.md)
- Audio usage: [Audio Overview](../audio/overview.md)
- Runtime loop & configuration: [Runtime Overview](../runtime/overview.md)
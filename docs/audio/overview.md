# Audio Overview

Brine2D provides a simple, async-first audio API for sound effects and music. Load audio with the content pipeline and play it via the engine’s `IAudio` service. Under the hood, the desktop backend uses SDL3 for mixing and playback.

## Concepts
- Sound effects (`ISound`): short clips (SFX, UI beeps).
- Music (`IMusic`): longer tracks (background music).
- Audio service (`IAudio`): play/stop/pause/resume, master volume.

Audio loading is asynchronous using the content manager:
- `LoadAsync<ISound>(...)`
- `LoadAsync<IMusic>(...)`

## Quick Start

Load and play a sound effect:
~~~csharp
using Brine2D.Audio;
using Brine2D.Content;

public sealed class GameplayScene : IScene
{
    private ISound _click = default!;
    private IAudio _audio = default!;
    private IContentManager _content = default!;

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        _audio = context.Services.GetRequiredService<IAudio>();
        _content = context.Services.GetRequiredService<IContentManager>();

        _click = await _content.LoadAsync<ISound>("Assets/Audio/ui_click.wav", ct);
    }

    protected override void Update(GameTime time)
    {
        if (Input.IsPressed(Keys.Enter))
        {
            _audio.Play(_click, volume: 0.8f, loop: false);
        }
    }
}
~~~

Play background music:
~~~csharp
private IMusic _bgm = default!;
private IAudio _audio = default!;
private IContentManager _content = default!;

public async Task InitializeAsync(IGameContext context, CancellationToken ct)
{
    _audio = context.Services.GetRequiredService<IAudio>();
    _content = context.Services.GetRequiredService<IContentManager>();

    _bgm = await _content.LoadAsync<IMusic>("Assets/Audio/theme.ogg", ct);

    // Start looping music at 60% volume
    _audio.PlayMusic(_bgm, volume: 0.6f, loop: true);
}
~~~

## Playback Control

Use `IAudio` to control playback:
- `Play(ISound sound, float volume = 1.0f, bool loop = false)`
- `PlayMusic(IMusic music, float volume = 1.0f, bool loop = true)`
- `PauseMusic()`, `ResumeMusic()`
- `Stop(ISound sound)`, `StopMusic()`, `StopAll()`
- `MasterVolume` (0.0–1.0) affects all playback

Example:
~~~csharp
_audio.MasterVolume = 0.75f; // reduce overall volume
_audio.PauseMusic();
// ...
_audio.ResumeMusic();
~~~

## Formats & Assets

SDL3 Mixer supports common formats like WAV, OGG, MP3 (platform support may vary).

Best practices:
- Use OGG/MP3 for music (smaller footprint), WAV for short SFX (fast decode).
- Keep filenames and paths consistent; prefer lowercase/underscored names.
- Preload frequently used sounds in `InitializeAsync` to avoid runtime hitches.

## Performance Notes

Brine2D pre-creates a pool of mixer tracks for low-latency playback. If you trigger many simultaneous sounds, the pool auto-expands. In tight loops:
- Avoid creating and disposing sounds repeatedly; cache them.
- Tune volumes via `MasterVolume` and per-play call rather than recalculating in hot paths.

## Troubleshooting

- No audio output: verify device permissions and format support; check logs for SDL errors.
- Playback fails: ensure asset path is correct and accessible.
- Stutter on play: preload assets in `InitializeAsync`; avoid loading during `Update`.

## Next Steps
- Gameplay patterns: [Gameplay Overview](../gameplay/overview.md)
- Content pipeline: [Content](../content/overview.md)
- Runtime & loop tuning: [Runtime Overview](../runtime/overview.md)
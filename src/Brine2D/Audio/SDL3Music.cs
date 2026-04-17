using Microsoft.Extensions.Logging;

namespace Brine2D.Audio;

/// <summary>
///     SDL3_mixer implementation of a music track for background audio playback.
/// </summary>
internal sealed class SDL3Music(string name, nint audio, ILogger<SDL3Music> logger)
    : SDL3AudioAsset(name, audio, logger), IMusic;
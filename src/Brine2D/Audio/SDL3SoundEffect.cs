using Microsoft.Extensions.Logging;

namespace Brine2D.Audio;

/// <summary>
///     SDL3_mixer implementation of a sound effect.
/// </summary>
internal sealed class SDL3SoundEffect(string name, nint audio, ILogger<SDL3SoundEffect> logger)
    : SDL3AudioAsset(name, audio, logger), ISoundEffect;
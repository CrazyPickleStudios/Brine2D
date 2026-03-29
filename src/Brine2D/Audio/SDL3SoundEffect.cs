using Microsoft.Extensions.Logging;

namespace Brine2D.Audio;

/// <summary>
/// SDL3_mixer implementation of a sound effect.
/// </summary>
public class SDL3SoundEffect : ISoundEffect
{
    private readonly ILogger<SDL3SoundEffect> _logger;
    private nint _audio;
    private int _disposed;

    public string Name { get; }
    public bool IsLoaded => _audio != nint.Zero && _disposed == 0;

    /// <summary>
    /// Internal SDL3 audio handle.
    /// </summary>
    internal nint Handle => _audio;

    internal SDL3SoundEffect(string name, nint audio, ILogger<SDL3SoundEffect> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _audio = audio;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Sound effect created: {Name}", name);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        if (_audio != nint.Zero)
        {
            SDL3.Mixer.DestroyAudio(_audio);
            _audio = nint.Zero;
            _logger.LogDebug("Sound effect disposed: {Name}", Name);
        }
    }
}
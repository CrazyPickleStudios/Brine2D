using Microsoft.Extensions.Logging;

namespace Brine2D.Audio.SDL;

public class SDL3SoundEffect : ISoundEffect
{
    private readonly ILogger<SDL3SoundEffect> _logger;

    private nint _audio; 
    private bool _disposed;

    public string Name { get; }
    public bool IsLoaded => _audio != IntPtr.Zero && !_disposed;

    internal nint Handle => _audio;

    public SDL3SoundEffect(string name, nint audio, ILogger<SDL3SoundEffect> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _audio = audio;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Sound effect created: {Name}", name);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_audio != IntPtr.Zero)
        {
            SDL3.Mixer.DestroyAudio(_audio); 
            _audio = IntPtr.Zero;
            _logger.LogDebug("Sound effect disposed: {Name}", Name);
        }

        _disposed = true;
    }
}
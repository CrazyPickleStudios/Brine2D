using Brine2D.Audio;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Audio;

/// <summary>
/// Represents a music track loaded and managed by SDL3 for background audio playback.
/// </summary>
/// <remarks>
/// This class wraps an SDL3 audio resource and provides lifecycle management through
/// the <see cref="IMusic"/> interface. Music tracks are typically longer audio files
/// intended for background playback, as opposed to short sound effects.
/// </remarks>
public class SDL3Music : IMusic
{
    private readonly ILogger<SDL3Music> _logger;

    private nint _audio;
    private bool _disposed;

    /// <summary>
    /// Gets the name or identifier of this music track.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the music track is loaded and ready for playback.
    /// </summary>
    /// <value>
    /// <c>true</c> if the audio handle is valid and the resource has not been disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsLoaded => _audio != IntPtr.Zero && !_disposed;

    /// <summary>
    /// Gets the native SDL3 audio handle for internal use.
    /// </summary>
    /// <remarks>
    /// This handle provides direct access to the underlying SDL3 audio resource
    /// and is intended for internal SDL3-specific operations.
    /// </remarks>
    internal nint Handle => _audio;

    /// <summary>
    /// Initializes a new instance of the <see cref="SDL3Music"/> class.
    /// </summary>
    /// <param name="name">The name or identifier for this music track.</param>
    /// <param name="audio">The native SDL3 audio handle.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public SDL3Music(string name, nint audio, ILogger<SDL3Music> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _audio = audio;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Music created: {Name}", name);
    }

    /// <summary>
    /// Releases the SDL3 audio resources associated with this music track.
    /// </summary>
    /// <remarks>
    /// This method destroys the underlying SDL3 audio handle and marks the resource as disposed.
    /// Subsequent calls to this method are safe and will have no effect.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        if (_audio != IntPtr.Zero)
        {
            SDL3.Mixer.DestroyAudio(_audio);
            _audio = IntPtr.Zero;
            _logger.LogDebug("Music disposed: {Name}", Name);
        }

        _disposed = true;
    }
}
using Brine2D.Audio;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Audio;

/// <summary>
/// Represents an SDL3-based implementation of a sound effect that can be played through the audio system.
/// </summary>
/// <remarks>
/// This class wraps an SDL3 audio resource and provides managed access to sound effect functionality.
/// It implements <see cref="IDisposable"/> to ensure proper cleanup of native audio resources.
/// </remarks>
public class SDL3SoundEffect : ISoundEffect
{
    /// <summary>
    /// Logger instance for diagnostic and debug information.
    /// </summary>
    private readonly ILogger<SDL3SoundEffect> _logger;

    /// <summary>
    /// Native handle to the SDL3 audio resource.
    /// </summary>
    private nint _audio;

    /// <summary>
    /// Indicates whether this sound effect has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Gets the name identifier of this sound effect.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the sound effect is loaded and ready for playback.
    /// </summary>
    /// <value>
    /// <c>true</c> if the audio handle is valid and the object has not been disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsLoaded => _audio != IntPtr.Zero && !_disposed;

    /// <summary>
    /// Gets the native SDL3 audio handle for internal use.
    /// </summary>
    /// <remarks>
    /// This property is intended for internal use by the SDL3 audio system.
    /// </remarks>
    internal nint Handle => _audio;

    /// <summary>
    /// Initializes a new instance of the <see cref="SDL3SoundEffect"/> class.
    /// </summary>
    /// <param name="name">The name identifier for this sound effect.</param>
    /// <param name="audio">The native SDL3 audio handle.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public SDL3SoundEffect(string name, nint audio, ILogger<SDL3SoundEffect> logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _audio = audio;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("Sound effect created: {Name}", name);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SDL3SoundEffect"/> and optionally releases the managed resources.
    /// </summary>
    /// <remarks>
    /// This method destroys the underlying SDL3 audio resource and marks the sound effect as disposed.
    /// Calling this method multiple times is safe and will have no effect after the first call.
    /// </remarks>
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
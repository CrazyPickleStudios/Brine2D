using Microsoft.Extensions.Logging;

namespace Brine2D.Audio;

/// <summary>
/// Base class for SDL3_mixer audio assets (sound effects and music).
/// Handles native audio handle lifetime and thread-safe disposal.
/// </summary>
internal abstract class SDL3AudioAsset : IDisposable
{
    private readonly ILogger _logger;
    private nint _audio;
    private int _disposed;

    public string Name { get; }

    public bool IsLoaded => _audio != nint.Zero && Volatile.Read(ref _disposed) == 0;

    internal nint Handle =>
        Volatile.Read(ref _disposed) == 0
            ? _audio
            : throw new ObjectDisposedException(Name);

    /// <summary>
    /// Attempts to retrieve the native audio handle without throwing.
    /// Returns <see langword="false"/> if the asset has been disposed or was never loaded.
    /// </summary>
    /// <remarks>
    /// The check is not atomic: a concurrent <see cref="Dispose"/> call between the
    /// handle read and the disposed-flag read could yield a stale handle. Callers must
    /// ensure that <see cref="Dispose"/> is not called concurrently — in practice this
    /// means both playback and disposal happen on the game thread.
    /// </remarks>
    internal bool TryGetHandle(out nint handle)
    {
        handle = Volatile.Read(ref _audio);
        if (handle == nint.Zero || Volatile.Read(ref _disposed) != 0)
        {
            handle = nint.Zero;
            return false;
        }

        return true;
    }

    protected SDL3AudioAsset(string name, nint audio, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfZero(audio);
        ArgumentNullException.ThrowIfNull(logger);
        Name = name;
        _audio = audio;
        _logger = logger;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var audio = Interlocked.Exchange(ref _audio, nint.Zero);
        if (audio != nint.Zero)
        {
            SDL3.Mixer.DestroyAudio(audio);
            _logger.LogDebug("Disposed: {Name}", Name);
        }
    }
}
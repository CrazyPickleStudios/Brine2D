namespace Brine2D.Audio;

/// <summary>
///     Represents a music track that can be managed by the audio system.
/// </summary>
/// <remarks>
///     Implementations should handle resource management and release any unmanaged resources
///     when <see cref="System.IDisposable.Dispose" /> is called.
/// </remarks>
public interface IMusic : IDisposable
{
    /// <summary>
    ///     Gets the total duration of the music track in seconds.
    /// </summary>
    float LengthSeconds { get; }
}
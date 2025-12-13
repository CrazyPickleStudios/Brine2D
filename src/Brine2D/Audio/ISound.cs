namespace Brine2D.Audio;

/// <summary>
///     Represents an audio sound resource that can be disposed when no longer needed.
/// </summary>
public interface ISound : IDisposable
{
    /// <summary>
    ///     Gets the total length of the sound in seconds.
    /// </summary>
    float LengthSeconds { get; }
}
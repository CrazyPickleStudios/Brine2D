namespace Brine2D.Audio;

/// <summary>
/// Represents streaming music (typically for background music).
/// </summary>
public interface IMusic : IDisposable
{
    /// <summary>
    /// Gets the name or path of the music track.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the music is loaded.
    /// </summary>
    bool IsLoaded { get; }
}
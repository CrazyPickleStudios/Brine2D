namespace Brine2D.Audio;

/// <summary>
/// Represents a loaded sound effect.
/// </summary>
public interface ISoundEffect : IDisposable
{
    /// <summary>
    /// Gets the name or path of the sound effect.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the sound effect is loaded.
    /// </summary>
    bool IsLoaded { get; }
}
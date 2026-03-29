namespace Brine2D.Audio;

/// <summary>
/// Low-level sound effect loading interface.
/// <para><strong>For most use cases, use <see cref="IAssetLoader"/> instead.</strong></para>
/// </summary>
public interface ISoundLoader : IDisposable
{
    /// <summary>Loads a sound effect from a file.</summary>
    Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a sound effect and frees its resources.
    /// Implementations must be idempotent — calling this twice with the same instance
    /// must not throw or cause a double-free of native resources.
    /// </summary>
    void UnloadSound(ISoundEffect sound);
}
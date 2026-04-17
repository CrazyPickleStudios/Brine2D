namespace Brine2D.Audio;

/// <summary>
/// Low-level music loading interface.
/// <para><strong>For most use cases, use <see cref="IAssetLoader"/> instead.</strong></para>
/// </summary>
public interface IMusicLoader : IDisposable
{
    /// <summary>Loads music from a file.</summary>
    Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads music and frees its resources.
    /// Implementations must be idempotent — calling this twice with the same instance
    /// must not throw or cause a double-free of native resources.
    /// </summary>
    /// <remarks>
    /// Must be called from the game thread. Implementations stop and destroy any
    /// active tracks referencing the asset before freeing it. Calling this
    /// concurrently with playback methods that reference the same asset is not safe.
    /// </remarks>
    void UnloadMusic(IMusic music);
}
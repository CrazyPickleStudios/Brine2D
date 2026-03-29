namespace Brine2D.Audio;

/// <summary>
/// Low-level music loading interface.
/// <para><strong>For most use cases, use <see cref="IAssetLoader"/> instead.</strong></para>
/// </summary>
public interface IMusicLoader : IDisposable
{
    /// <summary>Loads music from a file.</summary>
    Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Unloads music and frees its resources.</summary>
    void UnloadMusic(IMusic music);
}